using UnityEngine;

public static class DevLog
{
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Info(string message) => Debug.Log(message);

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Warning(string message) => Debug.LogWarning(message);

    public static void Error(string message) => Debug.LogError(message);
}
