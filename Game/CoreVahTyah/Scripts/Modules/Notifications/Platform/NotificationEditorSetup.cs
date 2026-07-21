#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// Editor-only helpers for the Notifications module inspector buttons.
    /// Lives in Assembly-CSharp (NOT an Editor/ folder) so ModuleNotifications can call it directly.
    /// </summary>
    internal static class NotificationEditorSetup
    {
        public const string Define = "VAHTYAH_MOBILE_NOTIFICATIONS";
        private const string PackageId = "com.unity.mobile.notifications";
        private const string SettingsPath = "Project/Mobile Notifications";

        private static AddRequest _request;

        // ── Install the package, then enable the runtime providers via a scripting define ────────────

        public static void InstallPackage()
        {
            if (_request != null)
            {
                Debug.Log("[Notifications] Install already in progress, please wait...");
                return;
            }

            Debug.Log($"[Notifications] Installing {PackageId}...");
            _request = Client.Add(PackageId);
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (_request == null || !_request.IsCompleted) return;
            EditorApplication.update -= Poll;

            if (_request.Status == StatusCode.Success)
            {
                Debug.Log($"[Notifications] Installed {_request.Result.packageId}. Adding define {Define} + recompiling.");
                AddDefine(BuildTargetGroup.Android);
                AddDefine(BuildTargetGroup.iOS);
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogError($"[Notifications] Install failed: {_request.Error?.message}");
            }

            _request = null;
        }

        private static void AddDefine(BuildTargetGroup group)
        {
            var target = NamedBuildTarget.FromBuildTargetGroup(group);
            List<string> defines = PlayerSettings.GetScriptingDefineSymbols(target)
                .Split(';')
                .Select(d => d.Trim())
                .Where(d => d.Length > 0)
                .ToList();

            if (defines.Contains(Define)) return;

            defines.Add(Define);
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
        }

        // ── Register icons straight into Project Settings → Mobile Notifications ──────────────────────
        // Uses reflection: the package's NotificationSettingsManager is an internal, version-sensitive editor
        // type, so there is no compile-time coupling. Any failure falls back to opening the settings window.

        public static void SetupAndroidIcons(ModuleNotifications.NotificationIcon[] icons)
        {
            var valid = (icons ?? Array.Empty<ModuleNotifications.NotificationIcon>())
                .Where(i => i != null && !string.IsNullOrEmpty(i.Id) && i.Texture != null)
                .ToArray();

            if (valid.Length == 0)
            {
                Debug.LogWarning("[Notifications] No valid icons (each needs an Id + a Texture). Nothing to setup.");
                return;
            }

            try
            {
                Type managerType = FindType("Unity.Notifications.NotificationSettingsManager");
                Type dataType = FindType("Unity.Notifications.DrawableResourceData");
                Type iconEnum = FindType("Unity.Notifications.NotificationIconType");

                if (managerType == null || dataType == null || iconEnum == null)
                    throw new Exception("Mobile Notifications editor types not found — is the package installed?");

                object manager = managerType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null)
                                 ?? managerType.GetProperty("Manager", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (manager == null) throw new Exception("Could not obtain NotificationSettingsManager instance.");

                IList list = GetMember(managerType, manager, "DrawableResources") as IList
                             ?? throw new Exception("Could not access DrawableResources list.");

                foreach (var icon in valid)
                {
                    object typeValue = Enum.Parse(iconEnum, icon.IsSmall ? "Small" : "Large", true);

                    // Replace any existing entry with the same Id.
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if ((GetMember(dataType, list[i], "Id") as string) == icon.Id) list.RemoveAt(i);
                    }

                    object data = Activator.CreateInstance(dataType);
                    SetMember(dataType, data, "Id", icon.Id);
                    SetMember(dataType, data, "Type", typeValue);
                    SetMember(dataType, data, "Asset", icon.Texture);
                    list.Add(data);
                }

                EditorUtility.SetDirty((UnityEngine.Object)manager);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Notifications] Registered {valid.Length} icon(s) into Mobile Notifications settings.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Notifications] Auto-setup failed ({e.Message}). Opening settings so you can add icons manually.");
                UnityEditor.SettingsService.OpenProjectSettings(SettingsPath);
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        private static object GetMember(Type type, object obj, string name)
        {
            var f = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (f != null) return f.GetValue(obj);
            var p = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            return p?.GetValue(obj);
        }

        private static void SetMember(Type type, object obj, string name, object value)
        {
            var f = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (f != null) { f.SetValue(obj, value); return; }
            var p = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanWrite) p.SetValue(obj, value);
        }
    }
}
#endif
