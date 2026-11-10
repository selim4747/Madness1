# Mod Dağıtım Mimarisi: Play Store IL2CPP ve itch.io Mono

Bu proje Android hedefinde IL2CPP kullanacak şekilde ayarlı (`ProjectSettings/ProjectSettings.asset`). IL2CPP sürümünde dışarıdan `Mod.dll` yüklenmez. C# modları ana oyunla birlikte derlenmeli; oyuncu menüden yalnızca bu derlemeye dahil edilmiş modları açıp kapatabilir. itch.io Mono sürümünde mevcut DLL yükleyicisi kullanılabilir.

## Bu güncellemede eklenen altyapı

- `Assets/Scripts/Mods/BuiltInModCatalog.cs`: derlemeye gömülü `IModModule` fabrikalarını kaydeder; seçimi `PlayerPrefs` ile cihazda saklar.
- `Assets/Scripts/Mods/BuiltInModSelectionUI.cs`: ana menüdeki bir panele eklenerek kayıtlı modlar için aç/kapat butonları üretir.
- `ModRuntime.ReloadModSelection()`: seçim değiştiğinde mod kayıtlarını yeniden uygular.
- `ModConfigApplier`: yeniden tarama/yüklemede eski config kancalarının birikmesini önler.

`ExampleMods/NightAnomalies/` klasöründe Mono DLL için örnek mod kaynakları vardır; henüz `BuiltInModCatalog` içinde otomatik kayıtlı değildir. Altyapı, anlaşılan modların C# kaynak kodu oyuna eklenip derlenince katalogda gösterilmeye hazırdır. Bir modun katalogda görünmesi için aşağıdaki kayıt kodu ve mod sınıfı projeye eklenmelidir.

## Yerleşik mod kaydı

Mod sınıfı `IModModule` uygular (tercihen `ModModuleBase` taban sınıfı). Ayrı bir C# dosyasında katalog kaydını yapın:

```csharp
using Madness.Mods;
using UnityEngine;

public static class ReleasedBuiltInMods
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        BuiltInModCatalog.Register(
            "nightmare_nights",
            "Kâbus Geceleri",
            "1.0.0",
            () => new NightmareNightsMod());
    }
}

public sealed class NightmareNightsMod : ModModuleBase
{
    public override string Id => "nightmare_nights";

    protected override void OnRegister(ModRuntime runtime)
    {
        Subscribe<NightContext>(ctx =>
        {
            ctx.PoolMultiplier *= 1.5f;
            ctx.SpawnIntervalMultiplier *= 0.8f;
        });
    }
}
```

Gerçek sınıfı `Assets/Scripts` altına ekleyin. Modun tamamen kaldırılması/yenilenmesi gerektiğinde `OnUnregister` override edip modun oluşturduğu nesneleri, coroutine'leri ve diğer kaynakları da temizleyin. Katalog kimliği ile modun `Id` değeri birebir aynı olmalıdır. Katalog kaydı olsa bile yeni mod ilk kullanımda varsayılan olarak **kapalıdır**; oyuncu seçim panelinden etkinleştirir. Böylece hiç mod seçilmezse vanilla oynanış korunur. Her sürüme yalnızca o sürümde lisans/izin alınmış kaynak kodları ekleyin.

## Menüde mod seçimi

1. Ana Menü sahnesindeki mevcut Canvas içinde bir panel oluşturun.
2. Panel içine `VerticalLayoutGroup` ekleyin (önerilen) ve paneli kaydırılabilir içerik alanına koyun.
3. Panel GameObject'ine `BuiltInModSelectionUI` ekleyin.
4. `List Root` alanına butonların üretileceği RectTransform'u; isteğe bağlı `Status Text` alanına durum yazısını atayın.
5. Play modunda birkaç kez aç/kapat yapın; uygulamayı kapatıp açtıktan sonra seçimin korunduğunu doğrulayın.

Mod seçimi aktif oyun sırasında da değiştirilebilir. Bunun modun o anda sahip olduğu tüm durumları güvenle temizlediği ayrıca doğrulanmalıdır; üretim arayüzünde seçimi yalnızca ana menüde değiştirmeye izin vermek daha güvenlidir.

## Sürüm ayrımı

| Dağıtım | Backend | C# modları | Dış mod dosyaları |
|---|---|---|---|
| Google Play Android | IL2CPP (mevcut ayar) | Kaynak kodu ana oyuna gömülü; katalog seçimi ile aç/kapat | Kod çalıştıran DLL sunmayın. Veri/ayar paketleri için ayrı doğrulama gerekir. |
| itch.io masaüstü | Mono | Mevcut `ModScriptLoader` üzerinden DLL desteği | Mod rehberinde desteklenen API/sürüm sınırlarını belirtin. DLL'ler güvenilmeyen kod çalıştırabilir. |

Ayrı build alın: Android için **IL2CPP** ve ARM64; itch.io hedefi için **Mono** ve masaüstü platformu. Mono build'de IL2CPP'ye özgü koşullu kodu, build hedefini, DLL bağımlılıklarını ve izin verilen framework/API seviyesini test edin. Android'in projenin mevcut scripting backend ayarı IL2CPP'dir; itch.io Mono ayarını Build Settings/Player Settings'te masaüstü hedefi için ayrıca seçip build profile veya CI betiğinde sabitleyin. Aynı proje dosyası üzerinde hedef değişimini sürümleme/otomasyonla tekrarlanabilir hale getirin.

## Yayın öncesi zorunlu testler

- Unity 6000.4.6f1 ile temiz import ve Android IL2CPP Development Build alın.
- Play Store'a gidecek IL2CPP build'de yerleşik mod açma/kapama, yeniden başlatma sonrası tercih, save/load ve temel gameplay testlerini çalıştırın.
- Mono masaüstü build'de `Mod.dll` yükleme, hatalı/uyumsuz mod, bağımlılık ve unload davranışlarını test edin.
- `Managed Stripping Level` Android'de 2. Yerleşik mod sınıflarının IL2CPP build'de kaldığını cihazda doğrulayın; katalogdaki factory referansı doğrudan olduğu için normalde sınıf erişilebilir olmalıdır.
- Herhangi bir modun kaydı ya da `OnRegister` hatası temel oyunu engellememeli; test edin.
- APK/AAB ve itch.io build çıktısını lisanslı içerik, platform kuralları, gizlilik ve mağaza koşulları açısından ayrıca gözden geçirin.

## Sınırlar / yapılmamış işler

Bu sandbox'ta Unity Editor/Unity CLI bulunmadığından Android veya Mono binary build alınmadı. Ayrıca belirli modların kaynak kodu/proje onayı verilmediği için gerçek mod sınıfları katalogda tanımlanmadı; altyapı ve seçim UI'si eklendi. Yayına hazır saymadan önce Unity'de sahneye UI component'ini bağlayın, seçilmiş mod sınıflarını ekleyin ve iki hedefi gerçek cihazlarda derleyip test edin.
