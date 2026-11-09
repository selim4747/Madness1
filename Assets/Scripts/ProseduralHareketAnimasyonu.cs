// ProseduralHareketAnimasyonu.cs — GERCEK yuruyus dongusu (walk cycle) uretir.
//
// FELSEFE: Tek bir sprite'i esnetmek "cizgi film" hissi verir. Gercek yuruyus, UZUVLARIN
// birbirine gore ZIT FAZDA salinmasidir (sag bacak onde iken sol kol onde). Bu script
// klasik animasyon ders kitabindaki 4 anahtar pozu (Contact / Down / Passing / Up)
// matematiksel olarak uretir:
//   - Bacaklar: 180 derece zit fazda salinir (biri onde, digeri arkada)
//   - Kollar: bacaklarin TERSI fazda (dogal denge — insan boyle yurur)
//   - Govde: bacak frekansinin 2 KATI hizda hafifce alcalip yukselir (her adimda bir dip)
//   - Bas: govdenin tersine cok hafif sallanir (atalet/yumusama hissi)
//
// KADEMELI KULLANIM:
//   * Hicbir uzuv baglamazsan  -> sadece ince govde salinimi + nefes (sprite tek parcaysa)
//   * Bacaklari baglarsan      -> gercek adim atma
//   * Kollari da baglarsan     -> tam, dogal yuruyus dongusu
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections;

public class ProseduralHareketAnimasyonu : MonoBehaviour
{
    [Header("Govde (ZORUNLU)")]
    [Tooltip("Karakterin ana govde gorselini tasiyan Transform. Rigidbody2D/NavMeshAgent'in oldugu objeden AYRI bir cocuk olmali.")]
    [SerializeField] private Transform govde;

    [Header("Uzuvlar (OPSIYONEL — bagladiklarin animasyona katilir)")]
    [Tooltip("Sol bacak/ayak objesi. Baglanirsa gercek adim atma baslar.")]
    [SerializeField] private Transform solBacak;
    [SerializeField] private Transform sagBacak;
    [Tooltip("Kollar baglanirsa bacaklarin TERSI yonde salinir — dogal yuruyus dengesi.")]
    [SerializeField] private Transform solKol;
    [SerializeField] private Transform sagKol;
    [Tooltip("Bas baglanirsa govdenin tersine cok hafif sallanir (atalet hissi).")]
    [SerializeField] private Transform bas;

    [Header("PIKSEL SANATI MODU (bu oyunun tarzi icin ONEMLI)")]
    [Tooltip("ISARETLI: animasyon PURUZSUZ akmaz, gercek pikselli oyunlar gibi belirli sayida AYRIK POZ arasinda SICRAR. Pikselli bir oyunda bu sart — kapali birakirsan 3D oyun gibi kaygan gorunur.")]
    [SerializeField] private bool pikselModu = true;

    [Tooltip("Yuruyus dongusundeki toplam kare (poz) sayisi. Klasik pikselli oyunlar 4 ya da 6 kullanir. 4 = daha retro/sert, 8 = daha yumusak ama hala ayrik.")]
    [Range(2, 12)]
    [SerializeField] private int yuruyusKareSayisi = 4;

    [Tooltip("Birim basina piksel (Pixels Per Unit) — sprite'larinin import ayarindaki degerle AYNI olmali. Uzuv konumlari bu izgaraya oturtulur, boylece piksel kaymasi/bulanikligi olmaz.")]
    [SerializeField] private float pixelsPerUnit = 16f;

    [Header("Yuruyus Ritmi")]
    [Tooltip("Saniyede kac adim atilacagi (hiza gore otomatik olceklenir). 2 = normal insan tempolu yuruyus.")]
    [SerializeField] private float adimFrekansi = 2f;
    [Tooltip("Karakterin hizi arttikca adim frekansi da artsin mi (kosarken daha sik adim).")]
    [SerializeField] private bool hizaGoreOlcekle = true;

