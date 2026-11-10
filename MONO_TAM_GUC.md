# Mono Derlemede Tam Güçlü Modlar

Bu proje iki şekilde derlenebilir:

| Derleme | Play Store | DLL Modları | Sınır |
|---------|------------|-------------|-------|
| **IL2CPP** | Evet | Hayır | Sadece config + AssetBundle |
| **Mono** | Hayır | **Evet** | **Yok** |

## Mono'da modlar ne yapabilir?
- Yeni `MonoBehaviour` ve sistemler eklemek
- Mevcut her mekaniği değiştirmek (hasar, satış, spawn, diyalog, faz…)
- AssetBundle'dan sahne, prefab, karakter yüklemek
- Kendi kayıt verisini tutmak
- `totalConversion` ile tamamen yeni oyun başlatmak
- İstediğin C# kodunu çalıştırmak

## Mod nasıl yazılır?
1. Class Library (.NET Standard 2.1) oluştur.
2. `ModModuleBase`'den türet.
3. `OnRegister` içinde `Subscribe`, `LoadAsset`, `RegisterContent` kullan.
4. DLL'i `Mod.dll` olarak mod klasörüne koy.
5. `manifest.json` içinde `"entryScript": "Mod.dll"`.

Örnek: `ExampleMods/full_power_example/`

## IL2CPP sürümünden Mono sürümüne yönlendirme
IL2CPP (Play Store) sürümünde istersen bir butonla Mono APK/AAB indirme linki verebilirsin.
Mono sürümü sideload veya alternatif mağaza için kullanılır.
Mod sistemi her iki derlemede de aynı koddur; sadece DLL yükleme Mono'da aktiftir.
