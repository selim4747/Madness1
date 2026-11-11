using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;

public class CustomerController : MonoBehaviour
{
    [Header("Müşterinin Türü (Data)")]
    [SerializeField] private CustomerData profile;
    public CustomerData Profile { get { return profile; } }

    [Header("Gorunum (opsiyonel — bos birakilirsa hicbir sey bozulmaz)")]
    [Tooltip("NPC'nin ana govde SpriteRenderer'i — yon donusu (flipX) icin.")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    [Header("Cinsiyete Gore Govde (opsiyonel — ikisi de bos birakilirsa eski tek-govde davranisi aynen calisir)")]
    [Tooltip("Erkek karakterler secildiginde AKTIF edilecek govde objesi.")]
    [SerializeField] private GameObject erkekGorunumu;
    [Tooltip("Kadin karakterler secildiginde AKTIF edilecek govde objesi.")]
    [SerializeField] private GameObject kadinGorunumu;
    [Tooltip("Tisort/kiyafet katmani — SecilenKimlik'in kendi tisortSprite'i varsa buraya atanir.")]
    [SerializeField] private SpriteRenderer tisortRenderer;
    [Tooltip("Animator Controller HENUZ yoksa bos birak — kod otomatik atlar, hata vermez.")]
    [SerializeField] private Animator animator;

    // LORE SİSTEMİ: Adamın kendi eşsiz kimliği, ismi ve zevkleri burada!
    public NPCKimligi SecilenKimlik { get; private set; }

    /// <summary>ÇOKLU KİŞİLİK (opsiyonel): bu ziyaret için SecilenKimlik'in "diğer kişiliği"
    /// mi ortaya çıktı? Aktifse, sohbet radarları musteriIsmi/ozelDiyaloglar yerine
    /// digerKisilikIsmi/digerKisilikDiyaloglari kullanmalı. Alttaki GERÇEK kimlik (musteriIsmi)
    /// hep aynı kalır — ilişki/dedikodu takibi bundan etkilenmez, sadece görünen isim/laf değişir.</summary>
    public bool DigerKisilikAktifMi { get; private set; }

    /// <summary>SecilenKimlik atandığı HER an (yeni gelişte ya da kayıttan dönüşte) çağrılır —
    /// karakter "Çoklu Kişilikli" işaretliyse, bu ziyaret için diğer kişiliğin çıkıp çıkmadığını
    /// yeniden zar atarak belirler.</summary>
    private void DigerKisiligiBelirle()
    {
        DigerKisilikAktifMi = SecilenKimlik != null && SecilenKimlik.cokluKisilikliMi &&
                               UnityEngine.Random.value < SecilenKimlik.digerKisilikIhtimali;
    }

    // KASANIN VE SPAWNER'IN ARADIĞI KAN DAMARLARI
    public BookData CarriedBook { get; set; }
    public float CounterArrivalTime { get; set; }
    public event Action<CustomerController, bool> OnFinished;

    private NavMeshAgent agent;
    private ShelfController targetShelf;
    private BookData targetOzelKitap; // OzelRafController'dan hedeflenen kitap (targetShelf yerine)

    // Müşterinin o anki ruh hali ve eylemi
    private enum State { Spawning, GoingToShelf, WaitingAtShelf, GoingToCounter, WaitingAtCounter, GoingToGarden, SittingInGarden, GoingToStudyDesk, StudyingAtDesk, Leaving, GoingToGate }
    private State state = State.Spawning;

    private Transform bahceMasasi;
    private Vector3 moddedInitialTarget;
    private bool hasModdedInitialTarget;

    private float bahceTimer = 0f;
    private float benimKalmaSurem = 25f; // her oturusta rastgele (25-70sn) yeniden belirlenir
    public bool BahcedeMi { get { return state == State.SittingInGarden; } }

    // ── 2. Duzey Ogrenci: calisma masasi ──
    [Header("2. Duzey Ogrenci Ayarlari")]
    [Tooltip("Kitabini aldiktan sonra, calisma masasina gitmeyi (masa musaitse) deneme ihtimali. Denemezse (ya da masa yoksa) normal sekilde kasaya gidip tam fiyat oder.")]
    [Range(0f, 100f)]
    [SerializeField] private float calismayiDenemeSansi = 50f;
    private Transform calismaMasasi;
    private float calismaTimer = 0f;

    private Transform kapiNoktasi; // ayrilirken kullanilan cikis/spawn noktasi
    private Transform gecitNoktasi; // giris/cikis kapisi
    private bool cikistaGecitGecildi;

    private float waitTimer = 0f;
    public float maxWaitTime = 5f; // Rafta inceleme süresi

    private float patienceTimer = 0f;
    public float maxPatienceTime = 30f; // Kasada sabretme süresi

    private float leavingTimer = 0f;
    private const float MAX_LEAVING_SECONDS = 60f; // Kapiya bu surede varamazsa zorla kaldirilir

    private float gardenGoingTimer = 0f;
    private const float MAX_GARDEN_GOING_SECONDS = 25f; // Bahce masasina bu surede varamazsa vazgecilir
    private float PARTIAL_PATH_ISINLAMA_SANIYE => portalConfig != null ? portalConfig.partialPathIsinlamaSaniye : 5f;
    private float partialPathSuresi = 0f;

    // GEÇİT IŞINLAMASI: NavMeshPortalConfig atanırsa onu kullanır, yoksa eski sabitler.
    [SerializeField] private NavMeshPortalConfig portalConfig;
    
    // Isinlanma sonrasi yolu geri yukleyebilmek icin son verilen hedef saklanir.
    private Vector3 sonHedef;
    private bool hedefVar = false;
    private int isinlanmaBeklemeKaresi = 0; // Warp sonrasi kac kare varis kontrolu atlanacak

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // 2D NAVMESH ZIRHI
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        // Gorsel alanlar Inspector'da BOS birakildiysa otomatik bulunur — prefablari tek tek
        // elle baglamak zorunda kalmayasin diye. Elle atadiysan ona dokunulmaz.
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        // DOĞDUĞU İLK MİLİSANİYE KİMLİĞİNİ SEÇER (NullReferenceException Zırhı)
        if (profile != null && profile.karakterHavuzu != null && profile.karakterHavuzu.Count > 0)
        {
            // OZEL GUN FILTRESI: sadece BUGUN gelebilecek karakterler arasindan secilir.
            // Turistler, gonderme karakterleri vs. boylece sadece kendi gunlerinde cikar.
            int bugun = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

            List<NPCKimligi> bugunUygunlar = new List<NPCKimligi>();
            for (int i = 0; i < profile.karakterHavuzu.Count; i++)
            {
                NPCKimligi k = profile.karakterHavuzu[i];
                if (k != null && k.BuGunGelebilirMi(bugun)) bugunUygunlar.Add(k);
            }

            // TANISILAN HIKAYE KONUKLARI: bir kez tanisildiysa ve "duzenli gelir" ayarliysa,
            // artik normal musteri olarak da bu havuza katilirlar.
            MusteriTipi buTip = MusteriTipi.Normal;
            if (profile.preferredRarity == Rarity.Rare) buTip = MusteriTipi.Zengin;
            else if (profile.preferredRarity == Rarity.Legendary || profile.preferredRarity == Rarity.Mythic) buTip = MusteriTipi.VIP;

            List<NPCKimligi> ekHavuz = HikayeKonuguYoneticisi.EkHavuzuAl(buTip);
            if (ekHavuz != null)
            {
                for (int i = 0; i < ekHavuz.Count; i++)
                {
                    NPCKimligi k = ekHavuz[i];
                    if (k != null && k.BuGunGelebilirMi(bugun)) bugunUygunlar.Add(k);
                }
            }

            // Hicbiri uygun degilse (orn. tum havuz ozel gun karakteriyse) normal
            // karakterlere geri duser — musteri akisi asla durmasin.
            if (bugunUygunlar.Count == 0)
            {
                for (int i = 0; i < profile.karakterHavuzu.Count; i++)
                {
                    NPCKimligi k = profile.karakterHavuzu[i];
                    if (k != null && !k.ozelGunKarakteri) bugunUygunlar.Add(k);
                }
            }

            // GECICI HIKAYE KONUKLARI: su an baska bir yerde (spawn noktasinda) hikaye konugu
            // olarak bekleyen biri, AYNI GUN normal musteri havuzunda TEKRAR cikmasin.
            if (GeciciHikayeKonuguYoneticisi.Instance != null)
            {
                bugunUygunlar.RemoveAll(k => k != null && GeciciHikayeKonuguYoneticisi.Instance.SuAnKonukMu(k.musteriIsmi));
            }

            if (bugunUygunlar.Count == 0) return; // gercekten hicbir uygun karakter yok

            // AGIRLIKLI SECIM: SadikMusteriYoneticisi varsa, iliski puani yuksek karakterler
            // (oyuncuyla cok konusulmus olanlar) daha sik secilir — "mudavim" hissi budur.
            // Yoksa (ya da hic iliski birikmemisse) davranis, eski uniform rastgele ile AYNIDIR.
            SecilenKimlik = SadikMusteriYoneticisi.Instance != null
                            ? SadikMusteriYoneticisi.Instance.AgirlikliSec(bugunUygunlar)
                            : bugunUygunlar[UnityEngine.Random.Range(0, bugunUygunlar.Count)];

            // COKLU KISILIK (opsiyonel, cilgin ozellik): secilen kisi isaretliyse, her gelisinde
            // ayri ayri bir ihtimalle TAMAMEN FARKLI bir isim/kisilikle gelebilir.
            DigerKisiligiBelirle();

            // CINSIYETE GORE GOVDE: erkekGorunumu/kadinGorunumu ATANMISSA, kimlige uygun
            // olani AKTIF edilir, digeri KAPATILIR — ve bodyRenderer, o an AKTIF olanin
            // SpriteRenderer'ina guncellenir (asagidaki tisort mantigi onu kullansin diye).
            // IKISI DE ATANMAMISSA (eski/basit prefablar): hicbir sey degismez, eski
            // tek-govde davranisi AYNEN devam eder — geriye donuk uyumlu.
            if (erkekGorunumu != null || kadinGorunumu != null)
            {
                bool kadinMi = SecilenKimlik.cinsiyet == Cinsiyet.Kadin;
                if (erkekGorunumu != null) erkekGorunumu.SetActive(!kadinMi);
                if (kadinGorunumu != null) kadinGorunumu.SetActive(kadinMi);

                GameObject aktifGorunum = kadinMi ? kadinGorunumu : erkekGorunumu;
                if (aktifGorunum != null)
                {
                    SpriteRenderer aktifRenderer = aktifGorunum.GetComponent<SpriteRenderer>();
                    if (aktifRenderer != null) bodyRenderer = aktifRenderer;
                }
            }

            // Kimlige ozel gorunum: iki yol da desteklenir —
            //  (a) tisortRenderer atanmissa: ayri tisort KATMANI olarak uygulanir
            //  (b) atanmamissa: ANA govde sprite'i dogrudan degistirilir (tam karakter
            //      sprite'lari kullaniyorsan bu daha pratik — ayri katman kurmana gerek yok)
            if (SecilenKimlik.tisortSprite != null)
            {
                if (tisortRenderer != null) tisortRenderer.sprite = SecilenKimlik.tisortSprite;
                else if (bodyRenderer != null) bodyRenderer.sprite = SecilenKimlik.tisortSprite;
            }

            // Katalogdaki "tanistigimiz musteriler" listesine ekler (isim/lore artik gorunur olur).
            CustomerCatalogUI.MarkMet(SecilenKimlik.musteriIsmi);

            // KISILIK: bu karakterin kendi sabir/inceleme sureleri uygulanir.
            maxPatienceTime *= Mathf.Max(0.2f, SecilenKimlik.sabirCarpani);
            maxWaitTime *= Mathf.Max(0.2f, SecilenKimlik.incelemeSuresiCarpani);
        }
    }

