# Mod Yöneticisi Kullanımı

Projeye harici ve yerleşik modları birlikte yöneten bir panel eklendi. `ModPanelController` bulunan panel açıldığında `ModManagerUI` otomatik olarak eklenir ve mod listesi oluşturulur. Inspector kurulumu yapılmış özel bir liste varsa `ModManagerUI` üzerindeki `listRoot` alanına bağlanabilir; boş bırakılırsa panel içinde otomatik liste oluşturulur.

## Panel özellikleri

Panelde mod adı, sürüm, yapımcı, mod türü, yükleme durumu ve bağımlılık sorunları gösterilir. Harici modlarda aç/kapat, ayarları uygula, yükleme sırasını artırma/azaltma ve silme işlemleri bulunur. Yerleşik modlar silinemez; yalnızca açılıp kapatılabilir.

Silme işlemi yanlış tıklamaya karşı iki aşamalıdır. İlk tıklama onay mesajını gösterir, beş saniye içinde ikinci tıklama mod klasörünü kalıcı olarak yok etmek yerine `Mods/.trash/` klasörüne taşır. Bu nedenle kurtarma işlemi daha sonra elle yapılabilir.

## Mod ayarları

Manifest dosyası `settings` alanıyla ayar tanımlayabilir. Desteklenen tipler `bool`, `int`, `float`, `string` ve `dropdown` değerleridir. Ayarlar save dosyasından bağımsız olarak `persistentDataPath/Mods/<modId>.settings.json` dosyasında saklanır.

DLL modları ayarları şu API üzerinden okuyabilir:

```csharp
float multiplier = ModAPI.GetSettingFloat("my_csharp_mod", "salePriceMultiplier", 29.0f);
string text = ModAPI.GetSetting("my_csharp_mod", "someText", "default");
bool enabled = ModAPI.GetSettingBool("my_csharp_mod", "enabled", true);
```

Örnek `my_csharp_mod` artık satış çarpanını paneldeki `salePriceMultiplier` ayarından okur. Ayar değişikliğinden sonra satırdaki `Uygula` butonuna basılmalıdır. DLL modlarının güvenli biçimde unload edilememesi nedeniyle en güvenilir uygulama yöntemi sahneyi veya oyunu yeniden başlatmaktır.

## Bağımlılıklar ve hatalar

`manifest.json` içindeki `dependencies` listesi eksik veya kapalı modları kontrol eder. Böyle bir mod yüklenmez ve panelde hata nedeni gösterilir. API uyumsuz manifest, eksik DLL, `IModModule` bulunamaması, tekrar eden ayar anahtarı, kendine bağımlılık ve geçersiz ayar tipi import sırasında reddedilir.

## Örnek ayar manifesti

```json
"settings": [
  {
    "key": "salePriceMultiplier",
    "label": "Satış kazancı çarpanı",
    "type": "float",
    "defaultValue": "29.0",
    "min": 0.1,
    "max": 100.0,
    "restartRequired": true
  }
]
```

Gerçek Unity Editor/CLI bu sandbox ortamında bulunmadığından Unity import, compile, Android IL2CPP ve runtime testleri çalıştırılamadı. Paketleme öncesinde statik C# parantez, JSON manifest ve kritik referans kontrolleri yapılmalıdır.
