using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Madness.Mods
{
    /// <summary>
    /// Mod yönetimi için yalnızca kontrol katmanıdır. Panel, ScrollRect, kart veya
    /// ayar satırı üretmez; Inspector'dan kullanıcının hazırladığı nesneler atanır.
    /// </summary>
    public sealed class ModManagerController : MonoBehaviour
    {
        [Header("Liste - UI'yi sen oluştur ve bağla")]
        [SerializeField] private ScrollRect modScrollRect;
        [SerializeField] private Transform modContent;
        [SerializeField] private ModCardUI modCardPrefab;
        [SerializeField] private bool includeBuiltIn = true;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Dropdown filterDropdown;
        [SerializeField] private TMP_Dropdown sortDropdown;

        [Header("Paneller - GameObject'leri sen oluştur ve bağla")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject diagnosticsPanel;
        [SerializeField] private GameObject importPanel;
        [SerializeField] private GameObject profilesPanel;
        [SerializeField] private GameObject saveCompatibilityPanel;

        [Header("Detay ve durum metinleri")]
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailBody;
        [SerializeField] private TMP_Text diagnosticsText;
        [SerializeField] private TMP_Text statusText;

        [Header("Ayar satırı - satır prefabını sen oluştur ve bağla")]
        [SerializeField] private Transform settingsContent;
        [SerializeField] private ModSettingUI settingPrefab;

        [Header("Kontrol butonları - isteğe bağlı")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button importButton;
        [SerializeField] private Button selectedToggleButton;
        [SerializeField] private Button selectedDeleteButton;
        [SerializeField] private Button deleteConfirmButton;
        [SerializeField] private Button deleteCancelButton;
        [SerializeField] private Button reloadButton;

        [Header("Kayıt uyumluluğu")]
        [SerializeField] private TMP_Text saveCompatibilityText;
        [SerializeField] private Button continueWithMissingModsButton;
        [SerializeField] private Button cancelSaveLoadButton;

        [Header("Profil")]
        [SerializeField] private TMP_InputField profileNameInput;
        [SerializeField] private TMP_Dropdown profileDropdown;
        [SerializeField] private Button saveProfileButton;
        [SerializeField] private Button applyProfileButton;
        [SerializeField] private Button deleteProfileButton;

        private readonly List<ModCardUI> cards = new List<ModCardUI>();
        private readonly List<ModSettingUI> settingRows = new List<ModSettingUI>();
        private ModRuntime runtime;
        private ModManifest selected;
        private bool selectedBuiltIn;
        private string pendingDeleteId;
        private int pendingSaveSlot = -1;
        private List<ModProfile> profiles = new List<ModProfile>();

        private void Start()
        {
            runtime = ModRuntime.EnsureExists();
            runtime.OnModsChanged += Refresh;
            BindButtons();
            LoadProfiles();
            Refresh();
            HidePanels();
        }

        private void OnDestroy()
        {
            if (runtime != null) runtime.OnModsChanged -= Refresh;
        }

        private void BindButtons()
        {
            AddClick(refreshButton, Refresh);
            AddClick(importButton, PickAndImportPackage);
            AddClick(selectedToggleButton, ToggleSelected);
            AddClick(selectedDeleteButton, AskDeleteSelected);
            AddClick(deleteConfirmButton, ConfirmDelete);
            AddClick(deleteCancelButton, CancelDelete);
            AddClick(reloadButton, ReloadMods);
            AddClick(continueWithMissingModsButton, ContinuePendingSaveLoad);
            AddClick(cancelSaveLoadButton, CancelPendingSaveLoad);
            AddClick(saveProfileButton, SaveCurrentProfile);
            AddClick(applyProfileButton, ApplySelectedProfile);
            AddClick(deleteProfileButton, DeleteSelectedProfile);
            if (searchInput != null) searchInput.onValueChanged.AddListener(_ => Refresh());
            if (filterDropdown != null) filterDropdown.onValueChanged.AddListener(_ => Refresh());
            if (sortDropdown != null) sortDropdown.onValueChanged.AddListener(_ => Refresh());
        }

        private void AddClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        public void Refresh()
        {
            if (runtime == null) runtime = ModRuntime.EnsureExists();
            ClearCards();
            List<ModEntry> entries = CollectMods();
            if (modContent == null || modCardPrefab == null)
            {
                SetStatus("Mod listesi için Content ve Card Prefab atanmalı.");
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                ModEntry entry = entries[i];
                ModCardUI card = Instantiate(modCardPrefab, modContent);
                card.Bind(entry.Manifest, entry.BuiltIn, runtime, SelectMod, ToggleMod, SaveSetting);
                cards.Add(card);
            }
            if (modScrollRect != null) modScrollRect.verticalNormalizedPosition = 1f;
            SetStatus("Mod sayısı: " + entries.Count);
        }

        private List<ModEntry> CollectMods()
        {
            List<ModEntry> result = new List<ModEntry>();
            for (int i = 0; i < runtime.LoadedManifests.Count; i++)
            {
                ModManifest manifest = runtime.LoadedManifests[i];
                if (manifest != null) result.Add(new ModEntry(manifest, false));
            }
            if (!includeBuiltIn) return ApplyListQuery(result);

            IReadOnlyList<string> ids = BuiltInModCatalog.GetAvailableIds();
            for (int i = 0; i < ids.Count; i++)
            {
                if (result.Any(x => string.Equals(x.Manifest.id, ids[i], StringComparison.OrdinalIgnoreCase))) continue;
                result.Add(new ModEntry(new ModManifest
                {
                    id = ids[i],
                    identity = BuiltInModCatalog.GetIdentity(ids[i]),
                    name = BuiltInModCatalog.GetDisplayName(ids[i]),
                    version = "yerleşik",
                    author = "NJG Games",
                    enabled = BuiltInModCatalog.IsEnabled(ids[i])
                }, true));
            }
            return ApplyListQuery(result);
        }

        private List<ModEntry> ApplyListQuery(List<ModEntry> result)
        {
            string query = searchInput == null ? "" : searchInput.text.Trim();
            int filter = filterDropdown == null ? 0 : filterDropdown.value;
            result = result.Where(x => MatchesListQuery(x, query, filter)).ToList();
            int sort = sortDropdown == null ? 0 : sortDropdown.value;
            if (sort == 1) result.Sort((a, b) => string.Compare(a.Manifest.name, b.Manifest.name, StringComparison.OrdinalIgnoreCase));
            else if (sort == 2) result.Sort((a, b) => a.Manifest.loadOrder.CompareTo(b.Manifest.loadOrder));
            else if (sort == 3) result.Sort((a, b) => string.Compare(runtime.GetModStatus(a.Manifest), runtime.GetModStatus(b.Manifest), StringComparison.OrdinalIgnoreCase));
            return result;
        }

        private bool MatchesListQuery(ModEntry entry, string query, int filter)
        {
            bool textMatch = string.IsNullOrEmpty(query) ||
                (entry.Manifest.name ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (entry.Manifest.id ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (entry.Manifest.author ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
            if (!textMatch) return false;
            string status = entry.BuiltIn ? (entry.Manifest.enabled ? "Açık" : "Kapalı") : runtime.GetModStatus(entry.Manifest);
            if (filter == 1) return entry.Manifest.enabled;
            if (filter == 2) return !entry.Manifest.enabled;
            if (filter == 3) return !entry.BuiltIn && status != "Hazır" && status != "Kapalı";
            if (filter == 4) return entry.BuiltIn;
            if (filter == 5) return !entry.BuiltIn;
            return true;
        }

        private void ClearCards()
        {
            for (int i = cards.Count - 1; i >= 0; i--)
                if (cards[i] != null) Destroy(cards[i].gameObject);
            cards.Clear();
        }

        public void SelectMod(ModManifest manifest, bool builtIn)
        {
            selected = manifest;
            selectedBuiltIn = builtIn;
            if (detailTitle != null) detailTitle.text = string.IsNullOrWhiteSpace(manifest.name) ? manifest.id : manifest.name;
            if (detailBody != null) detailBody.text = BuildDetails(manifest, builtIn);
            if (detailPanel != null) detailPanel.SetActive(true);
            ShowSettings(manifest, builtIn);
            ShowDiagnostics(manifest);
        }

        private string BuildDetails(ModManifest mod, bool builtIn)
        {
            string dependency = builtIn ? "Yok" : runtime.GetDependencyError(mod);
            if (string.IsNullOrEmpty(dependency)) dependency = "Yok";
            return "Kimlik: " + NullDash(mod.identity) + "\n" +
                   "Sürüm: " + NullDash(mod.version) + "\n" +
                   "Yapımcı: " + NullDash(mod.author) + "\n" +
                   "Durum: " + (builtIn ? (mod.enabled ? "Açık" : "Kapalı") : runtime.GetModStatus(mod)) + "\n" +
                   "Yükleme sırası: " + mod.loadOrder + "\n" +
                   "Bağımlılık: " + dependency + "\n\n" +
                   (string.IsNullOrWhiteSpace(mod.description) ? "Açıklama yok." : mod.description);
        }

        private string NullDash(string value) { return string.IsNullOrWhiteSpace(value) ? "-" : value; }

        public void ToggleMod(ModManifest manifest, bool builtIn)
        {
            if (manifest == null) return;
            bool changed = builtIn
                ? BuiltInModCatalog.SetEnabled(manifest.id, !manifest.enabled)
                : runtime.SetExternalModEnabled(manifest.id, !manifest.enabled);
            if (!changed) Refresh();
        }

        private void ToggleSelected()
        {
            if (selected != null) ToggleMod(selected, selectedBuiltIn);
        }

        public void AskDelete(ModManifest manifest, bool builtIn)
        {
            if (builtIn)
            {
                SetStatus("Yerleşik modlar silinemez.");
                return;
            }
            selected = manifest;
            selectedBuiltIn = false;
            pendingDeleteId = manifest.id;
            if (detailPanel != null) detailPanel.SetActive(true);
            if (deleteConfirmButton != null) deleteConfirmButton.gameObject.SetActive(true);
            SetStatus("Silmek için onayla: " + manifest.name);
        }

        private void AskDeleteSelected()
        {
            if (selected != null) AskDelete(selected, selectedBuiltIn);
        }

        private void ConfirmDelete()
        {
            if (string.IsNullOrWhiteSpace(pendingDeleteId)) return;
            string message;
            runtime.DeleteExternalMod(pendingDeleteId, out message);
            pendingDeleteId = null;
            SetStatus(message);
            Refresh();
        }

        private void CancelDelete()
        {
            pendingDeleteId = null;
            SetStatus("Silme iptal edildi.");
        }

        public void ChangeSelectedLoadOrder(int delta)
        {
            if (selected == null || selectedBuiltIn) return;
            if (runtime.SetModLoadOrder(selected.id, selected.loadOrder + delta)) Refresh();
        }

        public void ReloadMods()
        {
            runtime.ReloadModSelection();
            Refresh();
        }

        private void ShowSettings(ModManifest manifest, bool builtIn)
        {
            ClearSettingRows();
            if (settingsPanel != null) settingsPanel.SetActive(!builtIn && manifest != null && manifest.settings != null && manifest.settings.Count > 0);
            if (builtIn || manifest == null || settingPrefab == null || settingsContent == null || manifest.settings == null) return;
            for (int i = 0; i < manifest.settings.Count; i++)
            {
                ModSettingDefinition definition = manifest.settings[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.key)) continue;
                ModSettingUI row = Instantiate(settingPrefab, settingsContent);
                row.Bind(manifest, definition, SaveSetting);
                settingRows.Add(row);
            }
        }

        private void ClearSettingRows()
        {
            for (int i = settingRows.Count - 1; i >= 0; i--)
                if (settingRows[i] != null) Destroy(settingRows[i].gameObject);
            settingRows.Clear();
        }

        private void SaveSetting(ModManifest manifest, ModSettingDefinition definition, string value)
        {
            ModSettingsStore.Set(manifest.id, definition.key, value);
            if (definition.restartRequired) SetStatus("Ayar kaydedildi. Uygulamak için Modları Yeniden Yükle kullanılmalı.");
            else ReloadMods();
        }

        private void ShowDiagnostics(ModManifest manifest)
        {
            List<ModDiagnostic> found = runtime.Diagnostics.Where(x => string.Equals(x.modId, manifest.id, StringComparison.OrdinalIgnoreCase)).ToList();
            if (diagnosticsPanel != null) diagnosticsPanel.SetActive(found.Count > 0);
            if (diagnosticsText != null) diagnosticsText.text = found.Count == 0 ? "Hata yok." : string.Join("\n", found.Select(x => (x.error ? "Hata: " : "Uyarı: ") + x.message));
        }

        public void PickAndImportPackage()
        {
            if (!ModFilePicker.IsSupported)
            {
                SetStatus("Bu platformda mod dosyası seçilemiyor.");
                return;
            }
            SetStatus("Mod dosyası seçiliyor...");
            ModFilePicker.Open(OnPackagePicked);
        }

        private void OnPackagePicked(string path, string error)
        {
            if (!string.IsNullOrEmpty(error)) { SetStatus("Hata: " + error); return; }
            if (string.IsNullOrEmpty(path)) { SetStatus("Dosya seçilmedi."); return; }
            ModImportResult result = ModPackageImporter.ImportPackage(path);
            SetStatus(result.Message);
            if (result.Success) Refresh();
        }

        public void CheckSaveCompatibility(int slot)
        {
            SaveData data = SaveSystem.EnsureExists().PeekSlot(slot);
            List<string> missing = runtime.GetMissingRequiredMods(data);
            pendingSaveSlot = slot;
            if (saveCompatibilityPanel != null) saveCompatibilityPanel.SetActive(missing.Count > 0);
            if (saveCompatibilityText != null)
                saveCompatibilityText.text = missing.Count == 0 ? "Mod uyumluluğu tamam." : "Eksik modlar:\n- " + string.Join("\n- ", missing);
            if (missing.Count == 0) LoadCheckedSave();
        }

        private void LoadCheckedSave()
        {
            if (pendingSaveSlot < 0) return;
            int slot = pendingSaveSlot;
            pendingSaveSlot = -1;
            SaveSystem.RequestLoadSlot(slot);
            SetStatus("Kayıt yüklemeye hazır.");
        }

        public void ContinuePendingSaveLoad() { LoadCheckedSave(); }
        public void CancelPendingSaveLoad() { pendingSaveSlot = -1; if (saveCompatibilityPanel != null) saveCompatibilityPanel.SetActive(false); }

        public void ShowActiveModsSummary(TMP_Text target)
        {
            if (target == null) return;
            List<ModEntry> entries = CollectMods();
            int active = entries.Count(x => x.Manifest.enabled);
            int errors = entries.Count(x => !x.BuiltIn && runtime.GetModStatus(x.Manifest) != "Hazır" && runtime.GetModStatus(x.Manifest) != "Kapalı");
            target.text = "Aktif mod: " + active + "\nToplam mod: " + entries.Count + "\nUyarı/hata: " + errors;
        }

        private void LoadProfiles()
        {
            profiles = ModProfileStore.Load();
            if (profileDropdown == null) return;
            profileDropdown.ClearOptions();
            profileDropdown.AddOptions(profiles.Select(x => x.name).ToList());
        }

        private void SaveCurrentProfile()
        {
            string name = profileNameInput == null ? "" : profileNameInput.text;
            if (string.IsNullOrWhiteSpace(name)) { SetStatus("Profil adı girilmeli."); return; }
            ModProfileStore.Save(new ModProfile { name = name.Trim(), enabledIds = CollectMods().Where(x => x.Manifest.enabled).Select(x => x.Manifest.id).ToList() });
            LoadProfiles();
            SetStatus("Profil kaydedildi: " + name.Trim());
        }

        private void ApplySelectedProfile()
        {
            if (profileDropdown == null || profileDropdown.value < 0 || profileDropdown.value >= profiles.Count) return;
            ModProfile profile = profiles[profileDropdown.value];
            HashSet<string> enabled = new HashSet<string>(profile.enabledIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (ModEntry entry in CollectMods())
            {
                bool shouldEnable = enabled.Contains(entry.Manifest.id);
                if (entry.BuiltIn) BuiltInModCatalog.SetEnabled(entry.Manifest.id, shouldEnable);
                else runtime.SetExternalModEnabled(entry.Manifest.id, shouldEnable);
            }
            ReloadMods();
        }

        private void DeleteSelectedProfile()
        {
            if (profileDropdown == null || profileDropdown.value < 0 || profileDropdown.value >= profiles.Count) return;
            ModProfileStore.Delete(profiles[profileDropdown.value].name);
            LoadProfiles();
        }

        private void HidePanels()
        {
            if (detailPanel != null) detailPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (diagnosticsPanel != null) diagnosticsPanel.SetActive(false);
            if (importPanel != null) importPanel.SetActive(false);
            if (profilesPanel != null) profilesPanel.SetActive(false);
            if (saveCompatibilityPanel != null) saveCompatibilityPanel.SetActive(false);
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message ?? "";
        }

        private readonly struct ModEntry
        {
            public readonly ModManifest Manifest;
            public readonly bool BuiltIn;
            public ModEntry(ModManifest manifest, bool builtIn) { Manifest = manifest; BuiltIn = builtIn; }
        }
    }

    [Serializable]
    public sealed class ModProfile
    {
        public string name = "";
        public List<string> enabledIds = new List<string>();
    }

    [Serializable]
    internal sealed class ModProfileFile
    {
        public List<ModProfile> profiles = new List<ModProfile>();
    }

    internal static class ModProfileStore
    {
        private static string PathFor { get { return Path.Combine(Application.persistentDataPath, "mod_profiles.json"); } }

        public static List<ModProfile> Load()
        {
            try
            {
                if (!File.Exists(PathFor)) return new List<ModProfile>();
                ModProfileFile file = JsonUtility.FromJson<ModProfileFile>(File.ReadAllText(PathFor));
                return file == null || file.profiles == null ? new List<ModProfile>() : file.profiles;
            }
            catch (Exception ex) { Debug.LogWarning("[ModProfiles] Okunamadı: " + ex.Message); return new List<ModProfile>(); }
        }

        public static void Save(ModProfile profile)
        {
            List<ModProfile> profiles = Load();
            profiles.RemoveAll(x => string.Equals(x.name, profile.name, StringComparison.OrdinalIgnoreCase));
            profiles.Add(profile);
            Write(profiles);
        }

        public static void Delete(string name)
        {
            List<ModProfile> profiles = Load();
            profiles.RemoveAll(x => string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));
            Write(profiles);
        }

        private static void Write(List<ModProfile> profiles)
        {
            try { File.WriteAllText(PathFor, JsonUtility.ToJson(new ModProfileFile { profiles = profiles }, true)); }
            catch (Exception ex) { Debug.LogWarning("[ModProfiles] Yazılamadı: " + ex.Message); }
        }
    }
}
