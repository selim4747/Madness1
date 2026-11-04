// KaraKitapOzelData.cs — Kara Kitap Satıcısı'nın "KİŞİYE ÖZEL" ödül kitabı. SADECE
// hedefMusteriIsmi'nde belirtilen TEK bir NPC alabilir, o kişinin hikayesiyle alakalıdır
// (bkz. miras alınan loreBilgisi).
//
// TASARLANAN AKIŞ (henüz kodlanmadı — bu sınıf sadece VERİYİ tanımlar):
// oyuncu diyalog panelinden "Kitabı Bildir" der (kitap o an VERİLMEZ) → NPC o ziyarette
// eli boş gider → ERTESİ GÜN geri gelip kitabın TAM FİYATINI (miras alınan 'fiyat',
// müşterinin pazarlık/odemeCarpani indirimi UYGULANMAZ) öder → satın aldıktan sonra
// gorunmemeSuresiGun kadar müşteri havuzunda hiç çıkmaz.
//
// BookData'dan türer: kimlik / kapak / hikaye alanları oradan gelir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "YeniOzelKitap", menuName = "Delilik/Kara Kitap/Kisiye Ozel Kitap")]
public class KaraKitapOzelData : BookData
{
    [Header("Hedef Kişi")]
    [Tooltip("Bu kitabı SADECE alabilecek TEK NPC'nin adı — NPCKimligi.musteriIsmi ile BİREBİR aynı yazılmalı.")]
    public string hedefMusteriIsmi = "";

    [Header("Bildirim Sonrası Bekleme")]
    [Tooltip("Kitap SATIN ALINDIKTAN sonra bu NPC kaç gün müşteri havuzunda görünmeyecek (X).")]
    public int gorunmemeSuresiGun = 5;

    [Header("Kitabı Okuduktan Sonra Açılan Diyaloglar")]
    [Tooltip("Bu kişi kitabını SATIN ALDIKTAN sonra, normal sohbet havuzuna kalıcı olarak eklenecek ekstra satırlar — kitapta okuduklarına dair.")]
    public List<string> kitapSonrasiDiyaloglar = new List<string>();

    // NOT: Fiyat için ayrı bir alan YOK — miras alınan 'fiyat' zaten bu kitabın TAM
    // fiyatı. Satış anını yönetecek kod (henüz yazılmadı), bu kitap türü için
    // müşterinin pazarlık çarpanını (odemeCarpani) UYGULAMAMALI.
}