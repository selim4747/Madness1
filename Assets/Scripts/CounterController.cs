// CounterController.cs — Kasa: bekleyen musteri kuyrugu ve anlik satis
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;
using System.Collections.Generic;

public class CounterController : MonoBehaviour
{
    public static CounterController Instance { get; private set; }

    [Header("Kuyruk")]
    [SerializeField] private Transform[] queuePoints;
    [SerializeField] private float confirmRange = 1.8f;

    private readonly List<CustomerController> waiting = new List<CustomerController>();

    // Kasaya YOLDA olan (henuz fiziksel olarak varmamis) musteriler. RegisterWaitingCustomer'a
    // gore ayri tutulur ki herkes kendi sira noktasina yurusun, kasanin tam ortasinda ust uste
    // yigilmasinlar (eskiden hepsi kasa.transform.position'a yuruyordu).
    private readonly List<CustomerController> enRoute = new List<CustomerController>();

    /// <summary>CustomerController.GoToCounter() kasaya yola cikarken cagirir — henuz varmadan
    /// once bile cakismasin diye kendine ait bos bir sira noktasi ayirtir.</summary>
    public Vector3 GetApproachPoint(CustomerController customer)
    {
        if (!enRoute.Contains(customer) && !waiting.Contains(customer)) enRoute.Add(customer);

        if (queuePoints != null && queuePoints.Length > 0)
        {
            int index = waiting.Count + enRoute.IndexOf(customer);
            if (index < 0 || index >= queuePoints.Length) index = queuePoints.Length - 1;
            return queuePoints[index].position;
        }

        // Sira noktalari Inspector'dan ayarlanmamissa eski davranisa (kasanin kendisi) duser —
        // tek nokta oldugu icin yigilma riski var, queuePoints atamak sart.
        return transform.position;
    }

    /// <summary>Musteri kasaya varmadan vazgecerse (isyan/silinme vs.) yoldaki kaydini temizler.</summary>
    public void CancelApproach(CustomerController customer)
    {
        enRoute.Remove(customer);
    }

    public int WaitingCount { get { return waiting.Count; } }

    /// <summary>Siradaki (en onde bekleyen) musteri — vezne ekraninda bilgilerini gostermek icin.</summary>
    public CustomerController SiradakiMusteri
    {
        get { return waiting.Count > 0 ? waiting[0] : null; }
    }
    public bool HasWaitingCustomer { get { return waiting.Count > 0; } }

    public event Action<CustomerController, bool> OnFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterWaitingCustomer(CustomerController customer)
    {
        enRoute.Remove(customer); // artik yolda degil, resmen kuyruga giriyor
        if (waiting.Contains(customer)) return;

        waiting.Add(customer);
        customer.OnFinished += HandleCustomerFinished;

        // Müşteri sıraya girdiği an saatini kaydeder!
        customer.CounterArrivalTime = Time.time;

        RepositionQueue();
    }

    private void HandleCustomerFinished(CustomerController customer, bool served)
    {
        customer.OnFinished -= HandleCustomerFinished;
        waiting.Remove(customer);
        enRoute.Remove(customer);
        RepositionQueue();
    }

    private void RepositionQueue()
    {
        if (queuePoints == null || queuePoints.Length == 0) return;

        for (int i = 0; i < waiting.Count && i < queuePoints.Length; i++)
            waiting[i].MoveTo(queuePoints[i].position);
    }