    [Header("Bacak Hareketi")]
    [Tooltip("Bacaklarin ileri-geri salinim mesafesi (birim).")]
    [SerializeField] private float bacakSalinimMesafesi = 0.12f;
    [Tooltip("Bacaklarin adim sirasinda ne kadar yukari kalkacagi.")]
    [SerializeField] private float bacakKaldirmaYuksekligi = 0.05f;
    [Tooltip("Bacaklarin salinirken ne kadar donecegi (derece).")]
    [SerializeField] private float bacakDonmeAcisi = 12f;

    [Header("Kol Hareketi")]
    [SerializeField] private float kolSalinimMesafesi = 0.09f;
    [SerializeField] private float kolDonmeAcisi = 15f;

    [Header("Govde Hareketi")]
    [Tooltip("Govdenin her adimda ne kadar alcalip yukselecegi. Bacak frekansinin 2 KATI hizda calisir (her adimda bir dip).")]
    [SerializeField] private float govdeAlcalmaMiktari = 0.025f;
    [Tooltip("Govdenin yana dogru cok hafif sallanmasi (agirlik aktarimi hissi).")]
    [SerializeField] private float govdeYanSalinimi = 0.012f;
    [Tooltip("Yuruyus yonune gore govdenin one dogru cok hafif egilmesi (derece).")]
    [SerializeField] private float govdeEgilmeAcisi = 2.5f;

    [Header("Bas Hareketi")]
    [SerializeField] private float basSalinimMiktari = 0.008f;

    [Header("Bos Durunca (Idle)")]
    [SerializeField] private float idleNefesGenligi = 0.012f;
    [SerializeField] private float idleNefesHizi = 1.8f;
    [Tooltip("Uzuvlarin idle'da baslangic pozuna ne kadar hizli donecegi.")]
    [SerializeField] private float idleGecisHizi = 8f;

    [Header("Karakter Agirligi")]
    [Tooltip("Kucuk = hafif/cevik (hizli, kucuk adimlar). Buyuk = agir (yavas, genis adimlar). 1 = normal.")]
    [SerializeField] private float agirlik = 1f;

    [Header("Tetiklenebilir Animasyonlar")]
    [SerializeField] private float hasarTepkiSuresi = 0.18f;
    [SerializeField] private float hasarSikismaMiktari = 0.3f;
    [SerializeField] private float saldiriHazirlikSuresi = 0.12f;
    [SerializeField] private float olumAnimasyonuSuresi = 0.4f;

    // ── Uzuvlarin baslangic durumlari ──
    private struct UzuvDurumu
    {
        public Transform t;
        public Vector3 baslangicKonum;
        public Quaternion baslangicRotasyon;
        public bool gecerli;
    }

    private UzuvDurumu govdeD, solBacakD, sagBacakD, solKolD, sagKolD, basD;

    private Vector3 govdeBaslangicScale;
    private float dongufazi;      // yuruyus dongusunun mevcut fazi (0..2PI), surekli ilerler
    private float rastgeleFaz;     // her karakter farkli anda baslasin

    private UnityEngine.AI.NavMeshAgent agent;
    private Rigidbody2D rb;

    private Vector3 tetiklenenScaleOfseti = Vector3.one;
    private Coroutine tetiklenenRoutine;
    private bool olduMu = false;
    private bool devreDisi = false;
    private bool govdePozisyonGuvenli = true;

    private void Awake()
    {
        agent = GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
        rb = GetComponentInParent<Rigidbody2D>();

        if (govde == null)
        {
            devreDisi = true;
            DevLog.Warning("[ProseduralHareketAnimasyonu] '" + gameObject.name + "' icin 'Govde' alani BOS — animasyon calismiyor.");
            return;
        }

        govdeD = UzuvKaydet(govde);
        solBacakD = UzuvKaydet(solBacak);
        sagBacakD = UzuvKaydet(sagBacak);
        solKolD = UzuvKaydet(solKol);
        sagKolD = UzuvKaydet(sagKol);
        basD = UzuvKaydet(bas);

        govdeBaslangicScale = govde.localScale;

        // Govde, fizigi tasiyan objenin KENDISI ise pozisyonuna dokunmayiz (cakisma olmasin).
        bool govdeAyniObjede = (rb != null && govde == rb.transform) ||
                                (agent != null && govde == agent.transform);
        govdePozisyonGuvenli = !govdeAyniObjede;

        rastgeleFaz = Random.Range(0f, Mathf.PI * 2f);
        dongufazi = rastgeleFaz;
    }

