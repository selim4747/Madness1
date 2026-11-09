// LocalizationManager.cs — Coklu dil altyapisi (ilk surumde sadece Turkce)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;
using System.Collections.Generic;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [Serializable]
    public class LanguageEntry
    {
        public GameLanguage language;
        public TextAsset csvFile;   // format: anahtar;metin (her satirda bir kayit)
    }

    [Header("Dil dosyalari")]
    [SerializeField] private List<LanguageEntry> languages = new List<LanguageEntry>();

    private readonly Dictionary<string, string> table = new Dictionary<string, string>();

    public GameLanguage CurrentLanguage { get; private set; } = GameLanguage.Turkish;
    public event Action OnLanguageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        LoadLanguage(CurrentLanguage);
    }

    public void SetLanguage(GameLanguage language)
    {
        if (CurrentLanguage == language) return;

        CurrentLanguage = language;
        LoadLanguage(language);

        if (OnLanguageChanged != null) OnLanguageChanged();
    }

    private void LoadLanguage(GameLanguage language)
    {
        table.Clear();

        TextAsset file = FindFile(language);
        if (file == null)
        {
            DevLog.Warning("[Localization] Dil dosyasi bulunamadi: " + language);
            return;
        }

        string[] lines = file.text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;

            int sep = line.IndexOf(';');
            if (sep < 0) continue;

            string key = line.Substring(0, sep).Trim();
            string value = line.Substring(sep + 1).Trim();

            if (!table.ContainsKey(key)) table.Add(key, value);
        }
    }

    private TextAsset FindFile(GameLanguage language)
    {
        for (int i = 0; i < languages.Count; i++)
            if (languages[i].language == language) return languages[i].csvFile;

        return null;
    }

    /// <summary>Anahtardan metin getirir. Bulunamazsa anahtari doner (hata gizlenmez).</summary>
    public string Get(string key)
    {
        string value;
        if (table.TryGetValue(key, out value)) return value;

        return key;
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        SetLanguage((GameLanguage)data.language);
    }

    public void WriteToSave(SaveData data)
    {
        data.language = (int)CurrentLanguage;
    }
}