    /// <summary>PlayerInteraction tarafindan cagrilir. Menzildeki en yakin musteriyi onaylar.</summary>
    public bool TryConfirmNearest(Vector3 playerPosition)
    {
        CustomerController nearest = GetNearestInRange(playerPosition);
        if (nearest == null) return false;

        BookData book = nearest.CarriedBook;

        // YENİ DEV YAPAY ZEKA: Müşterinin elinde kitap yoksa şikayet etmeye gelmiştir!
        if (book == null)
        {
            DevLog.Info($"🗣️ Kasiyer: Kusura bakmayın {(nearest.SecilenKimlik != null ? nearest.SecilenKimlik.musteriIsmi : "efendim")}, o türde kitabımız kalmamış!");
            nearest.LeaveUnsatisfied(bahceDenensin: true); // Şikayetini dinledik, adamı yolladık — bahçe hakkı olsun
            return true;
        }

        // SATIR 69 ÇÖKME ZIRHLARI (Manager'lar yoksa bile oyun patlamaz!)
        int price = book.GetFinalPrice();

        // KISILIK: comert musteri fazla, pazarlikci az oder.
        // NOT: KaraKitapOzelData (kisiye ozel kitap) icin bu carpan UYGULANMAMALI —
        // "fiyati ne olursa olsun bariyer degil, tam fiyat oder" tasarim karari henuz
        // burada kodlanmadi (Kara Kitap Satici satis akisi kurulunca eklenecek).
        if (nearest.SecilenKimlik != null)
            price = Mathf.RoundToInt(price * Mathf.Max(0.1f, nearest.SecilenKimlik.odemeCarpani));

        if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(price);
        else DevLog.Warning("💸 EconomyManager sahnede yok, para havaya uçtu!");

        FloatingText.Spawn(nearest.transform.position, "+" + price, FloatingText.Renk.Altin);

        float waited = Time.time - nearest.CounterArrivalTime;
        if (SatisfactionTracker.Instance != null) SatisfactionTracker.Instance.RecordSale(waited);

        // Statik sınıflar doğrudan çağrılır
        PlayerProgressStats.AddBookSold(book.rarity);

        // OZEL RAF sayaclari: bu kitap joker mi, yoksa kisiye ozel mi?
        if (book is KaraKitapJokerData) PlayerProgressStats.AddJokerBookSold();
        else if (book is KaraKitapOzelData ozelKitap)
        {
            PlayerProgressStats.AddUniqueBookSold();

            // Kisiye ozel kitap satin alinince, o kisinin normal sohbet havuzuna kitapla
            // ilgili ekstra diyaloglar kalici olarak eklenir.
            if (OzelKitapDiyaloglariYoneticisi.Instance != null && nearest.SecilenKimlik != null)
                OzelKitapDiyaloglariYoneticisi.Instance.KitapSatildi(ozelKitap, nearest.SecilenKimlik.musteriIsmi);
        }

        // SARHOŞ KİTAP (çılgın özellik): hangi Kara Kitap satılırsa satılsın (joker ya da
        // özel), küçük bir ihtimalle sarhoş efekti tetiklenir.
        if ((book is KaraKitapJokerData || book is KaraKitapOzelData) && SarhosKitapEfekti.Instance != null)
            SarhosKitapEfekti.Instance.SansliTetikle();

        // VIP musteriye satis yapildiysa ayri sayaca da eklenir (CustomerData.preferredRarity
        // Legendary/Mythic ise MusteriTipi.VIP sayilir — HikayeKonuguYoneticisi ile ayni kural).
        if (nearest.Profile != null &&
            (nearest.Profile.preferredRarity == Rarity.Legendary || nearest.Profile.preferredRarity == Rarity.Mythic))
        {
            PlayerProgressStats.AddVIPCustomerServed();
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySell();
        else DevLog.Warning("🔊 AudioManager sahnede yok, ses çalınamadı!");

        nearest.CompleteSale();
        return true;
    }

    /// <summary>Etkilesim butonunun aktif olup olmayacagini UI'a bildirir.</summary>
    public bool IsAnyCustomerInRange(Vector3 playerPosition)
    {
        return GetNearestInRange(playerPosition) != null;
    }

    private CustomerController GetNearestInRange(Vector3 playerPosition)
    {
        CustomerController nearest = null;
        float best = confirmRange;

        for (int i = 0; i < waiting.Count; i++)
        {
            CustomerController c = waiting[i];

            // İŞTE KÖRLÜĞÜ BİTİRDİĞİMİZ YER! Kasiyer artık eli boş adamı da görüyor.
            // Eski kod: if (c == null || c.CarriedBook == null) continue;
            if (c == null) continue;

            float d = Vector3.Distance(playerPosition, c.transform.position);
            if (d <= best)
            {
                best = d;
                nearest = c;
            }
        }
        return nearest;
    }

}