// TutorialController.cs — Ilk oynanis rehberi (Patron NPC)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }

    [Header("Patron")]
    [SerializeField] private string patronName = "Patron";
    [Tooltip("SESLENDIRME icin: dogru 'blabla' havuzunun secilmesi icin (varsayilan Erkek+Yetiskin).")]
    [SerializeField] private Cinsiyet patronCinsiyeti = Cinsiyet.Erkek;
    [SerializeField] private YasGrubu patronYasGrubu = YasGrubu.Yetiskin;

    [Header("Hedefler")]
    [SerializeField] private ShelfController exampleShelf;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private float reachDistance = 1.5f;

    [Header("Zaman asimi")]
    [SerializeField] private float stepTimeout = 30f;

    private Transform player;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>GameInitializer tarafindan cagrilir (sadece yeni oyunda).</summary>
    public void BeginTutorial()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        StartCoroutine(RunTutorial());
    }

    private IEnumerator RunTutorial()
    {
        GameManager.Instance.SetPhase(GamePhase.Tutorial);

        // 1) Karsilama
        yield return Say(
            "Hos geldin. Burasi artik senin kutuphanen.",
            "Gunduzleri kitap satacaksin, geceleri ise... hayatta kalacaksin.");

        // 2) Rafa git
        yield return Say("Once su rafa bir bak. Parlayani goruyorsun, degil mi?");

        if (exampleShelf != null) exampleShelf.SetGlow(true);
        yield return WaitUntilNear(exampleShelf != null ? exampleShelf.transform : null);
        if (exampleShelf != null) exampleShelf.SetGlow(false);

        // 3) Kasa
        yield return Say(
            "Guzel. Musteriler kitabi kendileri alir, sana kasaya gelir.",
            "Sen sadece etkilesim tusuna basip satisi onaylarsin.");

        yield return WaitUntilNear(counterPoint);

        // 4) Market ve silah
        yield return Say(
            "Sag ustteki menuden Market'e girebilirsin.",
            "Orada stok, silah ve yukseltme satin alirsin.",
            "UNUTMA: gece baslamadan once elinde en az bir silah olmali!");

        // 5) Bitir
        yield return Say("Hazirsan, ilk gunun basliyor. Iyi sanslar.");

        CompleteTutorial();
    }

    private IEnumerator Say(params string[] texts)
    {
        List<DialogueLine> lines = new List<DialogueLine>();

        for (int i = 0; i < texts.Length; i++)
        {
            DialogueLine line = new DialogueLine();
            line.speakerName = patronName;
            line.text = texts[i];
            // SESLENDIRME: bu satirlar kod icinde olusturuluyor (Inspector'da DialogueLine
            // dizisi degil), o yuzden cinsiyet/yas grubu ELLE atanmali — atanmazsa struct'in
            // varsayilani (Kadin+Cocuk) kullanilir, ki bu Patron icin YANLIS olurdu.
            line.konusanCinsiyeti = patronCinsiyeti;
            line.konusanYasGrubu = patronYasGrubu;
            lines.Add(line);
        }

        // NOT: Artik "Kapat (X)" butonunu BEKLEMIYORUZ — oyuncu tanidigi tek butonla
        // (etkilesim/ilerlet) tum satirlari gecince OnNoMoreLines tetiklenir. Boylece
        // tutorial, oyuncu henuz X butonunu kesfetmeden de duzgun ilerler.
        bool tamamlandi = false;
        Action tamamlaHandler = () => tamamlandi = true;

        DialogueBox.Instance.OnNoMoreLines += tamamlaHandler;
        DialogueBox.Instance.Show(lines, null, false); // false = Kapat (X) butonu tutorial boyunca gizli

        while (!tamamlandi) yield return null;

        DialogueBox.Instance.OnNoMoreLines -= tamamlaHandler;
        DialogueBox.Instance.Close();
    }

    private IEnumerator WaitUntilNear(Transform target)
    {
        if (target == null || player == null) yield break;

        float elapsed = 0f;
        while (elapsed < stepTimeout)
        {
            if (Vector2.Distance(player.position, target.position) <= reachDistance) yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
        // Zaman asimi — oyuncuyu bekletmeye devam etme, sonraki adima gec
    }

    private void CompleteTutorial()
    {
        SaveSystem.Instance.Current.tutorialCompleted = true;
        GameManager.Instance.StartFirstDay();
    }
}
