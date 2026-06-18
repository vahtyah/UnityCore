using System;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Tiện ích gắn/gỡ SAUIGroup cho GameObject bằng code (thay vì cấu hình tay trong Inspector).
    /// </summary>
    public static class SAUIGroupManager
    {
        public static void Attach(GameObject go, Enum group)
        {
            SAUIGroup comp = go.AddComponent<SAUIGroup>();
            comp.Group = new SAEnumRef
            {
                typeName = group.GetType().AssemblyQualifiedName,
                value = Convert.ToInt32(group)
            };
            comp.EnsureSubscribed();
        }

        public static void Detach(GameObject go, Enum group)
        {
            int value = Convert.ToInt32(group);
            string typeName = group.GetType().AssemblyQualifiedName;

            foreach (SAUIGroup comp in go.GetComponents<SAUIGroup>())
            {
                if (comp.Group.value == value && comp.Group.typeName == typeName)
                {
                    UnityEngine.Object.Destroy(comp);
                    break;
                }
            }
        }
    }
}
