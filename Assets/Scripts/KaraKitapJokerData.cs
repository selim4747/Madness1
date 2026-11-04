// KaraKitapJokerData.cs — Kara Kitap Satıcısı'nın "JOKER" ödül kitabı. Türsüzdür,
// HERHANGİ bir müşteriye normal bir kitap kadar kolay satılabilir — özel bir bildirim
// ya da bekleme GEREKMEZ, Özel Raf'a düşer düşmez satışa hazırdır.
// BookData'dan türer: kimlik / kapak / hikaye / fiyat alanları oradan gelir, ekstra
// ayar YOK — bu sınıfın var olması, "bu kitap joker" bilgisinin kendisidir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

[CreateAssetMenu(fileName = "YeniJokerKitap", menuName = "Delilik/Kara Kitap/Joker Kitap")]
public class KaraKitapJokerData : BookData
{
}