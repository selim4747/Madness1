using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Madness.Mods
{
    /// <summary>Mod ayar satırı prefabına eklenir. Bir satırda yalnızca ilgili kontrol atanır.</summary>
    public sealed class ModSettingUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Toggle toggle;
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Dropdown dropdown;
        [SerializeField] private TMP_InputField input;
        [SerializeField] private TMP_Text valueText;

        private ModManifest manifest;
        private ModSettingDefinition definition;
        private Action<ModManifest, ModSettingDefinition, string> changed;
        private bool suppress;

        public void Bind(ModManifest mod, ModSettingDefinition setting, Action<ModManifest, ModSettingDefinition, string> onChanged)
        {
            manifest = mod;
            definition = setting;
            changed = onChanged;
            suppress = true;
            if (labelText != null) labelText.text = string.IsNullOrWhiteSpace(setting.label) ? setting.key : setting.label;
            string type = string.IsNullOrWhiteSpace(setting.type) ? "string" : setting.type.Trim().ToLowerInvariant();
            string current = ModSettingsStore.Get(mod.id, setting.key, setting.defaultValue);
            SetControlVisibility(type);

            if (type == "bool" && toggle != null)
                toggle.SetIsOnWithoutNotify(string.Equals(current, "true", StringComparison.OrdinalIgnoreCase));
            else if ((type == "float" || type == "int") && slider != null)
            {
                float number;
                if (!float.TryParse(current, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) number = setting.min;
                slider.minValue = setting.min;
                slider.maxValue = setting.max;
                slider.SetValueWithoutNotify(Mathf.Clamp(number, setting.min, setting.max));
                UpdateValueText(current);
            }
            else if (type == "dropdown" && dropdown != null)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(setting.options ?? new System.Collections.Generic.List<string>());
                int index = setting.options == null ? -1 : setting.options.IndexOf(current);
                dropdown.SetValueWithoutNotify(Mathf.Max(0, index));
            }
            else if (input != null) input.SetTextWithoutNotify(current);
            suppress = false;

            BindListeners(type);
        }

        private void SetControlVisibility(string type)
        {
            if (toggle != null) toggle.gameObject.SetActive(type == "bool");
            if (slider != null) slider.gameObject.SetActive(type == "float" || type == "int");
            if (dropdown != null) dropdown.gameObject.SetActive(type == "dropdown");
            if (input != null) input.gameObject.SetActive(type != "bool" && type != "float" && type != "int" && type != "dropdown");
        }

        private void BindListeners(string type)
        {
            if (toggle != null) { toggle.onValueChanged.RemoveAllListeners(); toggle.onValueChanged.AddListener(value => Save(value ? "true" : "false")); }
            if (slider != null) { slider.onValueChanged.RemoveAllListeners(); slider.onValueChanged.AddListener(value => Save(type == "int" ? Mathf.RoundToInt(value).ToString() : value.ToString("0.###", CultureInfo.InvariantCulture))); }
            if (dropdown != null) { dropdown.onValueChanged.RemoveAllListeners(); dropdown.onValueChanged.AddListener(index => { if (definition.options != null && index >= 0 && index < definition.options.Count) Save(definition.options[index]); }); }
            if (input != null) { input.onEndEdit.RemoveAllListeners(); input.onEndEdit.AddListener(Save); }
        }

        private void Save(string value)
        {
            if (suppress || changed == null) return;
            UpdateValueText(value);
            changed(manifest, definition, value);
        }

        private void UpdateValueText(string value)
        {
            if (valueText != null) valueText.text = value;
        }
    }
}
