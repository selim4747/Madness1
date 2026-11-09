using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Daha önce konuşulan müşteri geldiğinde küçük bir not düşer.
/// SadikMusteriYoneticisi ile uyumlu, yoksa kendi set'ini tutar.
/// </summary>
public class MusteriTekrarHosgeldin : MonoBehaviour
{
    public static MusteriTekrarHosgeldin Instance { get; private set; }
    private readonly HashSet<string> gorulenler = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void MusteriGeldi(string isim)
    {
        if (string.IsNullOrEmpty(isim)) return;
        if (gorulenler.Contains(isim))
        {
            if (ToastNotification.Instance != null)
                ToastNotification.Instance.Show(isim + " tekrar geldi!", 2f);
        }
        else
        {
            gorulenler.Add(isim);
        }
    }

    public void GunuSifirla() => gorulenler.Clear();
}
