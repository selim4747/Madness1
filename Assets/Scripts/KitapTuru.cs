public enum KitapTuru
{
    Roman,
    Tarih,
    Felsefe,
    Polisiye,
    Fantastik,
    Bilim,
    Korku,

    // DIKKAT: Yeni turler HER ZAMAN listenin SONUNA eklenmeli. Araya eklersen, mevcut
    // kitap asset'lerinin turleri kayar (orn. "Korku" olan kitap birden "Siir" olur),
    // cunku Unity enum'lari SIRA NUMARASI olarak kaydeder.
    Siir,
    Biyografi,
    CizgiRoman
}

public enum MusteriTipi
{
    Normal,
    Zengin,
    VIP
}

/// <summary>NPCKimligi'nin cinsiyeti — govde gorseli ve isim havuzu secimi bunu kullanir.</summary>
public enum Cinsiyet
{
    Kadin,
    Erkek
}