    private UzuvDurumu UzuvKaydet(Transform t)
    {
        UzuvDurumu d = new UzuvDurumu();
        d.t = t;
        d.gecerli = (t != null);
        if (d.gecerli)
        {
            d.baslangicKonum = t.localPosition;
            d.baslangicRotasyon = t.localRotation;
        }
        return d;
    }

    private float GuvenliAgirlik() { return Mathf.Max(0.2f, agirlik); }

    /// <summary>PIKSEL MODU'nun kalbi: surekli akan fazi, belirli sayida AYRIK kareye yuvarlar.
    /// Boylece animasyon puruzsuz kaymaz, gercek pikselli oyunlar gibi pozdan poza SICRAR.</summary>
    private float FaziKuantala(float faz)
    {
        if (!pikselModu) return faz;

        float tamDongu = Mathf.PI * 2f;
        float kareBoyu = tamDongu / yuruyusKareSayisi;
        return Mathf.Floor(faz / kareBoyu) * kareBoyu;
    }

    /// <summary>Konumu piksel izgarasina oturtur — piksel sanatinda alt-piksel kaymasi
    /// bulanikliga/titremeye yol acar, bu onu engeller.</summary>
    private Vector3 PikseleOturt(Vector3 konum)
    {
        if (!pikselModu || pixelsPerUnit <= 0f) return konum;

        return new Vector3(
            Mathf.Round(konum.x * pixelsPerUnit) / pixelsPerUnit,
            Mathf.Round(konum.y * pixelsPerUnit) / pixelsPerUnit,
            konum.z
        );
    }

    private Vector2 MevcutHiziAl()
    {
        if (agent != null) return agent.velocity;
        if (rb != null) return rb.linearVelocity;
        return Vector2.zero;
    }

    private void Update()
    {
        if (olduMu || devreDisi) return;

        Vector2 hiz = MevcutHiziAl();
        if (float.IsNaN(hiz.x) || float.IsNaN(hiz.y) || float.IsInfinity(hiz.x) || float.IsInfinity(hiz.y))
            return;

        float agirlikF = GuvenliAgirlik();
        float hizBuyuklugu = hiz.magnitude;
        bool yuruyorMu = hizBuyuklugu > 0.05f;

        if (yuruyorMu)
        {
            // Dongu fazini ilerlet. Agir karakter daha yavas, hizli giden daha sik adim atar.
            float frekans = adimFrekansi / agirlikF;
            if (hizaGoreOlcekle) frekans *= Mathf.Clamp(hizBuyuklugu / 2f, 0.6f, 2f);

            dongufazi += Time.deltaTime * frekans * Mathf.PI * 2f;
            if (dongufazi > Mathf.PI * 2f) dongufazi -= Mathf.PI * 2f;

            YuruyusUygula(hiz, agirlikF);
        }
        else
        {
            IdleUygula(agirlikF);
        }
    }

