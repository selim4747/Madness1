# Mod Oluşturma, Oyuna Entegre Etme ve Derleme Rehberi

Bu rehber iki ayrı dağıtım yolunu açıklar:

1. **Oyuna gömülü ve menüden seçilebilir modlar:** C# kaynak kodu oyuna eklenir, oyunla beraber derlenir. IL2CPP/Play Store ve Mono/itch.io için kullanılabilir. Oyuncu mod seçmezse oyun vanilla davranır.
2. **Sonradan kurulan C# DLL modları:** Yalnızca Mono masaüstü sürümünde yüklenir. IL2CPP/Android’de dış DLL çalıştırılmaz. Mevcut projede DLL modları `manifest.json` içindeki `enabled` değeriyle etkin/pasif olur; DLL’ler için hazır bir oyun içi aç/kapat listesi yoktur.

## Önemli: mevcut proje durumu

İşlenmiş proje arşivinde `BuiltInModCatalog`, `BuiltInModSelectionUI` ve mod seçilince yeniden yükleme altyapısı vardır. Catalog'daki yeni gömülü modlar **ilk açılışta kapalıdır**. Ancak Unity sahnesine henüz seçim ekranı veya menü butonu yerleştirilip bağlanmış değildir; bunu aşağıdaki bölümde yapacaksın.

Night Anomalies kaynak örneği `ExampleMods/NightAnomalies/` altındadır. Bu örnek dışarıdan DLL olarak yüklenmek üzere hazırlanmıştır; otomatik olarak gömülü mod kataloğuna kaydolmaz. Gömülü seçilebilir sürüm yapmak için kodu `Assets` içine taşıyıp katalog kaydını eklemek gerekir. İki dağıtım yolunun mod dosyaları ve yükleyicileri farklıdır.

---

# A. Oyuncunun seçtiği veya vanilla oynadığı sürüm

## A1. Menüye mod seçme ekranı ekle

1. `Assets/Scenes/AnaMenu.unity` sahnesini aç.
2. Canvas altında mod seçimi için bir Panel oluştur. Üzerine başlık koy (ör. **Modlar**).
3. Panel içine bir **Scroll View** ekle. İçindeki `Content` RectTransform'unu `BuiltInModSelectionUI` bileşeninin **List Root** alanına sürükle.
4. `Content` nesnesine `VerticalLayoutGroup` ve `ContentSizeFitter` ekle. `Vertical Fit = Preferred Size` yap. Butonların düzgün sıralanması için Scroll View kullanılması önerilir.
5. Mod panelindeki bir GameObject'e `BuiltInModSelectionUI` component'ini ekle.
6. İsteğe bağlı durum metni için bir Legacy UI Text oluşturup component'in **Status Text** alanına sürükle. Bileşen mod başına aç/kapat butonlarını kendisi üretir.
7. Panel başlangıçta görünmesin istiyorsan Panel'i inactive yap. Ana menüye bir **Modlar** Button ekle. Button'ın `OnClick()` listesine panel GameObject'ini sürükleyip `GameObject.SetActive(true)` seç.
8. Mod paneline bir **Geri/Devam** Button ekle; `OnClick()` üzerinden panel GameObject'ini `SetActive(false)` yap. Oyuncu seçimini yaptıktan sonra normal ana menüde Yeni Oyun/Yükle akışına döner.
9. Unity UI `Button` bileşeni kullandığından `UI > Button (Legacy)` veya `UnityEngine.UI.Button` kullanan standart Button kullan. Sahneye EventSystem ekli değilse `UI > Event System` oluştur.
10. Sahneyi kaydet. Play modunda aç/kapat, yeni oyun başlatmadan menüye geri dönerek seçimin kaybolmadığını doğrula.

`BuiltInModSelectionUI` listeyi etkin olduğu ilk seferde üretir. Panel açılınca bileşenin ilk `Start()` çağrısı çalışır. Seçim `PlayerPrefs` içinde saklanır ve değişiklik etkin oyun sistemlerine anında uygulanır; üretim sürümünde mod seçimini ana menüde yapmak tercih edilir.

## A2. Mod seçilmemişse vanilla davranış

- Yeni mod kataloğunda `IsEnabled()` ilk kullanım için `false` döndürür.
- Gömülü mod `Register()` edilmezse event kancaları da kaydolmaz; modun değiştirdiği mekanikler çalışmaz.
- Oyuncu hiçbir mod butonunu açmadan **Geri/Devam** ile çıkar ve Yeni Oyun/Yükle seçerse standart oyun akışı sürer.
- Oyuncu isterse bir veya birkaç modu açar; modlar seçimi kaydedilir ve sonraki açılışta korunur.
- Test sırasında eski PlayerPrefs seçimi varsa, daha önce açılmış modlar açık kalabilir. Temiz ilk kurulum testi için PlayerPrefs'i temizle veya oyunu farklı Company/Product Name ile çalıştır.

