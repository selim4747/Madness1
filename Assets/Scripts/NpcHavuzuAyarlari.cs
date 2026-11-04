// NpcHavuzuAyarlari.cs — Hizli NPC Olusturucu'nun TUM havuzlarini KALICI olarak sakladigi
// asset. Metin havuzlari (isim/soyad/meslek/diyalog) editor penceresinden hizlica duzenlenir.
// Sprite havuzlari (kiyafet) ise BU ASSET'IN KENDI Inspector'indan surukle-birak ile
// doldurulur (Sprite referanslari metin kutusuna sigmaz, bu normal Unity davranisidir).
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NpcHavuzuAyarlari", menuName = "Delilik/NPC Havuzu Ayarlari")]
public class NpcHavuzuAyarlari : ScriptableObject
{
    [Header("Kadın İsim Havuzu")]
    public List<string> kadinIsimleri = new List<string>();

    [Header("Erkek İsim Havuzu")]
    public List<string> erkekIsimleri = new List<string>();

    [Header("Soyad Havuzu")]
    public List<string> soyadlar = new List<string>();

    [Header("Meslek Havuzu")]
    public List<string> meslekler = new List<string>();

    [Header("Diyalog Havuzu (mağaza içi, tüm NPC'ler için ortak)")]
    [TextArea(1, 3)]
    public List<string> diyaloglar = new List<string>();

    // NOT: "Bahçe Diyalog Havuzu" buradan KALDIRILDI — bahçe sohbeti artık
    // BahceKonuVerisi/BahceSohbetYoneticisi (paylaşımlı konu grafiği) sisteminden geliyor.
    // Varsa eski bahceDiyaloglari verini BahceKonuVerisi asset'ine elle taşı (bkz. sohbet).

    [Header("Kişilik Çarpanı Aralıkları (rastgele bu aralıktan seçilir)")]
    [Tooltip("Sabir carpani araligi. 1 = normal (30sn bekler). Kucuk = cabuk sinirlenir, buyuk = sabirli.")]
    public float sabirCarpaniMin = 0.7f;
    public float sabirCarpaniMax = 1.5f;

    [Tooltip("Odeme carpani araligi. 1 = tam fiyat. Kucuk = pazarlikci, buyuk = comert.")]
    public float odemeCarpaniMin = 0.8f;
    public float odemeCarpaniMax = 1.3f;

    [Tooltip("Inceleme suresi carpani araligi. 1 = normal (5sn). Buyuk = uzun uzun inceler.")]
    public float incelemeCarpaniMin = 0.7f;
    public float incelemeCarpaniMax = 1.4f;

    [Header("Kıyafet Havuzu (SÜRÜKLE-BIRAK — bu alan metin kutusunda değil, doğrudan burada doldurulur)")]
    [Tooltip("Kadin karakterlere rastgele atanacak kiyafet/govde sprite'lari.")]
    public List<Sprite> kadinKiyafetHavuzu = new List<Sprite>();
    [Tooltip("Erkek karakterlere rastgele atanacak kiyafet/govde sprite'lari.")]
    public List<Sprite> erkekKiyafetHavuzu = new List<Sprite>();

    [Header("Meslek → Tür Eşleşmesi (opsiyonel, hafif temalı dağılım için)")]
    [Tooltip("Format: 'Meslek: Tur1, Tur2' — her satir bir meslegi 1+ turle iliskilendirir. Bu meslek secilince, sevilenTurler listesine ONCELIKLE buradan bir tur eklenir, kalanlar rastgele tamamlanir. Eslesmesi olmayan meslekler tamamen rastgele kalir.")]
    [TextArea(1, 3)]
    public List<string> meslekTurEslesmeleri = new List<string>();
}