    public void Baslat(object param1 = null, object param2 = null, object param3 = null, object param4 = null, object param5 = null, object param6 = null)
    {
        // Cikis/spawn noktasi ile giris icindeki ortak gecit noktasi ayri tutulur.
        kapiNoktasi = param1 as Transform;
        gecitNoktasi = param6 as Transform;

        // ONEMLI: Bahce sansi ARTIK BURADA denenmiyor — musteri once MUTLAKA bir rafa
        // gidip gelmeli (kitap alsin almasin farketmez), bahce zari HandleArrivedAtShelf()
        // icinde, raf ziyaretinden SONRA atiliyor. Boylece kimse "masaya gidip gelmeden"
        // dogrudan bahceye gitmiyor.
        if (gecitNoktasi != null)
        {
            state = State.GoingToGate;
            MoveThroughGate(gecitNoktasi.position);
        }
        else
        {
            RafAraVeyaKasayaGit();
        }
    }

    private void RafAraVeyaKasayaGit()
    {
        // KİMLİK KONTROLÜ
        if (SecilenKimlik == null)
        {
            DevLog.Error("🚨 HATA: Müşterinin kimliği yok! Inspector'dan CustomerData içine Karakter eklemeyi unuttun!");
            LeaveUnsatisfied();
            return;
        }

        // FindObjectsOfType yerine ShelfRegistry kullan (performans + otomatik oluşturma)
        var registry = ShelfRegistry.EnsureExists();
        var tumRaflar = registry.GetActiveShelves();

        List<ShelfController> uygunRaflar = new List<ShelfController>();

        foreach (var raf in tumRaflar)
        {
            if (raf != null && raf.gameObject.activeInHierarchy)
            {
                BookData raftakiKitap = raf.raftakiKitap;

                // Müşterinin zevkine uyuyor mu? (sevdigi turde OLMALI ve sevmedigi turde OLMAMALI)
                bool sevdigiTur = raftakiKitap != null && SecilenKimlik.sevilenTurler != null &&
                                   SecilenKimlik.sevilenTurler.Contains(raftakiKitap.kitapTuru);

                bool sevmedigiTur = raftakiKitap != null && SecilenKimlik.sevmedigiTurler != null &&
                                     SecilenKimlik.sevmedigiTurler.Contains(raftakiKitap.kitapTuru);

                if (sevdigiTur && !sevmedigiTur)
                {
                    uygunRaflar.Add(raf);
                }
            }
        }

        if (uygunRaflar.Count > 0)
        {
            // Sevdiği türü buldu!
            targetShelf = uygunRaflar[UnityEngine.Random.Range(0, uygunRaflar.Count)];
            state = State.GoingToShelf;
            Vector3 hedefNokta = hasModdedInitialTarget
                ? moddedInitialTarget
                : (targetShelf.ApproachPosition != Vector3.zero ? targetShelf.ApproachPosition : targetShelf.transform.position);
            MoveTo(hedefNokta);
            DevLog.Info($"🚶‍♂️ [BİLGİ] {SecilenKimlik.musteriIsmi} aradığı türü buldu, rafa gidiyor.");
        }
        else
        {
            // ÖZEL RAF YEDEĞİ: normal raflarda sevdiği tür yoksa, açık ve stoklu bir özel
            // raf var mı diye bakılır — o kişiye özel eşsiz kitap ya da herkese açık joker
            // kitap bulursa, MÜŞTERİ ELİ BOŞ DÖNMEZ.
            BookData ozelKitap = OzelRafController.Instance != null
                                  ? OzelRafController.Instance.EnUygunKitabiBul(SecilenKimlik) : null;

            if (ozelKitap != null)
            {
                targetShelf = null;
                targetOzelKitap = ozelKitap;
                state = State.GoingToShelf;
                MoveTo(OzelRafController.Instance.ApproachPosition);
                DevLog.Info($"✨ [ÖZEL RAF] {SecilenKimlik.musteriIsmi} özel rafta '{ozelKitap.kitapAdi}' buldu, oraya gidiyor.");
                return;
            }

            // Sevdiği tür yok, özel raf da yok/bos — kasaya gidip şikayet edecek!
            DevLog.Warning($"👀 [DİKKAT] {SecilenKimlik.musteriIsmi} sevdiği türü bulamadı, kasaya gidiyor!");
            CarriedBook = null;
            GoToCounter();
        }
    }

