# Mod Sistemi Tamamlama Notu

Önceki aşamada yalnızca çekirdek ve paket içe aktarma vardı.  
Bu güncelleme ile **modlar gerçekten çalışır**.

## Eklenenler
- `ModScriptLoader.cs` — `entryScript` (veya varsayılan Mod.dll) yükler, `IModModule` bulur ve kaydeder.
- `ModModuleBase.cs` — mod yazarları için kolay taban sınıf.
- `ModConfigApplier.cs` — `config.json` ile kod yazmadan temel mekanik değişiklikleri.
- `ModRuntime` artık tarama sonrası otomatik olarak script + config yükler.

## Sonuç
- Mevcut mekanikler değiştirilebilir (hasar, satış, para, gece, spawn vb.).
- C# DLL modları yeni mekanikler ekleyebilir.
- Data-only modlar `config.json` ile çalışır.
- Paket içe aktarma (Android file picker dahil) önceki gibi çalışır.

Vanilla (mod yok) davranış korunur.
