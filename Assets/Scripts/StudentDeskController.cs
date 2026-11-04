// StudentDeskController.cs — Tek bir ogrenci masasinin gorev sistemi (instance-based)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class StudentDeskController : MonoBehaviour
{
    [Header("Kimlik")]
    [Tooltip("Kayit ve UI listesinde bu masayi tanimlayan sabit numara (1, 2, 3...). Iki masaya ayni numarayi verme!")]
    [SerializeField] private int deskIndex = 1;

    [Header("Acilis")]
    [SerializeField] private int unlockCost = 400;
    [SerializeField] private GameObject deskVisual;
    [SerializeField] private GameObject lockedVisual;

    [Header("Talep")]
    [SerializeField] private int minBooks = 2;
    [SerializeField] private int maxBooks = 4;
    [SerializeField] private float requestTimeoutSeconds = 90f;
    [SerializeField] private int rewardPerBook = 150;

    [Header("Bildirim")]
    [SerializeField] private GameObject returnPendingIcon;

    public int DeskIndex { get { return deskIndex; } }
    public int UnlockCost { get { return unlockCost; } }
    public bool IsUnlocked { get; private set; }
    public bool HasActiveRequest { get; private set; }
    public List<ShelfController> RequestedShelves { get { return requested; } }

    /// <summary>Henuz TESLIM EDILMEMIS (hala beklenen) raflar — Istenen Kitaplar panelinde
    /// oyuncuya "daha ne getirmen lazim" gostermek icin. RequestedShelves'ten farkli olarak
    /// zaten getirilmis olanlari icermez.</summary>
    public List<ShelfController> BeklenenRaflar
    {
        get
        {
            List<ShelfController> liste = new List<ShelfController>();
            for (int i = 0; i < requested.Count; i++)
                if (!delivered.Contains(requested[i])) liste.Add(requested[i]);
            return liste;
        }
    }

    /// <summary>Bu masanin talep durumu degistiginde tetiklenir (UI/HUD dinleyebilir).</summary>
    public event Action OnRequestChanged;

    private readonly List<ShelfController> requested = new List<ShelfController>();
    private readonly List<ShelfController> delivered = new List<ShelfController>();
    private Coroutine timeoutRoutine;

    private void Start()
    {
        RefreshVisuals();
    }

    /// <summary>MarketUI / StudentDeskUnlockUI'daki "Ac" butonundan cagrilir.</summary>
    public bool TryUnlock()
    {
        if (IsUnlocked) return false;

        if (!EconomyManager.Instance.TrySpendGold(unlockCost))
        {
            ToastNotification.Instance.Show("Yeterli paran yok.", 2.5f);
            return false;
        }

        IsUnlocked = true;
        RefreshVisuals();
        AudioManager.Instance.PlayPurchase();
        ToastNotification.Instance.Show(deskIndex + ". Ogrenci Masasi acildi!", 3f);
        return true;
    }

    /// <summary>Kayittan yuklerken kullanilir — para harcamadan dogrudan durumu uygular.</summary>
    public void ApplyUnlockState(bool unlocked)
    {
        IsUnlocked = unlocked;
        RefreshVisuals();
    }

    /// <summary>StudentDeskManager tarafindan cagrilir — bu masaya yeni bir talep olusturur.</summary>
    public void CreateRequest()
    {
        if (!IsUnlocked || HasActiveRequest) return;

        List<ShelfController> stocked = new List<ShelfController>();
        List<ShelfController> all = ShopManager.Instance.UnlockedShelves;

        for (int i = 0; i < all.Count; i++)
        {
            // Stokta olmali VE su an baska bir masa tarafindan istenmiyor olmali
            if (all[i].HasStock && !StudentDeskManager.Instance.IsShelfRequestedByAnyDesk(all[i]))
                stocked.Add(all[i]);
        }

        if (stocked.Count < minBooks) return;   // yeterli cesit/musait raf yok

        // Karistir ve 2-4 tane sec
        for (int i = 0; i < stocked.Count; i++)
        {
            int r = Random.Range(i, stocked.Count);
            ShelfController tmp = stocked[i];
            stocked[i] = stocked[r];
            stocked[r] = tmp;
        }

        int count = Mathf.Min(Random.Range(minBooks, maxBooks + 1), stocked.Count);

        requested.Clear();
        delivered.Clear();

        for (int i = 0; i < count; i++)
        {
            requested.Add(stocked[i]);
            stocked[i].SetGlow(true);   // raf parlar
        }

        HasActiveRequest = true;
        if (OnRequestChanged != null) OnRequestChanged();

        ToastNotification.Instance.Show(deskIndex + ". masadaki ogrenci " + count + " kitap istiyor!", 3f);

        if (timeoutRoutine != null) StopCoroutine(timeoutRoutine);
        timeoutRoutine = StartCoroutine(TimeoutCountdown());
    }

    /// <summary>PlayerInteraction tarafindan cagrilir. Kabul edildiyse true.</summary>
    public bool DeliverBook(BookData book, ShelfController fromShelf)
    {
        if (!HasActiveRequest) return false;
        if (book == null || fromShelf == null) return false;
        if (!requested.Contains(fromShelf)) return false;
        if (delivered.Contains(fromShelf)) return false;

        delivered.Add(fromShelf);
        fromShelf.SetGlow(false);

        if (OnRequestChanged != null) OnRequestChanged();

        if (delivered.Count >= requested.Count) CompleteRequest();
        return true;
    }

    private void CompleteRequest()
    {
        if (timeoutRoutine != null) { StopCoroutine(timeoutRoutine); timeoutRoutine = null; }

        int reward = rewardPerBook * delivered.Count;
        EconomyManager.Instance.AddGold(reward);
        SatisfactionTracker.Instance.RecordPositiveEvent();
        PlayerProgressStats.AddStudentServed();

        ToastNotification.Instance.Show(deskIndex + ". masadaki ogrenci memnun ayrildi! +" + reward + " Para", 3f);

        // Kitaplar masada — oyuncunun raflara geri koymasi beklenir
        if (returnPendingIcon != null) returnPendingIcon.SetActive(true);

        StartCoroutine(ReturnBooksAfterDelay());
    }

    /// <summary>Ogrenci gidince kitaplar raflara geri doner.</summary>
    private IEnumerator ReturnBooksAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        for (int i = 0; i < delivered.Count; i++)
            if (delivered[i] != null) delivered[i].ReturnOne();

        ClearRequest();
        if (returnPendingIcon != null) returnPendingIcon.SetActive(false);
    }

    private IEnumerator TimeoutCountdown()
    {
        yield return new WaitForSeconds(requestTimeoutSeconds);

        // Sure doldu — ogrenci eli bos doner (kalici ceza yok)
        ToastNotification.Instance.Show(deskIndex + ". masadaki ogrenci bekleyemedi, eli bos dondu.", 3f);

        // Teslim edilenleri raflara geri koy
        for (int i = 0; i < delivered.Count; i++)
            if (delivered[i] != null) delivered[i].ReturnOne();

        ClearRequest();
        timeoutRoutine = null;
    }

    private void ClearRequest()
    {
        for (int i = 0; i < requested.Count; i++)
            if (requested[i] != null) requested[i].SetGlow(false);

        requested.Clear();
        delivered.Clear();
        HasActiveRequest = false;

        if (OnRequestChanged != null) OnRequestChanged();
    }

    private void RefreshVisuals()
    {
        if (deskVisual != null) deskVisual.SetActive(IsUnlocked);
        if (lockedVisual != null) lockedVisual.SetActive(!IsUnlocked);
        if (returnPendingIcon != null) returnPendingIcon.SetActive(false);
    }
}