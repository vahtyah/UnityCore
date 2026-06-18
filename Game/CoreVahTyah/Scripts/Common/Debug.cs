using System;
using System.Diagnostics;
using UnityEngineDebug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public static class Debug
{
    // ---- Log ----
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOP_BUILD")]
    public static void Log(object message) => UnityEngineDebug.Log(message);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOP_BUILD")]
    public static void Log(object message, Object context) => UnityEngineDebug.Log(message, context);

    // ---- Warning ----
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOP_BUILD")]
    public static void LogWarning(object message) => UnityEngineDebug.LogWarning(message);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOP_BUILD")]
    public static void LogWarning(object message, Object context) => UnityEngineDebug.LogWarning(message, context);

    // ---- Error ----
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOP_BUILD")]
    public static void LogError(object message) => UnityEngineDebug.LogError(message);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOP_BUILD")]
    public static void LogError(object message, Object context) => UnityEngineDebug.LogError(message, context);

    // ---- Format ----
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOP_BUILD")]
    public static void LogFormat(string format, params object[] args) => UnityEngineDebug.LogFormat(format, args);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOP_BUILD")]
    public static void LogFormat(Object context, string format, params object[] args) => UnityEngineDebug.LogFormat(context, format, args);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOP_BUILD")]
    public static void LogWarningFormat(string format, params object[] args) => UnityEngineDebug.LogWarningFormat(format, args);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOP_BUILD")]
    public static void LogWarningFormat(Object context, string format, params object[] args) => UnityEngineDebug.LogWarningFormat(context, format, args);

    // ---- Exception ----
    // KHÔNG strip: giữ ở production để crash/log reporting vẫn bắt được lỗi thật.
    public static void LogException(Exception exception) => UnityEngineDebug.LogException(exception);

    public static void LogException(Exception exception, Object context) => UnityEngineDebug.LogException(exception, context);
}