## A3. Yeni bir seçilebilir C# modu projeye ekle

Gömülü modlar için kaynak kodu Unity projesinde derlenebilir olmalıdır:

1. Modun `IModModule` uygulamasını `Assets/Scripts/Mods/` veya `Assets/Scripts/` altına `.cs` olarak ekle. `ModModuleBase` kullanmak event aboneliklerini ve kapatma işini kolaylaştırır.
2. Mod `Id` değeri benzersiz, sabit ve küçük harfli bir kimlik olsun. `OnRegister` içinde hook'ları kaydet; `OnUnregister` içinde oluşturduğu coroutine, GameObject, event veya diğer kaynakları temizle.
3. Aşağıdaki gibi bir katalog kaydı ekle. Kayıt, oyun başlamadan önce çalışmalıdır:

```csharp
using Madness.Mods;
using UnityEngine;

public static class ReleasedMods
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterMods()
    {
        BuiltInModCatalog.Register(
            "nightmare_nights",
            "Kâbus Geceleri",
            "1.0.0",
            () => new NightmareNightsMod());
    }
}
```

`NightmareNightsMod` sınıfı `IModModule` uygulamalı ve `Id` property'si **tam olarak** `nightmare_nights` döndürmelidir. `Register` içindeki mod ID'si ile `Id` uyuşmazsa mod çalışmaz. Katalogda sadece o oyun sürümüne lisans/izin alınmış modların kayıtları bulunmalı.

Örnek basit mod:

```csharp
using Madness.Mods;

public sealed class NightmareNightsMod : ModModuleBase
{
    public override string Id { get { return "nightmare_nights"; } }

    protected override void OnRegister(ModRuntime runtime)
    {
        Subscribe<NightContext>(ctx =>
        {
            ctx.PoolMultiplier *= 1.4f;
            ctx.SpawnIntervalMultiplier *= 0.85f;
        });
    }
}
```

Katalog kaydını ve mod sınıfını `Assets` altına koyduğunda, Unity bunları oyunun C# koduyla beraber derler. Bu yaklaşım hem IL2CPP hem Mono için kullanılabilir; IL2CPP’de dışarıdan DLL yükleme yapmaz.

## A4. Kaynak mod ile DLL modunu karıştırma

`ExampleMods/NightAnomalies/NightAnomaliesMod.cs` dış DLL örneğidir. Onu olduğu gibi `Assets` içine kopyalamadan önce karar ver:

- **Gömülü, seçilebilir mod istiyorsan:** C# dosyasını Unity projesinin `Assets` altına ekle ve ayrı katalog factory kaydını ekle. Ana oyun build'i bu kodu içerir; mod menüden seçilir. Bu yol IL2CPP için de uygundur. Örneğin IMGUI overlay'inin mobil UX'ini ayrıca gözden geçir; mobilde Canvas/touch arayüzü tercih et.
- **İndirilebilir topluluk DLL modu istiyorsan:** DLL'yi Mono masaüstü oyununun API'sine göre derle ve `Mods/<id>/` içine koy. Bu yol Android/IL2CPP’de çalışmaz. Mevcut DLL modunda etkinlik `manifest.json` içindeki `enabled` alanıyla belirlenir. Oyunculara DLL için oyun içi checkbox sunmak istenirse ayrıca genel DLL-mod yönetim paneli geliştirilmelidir; mevcut `BuiltInModSelectionUI` yalnızca BuiltInModCatalog kayıtlarını gösterir.

---

# B. Night Anomalies örnek modunu DLL olarak hazırlama (Mono/itch.io)

## B1. Oyunda gereken API değişiklikleri

Örnek mod, güncellenen kaynak projedeki şu kodları gerektirir:

- `EnemySpawnContext.IsElite`
- `EnemyKilledContext`
- `EnemyController.IsElite` ve `SetElite(bool)`
- `WaveManager` spawn sonrası elit durumunu uygulama
- `EnemyController` ölüm anında `EnemyKilledContext` yayınlama

Modu orijinal eski oyun DLL'sine karşı derleme; önce bu güncel proje kaynaklarından Mono oyunu derle. Elitler ayrı yeni prefab değil, mevcut düşman prefablarının güçlendirilmiş ve renklendirilmiş varyantıdır.

## B2. Mono masaüstü oyunu derle

