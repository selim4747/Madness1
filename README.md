# Madness1 — Kullanılmayan Mekanik Hook'ları + Menü Kilit Sistemi

## İçerik

- `Assets/Scripts/MenuUnlockController.cs`  
  Ana menü kilitleri. İstatistikler/Başarımlar kilitsiz. Çok Oyunculu 5. gün. Mod Oluştur 24. gece.

- `Assets/Scripts/Mods/ModContextsExtra.cs`  
  Kitap etkileri, Personel, Bahçe bitki, Özel etkinlik, Tüketilebilir, Dekorasyon context'leri.

- `Assets/Scripts/UnusedMechanicsHooks.cs`  
  Boş hook metodları. Orijinal oyunda hiçbir etki yapmaz.

## Kurulum

1. Bu klasördeki dosyaları Unity projenin `Assets/` altına aynı yollarla kopyala.
2. Ana menü sahnesine boş bir GameObject oluştur, `MenuUnlockController` ekle.
3. Inspector'da butonları ata (requiredDay ve alwaysUnlocked ayarlarını yap).
4. İstersen bir GameObject'e `UnusedMechanicsHooks` ekle (DontDestroyOnLoad ile çalışır).

## Not

Bu kodlar **orijinal oyunda hiçbir mekanik eklemez veya değiştirmez**.  
Sadece modların ileride dinleyebileceği boş context'ler ve menü kilit sistemi sağlar.
