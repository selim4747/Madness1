using UnityEngine;
using UnityEngine.UI;

namespace Madness.Mods
{
    /// <summary>Inspector'dan bir Canvas altındaki panele eklenir; gömülü mod seçim butonlarını üretir.</summary>
    public sealed class BuiltInModSelectionUI : MonoBehaviour
    {
        [SerializeField] private RectTransform listRoot;
        [SerializeField] private Text statusText;
        [SerializeField] private Vector2 buttonSize = new Vector2(360f, 64f);
        private Font font;

        private void Start()
        {
            font = LoadBuiltinFont();
            ModRuntime.EnsureExists();
            Rebuild();
        }

        // Unity 2022.2+ içinde "Arial.ttf" geçersiz (exception atar); "LegacyRuntime.ttf" kullanılır.
        private static Font LoadBuiltinFont()
        {
            try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (System.Exception) { }
            try { return Font.CreateDynamicFontFromOSFont("Arial", 16); }
            catch (System.Exception) { }
            return null;
        }

        public void Rebuild()
        {
            if (listRoot == null)
            {
                Debug.LogWarning("[BuiltInModSelectionUI] List Root atanmamış.");
                return;
            }
            for (int i = listRoot.childCount - 1; i >= 0; i--)
                Destroy(listRoot.GetChild(i).gameObject);

            var ids = BuiltInModCatalog.GetAvailableIds();
            if (ids.Count == 0)
            {
                SetStatus("Bu sürümde seçilebilir yerleşik mod yok.");
                return;
            }
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                bool enabled = BuiltInModCatalog.IsEnabled(id);
                GameObject go = new GameObject("BuiltInMod_" + id, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(listRoot, false);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = buttonSize;
                Image image = go.GetComponent<Image>();
                image.color = enabled ? new Color(0.16f, 0.48f, 0.28f, 1f) : new Color(0.28f, 0.28f, 0.3f, 1f);
                Button button = go.GetComponent<Button>();
                Text label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                label.transform.SetParent(go.transform, false);
                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8f, 4f);
                labelRect.offsetMax = new Vector2(-8f, -4f);
                label.font = font;
                label.fontSize = 20;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.text = BuiltInModCatalog.GetDisplayName(id) + (enabled ? "  [AÇIK]" : "  [KAPALI]");
                button.onClick.AddListener(() =>
                {
                    bool next = !BuiltInModCatalog.IsEnabled(id);
                    if (BuiltInModCatalog.SetEnabled(id, next))
                    {
                        SetStatus("Seçim kaydedildi. Mod durumu güncellendi.");
                        Rebuild();
                    }
                });
            }
            SetStatus("Mod seçimi cihazda saklanır. Değişiklikler anında uygulanır.");
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
