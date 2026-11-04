// BahceKonuVerisi.cs — Bahce sohbet sisteminin TUM icerigini tutan asset. Her cumle 1+
// KONUYA etiketlenebilir (coklu etiket = konular arasi DOGAL kopru). ARTIK cevaplar
// OYUNCU SECENEKLERI olarak sunulur: NPC bir sey soyler, oyuncuya 2-4 KISA secenek metni
// gosterilir (NPC'nin cevabini SPOILER etmeyen), oyuncu secer, secime gore NPC devam eder —
// ve secilen cevap FARKLI bir konuya da gecebilir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BahceSecenegi
{
    [Tooltip("SECENEK BUTONUNDA gosterilecek KISA metin — NPC'nin cevabini SPOILER ETMEMELI. Orn: 'Ailesini sor' (cevabin kendisi degil).")]
    public string secenekMetni;

    [Tooltip("Turkce disinda baska dillerde bu secenek metninin karsiligi. Bos birakilabilir.")]
    public List<DilMetniGirisi> ekDiller = new List<DilMetniGirisi>();

    [Tooltip("Bu secenek secilince NPC'nin soyleyecegi cumlenin id'si.")]
    public string hedefCumleId;
}

[System.Serializable]
public class BahceCumlesi
{
    [Tooltip("Benzersiz kimlik — secenekler bunu hedef alir. Orn: 'kitap_01'.")]
    public string id;

    [TextArea(1, 3)]
    public string metin;

    [Header("Yerelleştirme (opsiyonel — bos birakilirsa hep Turkce/'metin' gosterilir)")]
    [Tooltip("Turkce disinda baska dillerde bu cumlenin karsiligi. Bos birakilabilir.")]
    public List<DilMetniGirisi> ekDiller = new List<DilMetniGirisi>();

    [Header("Seslendirme (opsiyonel — bos birakilirsa varsayilan 'blabla' sesi calar)")]
    [Tooltip("Bu cumleye ozel gercek seslendirme klibi (Turkce/Ingilizce). Bos birakilabilir.")]
    public SesKlipSeti ses;

    [Tooltip("Bu cumlenin ait oldugu 1 VEYA DAHA FAZLA konu. Birden fazla konu = digerlerine dogal kopru.")]
    public List<string> konular = new List<string>();

    [Tooltip("Oyuncuya sunulacak secenekler (2-4 tane onerilir). Bos birakilirsa bu cumle bir 'yaprak' olur — konusma orada dogal olarak biter (ya da sonraki 'ilgili konu' aramasina duser).")]
    public List<BahceSecenegi> secenekler = new List<BahceSecenegi>();
}

[CreateAssetMenu(fileName = "BahceKonuVerisi", menuName = "Delilik/Bahce Konu Verisi")]
public class BahceKonuVerisi : ScriptableObject
{
    public List<BahceCumlesi> tumCumleler = new List<BahceCumlesi>();

    [Tooltip("Tanimli konu adlari — YENI CUMLE eklerken buradan SECILIR (serbest yazim yerine). Editor penceresindeki 'Konu Ekle' butonuyla buraya eklenir.")]
    public List<string> tanimliKonular = new List<string>();
}
