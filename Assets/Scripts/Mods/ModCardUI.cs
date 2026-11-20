using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Madness.Mods
{
    /// <summary>Mod kartı prefabına eklenir. Görsel nesneler Inspector'dan atanır.</summary>
    public sealed class ModCardUI : MonoBehaviour
    {
        [Header("Button prefabı - şu an sadece bu temel alanları bağla")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text dependencyText;
        [SerializeField] private GameObject expandablePanel;
        [Header("Durum ve aç/kapa metinleri")]
        [Tooltip("Kartın üzerinde görünen durum metni: Durum: Açık/Kapalı")]
        [SerializeField] private TMP_Text cardStatusText;
        [Tooltip("Açılan panelin içinde görünen durum metni: Durum: Açık/Kapalı")]
        [SerializeField] private TMP_Text panelStatusText;
        [Tooltip("Modu Aç/Modu Kapat butonunun yazısı")]
        [SerializeField] private TMP_Text toggleButtonText;
        [Header("Panel dışı aç/kapat kontrolü")]
        [Tooltip("Panelin dışında bulunan aç/kapat Button'u.")]
        [SerializeField] private Button externalToggleButton;
        [Tooltip("Panelin dışında bulunan Toggle component'i; Button yerine kullanılabilir.")]
        [SerializeField] private Toggle externalToggle;
        [Tooltip("Panel dışı butonun yazısı.")]
        [SerializeField] private TMP_Text externalToggleButtonText;
        [HideInInspector] [SerializeField] private Transform settingsContent;
        [HideInInspector] [SerializeField] private ModSettingUI settingPrefab;
        [Header("GameObject (2) uyumlu ayrı bilgi alanları - isteğe bağlı")]
        [SerializeField] private TMP_Text modNameText;
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private TMP_Text authorText;
        [SerializeField] private TMP_Text identityText;
        [SerializeField] private TMP_Text dependencyInfoText;
        [SerializeField] private TMP_Text descriptionInfoText;
        [SerializeField] private Toggle enabledToggle;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button closePanelButton;
        [HideInInspector] [SerializeField] private Button moveUpButton;
        [HideInInspector] [SerializeField] private Button moveDownButton;

        private ModManifest manifest;
        private bool builtIn;
        private ModRuntime runtime;
        private Action<ModManifest, bool> select;
        private Action<ModManifest, bool> toggle;
        private Action<ModManifest, ModSettingDefinition, string> settingChanged;
        private readonly System.Collections.Generic.List<ModSettingUI> settingRows = new System.Collections.Generic.List<ModSettingUI>();
        private Transform expandableOriginalParent;
        private int expandableOriginalSiblingIndex;
        private bool expandablePanelIsOverlay;
        private static ModCardUI activeOverlayCard;

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

            string modName = string.IsNullOrWhiteSpace(value.name) ? value.id : value.name;
            string author = Dash(value.author);
            string version = Dash(value.version);

            // Kapalı kart görünümünde her alan yalnızca kendi değerini göstermeli.
            if (titleText != null) titleText.text = modName;
            if (detailsText != null) detailsText.text = author;
            if (statusText != null) statusText.text = version;
            SetStatusText(value.enabled);
            if (descriptionText != null) descriptionText.text = string.IsNullOrWhiteSpace(value.description) ? "Açıklama yok." : value.description;
            if (dependencyText != null) dependencyText.text = isBuiltIn ? "Bağımlılık: Yok" : "Bağımlılık: " + (runtime.GetDependencyError(value) ?? "Yok");
            SetOptionalText(modNameText, modName);
            SetOptionalText(versionText, version);
            SetOptionalText(authorText, author);
            SetOptionalText(identityText, "Kimlik: " + Dash(value.identity));
            SetOptionalText(dependencyInfoText, "Bağımlılık: " + (isBuiltIn ? "Yok" : (runtime.GetDependencyError(value) ?? "Yok")));
            SetOptionalText(descriptionInfoText, string.IsNullOrWhiteSpace(value.description) ? "Açıklama yok." : value.description);
            RestoreExpandablePanelParent();
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
            if (externalToggle != null)
            {
                externalToggle.SetIsOnWithoutNotify(value.enabled);
                externalToggle.onValueChanged.RemoveAllListeners();
                externalToggle.onValueChanged.AddListener(OnExternalToggleChanged);
            }
            // Button.prefab bir prefab olduğu için root Button'ın OnClick'i sahnedeki
            // objeye Inspector'dan bağlanamaz. Klonun kendi ana butonunu burada bağla.
            Button rootButton = GetComponent<Button>();
            if (rootButton != null && rootButton != selectButton)
                BindButton(rootButton, OnSelectClicked);
            BindButton(selectButton, OnSelectClicked);
            BindButton(closePanelButton, ClosePanel);
            BindButton(moveUpButton, MoveUp);
            BindButton(moveDownButton, MoveDown);
            BindButton(externalToggleButton, OnExternalToggleClicked);
            UpdateMoveButtonState();
        }

        private string BuildDetails(ModManifest value)
        {
            return "Kimlik: " + Dash(value.identity) + "\n" +
                   "Sürüm: " + Dash(value.version) + "\n" +
                   "Yapımcı: " + Dash(value.author) + "\n" +
                   "Yükleme sırası: " + value.loadOrder;
        }

        private string Dash(string value) { return string.IsNullOrWhiteSpace(value) ? "-" : value; }
        private void SetOptionalText(TMP_Text target, string value)
        {
            if (target != null) target.text = value ?? "";
        }

        private void SetStatusText(bool enabled)
        {
            string status = enabled ? "Durum: Açık" : "Durum: Kapalı";
            SetOptionalText(cardStatusText, status);
            SetOptionalText(panelStatusText, status);

            TMP_Text label = toggleButtonText;
            if (label == null && enabledToggle != null)
                label = enabledToggle.GetComponentInChildren<TMP_Text>(true);
            SetOptionalText(label, enabled ? "Modu Kapat" : "Modu Aç");

            TMP_Text externalLabel = externalToggleButtonText;
            if (externalLabel == null && externalToggleButton != null)
                externalLabel = externalToggleButton.GetComponentInChildren<TMP_Text>(true);
            SetOptionalText(externalLabel, enabled ? "Modu Kapat" : "Modu Aç");
            if (externalToggle != null)
                externalToggle.SetIsOnWithoutNotify(enabled);
        }

        private void UpdateMoveButtonState()
        {
            if (runtime == null || manifest == null || builtIn)
            {
                SetMoveButtonState(moveUpButton, false);
                SetMoveButtonState(moveDownButton, false);
                return;
            }

            int lowest = manifest.loadOrder;
            int highest = manifest.loadOrder;
            for (int i = 0; i < runtime.LoadedManifests.Count; i++)
            {
                ModManifest other = runtime.LoadedManifests[i];
                if (other == null) continue;
                lowest = Mathf.Min(lowest, other.loadOrder);
                highest = Mathf.Max(highest, other.loadOrder);
            }

            // MoveUp +1 olduğundan en yüksek sırada yukarı tuşu; MoveDown -1
            // olduğundan en düşük sırada aşağı tuşu pasif görünür.
            SetMoveButtonState(moveUpButton, manifest.loadOrder >= highest);
            SetMoveButtonState(moveDownButton, manifest.loadOrder <= lowest);
        }

        private void SetMoveButtonState(Button button, bool dimmed)
        {
            if (button == null) return;
            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = dimmed ? 0.35f : 1f;
            group.interactable = !dimmed;
            group.blocksRaycasts = !dimmed;
        }
        private void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
        private void OnSelectClicked()
        {
            if (expandablePanel != null)
            {
                bool shouldOpen = !expandablePanel.activeSelf;
                if (activeOverlayCard != null && activeOverlayCard != this)
                    activeOverlayCard.ClosePanel();

                if (shouldOpen)
                {
                    MoveExpandablePanelToOverlay();
                    expandablePanel.SetActive(true);
                    activeOverlayCard = this;
                }
                else
                {
                    ClosePanel();
                }
            }
            if (select != null) select(manifest, builtIn);
        }
        private void ClosePanel()
        {
            if (expandablePanel != null) expandablePanel.SetActive(false);
            RestoreExpandablePanelParent();
            if (activeOverlayCard == this) activeOverlayCard = null;
        }

        private void MoveExpandablePanelToOverlay()
        {
            if (expandablePanel == null || expandablePanelIsOverlay) return;
            RectTransform panelRect = expandablePanel.transform as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (panelRect == null || canvas == null) return;

            expandableOriginalParent = panelRect.parent;
            expandableOriginalSiblingIndex = panelRect.GetSiblingIndex();
            Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            panelRect.SetParent(rootCanvas.transform, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.localRotation = Quaternion.identity;
            panelRect.localScale = Vector3.one;
            panelRect.SetAsLastSibling();
            expandablePanelIsOverlay = true;
        }

        private void RestoreExpandablePanelParent()
        {
            if (!expandablePanelIsOverlay || expandablePanel == null || expandableOriginalParent == null) return;
            RectTransform panelRect = expandablePanel.transform as RectTransform;
            if (panelRect == null) return;

            panelRect.SetParent(expandableOriginalParent, false);
            int maxIndex = expandableOriginalParent.childCount - 1;
            panelRect.SetSiblingIndex(Mathf.Clamp(expandableOriginalSiblingIndex, 0, maxIndex));
            expandablePanelIsOverlay = false;
            expandableOriginalParent = null;
        }
        private void OnToggleChanged(bool value)
        {
            SetStatusText(value);
            if (toggle != null) toggle(manifest, builtIn);
        }

        private void OnExternalToggleClicked()
        {
            bool nextValue = manifest != null && !manifest.enabled;
            SetStatusText(nextValue);
            if (toggle != null) toggle(manifest, builtIn);
        }

        private void OnExternalToggleChanged(bool value)
        {
            SetStatusText(value);
            if (toggle != null) toggle(manifest, builtIn);
        }

        private void LateUpdate()
        {
            // VerticalLayoutGroup yatay konumu tekrar yazabildiği için kartın
            // anchored Pos X değerini her frame 0'da tut.
            RectTransform rect = transform as RectTransform;
            if (rect != null && !Mathf.Approximately(rect.anchoredPosition.x, 1000f))
                rect.anchoredPosition = new Vector2(1000f, rect.anchoredPosition.y);
        }

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