    /// <summary>Klasik yuruyus dongusunun tum uzuvlara matematiksel uygulanmasi.</summary>
    private void YuruyusUygula(Vector2 hiz, float agirlikF)
    {
        // PIKSEL MODU: surekli faz yerine KUANTALANMIS faz kullanilir — animasyon ayrik
        // pozlar arasinda sicrar (gercek pikselli oyunlardaki gibi).
        float faz = FaziKuantala(dongufazi);

        // ── BACAKLAR: birbirine gore 180 derece (PI) zit fazda ──
        // sin(faz) ileri-geri salinim; bacagin havadayken hafif kalkmasi icin
        // |cos| ile ikinci bir bilesen kullanilir (adimin ortasinda en yuksek).
        float bacakOlcek = bacakSalinimMesafesi * agirlikF; // agir karakter daha genis adim atar

        UzuvSalinimUygula(solBacakD, faz, bacakOlcek, bacakKaldirmaYuksekligi, bacakDonmeAcisi, hiz.x);
        UzuvSalinimUygula(sagBacakD, faz + Mathf.PI, bacakOlcek, bacakKaldirmaYuksekligi, bacakDonmeAcisi, hiz.x);

        // ── KOLLAR: bacaklarin TERSI fazda (sol bacak onde -> sag kol onde) ──
        UzuvSalinimUygula(solKolD, faz + Mathf.PI, kolSalinimMesafesi, 0f, kolDonmeAcisi, hiz.x);
        UzuvSalinimUygula(sagKolD, faz, kolSalinimMesafesi, 0f, kolDonmeAcisi, hiz.x);

        // ── GOVDE: bacak frekansinin 2 KATI hizda alcalip yukselir ──
        // (her adimda bir dip — iki bacak da bir dongude birer kez yere basar)
        if (govdeD.gecerli)
        {
            float govdeAlcalma = -Mathf.Abs(Mathf.Sin(faz)) * govdeAlcalmaMiktari;
            float govdeYan = Mathf.Sin(faz) * govdeYanSalinimi;

            Vector3 yeniKonum = PikseleOturt(govdeD.baslangicKonum + new Vector3(govdeYan, govdeAlcalma, 0f));

            // Yuruyus yonune gore cok hafif one egilme
            float egilme = Mathf.Clamp(-hiz.x * govdeEgilmeAcisi, -govdeEgilmeAcisi, govdeEgilmeAcisi);
            // Adim ritmiyle senkron cok hafif ek salinim
            egilme += Mathf.Sin(faz) * 0.8f;

            if (GecerliMi(yeniKonum))
            {
                if (govdePozisyonGuvenli) govde.localPosition = yeniKonum;

                // Piksel modunda govde de dondurulmez (yukaridaki ayni sebep).
                if (!pikselModu)
                    govde.localRotation = govdeD.baslangicRotasyon * Quaternion.Euler(0f, 0f, egilme);

                govde.localScale = Vector3.Scale(govdeBaslangicScale, tetiklenenScaleOfseti);
            }
        }

        // ── BAS: govdenin TERSINE cok hafif sallanir (atalet/yumusama) ──
        if (basD.gecerli)
        {
            float basOfseti = -Mathf.Sin(faz) * basSalinimMiktari;
            Vector3 yeniBasKonum = PikseleOturt(basD.baslangicKonum + new Vector3(basOfseti, Mathf.Abs(basOfseti) * 0.5f, 0f));
            if (GecerliMi(yeniBasKonum)) bas.localPosition = yeniBasKonum;
        }
    }

    /// <summary>Tek bir uzva (bacak/kol) salinim uygular. faz kaydirmasiyla zit yon saglanir.</summary>
    private void UzuvSalinimUygula(UzuvDurumu uzuv, float faz, float ileriMesafe,
                                     float kaldirmaYuksekligi, float donmeAcisi, float yatayHiz)
    {
        if (!uzuv.gecerli) return;

        // Ileri-geri salinim (temel hareket)
        float ileriGeri = Mathf.Sin(faz) * ileriMesafe;

        // Kaldirma: bacak GERIDEN ONE giderken (salinimin ilk yarisi) havada olur.
        // Mathf.Max(0, ...) ile sadece o yarida yukari kalkar, digerinde yerde kalir.
        float kaldirma = kaldirmaYuksekligi > 0f
            ? Mathf.Max(0f, Mathf.Sin(faz)) * kaldirmaYuksekligi
            : 0f;

        Vector3 yeniKonum = PikseleOturt(uzuv.baslangicKonum + new Vector3(ileriGeri, kaldirma, 0f));

        // Salinima esli hafif donme (uzvun dogal acisi)
        float aci = Mathf.Sin(faz) * donmeAcisi;

        if (GecerliMi(yeniKonum))
        {
            uzuv.t.localPosition = yeniKonum;

            // PIKSEL MODU: sprite dondurmek piksel sanatinda bulaniklik/bozulma yapar
            // (piksel izgarasi bozulur), o yuzden pikselli oyunlar uzuv DONDURMEZ —
            // sadece konum degistirir. Kapaliyken normal donme uygulanir.
            if (!pikselModu)
                uzuv.t.localRotation = uzuv.baslangicRotasyon * Quaternion.Euler(0f, 0f, aci);
        }
    }