1. Projeyi Unity 6000.4.6f1'de aç.
2. `File > Build Profiles` üzerinden itch.io için hedef masaüstü platformunu seç.
3. `Player Settings > Other Settings > Scripting Backend` değerini **Mono** yap.
4. Ana menü sahnesinin ve oyun sahnesinin Build Profile'da yer aldığını kontrol et.
5. Masaüstü build alıp vanilla olarak başlattığını doğrula.
6. Oyunun `Madness_Data/Managed/` (platforma göre `<ProductName>_Data/Managed/`) klasörünü bul.

Android build ayarını değiştirme: Play Store için ayrı Android Build Profile'da scripting backend **IL2CPP** kalmalıdır. Her platform için ayrı build al.

## B3. DLL'yi derle

`ExampleMods/NightAnomalies/NightAnomalies.csproj` içindeki `GameManagedDir` özelliğini Mono build'in `..._Data/Managed` klasörüne ayarla. Örnek:

```bash
dotnet build ExampleMods/NightAnomalies/NightAnomalies.csproj \
  -p:GameManagedDir="C:/Games/Madness/Madness_Data/Managed" \
  -c Release
```

Başarılı derlemeden sonra `NightAnomalies.dll` çıktısını bul. Kaynak ZIP içinde önceden derlenmiş DLL yoktur; DLL, oyunun sürümüne ve Managed referanslarına göre üretilmelidir.

## B4. Modu elle kur

Şu yapıda bir klasör oluştur:

```text
night_anomalies/
├── manifest.json
└── NightAnomalies.dll
```

`manifest.json` mod kaynak paketinde hazırdır; `entryScript` değeri `NightAnomalies.dll` olmalıdır. Klasörü şu dizine kopyala:

```text
Application.persistentDataPath/Mods/night_anomalies/
```

Windows genellikle:

```text
%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\Mods\night_anomalies\
```

Oyunu yeniden başlat. Başlangıçta DLL yükleyicisi `enabled: true` modunu bulup kaydeder. Devre dışı bırakmak için manifestte `"enabled": false` yapıp oyunu yeniden başlat. **Bu, menüden seçme arayüzü değildir**; ayar dosyasıyla yapılan etkinleştirmedir. Masaüstü standalone build'de mevcut `ModFilePicker` çalışmadığı için bu örneği elle kur.

> DLL modları oyun sürecinde kod çalıştırabilir. Kaynağını denetlemediğin DLL'leri dağıtma veya kurma; mevcut sistem bir güvenlik sandbox'ı değildir.

---

# C. Android/Play Store IL2CPP derlemesi

1. Ayrı Android Build Profile seç.
2. `Player Settings > Other Settings > Scripting Backend = IL2CPP` ve hedef mimari olarak ARM64'i doğrula.
3. Yalnızca `Assets` içinde derlenen, onaylanmış yerleşik mod kaynaklarını dahil et.
4. Ana menüde mod seçme panelinin Android çözünürlüklerinde ve touch ile çalıştığını test et.
5. Mod seçmeden devam et: vanilla oynanışı doğrula.
6. Bir yerleşik modu seçip yeniden başlat: tercihin saklandığını ve modun doğru çalıştığını doğrula.
7. Android build'de dış `Mod.dll` çalışacağını varsayma. DLL dosyasını Android'e kurmak kodu etkinleştirmez.

AssetBundle/görsel içerik taşıyan veri modları için ayrıca hedef platformda bundle üretimi ve cihazda yükleme testi gerekir. Bu rehber harici kod çalıştırmayı Android'e açmaz.

---

# D. Yayın öncesi test listesi

- İlk kurulum/temiz tercihlerde hiçbir mod seçili değil; vanilla başlangıç çalışıyor.
- Her mod tek başına açılıp kapatılıyor; seçim uygulama yeniden başladığında saklanıyor.
- Mod kapatıldığında event aboneliği ve oluşturduğu nesneler temizleniyor.
- Birden fazla mod aynı event'e abone olduğunda öncelik ve etkileşim test ediliyor.
- Bozuk mod veya `OnRegister` hatası ana menüyü/vanilla oyunu kilitlemiyor.
- Mono standalone: DLL yükleniyor, etkin/pasif manifest ve eksik DLL senaryoları test ediliyor.
- Android IL2CPP: vanilla ve yerleşik modlu build cihazda test ediliyor; dinamik DLL beklenmiyor.
- Sürüm ve lisans: anlaşmalı mod geliştiricilerinin adı, sürümü ve dağıtım izni kaydediliyor.

## Proje dosyaları

- Örnek DLL mod kaynak paketi: `ExampleMods/NightAnomalies/`
- Mod seçme bileşeni: `Assets/Scripts/Mods/BuiltInModSelectionUI.cs`
- Gömülü mod kataloğu: `Assets/Scripts/Mods/BuiltInModCatalog.cs`
- Mono/IL2CPP dağıtım notları: `MOD_DAGITIM_MIMARISI.md`