    void Update()
    {
        // Z EKSENİ ZORBA KORUMASI
        if (Mathf.Abs(transform.position.z) > 0.01f)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        }

        if (agent == null || !agent.isActiveAndEnabled || agent.pathPending) return;

        // GEÇİT IŞINLAMASI: yol hesaplaması beklemeden, her frame önce bu kontrol edilir.

        // Yon (flipX) ve animasyon — Animator/bodyRenderer bos ise dokunmadan gecer, hata vermez.
        if (bodyRenderer != null && Mathf.Abs(agent.velocity.x) > 0.05f)
            bodyRenderer.flipX = agent.velocity.x < 0f;

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetFloat("Speed", agent.velocity.sqrMagnitude);

        // KUSURSUZ VARIŞ SENSÖRÜ
        if (HedefeVardiMi())
        {
            if (state == State.GoingToGate)
            {
                state = State.Spawning;
                RestoreNormalBraking();
                RafAraVeyaKasayaGit();
            }
            else if (state == State.GoingToShelf)
            {
                state = State.WaitingAtShelf;
                waitTimer = 0f;
                DevLog.Info($"📖 [EYLEM] {SecilenKimlik.musteriIsmi} kitabı inceliyor (5 Saniye)...");
            }
            else if (state == State.GoingToCounter)
            {
                state = State.WaitingAtCounter;
                patienceTimer = 0f;
                CounterArrivalTime = Time.time;

                DevLog.Info($"⏱️ [SIRA] {SecilenKimlik.musteriIsmi} kasaya vardı. Masaya kayıt yapılıyor!");

                // İŞTE KASANIN BUTONUNU ÇALIŞTIRACAK O HAYATİ BAĞLANTI:
                CounterController kasa = CounterController.Instance;
                if (kasa != null)
                {
                    kasa.RegisterWaitingCustomer(this);
                }
                else
                {
                    DevLog.Error("🚨 HATA: Sahnede CounterController bulunamadı! Kasa objeni kontrol et.");
                }
            }
            else if (state == State.GoingToGarden)
            {
                state = State.SittingInGarden;
                bahceTimer = 0f;
                benimKalmaSurem = BahceYoneticisi.Instance != null
                                   ? BahceYoneticisi.Instance.RastgeleKalmaSuresi() : 25f;
                DevLog.Info($"🌳 [BAHÇE] {SecilenKimlik.musteriIsmi} masaya oturdu, sohbete açık ({benimKalmaSurem:F0} sn kalacak).");
            }
            else if (state == State.GoingToStudyDesk)
            {
                state = State.StudyingAtDesk;
                calismaTimer = 0f;
                DevLog.Info($"📚 [ÖĞRENCİ] {SecilenKimlik.musteriIsmi} masaya oturdu, çalışıyor.");
            }
            else if (state == State.Leaving)
            {
                if (gecitNoktasi != null && !cikistaGecitGecildi)
                {
                    cikistaGecitGecildi = true;
                    RestoreNormalBraking();
                    Vector3 bahceCikis = CikisNoktasi();
                    if (Vector3.Distance(transform.position, bahceCikis) > 0.15f)
                    {
                        MoveTo(bahceCikis);
                        return;
                    }
                }
                DevLog.Info($"👋 [ÇIKIŞ] {SecilenKimlik.musteriIsmi} bahçe çıkış noktasına ulaştı.");
                OnFinished?.Invoke(this, false);
                Destroy(gameObject);
            }
        }

