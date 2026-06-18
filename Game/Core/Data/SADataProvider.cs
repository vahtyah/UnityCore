using System;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>Đánh dấu 1 class là "dữ liệu cấu hình module" (dùng cho Remote Config overlay).</summary>
    public interface ISAModuleData
    {
    }

    /// <summary>
    /// Lấy dữ liệu cấu hình module: ưu tiên Remote Config (JSON từ server),
    /// nếu không có thì dùng giá trị mặc định khai báo trong ScriptableObject.
    /// Hỗ trợ "overlay" — JSON server ghi đè field lên bản mặc định,
    /// và chuyển SAEnumRef <-> tên enum để JSON dễ đọc.
    /// </summary>
    public static class SADataProvider
    {
        private static readonly Regex _floatRegex = new Regex("-?\\d+\\.\\d+", RegexOptions.Compiled);

        /// <summary>Đọc config theo remoteKey; thiếu -> trả về soDefaults.</summary>
        public static T Resolve<T>(string remoteKey, T soDefaults) where T : class, ISAModuleData
        {
            string remoteJson = null;
            SATypedBus.Publish(new Ev.RemoteConfigGet { Key = remoteKey, Reply = v => remoteJson = v });

            if (string.IsNullOrWhiteSpace(remoteJson))
                return soDefaults;

            return Overlay(soDefaults, remoteJson);
        }

        /// <summary>Serialize data ra JSON (đẹp), đổi SAEnumRef thành tên enum, làm gọn số float.</summary>
        public static string ToJson<T>(T data) where T : class, ISAModuleData
        {
            string json = JsonUtility.ToJson(data, true);
            PatchEnumRefsToNames(data, ref json);
            return RoundFloats(json);
        }

        private static string RoundFloats(string json)
        {
            return _floatRegex.Replace(json, m =>
                double.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
                    ? ((float)result).ToString("G7", CultureInfo.InvariantCulture)
                    : m.Value);
        }

        /// <summary>Ghi đè JSON server lên bản source (chỉ field nào có trong JSON mới bị đổi).</summary>
        public static T Overlay<T>(T source, string overrideJson) where T : class, ISAModuleData
        {
            string baseJson = JsonUtility.ToJson(source);
            T clone = JsonUtility.FromJson<T>(baseJson);
            string patched = PatchNamesToEnumRefs(source, overrideJson);
            JsonUtility.FromJsonOverwrite(patched, clone);
            return clone;
        }

        // SAEnumRef -> "TênEnum" trong JSON xuất ra (dễ đọc).
        private static void PatchEnumRefsToNames<T>(T data, ref string json)
        {
            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.FieldType != typeof(SAEnumRef)) continue;

                SAEnumRef enumRef = (SAEnumRef)field.GetValue(data);
                string name = enumRef.GetEnum()?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    string raw = JsonUtility.ToJson(enumRef);
                    json = json.Replace($"\"{field.Name}\":{raw}", $"\"{field.Name}\":\"{name}\"");
                }
            }
        }

        // "TênEnum" trong JSON server -> SAEnumRef đầy đủ (khi đọc vào).
        private static string PatchNamesToEnumRefs<T>(T source, string json)
        {
            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.FieldType != typeof(SAEnumRef)) continue;

                string pattern = "\"" + Regex.Escape(field.Name) + "\":\"([^\"]*)\"";
                Match match = Regex.Match(json, pattern);
                if (match.Success)
                {
                    string value = match.Groups[1].Value;
                    SAEnumRef enumRef = ResolveEnumRef(field, source, value);
                    if (enumRef.IsSet)
                        json = json.Replace(match.Value, $"\"{field.Name}\":{JsonUtility.ToJson(enumRef)}");
                }
            }
            return json;
        }

        private static SAEnumRef ResolveEnumRef(FieldInfo field, object source, string enumName)
        {
            SAEnumRef existing = (SAEnumRef)field.GetValue(source);
            Type type = existing.IsSet ? Type.GetType(existing.typeName) : null;

            if (type == null)
            {
                SAEnumFilterAttribute filter = field.GetCustomAttribute<SAEnumFilterAttribute>();
                if (filter != null)
                    type = FindEnumTypeByCategory(filter.Category);
            }

            if (type == null)
                return default;

            try
            {
                int value = (int)Enum.Parse(type, enumName, ignoreCase: true);
                return new SAEnumRef { typeName = type.AssemblyQualifiedName, value = value };
            }
            catch
            {
                Debug.LogWarning($"[SADataProvider] Could not parse '{enumName}' as {type.Name}.");
                return default;
            }
        }

        // Tìm enum có [SAEnum(category)] trong mọi assembly đang nạp.
        private static Type FindEnumTypeByCategory(string category)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsEnum && type.GetCustomAttribute<SAEnumAttribute>()?.Category == category)
                        return type;
                }
            }
            return null;
        }
    }
}
