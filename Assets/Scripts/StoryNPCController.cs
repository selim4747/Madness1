// StoryNPCController.cs — Afor ve diger lore karakterleri
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StoryNPCController : NavMeshMover
{
    private enum State { Entering, Talking, GoingToShelf, GoingToCounter, WaitingAtCounter, GoingToGarden, SittingInGarden, Leaving }

    [Header("Gorsel")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private GameObject talkIcon;
    [Tooltip("Afor'un fes gorseli — sadece fes ondayken (24. gunden sonra) gosterilir.")]
    [SerializeField] private GameObject aforFesGorseli;
    [Tooltip("(A) SPRITE DEGISTIRME yolu — Afor'un FESLI hali. Doluysa ayri fes objesine gerek kalmaz.")]
    [SerializeField] private Sprite aforFesliSprite;
    [Tooltip("(A) SPRITE DEGISTIRME yolu — Afor'un FESSIZ hali.")]
    [SerializeField] private Sprite aforFessizSprite;
    [Tooltip("Animator Controller HENUZ yoksa bos birak — kod otomatik atlar, hata vermez.")]
    [SerializeField] private Animator animator;

    [Header("Seslendirme (varsayilan)")]
    [Tooltip("SESLENDIRME icin: dogru 'blabla' havuzunun secilmesi icin kullanilir. Hikaye konugu ise ve GERCEK NPCKimligi'si (konukData.duzenliKimlik) atanmissa ONA gore otomatik belirlenir — bu iki alan SADECE o durum gecerli DEGILSE (ozel gun diyalogu, gunluk NPC, ya da duzenliKimlik bos birakilmis hikaye konugu — orn. Afor) kullanilir. Varsayilan Erkek+Yetiskin (Afor icin dogru).")]
    [SerializeField] private Cinsiyet varsayilanCinsiyet = Cinsiyet.Erkek;
    [SerializeField] private YasGrubu varsayilanYasGrubu = YasGrubu.Yetiskin;

    private State state;
    private Coroutine hareketCoroutine;
    private string npcName;
    private string[] dialogueLines;
    private StoryDialogueData storyData;   // ozel gun diyaloguysa dolu, gunluk sohbetse null
    private HikayeKonuguData konukData;    // hikaye konuguysa dolu (bahce kosesinde bekleyen)
    private Transform doorPoint;
    private Transform counterPoint;
    private Transform gatewayPoint;
    private bool enteringViaGate;
    private bool leavingViaGate;
    private bool willBuy;
    private bool isFinale;
    private bool dialogueShown;
    private ShelfController targetShelf;
    private BookData carriedBook;


    public string NpcName { get { return npcName; } }
    public BookData CarriedBook { get { return carriedBook; } }

    /// <summary>SESLENDIRME icin: bu NPC'nin dogru cinsiyetini belirler. Hikaye konugu ise
    /// ve GERCEK NPCKimligi'si (duzenliKimlik) atanmissa ONU kullanir (en dogru kaynak —
    /// dukkanda gorunen ayni kisiyle AYNI blabla sesini calar); yoksa bu controller'daki
    /// varsayilanCinsiyet alanina duser (ozel gun diyalogu / gunluk NPC / Afor gibi).</summary>
    private Cinsiyet AktifCinsiyet()
    {
        if (konukData != null && konukData.duzenliKimlik != null) return konukData.duzenliKimlik.cinsiyet;
        return varsayilanCinsiyet;
    }

    /// <summary>SESLENDIRME icin: bu NPC'nin dogru yas grubunu belirler. AktifCinsiyet() ile
    /// AYNI mantik — hikaye konugunun GERCEK NPCKimligi'si varsa ONUN GetYasGrubu()'su, yoksa
    /// varsayilanYasGrubu alanina duser.</summary>
    private YasGrubu AktifYasGrubu()
    {
        if (konukData != null && konukData.duzenliKimlik != null) return konukData.duzenliKimlik.GetYasGrubu();
        return varsayilanYasGrubu;
    }

    private void OnDestroy()
    {
        // Herhangi bir sebeple obje yok olursa (havuzlama yok bu script'te, direkt Destroy)
        // asili kalmis abonelikler baska bir NPC'nin konusmasina karismasin diye temizlenir.
        if (DialogueBox.Instance != null)
        {
            DialogueBox.Instance.OnNoMoreLines -= HandleNoMoreLines;
            DialogueBox.Instance.OnNoMoreLines -= HandleBahceSohbetiBitti;
        }
    }

    /// <summary>Ozel gun diyalogu (StoryDialogueData) — kamera bu NPC'ye odaklanir.</summary>
    public void InitializeStory(StoryDialogueData data, Transform door, Transform counter, bool shouldBuy, Transform gate = null)
    {
        storyData = data;
        npcName = data.speakerName;
        dialogueLines = null;
        doorPoint = door;
        counterPoint = counter;
        gatewayPoint = gate;
        enteringViaGate = gate != null;
        leavingViaGate = false;
        willBuy = shouldBuy;
        isFinale = data.isFinale;
        dialogueShown = false;

        if (talkIcon != null) talkIcon.SetActive(true);

        UpdateFesGorseli();

        state = State.Entering;
        if (enteringViaGate) MoveThroughGate(gatewayPoint.position);
        else MoveTo(counterPoint.position);
    }

    /// <summary>Sabit sohbet metniyle gunluk NPC (Afor'un her gunku ziyareti) — kamera odaklanmaz.</summary>
    /// <summary>Hikaye konugu olarak baslatir — bahce kosesinde bekler, oyuncu yaklasinca
    /// kamera odakli kendi diyalogunu oynatir, sonra cikisa dogru gider.</summary>
    public void InitializeKonuk(HikayeKonuguData konuk, Transform beklemeNoktasi, Transform cikis)
    {
        konukData = konuk;
        storyData = null;
        npcName = konuk.isim;
        doorPoint = cikis;
        counterPoint = beklemeNoktasi;
        willBuy = false;
        isFinale = false;
        dialogueShown = false;

        if (talkIcon != null) talkIcon.SetActive(true);

        UpdateFesGorseli();

        // Zaten bekleme noktasinda spawn oldugu icin dogrudan "bekliyor" durumuna gecer.
        state = State.WaitingAtCounter;
    }

    public void InitializeDaily(string name, string[] lines, Transform door, Transform counter, bool shouldBuy, Transform gate = null)
    {
        storyData = null;
        npcName = name;
        dialogueLines = lines;
        doorPoint = door;
        counterPoint = counter;
        gatewayPoint = gate;
        enteringViaGate = gate != null;
        leavingViaGate = false;
        willBuy = shouldBuy;
        isFinale = false;
        dialogueShown = false;

        if (talkIcon != null) talkIcon.SetActive(true);

        UpdateFesGorseli();

        state = State.Entering;
        if (enteringViaGate) MoveThroughGate(gatewayPoint.position);
        else MoveTo(counterPoint.position);
    }

    /// <summary>Fes 24. gunden sonra Afor'daysa, gorunumunu ona gore ayarlar.
    /// Iki yol da desteklenir: tam sprite degistirme (A) ya da ayri fes objesi (B).</summary>
    /// <summary>FesYonetici, oyuncu butona basip fesi degistirdiginde bunu cagirir —
    /// Afor sahnedeyse gorunumu ANINDA guncellenir (sonraki spawn'i beklemez).</summary>
    public void FesGorunumunuGuncelle()
    {
        UpdateFesGorseli();
    }

    private void UpdateFesGorseli()
    {
        bool fesOnda = FesYonetici.Instance != null && FesYonetici.Instance.AforFesTakiyorMu();

        // (A) Sprite degistirme yolu
        if (bodyRenderer != null)
        {
            Sprite hedef = fesOnda ? aforFesliSprite : aforFessizSprite;
            if (hedef != null) bodyRenderer.sprite = hedef;
        }

        // (B) Ayri fes objesi yolu
        if (aforFesGorseli != null) aforFesGorseli.SetActive(fesOnda);
    }

    private void Update()
    {
        IsinlanmaSayaciniAzalt();


        // Yon (flipX) ve animasyon — bos ise dokunmadan gecer.
        if (bodyRenderer != null && Mathf.Abs(CurrentVelocity.x) > 0.05f)
            bodyRenderer.flipX = CurrentVelocity.x < 0f;

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetFloat("Speed", CurrentVelocity.sqrMagnitude);

        // Gece basladiysa NPC ayrilir
        if (GameManager.Instance.CurrentPhase != GamePhase.Day && state != State.Leaving)
        {
            // Diyalog tam bu NPC ile acikken zorla kesiliyorsak, kutuyu da kapat —
            // yoksa DialogueBox ekranda asili kalir, kimse ShowNext/Finish cagirmaz.
            // (Bahce sohbeti sirasinda state hala SittingInGarden oldugu icin onu da kapsar.)
            // NOT: ForceClose() onFinished callback'ini TETIKLEMEZ, o yuzden OnNoMoreLines
            // aboneliklerini burada DOGRUDAN temizlemek gerekiyor — yoksa asili kalip
            // ileride BASKA bir NPC'nin konusmasina yanlislikla karisirlar.
            if ((state == State.Talking || state == State.SittingInGarden) && DialogueBox.Instance != null)
            {
                DialogueBox.Instance.OnNoMoreLines -= HandleNoMoreLines;
                DialogueBox.Instance.OnNoMoreLines -= HandleBahceSohbetiBitti;
                DialogueBox.Instance.ForceClose();
            }

            LeaveNow();
            return;
        }

        if (!HasArrived) return;

        switch (state)
        {
            case State.Entering:
                if (enteringViaGate)
                {
                    enteringViaGate = false;
                    RestoreNormalBraking();
                    MoveTo(counterPoint.position);
                }
                else state = State.WaitingAtCounter;
                break;

            case State.GoingToShelf:
                PickUpBook();
                break;

            case State.GoingToCounter:
                state = State.WaitingAtCounter;
                break;

            case State.GoingToGarden:
                state = State.SittingInGarden;
                dialogueShown = false; // bahcede tekrar konusulabilsin diye sifirlanir
                if (talkIcon != null) talkIcon.SetActive(true);
                break;

            case State.Leaving:
                if (gatewayPoint != null && !leavingViaGate)
                {
                    leavingViaGate = true;
                    RestoreNormalBraking();
                    if (doorPoint != null && Vector3.Distance(transform.position, doorPoint.position) > 0.15f)
                    {
                        MoveTo(doorPoint.position);
                        break;
                    }
                }
                Destroy(gameObject);
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (dialogueShown) return;

        if (state == State.WaitingAtCounter)
        {
            ShowDialogue();
        }
        else if (state == State.SittingInGarden)
        {
            ShowBahceSohbeti();
        }
    }

    private void ShowDialogue()
    {
        if (dialogueShown) return;
        dialogueShown = true;
        state = State.Talking;

        if (talkIcon != null) talkIcon.SetActive(false);

        // Kamera odagi: hem ozel gun diyalogu hem hikaye konugu icin gecerli.
        bool odaklan = (storyData != null && storyData.focusCamera) ||
                        (konukData != null && konukData.focusCamera);
        Vector2 odakOfseti = storyData != null ? storyData.cameraFocusOffset
                            : (konukData != null ? konukData.cameraFocusOffset : Vector2.zero);

        if (odaklan && CameraFollow.Instance != null)
            CameraFollow.Instance.FocusOn(transform, odakOfseti);

        // Hareketli anlatim (konusurken sag-sol gidip gelme) — ikisinde de desteklenir.
        bool hareketli = (storyData != null && storyData.hareketliAnlatim) ||
                          (konukData != null && konukData.hareketliAnlatim);
        float hareketMesafe = storyData != null ? storyData.hareketMesafesi
                             : (konukData != null ? konukData.hareketMesafesi : 2f);

        if (hareketli)
        {
            if (hareketCoroutine != null) StopCoroutine(hareketCoroutine);
            hareketCoroutine = StartCoroutine(HareketliAnlatimDongusu(hareketMesafe));
        }

        List<DialogueLine> lines = new List<DialogueLine>();

        if (konukData != null && konukData.lines != null)
        {
            lines.AddRange(konukData.lines);
        }
        else if (storyData != null && storyData.lines != null)
        {
            // StoryDialogueData zaten DialogueLine[] tutuyor — dogrudan kullanilir.
            lines.AddRange(storyData.lines);
        }
        else if (dialogueLines != null)
        {
            for (int i = 0; i < dialogueLines.Length; i++)
            {
                DialogueLine line = new DialogueLine();
                line.speakerName = npcName;
                line.text = dialogueLines[i];
                // SESLENDIRME: bu satirlar kod icinde olusturuluyor, cinsiyet/yas grubu ELLE
                // atanmali — atanmazsa struct'in varsayilani (Kadin+Cocuk) kullanilir, ki bu
                // dogru olmayabilir. AktifCinsiyet()/AktifYasGrubu() once konukData.duzenliKimlik'e
                // bakar, yoksa bu controller'daki varsayilan alanlara duser.
                line.konusanCinsiyeti = AktifCinsiyet();
                line.konusanYasGrubu = AktifYasGrubu();
                lines.Add(line);
            }
        }

        // Kapat (X) butonu HICBIR ZAMAN gorunmez — patrondaki gibi, sadece etkilesim butonuyla
        // ilerlenir, satirlar bitince otomatik kapanir (asagidaki OnNoMoreLines aboneligi).
        if (DialogueBox.Instance != null) DialogueBox.Instance.OnNoMoreLines += HandleNoMoreLines;
        DialogueBox.Instance.Show(lines, OnDialogueFinished, false);
    }

    /// <summary>Bahce masasinda otururken tekrar konusulunca cagrilir. Gunun blok satirlarindan
    /// birini gosterir, her zaman kapanis lafi ile biter.</summary>
    private void ShowBahceSohbeti()
    {
        if (dialogueShown) return;
        dialogueShown = true;

        List<DialogueLine> lines = new List<DialogueLine>();

        string[] gunlukSatirlar = StoryManager.Instance != null ? StoryManager.Instance.GetTodayDailyLines() : null;
        if (gunlukSatirlar != null && gunlukSatirlar.Length > 0)
        {
            DialogueLine satir = new DialogueLine();
            satir.speakerName = npcName;
            satir.text = gunlukSatirlar[UnityEngine.Random.Range(0, gunlukSatirlar.Length)];
            // SESLENDIRME: bkz. yukaridaki dialogueLines dongusundeki ayni not.
            satir.konusanCinsiyeti = AktifCinsiyet();
            satir.konusanYasGrubu = AktifYasGrubu();
            lines.Add(satir);
        }

        DialogueLine kapanis = new DialogueLine();
        kapanis.speakerName = npcName;
        // YERELLEŞTIRME: sabit Turkce metin yerine LocalizationManager CSV'sinden ("afor_bahce_kapanis"
        // anahtari) cozulur — anahtar CSV'ye eklenmemisse ya da LocalizationManager sahnede
        // yoksa yine bu Turkce metne duser, hicbir zaman bos/cirkin gorunmez.
        kapanis.text = LokalizasyonYardimcisi.SistemMetniCoz("afor_bahce_kapanis", "Görüşürüz. Sonra konuşuruz.");
        kapanis.konusanCinsiyeti = AktifCinsiyet();
        kapanis.konusanYasGrubu = AktifYasGrubu();
        lines.Add(kapanis);

        if (DialogueBox.Instance != null) DialogueBox.Instance.OnNoMoreLines += HandleBahceSohbetiBitti;
        DialogueBox.Instance.Show(lines, null, false);
    }

    private void HandleNoMoreLines()
    {
        if (DialogueBox.Instance != null)
        {
            DialogueBox.Instance.OnNoMoreLines -= HandleNoMoreLines;
            DialogueBox.Instance.Close();
        }
    }

    private void HandleBahceSohbetiBitti()
    {
        if (DialogueBox.Instance != null)
        {
            DialogueBox.Instance.OnNoMoreLines -= HandleBahceSohbetiBitti;
            DialogueBox.Instance.Close();
        }

        dialogueShown = false; // bahcede tekrar konusulabilsin diye
    }

    private void OnDialogueFinished()
    {
        // Guvenlik: eger Close() ForceClose uzerinden geldiyse (gun sonu zorla kesme), OnNoMoreLines
        // aboneligi hala asili kalmis olabilir — burada da temizle ki baska bir NPC'nin
        // konusmasina yanlislikla karismasin.
        if (DialogueBox.Instance != null) DialogueBox.Instance.OnNoMoreLines -= HandleNoMoreLines;

        // Hareketli anlatim aciksa durdur — konusma bitti, artik sag-sol gidip gelmesin.
        if (hareketCoroutine != null)
        {
            StopCoroutine(hareketCoroutine);
            hareketCoroutine = null;
        }

        // HIKAYE KONUGU: konusma bittiyse artik "tanisilmis" sayilir. Ayarliysa bundan
        // sonra duzenli musteri olarak da gelmeye baslar.
        if (konukData != null && HikayeKonuguYoneticisi.Instance != null)
            HikayeKonuguYoneticisi.Instance.KonukTanisildi(konukData);

        // Diyalog bitince kamera oyuncuya geri doner (hem ozel gun hem konuk icin).
        bool odakVardi = (storyData != null && storyData.focusCamera) ||
                          (konukData != null && konukData.focusCamera);
        if (odakVardi && CameraFollow.Instance != null)
            CameraFollow.Instance.ReturnToPlayer();

        if (isFinale)
        {
            StoryManager.Instance.OnFinaleCompleted();
            LeaveNow();
            return;
        }

        if (willBuy)
        {
            targetShelf = ShopManager.Instance.FindRandomStockedShelf();
            if (targetShelf != null)
            {
                state = State.GoingToShelf;
                MoveTo(targetShelf.ApproachPosition);
                return;
            }
        }

        GoToGardenOrLeave();
    }

    /// <summary>Yapacak baska bir sey kalmayinca (kitap almiyorsa/aldiktan sonra) cagrilir.
    /// Bahce masasi bosta ve rezerve edilebiliyorsa oraya oturur, yoksa normal sekilde ayrilir.</summary>
    private void GoToGardenOrLeave()
    {
        if (BahceYoneticisi.Instance != null && BahceYoneticisi.Instance.AforMasasiniAyir())
        {
            state = State.GoingToGarden;
            if (talkIcon != null) talkIcon.SetActive(false);
            MoveTo(BahceYoneticisi.Instance.AforMasasi.position);
            return;
        }

        LeaveNow();
    }

    private void PickUpBook()
    {
        if (targetShelf != null && targetShelf.TryTakeOne())
        {
            carriedBook = targetShelf.book;

            // Lore karakteri kitabi dogrudan satin alir — kasa onayi gerekmez
            EconomyManager.Instance.AddGold(carriedBook.GetFinalPrice());
            PlayerProgressStats.AddBookSold(carriedBook.rarity);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySell();

            if (ToastNotification.Instance != null)
                ToastNotification.Instance.Show(npcName + " bir kitap aldi.", 2.5f);
        }

        GoToGardenOrLeave();
    }

    private void MoveThroughGate(Vector3 target)
    {
        if (agent != null) { agent.autoBraking = false; agent.stoppingDistance = 0f; }
        MoveTo(target);
    }
    private void RestoreNormalBraking()
    {
        if (agent != null) { agent.autoBraking = true; agent.stoppingDistance = stoppingDistance; }
    }
    private void LeaveNow()
    {
        state = State.Leaving;
        leavingViaGate = false;
        if (talkIcon != null) talkIcon.SetActive(false);
        if (gatewayPoint != null) MoveThroughGate(gatewayPoint.position);
        else if (doorPoint != null) MoveTo(doorPoint.position);

        // Bahce masasindaysa (gun sonu zorla ayrilma gibi durumlarda), masayi geri birak —
        // yoksa Afor bir daha asla bahceye oturamaz (masasi ebediyen "dolu" gorunur).
        if (BahceYoneticisi.Instance != null) BahceYoneticisi.Instance.AforMasasiniBirak();

        // Hareketli anlatim ortasinda zorla ayrilma olursa (orn. gece basladi), coroutine'i
        // durdur — yoksa NPC yok olurken bos yere sag-sol gitmeye devam etmeye calisir.
        if (hareketCoroutine != null)
        {
            StopCoroutine(hareketCoroutine);
            hareketCoroutine = null;
        }

        // Guvenlik agi: eger gun fazi diyalog ACIKKEN degisirse (Update()'teki zorla-ayril
        // dali), OnDialogueFinished() hic calismadan direkt buraya duserdik ve kamera
        // sonsuza kadar bu NPC'ye kilitli kalirdi (obje yok olunca hedefi de kaybederdi).
        // Cagrisi zararsiz: zaten odaklanma yoksa hicbir sey yapmaz.
        if (CameraFollow.Instance != null) CameraFollow.Instance.ReturnToPlayer();
    }

    /// <summary>Afor gibi ozel karakterler icin: konusma boyunca baslangic noktasindan
    /// sag-sol gidip gelir. Kamera zaten transform'u surekli takip ettigi (FocusOn) icin
    /// otomatik olarak onu izlemeye devam eder, ekstra bir sey yapmaya gerek yok.</summary>
    private IEnumerator HareketliAnlatimDongusu(float mesafe)
    {
        Vector3 merkez = transform.position;
        bool sagaGit = true;

        while (state == State.Talking)
        {
            Vector3 hedef = merkez + (sagaGit ? Vector3.right : Vector3.left) * mesafe;
            MoveTo(hedef);
            sagaGit = !sagaGit;

            // Hedefe varana (ya da makul bir sure gecene) kadar bekle
            float gecenSure = 0f;
            const float zamanAsimi = 3f;
            while (!HasArrived && gecenSure < zamanAsimi && state == State.Talking)
            {
                gecenSure += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.4f); // yon degistirmeden once kisa bir mola
        }
    }
}
