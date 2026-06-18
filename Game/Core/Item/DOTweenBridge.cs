using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Cầu nối tới DOTween qua reflection: cho phép tween float mà không cần
    /// reference cứng tới assembly DOTween (DOTween có thể không tồn tại trong project).
    /// </summary>
    internal static class DOTweenBridge
    {
        // Cache kết quả kiểm tra để chỉ phải dò reflection một lần.
        private static bool? _available;

        private static MethodInfo _toMethod;
        private static MethodInfo _setDelayMethod;
        private static MethodInfo _onCompleteMethod;
        private static MethodInfo _setUpdateMethod;
        private static Type _doGetterFloatType;
        private static Type _doSetterFloatType;
        private static Type _tweenCallbackType;

        /// <summary>Kiểm tra DOTween có sẵn trong project hay không (qua reflection).</summary>
        public static bool IsAvailable()
        {
            if (_available.HasValue)
            {
                return _available.Value;
            }
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Type type = assemblies.Select(a => a.GetType("DG.Tweening.DOTween")).FirstOrDefault(t => t != null);
                if (type == null)
                {
                    _available = false;
                    return false;
                }
                Assembly assembly = type.Assembly;
                Type getterDef = assembly.GetType("DG.Tweening.Core.DOGetter`1");
                Type setterDef = assembly.GetType("DG.Tweening.Core.DOSetter`1");
                if (getterDef == null || setterDef == null)
                {
                    _available = false;
                    return false;
                }
                _doGetterFloatType = getterDef.MakeGenericType(typeof(float));
                _doSetterFloatType = setterDef.MakeGenericType(typeof(float));
                _toMethod = type.GetMethod("To", new Type[4]
                {
                    _doGetterFloatType,
                    _doSetterFloatType,
                    typeof(float),
                    typeof(float)
                });
                if (_toMethod == null)
                {
                    _available = false;
                    return false;
                }
                Type extType = assemblies.Select(a => a.GetType("DG.Tweening.TweenSettingsExtensions")).FirstOrDefault(t => t != null);
                if (extType != null)
                {
                    Type tweenType = assembly.GetType("DG.Tweening.Tween");
                    _tweenCallbackType = assembly.GetType("DG.Tweening.TweenCallback");
                    _setDelayMethod = extType.GetMethods().FirstOrDefault(m => m.Name == "SetDelay" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2)?.MakeGenericMethod(tweenType);
                    _onCompleteMethod = extType.GetMethods().FirstOrDefault(m => m.Name == "OnComplete" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2)?.MakeGenericMethod(tweenType);
                    _setUpdateMethod = extType.GetMethods().FirstOrDefault(m => m.Name == "SetUpdate" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2)?.MakeGenericMethod(tweenType);
                }
                _available = true;
            }
            catch (Exception arg)
            {
                Debug.LogError($"[DOTweenBridge] IsAvailable failed: {arg}");
                _available = false;
            }
            return _available.Value;
        }

        /// <summary>Tween một giá trị float từ 0..1 thông qua getter/setter, gọi onComplete khi xong.</summary>
        public static void TweenFloat(float duration, float delay, bool ignoreTimeScale, Func<float> getter, Action<float> setter, Action onComplete)
        {
            if (!IsAvailable())
            {
                Debug.LogWarning("[DOTweenBridge] DOTween not available.");
                return;
            }
            try
            {
                // Bọc getter/setter của C# thành delegate kiểu DOGetter/DOSetter của DOTween.
                Delegate getterDelegate = Delegate.CreateDelegate(_doGetterFloatType, getter.Target, getter.Method);
                Delegate setterDelegate = Delegate.CreateDelegate(_doSetterFloatType, setter.Target, setter.Method);
                object tween = _toMethod.Invoke(null, new object[4] { getterDelegate, setterDelegate, 1f, duration });
                if (tween == null)
                {
                    onComplete?.Invoke();
                    return;
                }
                if (_setUpdateMethod != null && ignoreTimeScale)
                {
                    _setUpdateMethod.Invoke(null, new object[2] { tween, true });
                }
                if (_setDelayMethod != null && delay > 0f)
                {
                    _setDelayMethod.Invoke(null, new object[2] { tween, delay });
                }
                if (_onCompleteMethod != null && onComplete != null && _tweenCallbackType != null)
                {
                    Delegate callback = Delegate.CreateDelegate(_tweenCallbackType, onComplete.Target, onComplete.Method);
                    _onCompleteMethod.Invoke(null, new object[2] { tween, callback });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[DOTweenBridge] TweenFloat failed: " + ex.Message);
                onComplete?.Invoke();
            }
        }
    }
}
