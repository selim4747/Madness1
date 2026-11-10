using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Madness.Mods
{
    /// <summary>
    /// Modun içindeki sahneyi AssetBundle'dan yükler.
    /// Sahne Build Settings'te olmak zorunda değil.
    /// </summary>
    public static class ModSceneLoader
    {
        public static bool LoadFromMod(string modId, string sceneName = null, bool additive = false)
        {
            AssetBundle bundle = ModAssetLoader.LoadBundle(modId);
            if (bundle == null)
            {
                Debug.LogError("[ModSceneLoader] Bundle yok: " + modId);
                return false;
            }

            string[] scenes = bundle.GetAllScenePaths();
            if (scenes == null || scenes.Length == 0)
            {
                Debug.LogError("[ModSceneLoader] Bundle içinde sahne yok: " + modId);
                return false;
            }

            string target = null;
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                foreach (var s in scenes)
                    if (s.EndsWith(sceneName + ".unity") || System.IO.Path.GetFileNameWithoutExtension(s) == sceneName)
                    { target = s; break; }
            }
            if (target == null) target = scenes[0];

            var context = ModEventBus.Publish(new WorldLoadContext
            {
                WorldId = modId,
                SceneName = System.IO.Path.GetFileNameWithoutExtension(target),
                Additive = additive
            });
            if (context.CancelLoad) return false;

            SceneManager.LoadScene(target, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
            Debug.Log("[ModSceneLoader] Sahne yüklendi: " + target);
            return true;
        }

        public static string[] ListScenes(string modId)
        {
            return ModAssetLoader.GetScenePaths(modId);
        }
    }
}
