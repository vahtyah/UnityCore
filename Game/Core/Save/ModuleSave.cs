using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module lưu/đọc dữ liệu qua Easy Save 3 (ES3) bằng reflection,
    /// nên không bắt buộc phụ thuộc compile-time vào ES3. Hỗ trợ mã hóa AES.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/Save", fileName = "Module_Save", order = 18)]
    internal sealed class ModuleSave : SAModule
    {
        [Header("Encryption")]
        [SerializeField]
        private string _encryptionPassword = "changeme123";

        private static Type _es3Type;

        private static bool _es3TypeResolved;

        private static Type _es3SettingsType;

        private static MethodInfo _keyExistsMethod;

        private static readonly Dictionary<Type, MethodInfo> _saveCache = new Dictionary<Type, MethodInfo>();

        private static readonly Dictionary<Type, MethodInfo> _loadCache = new Dictionary<Type, MethodInfo>();

        private object _settings;

        // Tìm type ES3 trong các assembly đã load (bỏ qua trên WebGL platform == 17)
        private static Type ES3Type
        {
            get
            {
                if ((int)Application.platform == 17)
                {
                    return null;
                }
                if (_es3TypeResolved)
                {
                    return _es3Type;
                }
                _es3TypeResolved = true;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    _es3Type = assembly.GetType("ES3");
                    if (_es3Type != null)
                    {
                        return _es3Type;
                    }
                }
                return null;
            }
        }

        private static Type ES3SettingsType
        {
            get
            {
                if ((int)Application.platform == 17)
                {
                    return null;
                }
                if (_es3SettingsType != null)
                {
                    return _es3SettingsType;
                }
                if (ES3Type == null)
                {
                    return null;
                }
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    _es3SettingsType = assembly.GetType("ES3Settings");
                    if (_es3SettingsType != null)
                    {
                        return _es3SettingsType;
                    }
                }
                return null;
            }
        }

        private object Settings => _settings;

        public ModuleSave()
        {
            Priority = -1000;
        }

        private object BuildSettings()
        {
            Type es3SettingsType = ES3SettingsType;
            if (es3SettingsType == null)
            {
                return null;
            }
            // Tìm ctor ES3Settings(bool)
            ConstructorInfo ctor = es3SettingsType.GetConstructors().FirstOrDefault(delegate (ConstructorInfo c)
            {
                ParameterInfo[] parameters = c.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(bool);
            });
            if (ctor == null)
            {
                Debug.LogError("[ModuleSave] ES3Settings(bool) ctor not found.");
                return null;
            }
            object obj = ctor.Invoke(new object[1] { true });
            SetField(obj, "encryptionType", "AES");
            SetField(obj, "encryptionPassword", _encryptionPassword);
            return obj;
        }

        private static void SetField(object obj, string name, object value)
        {
            FieldInfo field = obj.GetType().GetField(name);
            if (field == null)
            {
                Debug.LogWarning("[ModuleSave] Field '" + name + "' not found.");
                return;
            }
            if (field.FieldType.IsEnum)
            {
                value = ((value is string value2) ? Enum.Parse(field.FieldType, value2) : Enum.ToObject(field.FieldType, value));
            }
            field.SetValue(obj, value);
        }

        public override Task InitializeAsync()
        {
            _settings = BuildSettings();
            PatchDefaultSettings();
            return Task.CompletedTask;
        }

        private void PatchDefaultSettings()
        {
            Type es3SettingsType = ES3SettingsType;
            if (es3SettingsType == null)
            {
                return;
            }
            object obj = es3SettingsType.GetProperty("defaultSettings", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
            if (obj != null)
            {
                SetField(obj, "encryptionType", "AES");
                SetField(obj, "encryptionPassword", _encryptionPassword);
            }
        }

        public override void Subscribe()
        {
            SATypedBus.On<Ev.SaveDataSave>(OnSave);
            SATypedBus.On<Ev.SaveDataLoad>(OnLoad);
        }

        private void OnSave(Ev.SaveDataSave e)
        {
            string key = e.Key;
            object obj = e.Value;
            if (obj == null || _settings == null)
            {
                e.Reply?.Invoke();
                return;
            }
            GetSaveMethod(obj.GetType())?.Invoke(null, new object[3] { key, obj, Settings });
            e.Reply?.Invoke();
        }

        private void OnLoad(Ev.SaveDataLoad e)
        {
            string key = e.Key;
            Type type = e.Type;
            if (type == null || _settings == null || !KeyExists(key))
            {
                e.Reply?.Invoke(null);
                return;
            }
            e.Reply?.Invoke(GetLoadMethod(type)?.Invoke(null, new object[2] { key, Settings }));
        }

        private bool KeyExists(string key)
        {
            if (_keyExistsMethod == null)
            {
                _keyExistsMethod = ES3Type?.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(delegate (MethodInfo m)
                {
                    if (m.Name != "KeyExists" || m.IsGenericMethodDefinition)
                    {
                        return false;
                    }
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType.Name == "ES3Settings";
                });
            }
            return _keyExistsMethod != null && (bool)(_keyExistsMethod.Invoke(null, new object[2] { key, Settings }) ?? ((object)false));
        }

        private static MethodInfo GetSaveMethod(Type type)
        {
            if (_saveCache.TryGetValue(type, out var value))
            {
                return value;
            }
            // Tìm Save<T>(string, T, ES3Settings)
            MethodInfo method = ES3Type?.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(delegate (MethodInfo m)
            {
                if (m.Name != "Save" || !m.IsGenericMethodDefinition)
                {
                    return false;
                }
                ParameterInfo[] parameters = m.GetParameters();
                return parameters.Length == 3 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType.IsGenericParameter && parameters[2].ParameterType.Name == "ES3Settings";
            });
            if (method == null)
            {
                Debug.LogError("[ModuleSave] Save<T>(string,T,ES3Settings) not found.");
            }
            return _saveCache[type] = method?.MakeGenericMethod(type);
        }

        private static MethodInfo GetLoadMethod(Type type)
        {
            if (_loadCache.TryGetValue(type, out var value))
            {
                return value;
            }
            // Tìm Load<T>(string, ES3Settings)
            MethodInfo method = ES3Type?.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(delegate (MethodInfo m)
            {
                if (m.Name != "Load" || !m.IsGenericMethodDefinition)
                {
                    return false;
                }
                ParameterInfo[] parameters = m.GetParameters();
                return parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType.Name == "ES3Settings";
            });
            if (method == null)
            {
                Debug.LogError("[ModuleSave] Load<T>(string,ES3Settings) not found.");
            }
            return _loadCache[type] = method?.MakeGenericMethod(type);
        }
    }
}