    /// <summary>Dururken: tum uzuvlar yumusakca baslangic pozuna doner, govde nefes alir.</summary>
    private void IdleUygula(float agirlikF)
    {
        float lerpHizi = Time.deltaTime * idleGecisHizi;

        UzuvIdleyeDon(solBacakD, lerpHizi);
        UzuvIdleyeDon(sagBacakD, lerpHizi);
        UzuvIdleyeDon(solKolD, lerpHizi);
        UzuvIdleyeDon(sagKolD, lerpHizi);
        UzuvIdleyeDon(basD, lerpHizi);

        if (govdeD.gecerli)
        {
            // Yavas, hafif nefes alma — sadece dikey olcek, konum degismez
            float nefes = Mathf.Sin(Time.time * idleNefesHizi / agirlikF + rastgeleFaz) * idleNefesGenligi;
            Vector3 nefesScale = new Vector3(1f - nefes * 0.5f, 1f + nefes, 1f);
            Vector3 hedefScale = Vector3.Scale(Vector3.Scale(govdeBaslangicScale, nefesScale), tetiklenenScaleOfseti);

            if (GecerliMi(hedefScale)) govde.localScale = hedefScale;

            if (govdePozisyonGuvenli)
                govde.localPosition = Vector3.Lerp(govde.localPosition, govdeD.baslangicKonum, lerpHizi);

            govde.localRotation = Quaternion.Lerp(govde.localRotation, govdeD.baslangicRotasyon, lerpHizi);
        }
    }

    private void UzuvIdleyeDon(UzuvDurumu uzuv, float lerpHizi)
    {
        if (!uzuv.gecerli) return;
        uzuv.t.localPosition = Vector3.Lerp(uzuv.t.localPosition, uzuv.baslangicKonum, lerpHizi);
        uzuv.t.localRotation = Quaternion.Lerp(uzuv.t.localRotation, uzuv.baslangicRotasyon, lerpHizi);
    }

    private static bool GecerliMi(Vector3 v)
    {
        return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
               !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }

    // ==========================================
    // TETIKLENEBILIR ANIMASYONLAR
    // ==========================================

    public void HasarTepkisiOynat()
    {
        if (olduMu || devreDisi) return;
        if (tetiklenenRoutine != null) StopCoroutine(tetiklenenRoutine);
        tetiklenenRoutine = StartCoroutine(HasarTepkisiRoutine());
    }

    private IEnumerator HasarTepkisiRoutine()
    {
        float t = 0f;
        while (t < hasarTepkiSuresi)
        {
            t += Time.deltaTime;
            float p = t / hasarTepkiSuresi;
            float sikisma = (1f - p) * hasarSikismaMiktari * Mathf.Sin(p * Mathf.PI);
            tetiklenenScaleOfseti = new Vector3(1f + sikisma, 1f - sikisma, 1f);
            yield return null;
        }
        tetiklenenScaleOfseti = Vector3.one;
    }

    public void SaldiriHazirligiOynat()
    {
        if (olduMu || devreDisi) return;
        if (tetiklenenRoutine != null) StopCoroutine(tetiklenenRoutine);
        tetiklenenRoutine = StartCoroutine(SaldiriHazirligiRoutine());
    }

    private IEnumerator SaldiriHazirligiRoutine()
    {
        float t = 0f;
        while (t < saldiriHazirlikSuresi)
        {
            t += Time.deltaTime;
            float p = t / saldiriHazirlikSuresi;
            float cekilme = Mathf.Sin(p * Mathf.PI) * 0.15f;
            tetiklenenScaleOfseti = new Vector3(1f - cekilme, 1f + cekilme, 1f);
            yield return null;
        }
        tetiklenenScaleOfseti = Vector3.one;
    }

    public void OlumAnimasyonuOynat(System.Action tamamlaninca = null)
    {
        if (devreDisi) { tamamlaninca?.Invoke(); return; }
        olduMu = true;
        if (tetiklenenRoutine != null) StopCoroutine(tetiklenenRoutine);
        tetiklenenRoutine = StartCoroutine(OlumAnimasyonuRoutine(tamamlaninca));
    }

