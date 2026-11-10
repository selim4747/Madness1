using UnityEngine;
using UnityEngine.UI;

namespace Madness.Mods
{
    /// <summary>
    /// Yeni sahne yüklemek için butona bağlanır.
    /// Inspector'dan sahne adı yaz veya mod manifest'indeki worldScene'i kullan.
    /// </summary>
    public sealed class ModSceneButton : MonoBehaviour
    {
        [Header("Bağlantı")]
        [Tooltip("Bu butona basınca sahne yüklenecek")]
        [SerializeField] private Button loadButton;

        [Header("Sahne")]
        [Tooltip("Yüklenecek sahne adı (Build Settings'te olmalı). Boş bırakılırsa seçili modun worldScene'i kullanılır.")]
        [SerializeField] private string sceneName = "";

        [Tooltip("Additive yükleme (mevcut sahnenin üstüne)")]
        [SerializeField] private bool additive = false;

        [Tooltip("Mod id (AssetBundle'dan sahne yüklemek için)")]
        [SerializeField] private string fromModId = "";

        [Tooltip("İşaretliyse sahneyi modun AssetBundle'ından yükler (Build Settings gerekmez)")]
        [SerializeField] private bool loadFromModBundle = true;

        private void Start()
        {
            if (loadButton != null)
                loadButton.onClick.AddListener(Load);
        }

        /// <summary>Buton OnClick'ine de doğrudan bağlanabilir.</summary>
        public void Load()
        {
            string target = ResolveSceneName();
            if (string.IsNullOrWhiteSpace(target))
            {
                Debug.LogWarning("[ModSceneButton] Yüklenecek sahne adı boş.");
                return;
            }

            if (loadFromModBundle && !string.IsNullOrWhiteSpace(fromModId))
            {
                ModSceneLoader.LoadFromMod(fromModId, target, additive);
            }
            else
            {
                ModWorldService.EnsureExists().TryLoadSceneWorld(
                    string.IsNullOrEmpty(fromModId) ? target : fromModId,
                    target,
                    additive);
            }
        }

        private string ResolveSceneName()
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
                return sceneName;

            if (!string.IsNullOrWhiteSpace(fromModId))
            {
                var runtime = ModRuntime.EnsureExists();
                foreach (var m in runtime.LoadedManifests)
                {
                    if (string.Equals(m.id, fromModId, System.StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(m.worldScene))
                        return m.worldScene;
                }
            }
            return null;
        }
    }
}
