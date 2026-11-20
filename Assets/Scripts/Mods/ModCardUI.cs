using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Madness.Mods
{
    /// <summary>Mod kartı prefabına eklenir. Görsel nesneler Inspector'dan atanır.</summary>
    public sealed class ModCardUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text dependencyText;
        [SerializeField] private GameObject expandablePanel;
        [SerializeField] private Transform settingsContent;
        [SerializeField] private ModSettingUI settingPrefab;
        [SerializeField] private Toggle enabledToggle;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button moveUpButton;
        [SerializeField] private Button moveDownButton;

        private ModManifest manifest;
        private bool builtIn;
        private ModRuntime runtime;
        private Action<ModManifest, bool> select;
        private Action<ModManifest, bool> toggle;
        private Action<ModManifest, ModSettingDefinition, string> settingChanged;
        private readonly System.Collections.Generic.List<ModSettingUI> settingRows = new System.Collections.Generic.List<ModSettingUI>();

        public void Bind(ModManifest value, bool isBuiltIn, ModRuntime modRuntime,
            Action<ModManifest, bool> onSelect, Action<ModManifest, bool> onToggle,
            Action<ModManifest, ModSettingDefinition, string> onSettingChanged = null)
        {
            manifest = value;
            builtIn = isBuiltIn;
            runtime = modRuntime;
            select = onSelect;
            toggle = onToggle;
            settingChanged = onSettingChanged;

            if (titleText != null) titleText.text = string.IsNullOrWhiteSpace(value.name) ? value.id : value.name;
            if (detailsText != null) detailsText.text = BuildDetails(value);
            if (statusText != null) statusText.text = isBuiltIn ? (value.enabled ? "Açık" : "Kapalı") : runtime.GetModStatus(value);
            if (descriptionText != null) descriptionText.text = string.IsNullOrWhiteSpace(value.description) ? "Açıklama yok." : value.description;
            if (dependencyText != null) dependencyText.text = isBuiltIn ? "Bağımlılık: Yok" : "Bağımlılık: " + (runtime.GetDependencyError(value) ?? "Yok");
            ClearSettingRows();
            if (expandablePanel != null) expandablePanel.SetActive(false);
            if (!isBuiltIn && settingsContent != null && settingPrefab != null && value.settings != null)
                for (int i = 0; i < value.settings.Count; i++)
                {
                    ModSettingDefinition definition = value.settings[i];
                    if (definition == null || string.IsNullOrWhiteSpace(definition.key)) continue;
                    ModSettingUI row = UnityEngine.Object.Instantiate(settingPrefab, settingsContent);
                    row.Bind(value, definition, settingChanged);
                    settingRows.Add(row);
                }
            if (enabledToggle != null)
            {
                enabledToggle.SetIsOnWithoutNotify(value.enabled);
                enabledToggle.onValueChanged.RemoveAllListeners();
                enabledToggle.onValueChanged.AddListener(OnToggleChanged);
            }
            BindButton(selectButton, OnSelectClicked);
            BindButton(moveUpButton, MoveUp);
            BindButton(moveDownButton, MoveDown);
        }

        private string BuildDetails(ModManifest value)
        {
            return "Kimlik: " + Dash(value.identity) + "\n" +
                   "Sürüm: " + Dash(value.version) + "\n" +
                   "Yapımcı: " + Dash(value.author) + "\n" +
                   "Yükleme sırası: " + value.loadOrder;
        }

        private string Dash(string value) { return string.IsNullOrWhiteSpace(value) ? "-" : value; }
        private void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
        private void OnSelectClicked()
        {
            if (expandablePanel != null) expandablePanel.SetActive(!expandablePanel.activeSelf);
            if (select != null) select(manifest, builtIn);
        }
        private void OnToggleChanged(bool value) { if (toggle != null) toggle(manifest, builtIn); }

        public void MoveUp() { SendLoadOrderChange(1); }
        public void MoveDown() { SendLoadOrderChange(-1); }
        private void SendLoadOrderChange(int delta) { if (!builtIn && runtime != null) runtime.SetModLoadOrder(manifest.id, manifest.loadOrder + delta); }

        private void ClearSettingRows()
        {
            for (int i = settingRows.Count - 1; i >= 0; i--)
                if (settingRows[i] != null) UnityEngine.Object.Destroy(settingRows[i].gameObject);
            settingRows.Clear();
        }
    }
}
