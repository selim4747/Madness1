using UnityEngine;
using System;

/// <summary>
/// Günlük mini hedefler. Sahnede yoksa hiçbir şey olmaz.
/// </summary>
public class GunlukHedefYoneticisi : MonoBehaviour
{
    public static GunlukHedefYoneticisi Instance { get; private set; }

    [Header("Hedefler")]
    public int hedefKitapSat = 5;
    public int hedefSohbet = 3;
    public int hedefHirsiz = 1;

    [Header("Ödül")]
    public int odulAltin = 50;

    public int satilanKitap { get; private set; }
    public int sohbetSayisi { get; private set; }
    public int yakalananHirsiz { get; private set; }
    public bool odulAlindi { get; private set; }

    public event Action OnHedefGuncellendi;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void KitapSatildi()
    {
        satilanKitap++;
        KontrolEt();
    }

    public void SohbetEdildi()
    {
        sohbetSayisi++;
        KontrolEt();
    }

    public void HirsizYakalandi()
    {
        yakalananHirsiz++;
        KontrolEt();
    }

    private void KontrolEt()
    {
        OnHedefGuncellendi?.Invoke();
        if (odulAlindi) return;
        if (satilanKitap >= hedefKitapSat && sohbetSayisi >= hedefSohbet && yakalananHirsiz >= hedefHirsiz)
        {
            odulAlindi = true;
            if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(odulAltin);
            if (ToastNotification.Instance != null)
                ToastNotification.Instance.Show("Günlük hedefler tamam! +" + odulAltin + " Altın", 3f);
        }
    }

    public void GunuSifirla()
    {
        satilanKitap = 0;
        sohbetSayisi = 0;
        yakalananHirsiz = 0;
        odulAlindi = false;
        OnHedefGuncellendi?.Invoke();
    }
}
