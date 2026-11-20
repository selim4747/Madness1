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
        [SerializeField] private Toggle enabledToggle;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button moveUpButton;
        [SerializeField] private Button moveDownButton;

        private ModManifest manifest;
        private bool builtIn;
        private ModRuntime runtime;
        private Action<ModManifest, bool> select;
        private Action<ModManifest, bool> toggle;
        private Action<ModManifest, bool> delete;

        public void Bind(ModManifest value, bool isBuiltIn, ModRuntime modRuntime,
            Action<ModManifest, bool> onSelect, Action<ModManifest, bool> onToggle,
            Action<ModManifest, bool> onDelete)
        {
            manifest = value;
            builtIn = isBuiltIn;
            runtime = modRuntime;
            select = onSelect;
            toggle = onToggle;
            delete = onDelete;

            if (titleText != null) titleText.text = string.IsNullOrWhiteSpace(value.name) ? value.id : value.name;
            if (detailsText != null) detailsText.text = BuildDetails(value);
            if (statusText != null) statusText.text = isBuiltIn ? (value.enabled ? "Açık" : "Kapalı") : runtime.GetModStatus(value);
            if (enabledToggle != null)
            {
                enabledToggle.SetIsOnWithoutNotify(value.enabled);
                enabledToggle.onValueChanged.RemoveAllListeners();
                enabledToggle.onValueChanged.AddListener(OnToggleChanged);
            }
            BindButton(selectButton, OnSelectClicked);
            BindButton(deleteButton, OnDeleteClicked);
            BindButton(moveUpButton, MoveUp);
            BindButton(moveDownButton, MoveDown);
            if (deleteButton != null) deleteButton.gameObject.SetActive(!builtIn);
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
        private void OnSelectClicked() { if (select != null) select(manifest, builtIn); }
        private void OnDeleteClicked() { if (delete != null) delete(manifest, builtIn); }
        private void OnToggleChanged(bool value) { if (toggle != null) toggle(manifest, builtIn); }

        public void MoveUp() { SendLoadOrderChange(1); }
        public void MoveDown() { SendLoadOrderChange(-1); }
        private void SendLoadOrderChange(int delta) { if (!builtIn && runtime != null) runtime.SetModLoadOrder(manifest.id, manifest.loadOrder + delta); }
    }
}
