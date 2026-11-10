// DialogueBox.cs — Evrensel diyalog kutusu (tum NPC'ler kullanir)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Madness.Mods;

[Serializable]
public struct DialogueLine
{
    public string speakerName;
    [TextArea(2, 4)] public string text;

    [Header("Yerelleştirme (opsiyonel — bos birakilirsa hep Turkce/'text' gosterilir)")]
    [Tooltip("Turkce disinda baska dillerde bu satirin karsiligi. Bos birakilabilir (o zaman o dilde de Turkce metin gosterilir).")]
    public List<DilMetniGirisi> ekDiller;

    [Header("Seslendirme (opsiyonel — bos birakilirsa varsayilan 'blabla' sesi calar)")]
    [Tooltip("Bu satira ozel gercek seslendirme klibi (Turkce/Ingilizce). Bos birakilabilir.")]
    public SesKlipSeti ses;
    [Tooltip("Klip atanmamissa dogru cinsiyette 'blabla' sesi calinabilmesi icin.")]
    public Cinsiyet konusanCinsiyeti;
    [Tooltip("Klip atanmamissa dogru YAS GRUBUNDA 'blabla' sesi calinabilmesi icin.")]
    public YasGrubu konusanYasGrubu;
}

public class DialogueBox : MonoBehaviour
{
    public static DialogueBox Instance { get; private set; }

    [Header("Referanslar")]
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI speakerLabel;
    [SerializeField] private TextMeshProUGUI bodyLabel;
    [SerializeField] private Button closeButton;

    [Header("Seçenek Butonları (Bahçe sohbeti gibi çok-seçenekli konuşmalar için — opsiyonel)")]
    [Tooltip("Secenekler gosterilirken aktif olan container. Kullanilmiyorsa bos birakilabilir.")]
    [SerializeField] private GameObject secenekPaneli;
    [Tooltip("Sabit sayida buton (onerilen: 4). Kullanilmayanlar otomatik gizlenir.")]
    [SerializeField] private Button[] secenekButonlari;
    [SerializeField] private TextMeshProUGUI[] secenekYazilari;

    private Action<int> secimCagrisi;

    public bool SecenekModundaMi { get { return secenekPaneli != null && secenekPaneli.activeSelf; } }

    public bool IsOpen { get { return root != null && root.activeSelf; } }

    /// <summary>Panel herhangi bir sekilde kapandiginda tetiklenir (Close() veya ForceClose()).
    /// PlayerInteraction bunu dinleyip "su an kiminle konusuyordum" bilgisini temizler.</summary>
    public event Action OnClosed;

