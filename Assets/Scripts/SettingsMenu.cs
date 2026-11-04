// SettingsMenu.cs — Ses ve dil ayarlari
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;

    [Header("Ses")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI musicLabel;
    [SerializeField] private TextMeshProUGUI sfxLabel;

    [Header("Dil")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);

        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        // PanelYoneticisi sadece OYUN sahnesinde var — Ana Menu'den aciliyorsa Instance null
        // olur, bu durumda kayit sessizce atlanir (hata vermez).
        if (PanelYoneticisi.Instance != null && root != null) PanelYoneticisi.Instance.Kaydet(root);
    }

    public void Show()
    {
        if (PanelYoneticisi.Instance != null) PanelYoneticisi.Instance.DigerleriniKapat(root);
        RefreshFromCurrentValues();
        if (root != null) root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);

        // Ayarlar hemen diske yazilir
        if (SaveSystem.Instance != null && SaveSystem.Instance.ActiveSlot >= 0)
            SaveSystem.Instance.SaveToSlot(SaveSystem.Instance.ActiveSlot);
    }

    private void RefreshFromCurrentValues()
    {
        if (AudioManager.Instance != null)
        {
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }

        if (languageDropdown != null && LocalizationManager.Instance != null)
            languageDropdown.SetValueWithoutNotify((int)LocalizationManager.Instance.CurrentLanguage);

        UpdateLabels();
    }

    private void OnMusicChanged(float value)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetMusicVolume(value);
        UpdateLabels();
    }

    private void OnSfxChanged(float value)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetSfxVolume(value);
        UpdateLabels();
    }

    private void OnLanguageChanged(int index)
    {
        if (LocalizationManager.Instance == null) return;
        LocalizationManager.Instance.SetLanguage((GameLanguage)index);
    }

    private void UpdateLabels()
    {
        if (AudioManager.Instance == null) return;

        if (musicLabel != null)
            musicLabel.text = "Muzik: %" + Mathf.RoundToInt(AudioManager.Instance.MusicVolume * 100f);

        if (sfxLabel != null)
            sfxLabel.text = "Efekt: %" + Mathf.RoundToInt(AudioManager.Instance.SfxVolume * 100f);
    }
}