        // ZAMANLAYICILAR (Timers)
        if (state == State.WaitingAtShelf)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= maxWaitTime)
            {
                HandleArrivedAtShelf();
            }
        }

        if (state == State.WaitingAtCounter)
        {
            patienceTimer += Time.deltaTime;
            if (patienceTimer >= maxPatienceTime)
            {
                DevLog.Warning($"😡 [İSYAN] {SecilenKimlik.musteriIsmi} kasada ağaç oldu, dükkanı terk ediyor!");
                LeaveUnsatisfied();
            }
        }

        if (state == State.SittingInGarden)
        {
            bahceTimer += Time.deltaTime;
            if (bahceTimer >= benimKalmaSurem)
            {
                BahceyiTerkEt();
            }
        }

        if (state == State.StudyingAtDesk)
        {
            calismaTimer += Time.deltaTime;
            float beklemeSuresi = OgrenciCalismaMasalari.Instance != null
                                   ? OgrenciCalismaMasalari.Instance.MasaBeklemeSuresi : 20f;

            if (calismaTimer >= beklemeSuresi)
            {
                DevLog.Info($"📚 [ÖĞRENCİ] {SecilenKimlik.musteriIsmi} çalışmayı bitirip kitabı rafa bırakıyor.");
                PlayerProgressStats.AddStudyDeskSession();

                // Kitabi RAFA GERI koy (TryTakeOne ile dengeli — stok dogru kalir).
                if (targetShelf != null) targetShelf.ReturnOne();
                CarriedBook = null;

                if (OgrenciCalismaMasalari.Instance != null && calismaMasasi != null)
                    OgrenciCalismaMasalari.Instance.MasayiBirak(calismaMasasi);
                calismaMasasi = null;

                state = State.Leaving;
                leavingTimer = 0f;
                OnFinished?.Invoke(this, false);
                CikisaYuru();
            }
        }

        if (state == State.GoingToGarden)
        {
            gardenGoingTimer += Time.deltaTime;

            // YOL "PathPartial" ISE (hedefe hicbir zaman ulasamayan bir yol bulunmus demektir,
            // ne kadar beklense de duzelmez) — birkac saniye sonra DOGRUDAN masaya isinlanir,
            // 25 saniye boyu bosuna beklemek yerine.
            if (agent != null && agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial)
            {
                partialPathSuresi += Time.deltaTime;
                if (partialPathSuresi >= PARTIAL_PATH_ISINLAMA_SANIYE && bahceMasasi != null)
                {
                    DevLog.Warning($"⚡ [BAHÇE — KALICI PARTIAL] {(SecilenKimlik != null ? SecilenKimlik.musteriIsmi : "Müşteri")} masaya giden yol NavMesh ile tam bağlı değil (PathPartial {PARTIAL_PATH_ISINLAMA_SANIYE} sn sürdü) — doğrudan masaya ışınlanıyor.");
                    agent.Warp(bahceMasasi.position);
                    isinlanmaBeklemeKaresi = 3;
                    partialPathSuresi = 0f;
                }
            }
            else
            {
                partialPathSuresi = 0f; // Complete/Invalid'e donerse sayac sifirlanir
            }

            if (gardenGoingTimer >= MAX_GARDEN_GOING_SECONDS)
            {
                // TESHIS: NEDEN ulasamadigini Console'a yazdirir — pathStatus "Invalid"/"Partial"
                // ise NavMesh baglantisi (geçit bandi) eksik demektir, "Complete" ise yol var
                // ama baska bir sebeple ilerleyemiyor demektir.
                string durum = agent != null ? agent.pathStatus.ToString() : "agent yok";
                float kalanMesafe = agent != null ? agent.remainingDistance : -1f;
                float hiz = agent != null ? agent.velocity.magnitude : -1f;
                bool durduMu = agent != null && agent.isStopped;
                bool navMeshte = agent != null && agent.isOnNavMesh;
                Vector3 hedefKonum = agent != null ? agent.destination : Vector3.zero;
                DevLog.Warning($"⚠️ [BAHÇE ZORLA TEMİZLEME] {(SecilenKimlik != null ? SecilenKimlik.musteriIsmi : "Müşteri")} bahçe masasına ulaşamadı ({MAX_GARDEN_GOING_SECONDS} sn, pathStatus={durum}, kalanMesafe={kalanMesafe:F2}, hiz={hiz:F2}, isStopped={durduMu}, isOnNavMesh={navMeshte}, agent.destination=({hedefKonum.x:F2},{hedefKonum.y:F2}), mevcutPos=({transform.position.x:F2},{transform.position.y:F2}), hedefPos=({(bahceMasasi != null ? bahceMasasi.position.x.ToString("F2") : "?")},{(bahceMasasi != null ? bahceMasasi.position.y.ToString("F2") : "?")})), masa serbest bırakılıp normal çıkışa yönlendirildi.");

                if (BahceYoneticisi.Instance != null && bahceMasasi != null) BahceYoneticisi.Instance.MasayiBirak(bahceMasasi);
                bahceMasasi = null;

                state = State.Leaving;
                leavingTimer = 0f;
                OnFinished?.Invoke(this, false);
                CikisaYuru();
            }
        }

        // GÜVENLİK AĞI: kapiya giderken NavMesh/patika sorunuyla takilirsa
        // (masada/kasada sikisip kalanlar), belirli bir sureden sonra zorla kaldirilir.
        if (state == State.Leaving)
        {
            leavingTimer += Time.deltaTime;
            if (leavingTimer >= MAX_LEAVING_SECONDS)
            {
                DevLog.Warning($"⚠️ [ZORLA TEMİZLEME] {(SecilenKimlik != null ? SecilenKimlik.musteriIsmi : "Müşteri")} kapıya ulaşamadı, {MAX_LEAVING_SECONDS} sn sonra zorla kaldırıldı.");
                Destroy(gameObject);
                return;
            }
        }
    }

    private bool HedefeVardiMi()
    {
        if (agent.pathPending) return false;

        // Isinlanma (Warp) sonrasi birkac kare, agent'in yolu henuz yeniden hesaplanmamis
        // olur ve "vardim" sanip asamalari ATLAYABILIR. Bunun icin pathStatus'e bakmak
        // YANLIS oldu (varista da false donuyordu, NPC'ler donup kaliyordu) — onun yerine
        // isinlanmadan sonra SADECE birkac kare bekliyoruz.
        if (isinlanmaBeklemeKaresi > 0)
        {
            isinlanmaBeklemeKaresi--;
            return false;
        }

        // YENI: yola giden hicbir NavMesh baglantisi YOKSA (PathInvalid), "vardi" SAYILMAZ.
        // Eskiden remainingDistance boyle durumlarda yanlislikla kucuk okunabiliyordu — bu da
        // musterinin HIC HAREKET ETMEDEN "vardim" sanmasina (sahte-varis) yol aciyordu.
        // NOT: sadece PathInvalid haric tutuluyor — PathPartial/PathComplete'e DOKUNULMADI,
        // cunku daha once pathStatus'u genel olarak kontrol etmek normal varisi da bozmustu.
        if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid) return false;

        // ARTIK: mesafe yeterince kucukse DOGRUDAN "vardi" sayilir. Eskiden ayrica "hiz TAM
        // SIFIR" da isteniyordu — ama NavMeshAgent durma noktasinda ufak bir titresim/kalinti
        // hiz gosterebiliyor (tam sifira inmiyor), bu da musteri hedefe 0.02 birim mesafede
        // olsa bile "henuz varmadi" sanilmasina (sahte-VARMAMA) yol aciyordu.
        return agent.remainingDistance <= agent.stoppingDistance + 0.1f;
    }

    private void HandleArrivedAtShelf()
    {
        // OZEL RAF DALI: hedef normal raf degil, OzelRafController'daki belirli bir kitapsa.
        if (targetOzelKitap != null)
        {
            BookData alinan = OzelRafController.Instance != null
                               ? OzelRafController.Instance.TryTakeOne(targetOzelKitap) : null;

            CarriedBook = alinan; // stok tam o anda bittiyse null gelebilir, guvenlidir
            targetOzelKitap = null;

            if (alinan != null)
                DevLog.Info($"✨ [ÖZEL RAF] {SecilenKimlik.musteriIsmi} '{alinan.kitapAdi}' kitabını eline aldı!");

            // ONEMLI: bahce hakki artik BURADA denenmiyor — musteri ONCE kasaya gidip
            // GERCEKTEN islem gormeli (satin alsin ya da "kitap yok" diye gonderilsin).
            // Bkz. CompleteSale() / LeaveUnsatisfied(bahceDenensin: true).
            GoToCounter();
            return;
        }

        if (targetShelf != null)
        {
            // 2. DUZEY OGRENCI DALI: kendi kitabini alip bazen calisma masasina gider.
            bool ikinciDuzeyOgrenci = CustomerSpawner.Instance != null &&
                                       profile == CustomerSpawner.Instance.ogrenciCustomerData;

            if (ikinciDuzeyOgrenci && UnityEngine.Random.Range(0f, 100f) < calismayiDenemeSansi)
            {
                Transform masa = OgrenciCalismaMasalari.Instance != null
                                  ? OgrenciCalismaMasalari.Instance.MasaAyir() : null;

                if (masa != null)
                {
                    // Calisma yolunda kitap GERCEKTEN rafdan dusulur (TryTakeOne) —
                    // cunku sonunda ReturnOne ile GERI eklenecek, dengeli olmasi icin.
                    BookData kitap = targetShelf.TryTakeOne();
                    if (kitap != null)
                    {
                        CarriedBook = kitap;

                        // Calisma ucreti: kitabin tam fiyatinin %20'si, HEMEN odenir.
                        int calismaUcreti = Mathf.RoundToInt(kitap.GetFinalPrice() * 0.2f);
                        if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(calismaUcreti);

                        calismaMasasi = masa;
                        state = State.GoingToStudyDesk;
                        MoveTo(masa.position);
                        DevLog.Info($"📚 [ÖĞRENCİ] {SecilenKimlik.musteriIsmi} kitabı alıp çalışma masasına gidiyor ({calismaUcreti} Para).");
                        return;
                    }

                    // Stok tam o an tukenmisti — ayirdigimiz masayi birakip normal akisa don.
                    OgrenciCalismaMasalari.Instance.MasayiBirak(masa);
                }
                // Masa musait degilse: normal satin alma akisina duser (asagida).
            }

            // NORMAL AKIS (2. Duzey ogrenci calismayi secmedi/masa yoktu, ya da baska musteri tipi)
            CarriedBook = targetShelf.raftakiKitap;
            DevLog.Info($"🛒 [ALIŞVERİŞ] {SecilenKimlik.musteriIsmi} kitabı eline aldı!");
        }

        // ONEMLI: bahce hakki artik kasadan SONRA denenir (bkz. CompleteSale() /
        // LeaveUnsatisfied(bahceDenensin: true)) — "kutuphaneye (satis masasi) gelsin,
        // sonra bahceye" kurali boylece saglanir.
        GoToCounter();
    }

    /// <summary>Raf ziyaretinden SONRA cagrilir. Karakter sosyalse ve zar tutarsa, bos bir
    /// bahce masasi bulup oraya yonlendirir. Basarili olursa true doner (cagiran GoToCounter'i
    /// ATLAMALI), basarisizsa false doner (cagiran normal sekilde kasaya yonlendirmeli).</summary>
    private bool BahceyeGitmeyiDene()
    {
        if (SecilenKimlik == null || !SecilenKimlik.sosyal || BahceYoneticisi.Instance == null) return false;
        if (!BahceYoneticisi.Instance.BahceyeOturulabilirMi()) return false; // mesai bittiyse HAYIR
        if (UnityEngine.Random.value >= BahceYoneticisi.Instance.BahceIhtimali) return false;

        Transform masa = BahceYoneticisi.Instance.MasaAyir(this);
        if (masa == null) return false;

        bahceMasasi = masa;
        bahceSohbetSayaci = 0;
        gardenGoingTimer = 0f;
        state = State.GoingToGarden;
        MoveTo(masa.position);
        DevLog.Info($"🌳 [BAHÇE] {SecilenKimlik.musteriIsmi} rafı ziyaret ettikten sonra bahçe masasına oturmaya gidiyor.");
        return true;
    }

    private int bahceSohbetSayaci = 0;

    /// <summary>BahceSohbetRadari, oyuncuyla GERCEK bir konusma gosterdiginde bunu cagirir.
    /// NOT: artik "sikilip erken kalkma" YOK — musteri SADECE 25-70sn'lik rastgele suresi
    /// dolunca kalkar (bkz. benimKalmaSurem). Sayac yine de tutuluyor (ileride istatistik
    /// icin kullanilabilir), ama kimseyi masadan kaldirmiyor.</summary>
    public void BahceSohbetiKaydet()
    {
        bahceSohbetSayaci++;
    }

    /// <summary>Bahce masasindan ayrilma islemi — normal sure dolunca (Update), sikilinca
    /// (BahceSohbetiKaydet) ya da mesai bitince (BahceYoneticisi.HerkesiKaldir) BURADAN cagrilir.</summary>
    public void BahceyiTerkEt()
    {
        if (state != State.SittingInGarden && state != State.GoingToGarden) return;

        DevLog.Info($"🌳 [BAHÇE] {SecilenKimlik.musteriIsmi} bahçeden kalkıp ayrılıyor.");
        PlayerProgressStats.AddGardenVisitServed();
        if (BahceYoneticisi.Instance != null && bahceMasasi != null) BahceYoneticisi.Instance.MasayiBirak(bahceMasasi);
        bahceMasasi = null;
        if (BahceSohbetYoneticisi.Instance != null) BahceSohbetYoneticisi.Instance.KonusmayiTemizle(this);

        state = State.Leaving;
        leavingTimer = 0f;
        OnFinished?.Invoke(this, false);
        CikisaYuru();
    }

    /// <summary>Ayrilirken (satis/isyan/bahce zaman asimi) gidilecek gercek nokta — spawner'in
    /// verdigi kapiNoktasi varsa onu kullanir, yoksa eski sabit yedek degere duser.</summary>
    private Vector3 CikisNoktasi()
    {
        // kapiNoktasi bahçe tarafındaki başlangıç/sonlanma noktasıdır.
        return kapiNoktasi != null ? kapiNoktasi.position : new Vector3(0, -15, 0);
    }
    private void CikisaYuru()
    {
        state = State.Leaving;
        leavingTimer = 0f;
        cikistaGecitGecildi = false;
        if (gecitNoktasi != null) MoveThroughGate(gecitNoktasi.position);
        else MoveTo(CikisNoktasi());
    }
    private void MoveThroughGate(Vector3 target)
    {
        if (agent != null)
        {
            agent.autoBraking = false;
            agent.stoppingDistance = 0f;
        }
        MoveTo(target);
    }
    private void RestoreNormalBraking()
    {
        if (agent != null)
        {
            agent.autoBraking = true;
            agent.stoppingDistance = 0.1f;
        }
    }

    public void MoveTo(Vector3 hedef)
    {
        if (agent == null || !agent.isActiveAndEnabled) return;

        if (!agent.isOnNavMesh)
        {
            // Agent NavMesh uzerinde degil (spawn noktasi bake edilmemis bir alanda olabilir).
            // Cokmek yerine en yakin gecerli NavMesh noktasina isinlanmayi dene.
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                DevLog.Warning($"⚠️ [NAVMESH] {gameObject.name} NavMesh uzerinde degil, 5 birim icinde gecerli nokta da bulunamadi — hareket edemiyor. Spawn noktasinin (kapiNoktasi) NavMesh ile kapli oldugundan emin ol.");
                return;
            }
        }

        agent.isStopped = false;
        agent.SetDestination(hedef);

        // YENI: HER yeni hedef verildiginde (isinlanma disinda da) birkac kare bekleme payi.
        // NavMeshAgent'in remainingDistance'i, SetDestination'dan hemen sonra AYNI karede
        // henuz eski (kucuk) degeri gosterebiliyor — bu da "hic hareket etmeden vardim" gibi
        // gorunen SAHTE-VARIS'e yol aciyordu (bahceye giderken 1 saniye bile beklemeden
        // hemen "vardi" sayilip ayrilma hatasi tam buydu).
        isinlanmaBeklemeKaresi = Mathf.Max(isinlanmaBeklemeKaresi, 3);

        // Hedefi sakla — isinlanma (Warp) mevcut yolu SILDIGI icin, isinlanmadan hemen sonra
        // bu hedefi yeniden vermek zorundayiz, yoksa NPC olduğu yerde donup kalir.
        sonHedef = hedef;
        hedefVar = true;
    }

    public void GoToCounter()
    {
        state = State.GoingToCounter;
        CounterController kasa = CounterController.Instance;
        if (kasa != null)
        {
            // ESKIDEN: MoveTo(kasa.transform.position) — herkes kasanin TAM AYNI noktasina
            // yuruyup ust uste yigiliyordu, hicbiri "vardim" sayilamiyordu. ARTIK: her musteri
            // yola cikarken kendine ait bos bir sira noktasi ayirtiyor.
            MoveTo(kasa.GetApproachPoint(this));
        }
        else
        {
            LeaveUnsatisfied();
        }
    }

    public void CompleteSale()
    {
        // KRITIK SIRA: OnFinished HER ZAMAN (bahceye gitse de gitmese de) burada, EN BASTA
        // tetiklenir — cunku CounterController bunu dinleyip musteriyi 'waiting' listesinden
        // ÇIKARIYOR. Eger bahceye gidecegi icin asagida erken 'return' edilirse VE bu satir
        // atlanirsa, musteri sonsuza kadar 'waiting' listesinde KALIR — baska biri kasaya
        // gelince RepositionQueue() bu musteriyi de bir kuyruk noktasina ISINLAR, bahce
        // hedefinin UZERINE YAZAR (agent.destination bozulur, musteri "takilir" gibi gorunur).
        OnFinished?.Invoke(this, true);

        // Elinde kitap varsa, o kitabin turu bu musterinin "sevdigi tur" olarak katalogda acilir.
        if (CarriedBook != null && SecilenKimlik != null)
            CustomerCatalogUI.RevealPreference(SecilenKimlik.musteriIsmi, CarriedBook.kitapTuru);

        // ONEMLI: kasadan basariyla islem gordukten sonra bahce hakki BURADA denenir —
        // "kütüphaneye (satis masasi) gelsin, sonra bahceye" kurali boylece saglanir.
        if (BahceyeGitmeyiDene()) return;

        state = State.Leaving;
        leavingTimer = 0f;

        CikisaYuru(); // Önce geçit, ardından bahçe tarafındaki spawn/çıkış noktası
        DevLog.Info($"✅ [SATIŞ] {SecilenKimlik.musteriIsmi} ödemeyi yaptı ve mutlu ayrılıyor.");
    }

    /// <summary>bahceDenensin: SADECE kasada GERCEKTEN islem goren (kitap yoktu, kasiyer
    /// nazikce gonderdi) musteriler icin true verilir — sabirsizliktan isyan edip giden ya
    /// da hata durumunda ayrilan musteriler icin FALSE kalmali (ofkeliyken bahceye oturmaz).</summary>
    public void LeaveUnsatisfied(bool bahceDenensin = false)
    {
        // KRITIK SIRA: kasa kaydi/OnFinished HER ZAMAN en basta temizlenir — CompleteSale()'deki
        // AYNI hatanin burada da olmamasi icin (musteri 'waiting' listesinde sonsuza kadar
        // kalip, baska biri kasaya gelince bahce hedefinin uzerine kuyruk noktasi yazilmasin diye).
        CounterController kasa = CounterController.Instance;
        if (kasa != null)
        {
            kasa.CancelApproach(this); // henuz kasaya varmadan vazgectiyse yoldaki kaydini da sil
            OnFinished?.Invoke(this, false); // 'waiting' listesindeyse burada cikarilir
        }

        if (bahceDenensin && BahceyeGitmeyiDene()) return;

        state = State.Leaving;
        leavingTimer = 0f;

        // Bahce masasindaysa (normalde buraya girmez ama guvenlik icin) masayi birak.
        if (BahceYoneticisi.Instance != null && bahceMasasi != null)
        {
            BahceYoneticisi.Instance.MasayiBirak(bahceMasasi);
            bahceMasasi = null;
            if (BahceSohbetYoneticisi.Instance != null) BahceSohbetYoneticisi.Instance.KonusmayiTemizle(this);
        }

        // Calisma masasindaysa da ayni sekilde birak, elindeki kitap varsa rafa koy.
        if (OgrenciCalismaMasalari.Instance != null && calismaMasasi != null)
        {
            OgrenciCalismaMasalari.Instance.MasayiBirak(calismaMasasi);
            calismaMasasi = null;
            if (targetShelf != null && CarriedBook != null) targetShelf.ReturnOne();
            CarriedBook = null;
        }

        CikisaYuru();
    }

    public void SetModdedTargetPosition(Vector3 position)
    {
        moddedInitialTarget = position;
        hasModdedInitialTarget = true;
    }

    // ==========================================
    // OTURUM KAYDI (Ana Menu'ye donup geri gelince ayni yerden devam)
    // ==========================================

    /// <summary>Bu musterinin o anki tam durumunu kayit verisine cevirir.</summary>
    public CustomerSaveEntry DurumuKaydet(int prefabTipi)
    {
        CustomerSaveEntry e = new CustomerSaveEntry();
        e.prefabTipi = prefabTipi;
        e.x = transform.position.x;
        e.y = transform.position.y;
        e.durum = (int)state;
        e.kimlikIsmi = SecilenKimlik != null ? SecilenKimlik.musteriIsmi : "";
        e.tasinanKitapId = CarriedBook != null ? CarriedBook.bookId : "";
        e.bekleyisSayaci = waitTimer;
        e.sabirSayaci = patienceTimer;
        return e;
    }

    /// <summary>Kayittan gelen durumu bu musteriye uygular ve hedefine dogru yola devam ettirir.</summary>
    public void DurumuGeriYukle(CustomerSaveEntry e, CustomerData profilVerisi, Transform kapi)
    {
        kapiNoktasi = kapi; // cikis noktasi da geri yuklenmeli, yoksa yanlis yere gider
        // Kimligi isme gore havuzdan bul (rastgele secilen kisi ayni olsun)
        if (profilVerisi != null && profilVerisi.karakterHavuzu != null)
        {
            for (int i = 0; i < profilVerisi.karakterHavuzu.Count; i++)
            {
                if (profilVerisi.karakterHavuzu[i] != null &&
                    profilVerisi.karakterHavuzu[i].musteriIsmi == e.kimlikIsmi)
                {
                    SecilenKimlik = profilVerisi.karakterHavuzu[i];
                    DigerKisiligiBelirle(); // Coklu Kisilik — kayittan donuste de tekrar yeniden belirlenir

                    if (SecilenKimlik.tisortSprite != null)
                    {
                        if (tisortRenderer != null) tisortRenderer.sprite = SecilenKimlik.tisortSprite;
                        else if (bodyRenderer != null) bodyRenderer.sprite = SecilenKimlik.tisortSprite;
                    }
                    break;
                }
            }
        }

        // Konumu geri yukle (NavMeshAgent icin Warp sart, transform yeterli degil)
        Vector3 kayitliKonum = new Vector3(e.x, e.y, 0f);
        if (agent != null && agent.isActiveAndEnabled)
        {
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(kayitliKonum, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
                agent.Warp(hit.position);
            else
                agent.Warp(kayitliKonum);
        }
        else
        {
            transform.position = kayitliKonum;
        }

        state = (State)e.durum;
        waitTimer = e.bekleyisSayaci;
        patienceTimer = e.sabirSayaci;

        // Tasidigi kitabi geri bul
        if (!string.IsNullOrEmpty(e.tasinanKitapId))
        {
            BookData[] tumKitaplar = Resources.LoadAll<BookData>("Books");
            for (int i = 0; i < tumKitaplar.Length; i++)
            {
                if (tumKitaplar[i] != null && tumKitaplar[i].bookId == e.tasinanKitapId)
                {
                    CarriedBook = tumKitaplar[i];
                    break;
                }
            }
        }

        // Kaldigi asamaya gore yoluna devam et
        DevamEt();
    }

    /// <summary>Geri yuklenen duruma gore NPC'yi tekrar yola koyar.</summary>
    private void DevamEt()
    {
        switch (state)
        {
            case State.GoingToGate:
                if (gecitNoktasi != null) MoveTo(gecitNoktasi.position);
                else RafAraVeyaKasayaGit();
                break;
            case State.GoingToShelf:
            case State.WaitingAtShelf:
                // Hedef raf bilgisi kaydedilmedigi icin en uygun rafi yeniden bulur
                RafAraVeyaKasayaGit();
                break;

            case State.GoingToCounter:
            case State.WaitingAtCounter:
                GoToCounter();
                break;

            case State.GoingToGarden:
            case State.SittingInGarden:
                if (BahceYoneticisi.Instance != null)
                {
                    Transform masa = BahceYoneticisi.Instance.MasaAyir(this);
                    if (masa != null) { bahceMasasi = masa; MoveTo(masa.position); }
                    else { state = State.Leaving; CikisaYuru(); }
                }
                break;

            case State.GoingToStudyDesk:
            case State.StudyingAtDesk:
                // Hangi rafdan aldigi kaydedilmedigi icin (raf referansi tasinmiyor), elindeki
                // kitap varsa dogrudan kasaya yonlendirilir — calisma indirimi kaybolur ama
                // musteri kaybolmaz/takilmaz, en guvenli sonuc budur.
                if (OgrenciCalismaMasalari.Instance != null)
                {
                    Transform masa = OgrenciCalismaMasalari.Instance.MasaAyir();
                    if (masa != null) { calismaMasasi = masa; MoveTo(masa.position); }
                    else GoToCounter();
                }
                else GoToCounter();
                break;

            case State.Leaving:
                if (gecitNoktasi != null && !cikistaGecitGecildi) MoveThroughGate(gecitNoktasi.position);
                else MoveTo(CikisNoktasi());
                break;

            default:
                RafAraVeyaKasayaGit();
                break;
        }
    }
}
