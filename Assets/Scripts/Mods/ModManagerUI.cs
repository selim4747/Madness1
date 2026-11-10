using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Madness.Mods
{
    /// <summary>
    /// Inspector kurulumu gerektirmeden mod listesi oluşturan yönetim paneli.
    /// Mevcut bir panel nesnesine eklenebilir veya ModPanelController tarafından otomatik eklenir.
    /// </summary>
    public sealed class ModManagerUI : MonoBehaviour
    {
        [SerializeField] private Transform listRoot;
        [SerializeField] private Text statusText;
        [SerializeField] private bool autoBuild = true;
        [SerializeField] private bool includeBuiltIn = true;
        private readonly List<GameObject> rows = new List<GameObject>();
        private ModRuntime runtime;
        private string pendingDeleteId;
        private float pendingDeleteUntil;

        private void Start()
        {
            runtime = ModRuntime.EnsureExists();
            runtime.OnModsChanged += Refresh;
            if (autoBuild && listRoot == null) BuildPanel();
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

        private void BuildPanel()
        {
            GameObject root = new GameObject("ModManagerList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            root.transform.SetParent(transform, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(20f, 20f); rect.offsetMax = new Vector2(-20f, -20f);
            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            listRoot = root.transform;
        }

        public void Refresh()
        {
            if (listRoot == null) return;
            for (int i = 0; i < rows.Count; i++) if (rows[i] != null) Destroy(rows[i]);
            rows.Clear();
            if (runtime == null) runtime = ModRuntime.EnsureExists();

            for (int i = 0; i < runtime.LoadedManifests.Count; i++)
                AddRow(runtime.LoadedManifests[i], false);

            if (includeBuiltIn)
            {
                IReadOnlyList<string> ids = BuiltInModCatalog.GetAvailableIds();
                for (int i = 0; i < ids.Count; i++)
                {
                    bool alreadyListed = false;
                    for (int j = 0; j < runtime.LoadedManifests.Count; j++)
                        if (string.Equals(runtime.LoadedManifests[j].id, ids[i], StringComparison.OrdinalIgnoreCase)) { alreadyListed = true; break; }
                    if (alreadyListed) continue;
                    AddRow(new ModManifest { id = ids[i], name = BuiltInModCatalog.GetDisplayName(ids[i]), version = "built-in", enabled = BuiltInModCatalog.IsEnabled(ids[i]) }, true);
                }
            }
            if (statusText != null) statusText.text = "Mod: " + (runtime.LoadedManifests.Count + (includeBuiltIn ? BuiltInModCatalog.GetAvailableIds().Count : 0));
        }

        private void AddRow(ModManifest manifest, bool builtIn)
        {
            GameObject row = new GameObject("ModRow_" + manifest.id, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(listRoot, false);
            rows.Add(row);
            Image bg = row.GetComponent<Image>(); bg.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);
            LayoutElement element = row.GetComponent<LayoutElement>(); element.minHeight = 88f; element.preferredHeight = 88f;
            VerticalLayoutGroup rowLayout = row.GetComponent<VerticalLayoutGroup>(); rowLayout.padding = new RectOffset(10, 10, 8, 8); rowLayout.spacing = 5; rowLayout.childControlHeight = true; rowLayout.childForceExpandHeight = false;

            GameObject top = new GameObject("Top", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            top.transform.SetParent(row.transform, false);
            HorizontalLayoutGroup topLayout = top.GetComponent<HorizontalLayoutGroup>(); topLayout.spacing = 8; topLayout.childControlWidth = true; topLayout.childForceExpandWidth = false;
            top.GetComponent<LayoutElement>().preferredHeight = 32f;

            Toggle toggle = CreateToggle(top.transform, manifest.enabled);
            toggle.onValueChanged.AddListener(value => SetEnabled(manifest.id, value, builtIn));
            Text title = CreateText(top.transform, (string.IsNullOrEmpty(manifest.name) ? manifest.id : manifest.name) + "  v" + manifest.version, 18, true);
            title.gameObject.AddComponent<LayoutElement>().preferredWidth = 260f;
            string state = builtIn ? (manifest.enabled ? "Açık" : "Kapalı") : runtime.GetModStatus(manifest);
            Text stateText = CreateText(top.transform, state, 15, false); stateText.color = state == "Hazır" || state == "Açık" ? Color.green : Color.yellow;
            stateText.gameObject.AddComponent<LayoutElement>().preferredWidth = 145f;
            if (!builtIn)
            {
                Button reload = CreateButton(top.transform, "Uygula", 58); reload.onClick.AddListener(() => runtime.ReloadModSelection());
                Button up = CreateButton(top.transform, "↑", 30); up.onClick.AddListener(() => ChangeOrder(manifest, 1));
                Button down = CreateButton(top.transform, "↓", 30); down.onClick.AddListener(() => ChangeOrder(manifest, -1));
                Button delete = CreateButton(top.transform, "Sil", 50); delete.onClick.AddListener(() => Delete(manifest));
            }

            Text details = CreateText(row.transform, BuildDetails(manifest, builtIn), 13, false);
            details.color = new Color(0.75f, 0.8f, 0.85f);
            details.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
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
            if (builtIn || manifest.settings == null || manifest.settings.Count == 0) return;
            for (int i = 0; i < manifest.settings.Count; i++)
            {
                ModSettingDefinition setting = manifest.settings[i];
                if (setting == null || string.IsNullOrWhiteSpace(setting.key)) continue;
                GameObject line = new GameObject("Setting_" + setting.key, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                line.transform.SetParent(parent, false);
                HorizontalLayoutGroup layout = line.GetComponent<HorizontalLayoutGroup>(); layout.spacing = 6; layout.childControlWidth = true; layout.childForceExpandWidth = false;
                line.GetComponent<LayoutElement>().preferredHeight = 30f;
                CreateText(line.transform, string.IsNullOrEmpty(setting.label) ? setting.key : setting.label, 14, false).gameObject.AddComponent<LayoutElement>().preferredWidth = 220f;
                string current = ModSettingsStore.Get(manifest.id, setting.key, setting.defaultValue);
                if (setting.type.Equals("bool", StringComparison.OrdinalIgnoreCase))
                {
                    Toggle toggle = CreateToggle(line.transform, string.Equals(current, "true", StringComparison.OrdinalIgnoreCase));
                    toggle.onValueChanged.AddListener(v => SaveSetting(manifest, setting, v.ToString()));
                }
                else if (setting.type.Equals("float", StringComparison.OrdinalIgnoreCase) || setting.type.Equals("int", StringComparison.OrdinalIgnoreCase))
                {
                    float value; if (!float.TryParse(current, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value)) value = setting.min;
                    Slider slider = CreateSlider(line.transform, Mathf.Clamp(value, setting.min, setting.max), setting.min, setting.max);
                    Text valueText = CreateText(line.transform, current, 13, false);
                    slider.onValueChanged.AddListener(v => { string text = setting.type.Equals("int", StringComparison.OrdinalIgnoreCase) ? Mathf.RoundToInt(v).ToString() : v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture); valueText.text = text; SaveSetting(manifest, setting, text); });
                }
                else if (setting.type.Equals("dropdown", StringComparison.OrdinalIgnoreCase) && setting.options != null)
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

        private void ChangeOrder(ModManifest manifest, int delta)
        {
            if (runtime.SetModLoadOrder(manifest.id, manifest.loadOrder + delta)) Refresh();
        }

        private void Delete(ModManifest manifest)
        {
            if (pendingDeleteId != manifest.id || Time.unscaledTime > pendingDeleteUntil)
            {
                pendingDeleteId = manifest.id; pendingDeleteUntil = Time.unscaledTime + 5f;
                if (statusText != null) statusText.text = "Silmek için tekrar basın: " + manifest.name;
                return;
            }
            string message;
            runtime.DeleteExternalMod(manifest.id, out message);
            pendingDeleteId = null; pendingDeleteUntil = 0f;
            if (statusText != null) statusText.text = message;
            Refresh();
        }

        private void SaveSetting(ModManifest manifest, ModSettingDefinition setting, string value)
        {
            ModSettingsStore.Set(manifest.id, setting.key, value);
            if (setting.restartRequired && statusText != null) statusText.text = "Ayar kaydedildi. Uygulanması için modları yeniden yükleyin.";
        }

        private static Text CreateText(Transform parent, string value, int size, bool bold)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>(); text.text = value; text.fontSize = size; text.color = Color.white; text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bold) text.fontStyle = FontStyle.Bold;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, float width)
        {
            GameObject go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement)); go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredWidth = width; go.GetComponent<Image>().color = new Color(0.16f, 0.28f, 0.4f, 1f);
            Button button = go.GetComponent<Button>(); CreateText(go.transform, label, 14, true).alignment = TextAnchor.MiddleCenter; return button;
        }

        private static Toggle CreateToggle(Transform parent, bool value)
        {
            GameObject go = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(LayoutElement)); go.transform.SetParent(parent, false); go.GetComponent<LayoutElement>().preferredWidth = 32f;
            Toggle toggle = go.GetComponent<Toggle>(); toggle.isOn = value; return toggle;
        }

        private static Slider CreateSlider(Transform parent, float value, float min, float max)
        {
            GameObject go = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement)); go.transform.SetParent(parent, false); go.GetComponent<LayoutElement>().preferredWidth = 170f;
            Slider slider = go.GetComponent<Slider>(); slider.minValue = min; slider.maxValue = max; slider.value = value; return slider;
        }

        private static Dropdown CreateDropdown(Transform parent, List<string> options, string value)
        {
            GameObject go = new GameObject("Dropdown", typeof(RectTransform), typeof(Dropdown), typeof(LayoutElement)); go.transform.SetParent(parent, false); go.GetComponent<LayoutElement>().preferredWidth = 180f;
            Dropdown dropdown = go.GetComponent<Dropdown>(); dropdown.options.Clear(); for (int i = 0; i < options.Count; i++) dropdown.options.Add(new Dropdown.OptionData(options[i])); dropdown.value = Mathf.Max(0, options.IndexOf(value)); return dropdown;
        }

        private static InputField CreateInput(Transform parent, string value)
        {
            GameObject go = new GameObject("Input", typeof(RectTransform), typeof(InputField), typeof(LayoutElement)); go.transform.SetParent(parent, false); go.GetComponent<LayoutElement>().preferredWidth = 180f;
            InputField input = go.GetComponent<InputField>(); input.text = value; return input;
        }
    }
}
