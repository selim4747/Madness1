// OgrenciCalismaMasalari.cs — 2. Duzey ogrencilerin kitap okumak icin oturdugu masalar.
// StudentDeskController'dan (3. Duzey, kitap TESLIMAT sistemi) TAMAMEN AYRI ve daha basit:
// burada sadece "oturacak bos bir yer var mi" kontrolu var, teslimat/talep sistemi yok.
// BahceYoneticisi'nin masa ayirma mantigiyla ayni desen.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class OgrenciCalismaMasalari : MonoBehaviour
{
    public static OgrenciCalismaMasalari Instance { get; private set; }

    [Header("Calisma Masalari")]
    [Tooltip("2. Duzey ogrencilerin oturup kitap okuyacagi noktalar.")]
    [SerializeField] private List<Transform> masalar = new List<Transform>();

    [Header("Masa Bekleme Suresi")]
    [Tooltip("Ogrencinin masada oturup kitap okurken bekleyecegi sure (saniye).")]
    [SerializeField] private float masaBeklemeSuresi = 20f;
    public float MasaBeklemeSuresi { get { return masaBeklemeSuresi; } }

    private readonly HashSet<Transform> doluMasalar = new HashSet<Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Bos bir masa ayirmayi dener. Bulursa dolu isaretler ve dondurur, yoksa null doner.</summary>
    public Transform MasaAyir()
    {
        for (int i = 0; i < masalar.Count; i++)
        {
            Transform m = masalar[i];
            if (m != null && !doluMasalar.Contains(m))
            {
                doluMasalar.Add(m);
                return m;
            }
        }
        return null; // hepsi dolu
    }

    /// <summary>Ogrenci kalkinca masayi serbest birakir.</summary>
    public void MasayiBirak(Transform masa)
    {
        if (masa != null) doluMasalar.Remove(masa);
    }
}