using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    public int stokYenilemeFiyati = 200;
    public int eklenecekKitapMiktari = 30;
    public int yeniRafFiyati = 500;

    // PATRONUN MİMARİSİ: Scriptler artık GetComponentInChildren ile aranıyor.
    public List<ShelfController> AllShelves
    {
        get
        {
            List<ShelfController> list = new List<ShelfController>();
            if (KasaEtkilesim.Instance != null)
            {
                foreach (var raf in KasaEtkilesim.Instance.tumRaflar)
                {
                    if (raf != null)
                    {
                        var c = raf.GetComponentInChildren<ShelfController>();
                        if (c != null) list.Add(c);
                    }
                }
            }
            return list;
        }
    }

    public List<ShelfController> UnlockedShelves
    {
        get
        {
            List<ShelfController> list = new List<ShelfController>();
            if (KasaEtkilesim.Instance != null)
            {
                foreach (var raf in KasaEtkilesim.Instance.tumRaflar)
                {
                    if (raf != null && raf.activeInHierarchy)
                    {
                        var c = raf.GetComponentInChildren<ShelfController>();
                        if (c != null) list.Add(c);
                    }
                }
            }
            return list;
        }
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>Yeni raf acmanin GUNCEL maliyeti — EKONOMİ ENFLASYONU carpani uygulanmis
    /// olarak. Bir UI bu fiyati gostermek isterse yeniRafFiyati yerine BUNU okumali.</summary>
    public int GuncelYeniRafFiyati
    {
        get
        {
            int ham = yeniRafFiyati;
            if (SatinAlmaEnflasyonYoneticisi.Instance != null)
                ham = SatinAlmaEnflasyonYoneticisi.Instance.UygulaVergi(ham);
            return ham;
        }
    }

    public void StokKolisSatinAl()
    {
        if (KasaEtkilesim.Instance == null) return;

        // EKONOMİ SİSTEMİ BAĞLANTISI: Parayı EconomyManager'dan kesiyoruz!
        // NOT: bu kucuk/operasyonel bir harcama oldugu icin EKONOMİ ENFLASYONU vergisine
        // TABI DEGIL — sadece raf/dukkan/yukseltme gibi buyuk yatirimlar vergilenir.
        if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendGold(stokYenilemeFiyati))
        {
            // Şu an dükkanda aktif olarak bulunan / stokta olan kitapları listele
            List<BookData> mevcutKitaplar = new List<BookData>(KasaEtkilesim.Instance.kitapStoklari.Keys);

            if (mevcutKitaplar.Count > 0)
            {
                // PATRONUN YENİ LOJİSTİK SİSTEMİ: Toplam 30 adetlik stoğu, açık olan kitaplar arasında rastgele dağıtıyoruz
                for (int i = 0; i < eklenecekKitapMiktari; i++)
                {
                    BookData rastgeleKitap = mevcutKitaplar[Random.Range(0, mevcutKitaplar.Count)];
                    KasaEtkilesim.Instance.kitapStoklari[rastgeleKitap] += 1;
                }
            }
        }
    }

    public void YeniRafSatinAl()
    {
        if (KasaEtkilesim.Instance == null) return;

        foreach (var rafObj in KasaEtkilesim.Instance.tumRaflar)
        {
            // Kapalı olan (boşta bekleyen) ilk rafı bul
            if (rafObj != null && !rafObj.activeInHierarchy)
            {
                // EKONOMİ SİSTEMİ BAĞLANTISI + EKONOMİ SIKILAŞTIRMA: guncel (enflasyon
                // carpani uygulanmis) fiyat uzerinden Parayı EconomyManager'dan kesiyoruz!
                int fiyat = GuncelYeniRafFiyati;
                if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendGold(fiyat))
                {
                    rafObj.SetActive(true);

                    if (SatinAlmaEnflasyonYoneticisi.Instance != null)
                        SatinAlmaEnflasyonYoneticisi.Instance.SatinAlmaKaydet();

                    ShelfController c = rafObj.GetComponentInChildren<ShelfController>();
                    if (c != null && KasaEtkilesim.Instance.oyundakiTumKitaplar.Count > 0)
                    {
                        // 1. ADIM: Raflarda OLMAYAN (hiç keşfedilmemiş) kitapları filtrele
                        List<BookData> kullanilmamisKitaplar = new List<BookData>();
                        foreach (var kitap in KasaEtkilesim.Instance.oyundakiTumKitaplar)
                        {
                            if (!KasaEtkilesim.Instance.kesfedilenKitaplar.Contains(kitap))
                            {
                                kullanilmamisKitaplar.Add(kitap);
                            }
                        }

                        BookData secilenKitap = null;

                        // 2. ADIM: Eğer depoda yepyeni kitap kaldıysa, onlardan rastgele birini seç!
                        if (kullanilmamisKitaplar.Count > 0)
                        {
                            secilenKitap = kullanilmamisKitaplar[Random.Range(0, kullanilmamisKitaplar.Count)];
                        }
                        // 3. ADIM (GÜVENLİK SİGORTASI): Oyun çökmesin diye mecburen eski kitaplardan birini koyar.
                        else
                        {
                            secilenKitap = KasaEtkilesim.Instance.oyundakiTumKitaplar[Random.Range(0, KasaEtkilesim.Instance.oyundakiTumKitaplar.Count)];
                            Debug.Log("Patron, oyundaki tüm farklı kitapları tükettik! Mecburen aynı kitabı koydum. Acil yeni kitap yaz!");
                        }

                        // 4. ADIM: Seçilen eşsiz kitabı rafa yerleştir ve açılış stoğunu ver
                        c.KitapAta(secilenKitap);

                        if (!KasaEtkilesim.Instance.kitapStoklari.ContainsKey(secilenKitap))
                            KasaEtkilesim.Instance.kitapStoklari[secilenKitap] = 0;

                        KasaEtkilesim.Instance.kitapStoklari[secilenKitap] += 15;

                        // Kitabı keşfedilenlere ekle ki bir sonraki rafta tekrar çıkmasın!
                        if (!KasaEtkilesim.Instance.kesfedilenKitaplar.Contains(secilenKitap))
                            KasaEtkilesim.Instance.kesfedilenKitaplar.Add(secilenKitap);
                    }
                }
                break; // İlk rafı bulup işlemi yaptıktan veya para yetmediğini anladıktan sonra döngüden çık.
            }
        }
    }

    public int GetTotalStock()
    {
        if (KasaEtkilesim.Instance == null) return 0;
        int toplam = 0;
        foreach (var stok in KasaEtkilesim.Instance.kitapStoklari.Values) toplam += stok;
        return toplam;
    }

    public void DistributeRandomCopies(int amount = 1) { StokKolisSatinAl(); }

    public ShelfController FindRandomStockedShelf()
    {
        List<ShelfController> list = UnlockedShelves;
        if (list.Count > 0) return list[Random.Range(0, list.Count)];
        return null;
    }
    public void ApplySave(SaveData data) { }
    public void WriteToSave(SaveData currentData) { }
}
