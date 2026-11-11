using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Madness.Mods
{
    /// <summary>
    /// Inspector'dan yapılandırılabilen mod yönetim paneli. Component doğrudan sahnedeki
    /// panel GameObject'ine eklenebilir; listRoot, ScrollRect, ölçüler, renkler ve yazı
    /// boyutları Inspector'dan atanır. Eksik görsel alt nesneleri güvenli varsayılanlarla kurar.
    /// </summary>
    public sealed class ModManagerUI : MonoBehaviour
    {
        [Header("Bağlantılar")]
        [SerializeField] private Transform hierarchyPanel;
        [SerializeField] private Transform listRoot;
        [SerializeField] private Text statusText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Image panelImage;

        [Header("İçerik")]
        [SerializeField] private bool allowRuntimeBuild = true;
        [SerializeField] private bool includeBuiltIn = true;
        [SerializeField] private bool configurePanelRect = false;
        [SerializeField] private Vector2 panelSize = new Vector2(700f, 800f);
        [SerializeField] private Vector2 panelPosition = Vector2.zero;
        [SerializeField] private bool createScrollRectWhenMissing = true;
        [SerializeField] private bool keepGeneratedContentUnderHierarchyPanel = true;

        [Header("Yerleşim")]
        [SerializeField] private RectOffset listPadding;
        [SerializeField] private float listSpacing = 8f;
        [SerializeField] private float rowHeight = 88f;
        [SerializeField] private float rowPadding = 10f;
        [SerializeField] private float settingHeight = 30f;
        [SerializeField] private float titleWidth = 260f;
        [SerializeField] private float controlWidth = 180f;

        [Header("Renkler")]
        [SerializeField] private Color rowColor = new Color(0.08f, 0.1f, 0.14f, 0.95f);
        [SerializeField] private Color buttonColor = new Color(0.16f, 0.28f, 0.4f, 1f);
        [SerializeField] private Color controlColor = new Color(0.18f, 0.2f, 0.24f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color secondaryTextColor = new Color(0.75f, 0.8f, 0.85f);
        [SerializeField] private Color accentColor = new Color(0.25f, 0.8f, 0.4f, 1f);

        [Header("Yazı boyutları")]
        [SerializeField] private int titleFontSize = 18;
        [SerializeField] private int bodyFontSize = 14;
        [SerializeField] private int detailFontSize = 13;
        [SerializeField] private int buttonFontSize = 14;

        private readonly List<GameObject> rows = new List<GameObject>();
        private ModRuntime runtime;
        private string pendingDeleteId;
        private float pendingDeleteUntil;

        private void Awake()
        {
            // Unity, MonoBehaviour alan başlatıcılarında RectOffset gibi UnityEngine
            // nesnelerinin özelliklerini ayarlamaya izin vermez. Bu nedenle varsayılan
            // değeri yalnızca runtime'da ve Inspector değeri yoksa oluşturuyoruz.
            if (listPadding == null)
                listPadding = new RectOffset(20, 20, 20, 20);
            ConfigurePanelFromInspector();
            EnsureListFromInspector();
        }

        private void Start()
        {
            runtime = ModRuntime.EnsureExists();
            runtime.OnModsChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (runtime != null) runtime.OnModsChanged -= Refresh;
        }

        public static ModManagerUI Attach(GameObject panel)
        {
            if (panel == null) return null;
            ModManagerUI ui = panel.GetComponent<ModManagerUI>();
            return ui != null ? ui : panel.AddComponent<ModManagerUI>();
        }

        private void ConfigurePanelFromInspector()
        {
            if (hierarchyPanel == null) hierarchyPanel = transform;
            RectTransform rect = hierarchyPanel as RectTransform;
            if (rect == null) rect = GetComponent<RectTransform>();
            if (rect != null && configurePanelRect)
            {
                rect.sizeDelta = panelSize;
                rect.anchoredPosition = panelPosition;
            }
            if (panelImage == null) panelImage = GetComponent<Image>();
        }

        private void EnsureListFromInspector()
        {
            if (listRoot == null && allowRuntimeBuild)
            {
                Transform parent = keepGeneratedContentUnderHierarchyPanel && hierarchyPanel != null ? hierarchyPanel : transform;
                GameObject root = CreateUIObject("ModManagerList", parent);
                listRoot = root.transform;
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(listPadding.left, listPadding.bottom);
                rect.offsetMax = new Vector2(-listPadding.right, -listPadding.top);
            }
            if (listRoot == null) return;
            VerticalLayoutGroup layout = listRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null && allowRuntimeBuild) layout = listRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = listPadding; layout.spacing = listSpacing;
                layout.childControlWidth = true; layout.childControlHeight = true;
                layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            }
            ContentSizeFitter fitter = listRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null && allowRuntimeBuild) fitter = listRoot.gameObject.AddComponent<ContentSizeFitter>();
            if (fitter != null) fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (scrollRect == null) scrollRect = listRoot.GetComponentInParent<ScrollRect>();
            if (scrollRect != null && keepGeneratedContentUnderHierarchyPanel && hierarchyPanel != null && !scrollRect.transform.IsChildOf(hierarchyPanel))
                scrollRect = null;
            if (scrollRect == null && createScrollRectWhenMissing && allowRuntimeBuild) CreateScrollRectAroundList();
        }

        private void CreateScrollRectAroundList()
        {
            if (listRoot == null || listRoot.parent == null) return;
            Transform oldParent = listRoot.parent;
            GameObject scrollObject = CreateUIObject("ModManagerScrollRect", oldParent);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero; scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = Vector2.zero; scrollRectTransform.offsetMax = Vector2.zero;
            GameObject viewportObject = CreateUIObject("ModManagerViewport", scrollObject.transform);
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero; viewportRect.offsetMax = Vector2.zero;
            viewportObject.AddComponent<RectMask2D>();
            scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect; scrollRect.horizontal = false; scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            listRoot.SetParent(viewportObject.transform, false);
            scrollRect.content = listRoot as RectTransform;
            scrollObject.transform.SetAsFirstSibling();
        }

        public void Refresh()
        {
            if (listRoot == null) return;
            for (int i = 0; i < rows.Count; i++) if (rows[i] != null) Destroy(rows[i]);
            rows.Clear();
            if (runtime == null) runtime = ModRuntime.EnsureExists();
            for (int i = 0; i < runtime.LoadedManifests.Count; i++) AddRow(runtime.LoadedManifests[i], false);
            if (includeBuiltIn)
            {
                IReadOnlyList<string> ids = BuiltInModCatalog.GetAvailableIds();
                for (int i = 0; i < ids.Count; i++)
                {
                    bool listed = false;
                    for (int j = 0; j < runtime.LoadedManifests.Count; j++)
                        if (string.Equals(runtime.LoadedManifests[j].id, ids[i], StringComparison.OrdinalIgnoreCase)) { listed = true; break; }
                    if (!listed) AddRow(new ModManifest { id = ids[i], name = BuiltInModCatalog.GetDisplayName(ids[i]), version = "built-in", enabled = BuiltInModCatalog.IsEnabled(ids[i]) }, true);
                }
            }
            if (statusText != null) statusText.text = "Mod: " + rows.Count;
        }

        private void AddRow(ModManifest manifest, bool builtIn)
        {
            GameObject row = CreateUIObject("ModRow_" + manifest.id, listRoot);
            rows.Add(row);
            Image bg = row.AddComponent<Image>(); bg.color = rowColor;
            LayoutElement rowElement = row.AddComponent<LayoutElement>(); rowElement.minHeight = rowHeight; rowElement.preferredHeight = rowHeight;
            VerticalLayoutGroup rowLayout = row.AddComponent<VerticalLayoutGroup>();
            rowLayout.padding = new RectOffset((int)rowPadding, (int)rowPadding, (int)rowPadding, (int)rowPadding);
            rowLayout.spacing = 5f; rowLayout.childControlHeight = true; rowLayout.childForceExpandHeight = false;
            GameObject top = CreateUIObject("Top", row.transform);
            HorizontalLayoutGroup topLayout = top.AddComponent<HorizontalLayoutGroup>(); topLayout.spacing = 8f;
            topLayout.childControlWidth = true; topLayout.childForceExpandWidth = false;
            top.AddComponent<LayoutElement>().preferredHeight = 32f;
            Toggle toggle = CreateToggle(top.transform, manifest.enabled);
            toggle.onValueChanged.AddListener(value => SetEnabled(manifest.id, value, builtIn));
            Text title = CreateText(top.transform, (string.IsNullOrEmpty(manifest.name) ? manifest.id : manifest.name) + "  v" + manifest.version, titleFontSize, true);
            title.gameObject.AddComponent<LayoutElement>().preferredWidth = titleWidth;
            string state = builtIn ? (manifest.enabled ? "Açık" : "Kapalı") : runtime.GetModStatus(manifest);
            Text stateText = CreateText(top.transform, state, bodyFontSize, false); stateText.color = state == "Hazır" || state == "Açık" ? accentColor : Color.yellow;
            stateText.gameObject.AddComponent<LayoutElement>().preferredWidth = 145f;
            if (!builtIn)
            {
                Button reload = CreateButton(top.transform, "Uygula", 58); reload.onClick.AddListener(() => runtime.ReloadModSelection());
                Button up = CreateButton(top.transform, "↑", 30); up.onClick.AddListener(() => ChangeOrder(manifest, 1));
                Button down = CreateButton(top.transform, "↓", 30); down.onClick.AddListener(() => ChangeOrder(manifest, -1));
                Button delete = CreateButton(top.transform, "Sil", 50); delete.onClick.AddListener(() => Delete(manifest));
            }
            Text details = CreateText(row.transform, BuildDetails(manifest, builtIn), detailFontSize, false);
            details.color = secondaryTextColor; details.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
            BuildSettings(row.transform, manifest, builtIn);
        }

        private string BuildDetails(ModManifest manifest, bool builtIn)
        {
            if (builtIn) return "Yerleşik mod";
            string dependency = runtime.GetDependencyError(manifest);
            string result = string.IsNullOrEmpty(manifest.author) ? "Harici mod" : "Yapımcı: " + manifest.author;
            if (!string.IsNullOrEmpty(dependency)) result += " | " + dependency;
            for (int i = runtime.Diagnostics.Count - 1; i >= 0; i--)
                if (string.Equals(runtime.Diagnostics[i].modId, manifest.id, StringComparison.OrdinalIgnoreCase)) { result += " | Hata: " + runtime.Diagnostics[i].message; break; }
            if (manifest.totalConversion) result += " | Total conversion";
            return result;
        }

        private void BuildSettings(Transform parent, ModManifest manifest, bool builtIn)
        {
            if (builtIn || manifest.settings == null) return;
            for (int i = 0; i < manifest.settings.Count; i++)
            {
                ModSettingDefinition setting = manifest.settings[i];
                if (setting == null || string.IsNullOrWhiteSpace(setting.key)) continue;
                string settingType = string.IsNullOrEmpty(setting.type) ? "string" : setting.type.Trim().ToLowerInvariant();
                GameObject line = CreateUIObject("Setting_" + setting.key, parent);
                HorizontalLayoutGroup layout = line.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 6; layout.childControlWidth = true; layout.childForceExpandWidth = false;
                line.AddComponent<LayoutElement>().preferredHeight = settingHeight;
                Text label = CreateText(line.transform, string.IsNullOrEmpty(setting.label) ? setting.key : setting.label, bodyFontSize, false);
                label.gameObject.AddComponent<LayoutElement>().preferredWidth = titleWidth;
                string current = ModSettingsStore.Get(manifest.id, setting.key, setting.defaultValue);
                if (settingType == "bool")
                {
                    Toggle toggle = CreateToggle(line.transform, string.Equals(current, "true", StringComparison.OrdinalIgnoreCase));
                    toggle.onValueChanged.AddListener(v => SaveSetting(manifest, setting, v.ToString()));
                }
                else if (settingType == "float" || settingType == "int")
                {
                    float value; if (!float.TryParse(current, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) value = setting.min;
                    Slider slider = CreateSlider(line.transform, Mathf.Clamp(value, setting.min, setting.max), setting.min, setting.max);
                    Text valueText = CreateText(line.transform, current, bodyFontSize, false); valueText.gameObject.AddComponent<LayoutElement>().preferredWidth = 70f;
                    slider.onValueChanged.AddListener(v => { string text = settingType == "int" ? Mathf.RoundToInt(v).ToString() : v.ToString("0.###", CultureInfo.InvariantCulture); valueText.text = text; SaveSetting(manifest, setting, text); });
                }
                else if (settingType == "dropdown" && setting.options != null && setting.options.Count > 0)
                {
                    Dropdown dropdown = CreateDropdown(line.transform, setting.options, current);
                    dropdown.onValueChanged.AddListener(v => { if (v >= 0 && v < setting.options.Count) SaveSetting(manifest, setting, setting.options[v]); });
                }
                else
                {
                    InputField input = CreateInput(line.transform, current);
                    input.onEndEdit.AddListener(value => SaveSetting(manifest, setting, value));
                }
            }
        }

        private void SetEnabled(string id, bool enabled, bool builtIn)
        {
            bool result = builtIn ? BuiltInModCatalog.SetEnabled(id, enabled) : runtime.SetExternalModEnabled(id, enabled);
            if (!result) Refresh();
        }
        private void ChangeOrder(ModManifest manifest, int delta) { if (runtime.SetModLoadOrder(manifest.id, manifest.loadOrder + delta)) Refresh(); }
        private void Delete(ModManifest manifest)
        {
            if (pendingDeleteId != manifest.id || Time.unscaledTime > pendingDeleteUntil)
            {
                pendingDeleteId = manifest.id; pendingDeleteUntil = Time.unscaledTime + 5f;
                if (statusText != null) statusText.text = "Silmek için tekrar basın: " + manifest.name;
                return;
            }
            string message; runtime.DeleteExternalMod(manifest.id, out message); pendingDeleteId = null; pendingDeleteUntil = 0f;
            if (statusText != null) statusText.text = message; Refresh();
        }
        private void SaveSetting(ModManifest manifest, ModSettingDefinition setting, string value)
        {
            ModSettingsStore.Set(manifest.id, setting.key, value);
            if (setting.restartRequired && statusText != null) statusText.text = "Ayar kaydedildi. Uygulanması için modları yeniden yükleyin.";
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go;
        }
        private Text CreateText(Transform parent, string value, int size, bool bold)
        {
            Text text = CreateUIObject("Text", parent).AddComponent<Text>(); text.text = value; text.fontSize = size; text.color = textColor; text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (bold) text.fontStyle = FontStyle.Bold; return text;
        }
        private Button CreateButton(Transform parent, string label, float width)
        {
            GameObject go = CreateUIObject("Button_" + label, parent); Image image = go.AddComponent<Image>(); image.color = buttonColor; Button button = go.AddComponent<Button>(); LayoutElement element = go.AddComponent<LayoutElement>(); element.preferredWidth = width; Text text = CreateText(go.transform, label, buttonFontSize, true); text.alignment = TextAnchor.MiddleCenter; return button;
        }
        private Toggle CreateToggle(Transform parent, bool value)
        {
            GameObject go = CreateUIObject("Toggle", parent); LayoutElement element = go.AddComponent<LayoutElement>(); element.preferredWidth = 32f; Image background = go.AddComponent<Image>(); background.color = controlColor;
            GameObject check = CreateUIObject("Checkmark", go.transform); Image checkImage = check.AddComponent<Image>(); checkImage.color = accentColor; RectTransform checkRect = check.GetComponent<RectTransform>(); checkRect.anchorMin = new Vector2(.2f, .2f); checkRect.anchorMax = new Vector2(.8f, .8f); checkRect.offsetMin = Vector2.zero; checkRect.offsetMax = Vector2.zero;
            Toggle toggle = go.AddComponent<Toggle>(); toggle.targetGraphic = background; toggle.graphic = checkImage; toggle.isOn = value; return toggle;
        }
        private Slider CreateSlider(Transform parent, float value, float min, float max)
        {
            GameObject go = CreateUIObject("Slider", parent); LayoutElement element = go.AddComponent<LayoutElement>(); element.preferredWidth = controlWidth; element.preferredHeight = 22f; Image background = go.AddComponent<Image>(); background.color = controlColor;
            GameObject fill = CreateUIObject("Fill", go.transform); Image fillImage = fill.AddComponent<Image>(); fillImage.color = accentColor; RectTransform fillRect = fill.GetComponent<RectTransform>(); fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = new Vector2(.5f, 1f); fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
            GameObject handle = CreateUIObject("Handle", go.transform); Image handleImage = handle.AddComponent<Image>(); handleImage.color = Color.white; RectTransform handleRect = handle.GetComponent<RectTransform>(); handleRect.sizeDelta = new Vector2(18f, 26f);
            Slider slider = go.AddComponent<Slider>(); slider.fillRect = fillRect; slider.handleRect = handleRect; slider.targetGraphic = handleImage; slider.minValue = min; slider.maxValue = max; slider.value = value; return slider;
        }
        private Dropdown CreateDropdown(Transform parent, List<string> options, string value)
        {
            GameObject go = CreateUIObject("Dropdown", parent); Image image = go.AddComponent<Image>(); image.color = controlColor; LayoutElement element = go.AddComponent<LayoutElement>(); element.preferredWidth = controlWidth;
            Text caption = CreateText(go.transform, value, bodyFontSize, false); caption.gameObject.AddComponent<LayoutElement>().preferredWidth = controlWidth - 12f;
            GameObject template = CreateUIObject("Template", go.transform); template.AddComponent<Image>().color = controlColor; ScrollRect templateScroll = template.AddComponent<ScrollRect>(); template.SetActive(false);
            RectTransform templateRect = template.GetComponent<RectTransform>(); templateRect.anchorMin = new Vector2(0f, 0f); templateRect.anchorMax = new Vector2(1f, 0f); templateRect.pivot = new Vector2(0.5f, 1f); templateRect.sizeDelta = new Vector2(0f, 180f);
            GameObject viewport = CreateUIObject("Viewport", template.transform); viewport.AddComponent<RectMask2D>(); RectTransform viewportRect = viewport.GetComponent<RectTransform>(); viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one; viewportRect.offsetMin = Vector2.zero; viewportRect.offsetMax = Vector2.zero;
            GameObject content = CreateUIObject("Content", viewport.transform); RectTransform contentRect = content.GetComponent<RectTransform>(); contentRect.anchorMin = new Vector2(0f, 1f); contentRect.anchorMax = Vector2.one; contentRect.pivot = new Vector2(0.5f, 1f); VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>(); contentLayout.childForceExpandHeight = false; contentLayout.childControlHeight = true;
            GameObject item = CreateUIObject("Item", content.transform); Toggle itemToggle = item.AddComponent<Toggle>(); item.AddComponent<Image>().color = controlColor; LayoutElement itemLayout = item.AddComponent<LayoutElement>(); itemLayout.minHeight = 28f; itemLayout.preferredHeight = 28f; Text itemText = CreateText(item.transform, "Option", bodyFontSize, false); itemText.name = "Item Label"; itemText.gameObject.AddComponent<LayoutElement>().preferredWidth = controlWidth - 12f; itemToggle.targetGraphic = item.GetComponent<Image>();
            templateScroll.viewport = viewportRect; templateScroll.content = contentRect;
            Dropdown dropdown = go.AddComponent<Dropdown>(); dropdown.captionText = caption; dropdown.itemText = itemText; dropdown.template = template.GetComponent<RectTransform>(); dropdown.options.Clear(); for (int i = 0; i < options.Count; i++) dropdown.options.Add(new Dropdown.OptionData(options[i])); dropdown.value = Mathf.Max(0, options.IndexOf(value)); return dropdown;
        }
        private InputField CreateInput(Transform parent, string value)
        {
            GameObject go = CreateUIObject("Input", parent); go.AddComponent<Image>().color = controlColor; LayoutElement element = go.AddComponent<LayoutElement>(); element.preferredWidth = controlWidth;
            Text text = CreateText(go.transform, value, bodyFontSize, false); text.raycastTarget = true; InputField input = go.AddComponent<InputField>(); input.textComponent = text; input.text = value; return input;
        }
    }
}
