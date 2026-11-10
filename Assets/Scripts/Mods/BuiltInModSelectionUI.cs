using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Madness.Mods
{
    /// <summary>
    /// Mod listesi paneline eklenir. Panelin boyutu, yeri, renkleri ve satır görünümü Inspector'dan
    /// ayarlanır. Değişiklikleri editörde görmek için bileşene sağ tık → "Düzeni Uygula".
    /// Hiçbir hata menüyü bozmaz.
    /// </summary>
    public sealed class BuiltInModSelectionUI : MonoBehaviour
    {
        [Header("Bağlantılar")]
        [SerializeField] private RectTransform listRoot;
        [SerializeField] private Text statusText;
        [SerializeField] private TMP_Text statusTextTMP;

        [Header("Panelin yeri ve boyutu (bu bileşenin olduğu nesne)")]
        [SerializeField] private bool overrideRect = true;
        [SerializeField] private Vector2 panelSize = new Vector2(560f, 640f);
        [Tooltip("Ekran ortasına göre kayma. (0,0) = tam orta.")]
        [SerializeField] private Vector2 panelPosition = Vector2.zero;

        [Header("Panel rengi")]
        [SerializeField] private bool overridePanelColor = false;
        [SerializeField] private Image panelImage;
        [SerializeField] private Color panelColor = new Color(0.10f, 0.13f, 0.20f, 0.98f);

        [Header("Liste yerleşimi")]
        [Tooltip("Açıksa liste alanı paneli doldurur ve satırları otomatik dizer. Kendi layout'unu kullanacaksan kapat.")]
        [SerializeField] private bool autoLayout = true;
        [Tooltip("Üst boşluk: kapat (X) butonu ve başlık için yer bırakır.")]
        [SerializeField] private int paddingLeft = 28;
        [SerializeField] private int paddingRight = 28;
        [SerializeField] private int paddingTop = 90;
        [SerializeField] private int paddingBottom = 28;
        [SerializeField] private float rowSpacing = 14f;
        [SerializeField] private float rowHeight = 72f;

        [Header("Satır görünümü")]
        [SerializeField] private Color rowOnColor = new Color(0.16f, 0.55f, 0.30f, 1f);
        [SerializeField] private Color rowOffColor = new Color(0.30f, 0.32f, 0.38f, 1f);
        [SerializeField] private Color rowTextColor = Color.white;
        [SerializeField] private int rowFontSize = 28;

        private Font font;

        private void OnEnable()
        {
            ApplyLayout();
            Rebuild();
        }

        private void OnDisable()
        {
            if (listRoot == null) return;
            for (int i = listRoot.childCount - 1; i >= 0; i--)
                Destroy(listRoot.GetChild(i).gameObject);
        }

        [ContextMenu("Düzeni Uygula")]
        public void ApplyLayout()
        {
            try
            {
                RectTransform self = transform as RectTransform;
                if (overrideRect && self != null)
                {
                    self.anchorMin = self.anchorMax = new Vector2(0.5f, 0.5f);
                    self.pivot = new Vector2(0.5f, 0.5f);
                    self.sizeDelta = panelSize;
                    self.anchoredPosition = panelPosition;
                }
                if (overridePanelColor)
                {
                    Image img = panelImage != null ? panelImage : GetComponent<Image>();
                    if (img != null) img.color = panelColor;
                }
                if (autoLayout && listRoot != null)
                {
                    if (listRoot != transform)
                    {
                        listRoot.anchorMin = Vector2.zero;
                        listRoot.anchorMax = Vector2.one;
                        listRoot.offsetMin = Vector2.zero;
                        listRoot.offsetMax = Vector2.zero;
                    }
                    LayoutGroup existing = listRoot.GetComponent<LayoutGroup>();
                    VerticalLayoutGroup vlg = existing as VerticalLayoutGroup;
                    if (existing == null) vlg = listRoot.gameObject.AddComponent<VerticalLayoutGroup>();
                    if (vlg != null)
                    {
                        vlg.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
                        vlg.spacing = rowSpacing;
                        vlg.childAlignment = TextAnchor.UpperCenter;
                        vlg.childControlWidth = true;
                        vlg.childControlHeight = true;
                        vlg.childForceExpandWidth = true;
                        vlg.childForceExpandHeight = false;
                    }
                }
            }
            catch (System.Exception ex) { Debug.LogException(ex); }
        }

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
            try
            {
                if (listRoot == null)
                {
                    Debug.LogWarning("[BuiltInModSelectionUI] List Root atanmamış.");
                    return;
                }
                if (font == null) font = LoadBuiltinFont();
                ModRuntime.EnsureExists();

                for (int i = listRoot.childCount - 1; i >= 0; i--)
                    Destroy(listRoot.GetChild(i).gameObject);

                var ids = BuiltInModCatalog.GetAvailableIds();
                if (ids.Count == 0)
                {
                    SetStatus("Bu sürümde seçilebilir yerleşik mod yok.");
                    return;
                }
                for (int i = 0; i < ids.Count; i++) CreateRow(ids[i]);
                SetStatus("Bir moda dokunarak aç/kapat.");
            }
            catch (System.Exception ex) { Debug.LogException(ex); }
        }

        private void CreateRow(string id)
        {
            bool enabled = BuiltInModCatalog.IsEnabled(id);
            GameObject go = new GameObject("BuiltInMod_" + id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(listRoot, false);
            go.GetComponent<Image>().color = enabled ? rowOnColor : rowOffColor;
            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredHeight = rowHeight;
            le.minHeight = rowHeight;
            le.flexibleWidth = 1f;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(go.transform, false);
            RectTransform lr = labelObject.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(12f, 4f);
            lr.offsetMax = new Vector2(-12f, -4f);
            Text label = labelObject.GetComponent<Text>();
            label.font = font;
            label.fontSize = rowFontSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = rowFontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = rowTextColor;
            label.raycastTarget = false;
            label.text = BuiltInModCatalog.GetDisplayName(id) + (enabled ? "  [AÇIK]" : "  [KAPALI]");

            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                try
                {
                    bool next = !BuiltInModCatalog.IsEnabled(id);
                    if (BuiltInModCatalog.SetEnabled(id, next))
                    {
                        Rebuild();
                        SetStatus(BuiltInModCatalog.GetDisplayName(id) + (next ? " açıldı." : " kapatıldı."));
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    SetStatus("Mod değiştirilemedi. Tekrar dene.");
                }
            });
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
            if (statusTextTMP != null) statusTextTMP.text = message;
        }
    }
}