    private IEnumerator OlumAnimasyonuRoutine(System.Action tamamlaninca)
    {
        Vector3 baslangic = govde.localScale;
        float t = 0f;

        float sismeSuresi = olumAnimasyonuSuresi * 0.25f;
        while (t < sismeSuresi)
        {
            t += Time.deltaTime;
            govde.localScale = Vector3.Lerp(baslangic, baslangic * 1.15f, t / sismeSuresi);
            yield return null;
        }

        float collapseSuresi = olumAnimasyonuSuresi * 0.75f;
        Vector3 sismisScale = baslangic * 1.15f;
        t = 0f;
        while (t < collapseSuresi)
        {
            t += Time.deltaTime;
            float p = t / collapseSuresi;
            govde.localScale = Vector3.Lerp(sismisScale, Vector3.zero, p * p);
            yield return null;
        }

        govde.localScale = Vector3.zero;
        tamamlaninca?.Invoke();
    }

    public void Sifirla()
    {
        if (devreDisi) return;
        olduMu = false;
        tetiklenenScaleOfseti = Vector3.one;
        dongufazi = rastgeleFaz;

        UzuvSifirla(govdeD); UzuvSifirla(solBacakD); UzuvSifirla(sagBacakD);
        UzuvSifirla(solKolD); UzuvSifirla(sagKolD); UzuvSifirla(basD);

        if (govdeD.gecerli) govde.localScale = govdeBaslangicScale;
    }

    private void UzuvSifirla(UzuvDurumu uzuv)
    {
        if (!uzuv.gecerli) return;
        uzuv.t.localPosition = uzuv.baslangicKonum;
        uzuv.t.localRotation = uzuv.baslangicRotasyon;
    }

    // ==========================================
    // EDITOR GIZMO — uzuvlarin nerede oldugunu ve ne kadar salinacagini Scene'de gosterir
    // ==========================================
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Govde: sari kure + alcalma araligi
        if (govde != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(govde.position, 0.06f);
            Gizmos.DrawLine(govde.position, govde.position + Vector3.down * govdeAlcalmaMiktari);
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(govde.position + Vector3.up * 0.12f, "Govde");
        }

        // Bacaklar: yesil — ileri-geri salinim araligi cizilir
        UzuvGizmoCiz(solBacak, "Sol Bacak", Color.green, bacakSalinimMesafesi, bacakKaldirmaYuksekligi);
        UzuvGizmoCiz(sagBacak, "Sag Bacak", new Color(0f, 0.7f, 0f), bacakSalinimMesafesi, bacakKaldirmaYuksekligi);

        // Kollar: mavi
        UzuvGizmoCiz(solKol, "Sol Kol", Color.cyan, kolSalinimMesafesi, 0f);
        UzuvGizmoCiz(sagKol, "Sag Kol", new Color(0f, 0.6f, 0.8f), kolSalinimMesafesi, 0f);

        // Bas: magenta
        UzuvGizmoCiz(bas, "Bas", Color.magenta, basSalinimMiktari, 0f);
    }

    private void UzuvGizmoCiz(Transform uzuv, string etiket, Color renk, float salinim, float kaldirma)
    {
        if (uzuv == null) return;

        Gizmos.color = renk;
        Gizmos.DrawWireSphere(uzuv.position, 0.05f);

        // Ileri-geri salinim araligi (yatay cizgi + uc noktalar)
        Vector3 sol = uzuv.position + Vector3.left * salinim;
        Vector3 sag = uzuv.position + Vector3.right * salinim;
        Gizmos.DrawLine(sol, sag);
        Gizmos.DrawWireCube(sol, Vector3.one * 0.03f);
        Gizmos.DrawWireCube(sag, Vector3.one * 0.03f);

        // Kaldirma yuksekligi (dikey cizgi)
        if (kaldirma > 0f)
            Gizmos.DrawLine(uzuv.position, uzuv.position + Vector3.up * kaldirma);

        UnityEditor.Handles.color = renk;
        UnityEditor.Handles.Label(uzuv.position + Vector3.up * 0.1f, etiket);
    }
#endif
}