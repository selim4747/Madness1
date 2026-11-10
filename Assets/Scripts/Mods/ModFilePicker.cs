using System;
using UnityEngine;

namespace Madness.Mods
{
    public static class ModFilePicker
    {
        public static bool IsSupported
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return true;
#elif UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static void Open(Action<string, string> completed)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass picker = new AndroidJavaClass("com.madness.modimport.MadnessFilePicker"))
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    picker.CallStatic("open", activity);
                }
                ModFilePickerCallbacks.Begin(completed);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                if (completed != null) completed(null, "Android dosya seçici başlatılamadı.");
            }
#elif UNITY_EDITOR
            // macOS'ta tek bir "zip,mod,madnessmod" uzantı dizgisi dosyaları gri bırakabiliyor; filtre listesi + "Tüm dosyalar" kullanılır.
            string path = UnityEditor.EditorUtility.OpenFilePanelWithFilters("Mod dosyası seç", "",
                new string[] { "Mod paketleri", "zip,mod,madnessmod", "Tüm dosyalar", "*" });
            Debug.Log("[ModFilePicker] Editör dosya penceresi kapandı. Seçilen: " + (string.IsNullOrEmpty(path) ? "(boş / iptal)" : path));
            if (completed != null) completed(string.IsNullOrEmpty(path) ? null : path, null);
#else
            if (completed != null) completed(null, "Bu platformda dosya seçici kullanılamıyor.");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private sealed class ModFilePickerCallbacks : MonoBehaviour
        {
            private static ModFilePickerCallbacks instance;
            private static Action<string, string> pending;
            private float nextPollTime;

            public static void Begin(Action<string, string> callback)
            {
                pending = callback;
                if (instance == null)
                {
                    GameObject go = new GameObject("ModFilePickerCallbacks");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    instance = go.AddComponent<ModFilePickerCallbacks>();
                }
                instance.nextPollTime = 0f;
            }

            private void Update()
            {
                if (pending == null || Time.unscaledTime < nextPollTime) return;
                nextPollTime = Time.unscaledTime + 0.15f;
                string path;
                string error;
                using (AndroidJavaClass picker = new AndroidJavaClass("com.madness.modimport.MadnessFilePicker"))
                {
                    path = picker.CallStatic<string>("pollPath");
                    error = picker.CallStatic<string>("pollError");
                }
                if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(error)) return;
                Action<string, string> callback = pending;
                pending = null;
                callback(path, error);
            }
        }
#endif
    }
}