    private readonly Queue<DialogueLine> queue = new Queue<DialogueLine>();
    private Action onFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        // KORUMALI kayit: baska bir panel (Market, Katalog, Basarimlar vs.) acilirken
        // DialogueBox ASLA otomatik kapatilmaz — sadece kendi Close()'u kapatabilir.
        // Diyalog gorunurken oyuncu baska bir panel actiginda konusma kaybolmasin diye.
        if (PanelYoneticisi.Instance != null && root != null)
            PanelYoneticisi.Instance.Kaydet(root, korumali: true);
    }

    /// <summary>SARHOŞ KİTAP (çılgın özellik) aktifse metnin harflerini rastgele karıştırır —
    /// boşluklar yerinde durur, sadece harfler yer değiştirir. Efekt yoksa metin AYNEN döner.</summary>
    private string EfektUygula(string metin)
    {
        if (string.IsNullOrEmpty(metin)) return metin;
        if (SarhosKitapEfekti.Instance == null || !SarhosKitapEfekti.Instance.EfektAktifMi) return metin;

        char[] harfler = metin.ToCharArray();
        for (int i = 0; i < harfler.Length; i++)
        {
            if (harfler[i] == ' ' || harfler[i] == '\n') continue;

            int j = UnityEngine.Random.Range(0, harfler.Length);
            if (harfler[j] == ' ' || harfler[j] == '\n') continue;

            char gecici = harfler[i];
            harfler[i] = harfler[j];
            harfler[j] = gecici;
        }
        return new string(harfler);
    }

    /// <summary>GENEL AMAÇLI — sadece bahçe sohbeti icin degil, HERHANGI bir çok-seçenekli
    /// konuşma için kullanılabilir (ileride başka sistemler de kullanabilir). Bu metod
    /// hiçbir bahçeye-özgü kavram bilmez — sadece metin + seçenek listesi + callback alır.
    /// NPC bir cumle soyler, oyuncuya SECENEKLER sunulur — bir tanesine tiklaninca
    /// secilince cagrisi tetiklenir (index ile). secenekMetinleri BOSSA, secenek paneli hic
    /// gorunmez (normal "Kapat" ile biten bir konusma gibi davranir).</summary>
    public void SecenekliGoster(string konusanIsim, string npcMetni, List<string> secenekMetinleri, Action<int> secilince)
    {
        // SESLENDIRME parametresi verilmemis eski cagrilar icin: varsayilan (bos ses +
        // Erkek + Yetiskin) ile devam eder, yani ozel klip atanmamissa erkek-yetiskin
        // "blabla" sesi calar.
        SecenekliGoster(konusanIsim, npcMetni, secenekMetinleri, secilince, default(SesKlipSeti), Cinsiyet.Erkek, YasGrubu.Yetiskin);
    }

    /// <summary>SESLENDIRME destekli tam surum. Cagiran taraf elindeyse konusan NPC'nin
    /// gercek cinsiyet+yas grubunu (dogru "blabla" havuzu icin) ve varsa o satira ozel
    /// SesKlipSeti'ni verebilir. Vermek istemeyen eski kod, yukaridaki 4-parametreli kisa
    /// surumu kullanmaya devam edebilir (otomatik olarak bu metoda yonlenir).</summary>
    public void SecenekliGoster(string konusanIsim, string npcMetni, List<string> secenekMetinleri, Action<int> secilince, SesKlipSeti ses, Cinsiyet cinsiyet, YasGrubu yasGrubu)
    {
        queue.Clear();
        onFinished = null;

        if (root != null) root.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        if (speakerLabel != null) speakerLabel.text = konusanIsim;
        if (bodyLabel != null) bodyLabel.text = EfektUygula(npcMetni);

        if (SeslendirmeYoneticisi.Instance != null)
            SeslendirmeYoneticisi.Instance.SatirSesiniCal(ses, cinsiyet, yasGrubu);

        secimCagrisi = secilince;

        bool secenekVarMi = secenekMetinleri != null && secenekMetinleri.Count > 0;
        if (secenekPaneli != null) secenekPaneli.SetActive(secenekVarMi);

        if (secenekButonlari != null)
        {
            for (int i = 0; i < secenekButonlari.Length; i++)
            {
                if (secenekButonlari[i] == null) continue;

                bool gosterilsin = secenekVarMi && i < secenekMetinleri.Count;
                secenekButonlari[i].gameObject.SetActive(gosterilsin);
                secenekButonlari[i].onClick.RemoveAllListeners();

                if (gosterilsin)
                {
                    if (secenekYazilari != null && i < secenekYazilari.Length && secenekYazilari[i] != null)
                        secenekYazilari[i].text = secenekMetinleri[i];

                    int index = i; // kapali degisken yakalamasi icin yerel kopya
                    secenekButonlari[i].onClick.AddListener(() => SecenekSecildi(index));
                }
            }
        }
    }

    private void SecenekSecildi(int index)
    {
        if (secenekPaneli != null) secenekPaneli.SetActive(false);

        Action<int> cb = secimCagrisi;
        secimCagrisi = null;

        if (cb != null) cb(index);
    }

    /// <summary>Basit gosterim — bitince bir sey yapilmaz.</summary>
    public void Show(List<DialogueLine> lines)
    {
        Show(lines, null, true);
    }

    public void Show(List<DialogueLine> lines, Action finishedCallback)
    {
        Show(lines, finishedCallback, true);
    }

    /// <summary>Yeni bir konusma baslatir/gosterileni degistirir. Panel zaten acik olsa bile
    /// calisir — musteri sohbetinde "farkli laf getir" tam bunu yapar.
    /// kapatilabilir=false ise Kapat (X) butonu bu konusma boyunca GORUNMEZ olur — tutorial
    /// gibi, oyuncunun erken kapatip akisi kirmasi istenmeyen durumlar icin.</summary>
    public void Show(List<DialogueLine> lines, Action finishedCallback, bool kapatilabilir)
    {
        onFinished = finishedCallback;
        queue.Clear();

        if (lines != null)
        {
            string speaker = lines.Count > 0 ? lines[0].speakerName : "";
            lines = ModDialogue.Apply("dialogue.show", speaker, lines);
            for (int i = 0; i < lines.Count; i++) queue.Enqueue(lines[i]);
        }

        if (queue.Count == 0)
        {
            Close();
            return;
        }

        if (root != null) root.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(kapatilabilir);
        DisplayNext();
    }

    /// <summary>Etkilesim butonundan cagrilir: HAZIRDA bekleyen siradaki satiri gosterir
    /// (onceden Show() ile verilmis coklu satirlik bir hikaye diyalogunda kullanilir).
    /// Kuyrukta baska satir yoksa HICBIR SEY YAPMAZ — kapatmaz. Kapatma SADECE Close() ile olur.</summary>
    /// <summary>Kuyrukta gosterilecek satir kalmadigi halde oyuncu YINE de "ilerlet" (etkilesim
    /// butonu) basarsa tetiklenir. TutorialController gibi coklu-satirlik akislarin, X butonuna
    /// hic gerek kalmadan "oyuncu okudu, devam etmek istiyor" sinyalini almasi icin var.</summary>
    public event Action OnNoMoreLines;

    public void ShowNext()
    {
        if (queue.Count == 0)
        {
            if (OnNoMoreLines != null) OnNoMoreLines();
            return;
        }
        DisplayNext();
    }

    private void DisplayNext()
    {
        DialogueLine line = queue.Dequeue();
        string cozulmusMetin = LokalizasyonYardimcisi.MetniCoz(line.text, line.ekDiller);

        if (speakerLabel != null) speakerLabel.text = line.speakerName;
        if (bodyLabel != null) bodyLabel.text = EfektUygula(cozulmusMetin);

        if (SeslendirmeYoneticisi.Instance != null)
            SeslendirmeYoneticisi.Instance.SatirSesiniCal(line.ses, line.konusanCinsiyeti, line.konusanYasGrubu);
    }

    /// <summary>Panelin kendi Kapat (X) butonundan cagrilir. Konusma GERCEKTEN burada biter —
    /// callback (varsa) burada calisir (orn. NPC gitsin/kitap alsin).</summary>
    public void Close()
    {
        queue.Clear();
        secimCagrisi = null;
        if (secenekPaneli != null) secenekPaneli.SetActive(false);
        if (root != null) root.SetActive(false);

        Action callback = onFinished;
        onFinished = null;

        if (callback != null) callback();
        if (OnClosed != null) OnClosed();
    }

    /// <summary>Geri cagriyi CALISTIRMADAN aninda kapatir. Bir NPC diyalog ortasindayken
    /// zorla ayrilmak zorunda kalirsa (orn. gece basladi) kullanilir — normal akiste kullanma.</summary>
    public void ForceClose()
    {
        queue.Clear();
        onFinished = null;
        secimCagrisi = null;
        if (secenekPaneli != null) secenekPaneli.SetActive(false);
        if (root != null) root.SetActive(false);
        if (OnClosed != null) OnClosed();
    }
}
