// StoryManager.cs — Hikaye olaylarinin merkezi yoneticisi (diyalog + cutscene)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;
using System.Collections.Generic;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    [System.Serializable]
    public class DailyTalkBlock
    {
        [Tooltip("Sadece Inspector'da hangi araligi kapsadigini hatirlamak icin — kodu etkilemez.")]
        public string aciklama = "Gun 1-10";

        [TextArea(2, 4)]
        public string[] satirlar;
    }

    [Header("Gunluk NPC (Afor) — ozel gun yoksa bu calisir")]
    [SerializeField] private GameObject storyNPCPrefab;
    [SerializeField] private Transform doorPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private string dailyNPCName = "Afor";

    [Tooltip("100 gun 10'ar gunluk 10 boluma ayrilir. Element 0 = gun 1-10, Element 1 = gun 11-20, ... Element 9 = gun 91-100. Bir blok bos birakilirsa o araliktaki gunlerde asagidaki 'Genel Yedek Sohbet' kullanilir.")]
    [SerializeField] private DailyTalkBlock[] gunlukSohbetBloklari = new DailyTalkBlock[10];

    [TextArea(2, 4)]
    [Tooltip("Herhangi bir blok icin satir girilmemisse kullanilan genel yedek liste.")]
    [SerializeField] private string[] dailySmallTalk;

    [Header("Diyalog Olaylari (kamera NPC'ye odaklanir)")]
    [Tooltip("Belirli gunlerde tetiklenecek ozel diyaloglar. Gunluk Afor sohbetinin YERINE gecer.")]
    [SerializeField] private List<StoryDialogueData> dialogueEvents = new List<StoryDialogueData>();

    [Header("Cutscene Olaylari (video + anlatim)")]
    [Tooltip("Gece baslamadan hemen once (oyuncu 'Geceye Gec' basinca) tetiklenir.")]
    [SerializeField] private List<StoryCutsceneData> cutsceneEvents = new List<StoryCutsceneData>();

    private readonly HashSet<string> triggeredEventIds = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GameManager.Instance.OnDayChanged += HandleDayChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnDayChanged -= HandleDayChanged;
    }

    // ────────────────────────────────────────────
    // GUN BASLARKEN — diyalog (Afor ozel gun ya da gunluk sohbet)
    // ────────────────────────────────────────────

    private void HandleDayChanged(int day)
    {
        StoryDialogueData special = FindDialogueForDay(day);

        if (special != null)
        {
            triggeredEventIds.Add(special.eventId);
            SpawnStoryNPC(special);
        }
        else
        {
            SpawnDailyNPC();
        }
    }

    private StoryDialogueData FindDialogueForDay(int day)
    {
        for (int i = 0; i < dialogueEvents.Count; i++)
        {
            StoryDialogueData d = dialogueEvents[i];
            if (d != null && d.triggerDay == day && !triggeredEventIds.Contains(d.eventId))
                return d;
        }
        return null;
    }

    private void SpawnStoryNPC(StoryDialogueData data)
    {
        if (doorPoint == null) return;

        GameObject prefab = data.npcPrefabOverride != null ? data.npcPrefabOverride : storyNPCPrefab;
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, doorPoint.position, Quaternion.identity);
        StoryNPCController npc = go.GetComponent<StoryNPCController>();

        bool willBuy = !data.isFinale && UnityEngine.Random.value < 0.5f;
        npc.InitializeStory(data, doorPoint, counterPoint, willBuy);
    }

    private void SpawnDailyNPC()
    {
        if (storyNPCPrefab == null || doorPoint == null) return;

        GameObject go = Instantiate(storyNPCPrefab, doorPoint.position, Quaternion.identity);
        StoryNPCController npc = go.GetComponent<StoryNPCController>();

        string[] lines = GetDailyTalkForCurrentDay();

        bool willBuy = UnityEngine.Random.value < 0.5f;
        npc.InitializeDaily(dailyNPCName, lines, doorPoint, counterPoint, willBuy);
    }

    /// <summary>100 gunu 10'ar gunluk bloklara boler (gun 1-10 -> blok 0, 11-20 -> blok 1, ...)
    /// ve o bloktaki satirlari doner. Blok bossa genel yedek listeye duser.</summary>
    /// <summary>Disaridan erisim icin — Afor bahce masasinda tekrar konusulunca da ayni
    /// gunun blok satirlarini kullanabilsin diye (GetDailyTalkForCurrentDay ozel'di).</summary>
    public string[] GetTodayDailyLines()
    {
        return GetDailyTalkForCurrentDay();
    }

    private string[] GetDailyTalkForCurrentDay()
    {
        int day = GameManager.Instance.CurrentDay;
        int blokIndex = (day - 1) / 10; // gun 1-10 -> 0, gun 11-20 -> 1, ... gun 91-100 -> 9

        if (gunlukSohbetBloklari != null && blokIndex >= 0 && blokIndex < gunlukSohbetBloklari.Length)
        {
            DailyTalkBlock blok = gunlukSohbetBloklari[blokIndex];
            if (blok != null && blok.satirlar != null && blok.satirlar.Length > 0)
                return blok.satirlar;
        }

        return dailySmallTalk; // ilgili blok doldurulmamissa genel yedek
    }

    /// <summary>Finale diyalogu bitince StoryNPCController cagirir.</summary>
    public void OnFinaleCompleted()
    {
        GameManager.Instance.TriggerStoryEnding();
    }

    // ────────────────────────────────────────────
    // GECE BASLAMADAN ONCE — cutscene (GameManager.RequestGoToNight cagirir)
    // ────────────────────────────────────────────

    /// <summary>Bugun icin oynatilmamis bir cutscene var mi? GameManager gece gecisinden once sorar.</summary>
    public StoryCutsceneData FindCutsceneForDay(int day)
    {
        for (int i = 0; i < cutsceneEvents.Count; i++)
        {
            StoryCutsceneData c = cutsceneEvents[i];
            if (c != null && c.triggerDay == day && !triggeredEventIds.Contains(c.eventId))
                return c;
        }
        return null;
    }

    /// <summary>GameManager cagirir. Cutscene bitince onComplete calisir.</summary>
    public void PlayCutscene(StoryCutsceneData data, Action onComplete)
    {
        triggeredEventIds.Add(data.eventId);

        if (CutscenePlayer.Instance == null)
        {
            if (onComplete != null) onComplete();
            return;
        }

        CutscenePlayer.Instance.Play(data, onComplete);
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        triggeredEventIds.Clear();
        if (data.triggeredStoryEventIds != null)
            for (int i = 0; i < data.triggeredStoryEventIds.Count; i++)
                triggeredEventIds.Add(data.triggeredStoryEventIds[i]);
    }

    public void WriteToSave(SaveData data)
    {
        data.triggeredStoryEventIds = new List<string>(triggeredEventIds);
    }
}