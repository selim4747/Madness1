# Madness — Modlama Altyapısı Değişiklik Raporu

**Rapor türü:** İlk modlama altyapısı dönüşümü  
**Proje:** Madness1  
**Unity sürümü:** `6000.4.6f1`  
**Durum:** Mod çalışma çekirdeği hazırlandı; örnek mod kodları henüz eklenmedi.  
**Amaç:** Mevcut oyunun davranışını bozmadan, ileride mekanikleri, içerikleri ve dünyayı değiştirebilen modlar için güvenli bir temel oluşturmak.

---

## 1. Kısa özet

Projeye doğrudan rastgele mod kodu çalıştıran bir sistem eklenmedi. Bunun yerine mevcut oyun kodları ile ileride yazılacak modlar arasına kontrollü bir API katmanı yerleştirildi.

Yeni yapı şu mantıkla çalışıyor:

```text
Mevcut oyun kodu
      ↓
Mod context / event kancası
      ↓
Modlar müdahale edebilir veya sonucu değiştirebilir
      ↓
Oyun işlemi devam eder
```

Örneğin oyun bir satış yapmak istediğinde artık yalnızca doğrudan para eklemiyor. Önce satış bilgisini bir `SaleContext` içine koyuyor. Modlar bu context üzerinden:

- fiyatı değiştirebilir,
- satışı iptal edebilir,
- satışı farklı bir kurala bağlayabilir.

Mod aktif değilse veya ilgili hook’a bağlanmıyorsa mevcut işlem eski mantığıyla devam eder.

---

## 2. Değişikliklerin temel hedefi

İstenen mod sistemi yalnızca şu tür basit değişikliklerle sınırlı kalmamalıydı:

- sprite değiştirme,
- sayı değiştirme,
- can veya hız değeri değiştirme,
- kitap fiyatı değiştirme.

Bu nedenle altyapı aşağıdaki seviyeye göre hazırlandı:

- oyun fazlarını değiştirebilmek,
- yeni gün/gece davranışları ekleyebilmek,
- satış sistemini değiştirebilmek,
- hasar sistemine müdahale edebilmek,
- müşteri ve düşman üretimini değiştirebilmek,
- spawn konumlarını değiştirebilmek,
- yeni dünya/sahne yükleyebilmek,
- mod başına özel kayıt tutabilmek,
- ileride tamamen yeni mekanikleri API üzerinden ekleyebilmek.

---

## 3. Eklenen dosyalar

Yeni dosyalar şu klasörde bulunuyor:

```text
Assets/Scripts/Mods/
```

### 3.1 `ModModels.cs`

Mod sisteminin ortak veri modellerini ve context sınıflarını içerir.

#### `ModManifest`

Bir modun kimliğini ve temel ayarlarını temsil eder.

Desteklenen temel alanlar:

```json
{
  "id": "example_mod",
  "name": "Example Mod",
  "version": "1.0.0",
  "apiVersion": "1",
  "author": "Mod Author",
  "enabled": true,
  "loadOrder": 100,
  "entryScript": "main.lua",
  "worldScene": "",
  "dependencies": []
}
```

Bu yapı sayesinde ileride:

- mod sürümü,
- oyun/API uyumluluğu,
- yükleme sırası,
- mod bağımlılıkları,
- giriş kodu,
- modun kendi dünya sahnesi

tanımlanabilir.

#### `ModSaveEntry`

Her modun kendi kayıt verisini ana oyunun kayıt verisinden ayırır.

```text
Ana SaveData
 ├── oyuncu verileri
 ├── oyun ilerlemesi
 ├── müşteri verileri
 └── modStates
      ├── mod_a verisi
      ├── mod_b verisi
      └── mod_c verisi
```

Bu sayede her yeni mod için `SaveData` sınıfına yeni bir alan eklemek gerekmez.

#### `IModModule`

İleride yazılacak mod modülleri için sözleşmedir.

Her mod:

- kendisini tanımlayabilir,
- event kancalarını kaydedebilir,
- içerik registry’sine içerik ekleyebilir,
- kapanırken kendi event’lerini temizleyebilir.

#### Context sınıfları

Context, oyunun gerçekleştirmek üzere olduğu işlemin modlara açılmış kontrollü temsilidir.

Eklenen context türleri:

| Context | Kullanım alanı |
|---|---|
| `PhaseChangedContext` | Oyun fazını değiştirme |
| `DayContext` | Gün numarası ve gün süresi |
| `NightContext` | Gece türü, düşman havuzu ve spawn aralığı |
| `GoldContext` | Para ekleme/harcama |
| `SaleContext` | Satış fiyatı ve satış iptali |
| `CustomerSpawnContext` | Müşteri prefabı, profili ve doğma konumu |
| `EnemySpawnContext` | Düşman tipi, konumu, can/hasar çarpanları |
| `DamageContext` | Oyuncu, düşman veya boss hasarı |
| `WorldLoadContext` | Dünya/sahne yükleme isteği |

---

### 3.2 `ModEventBus.cs`

Modların oyun olaylarına bağlandığı merkezi event sistemidir.

#### Öncelik sistemi

Modlar hook kaydederken öncelik verebilir:

```csharp
ModEventBus.Subscribe<SaleContext>(OnSale, priority: 100, owner: "my_mod");
```

Yüksek öncelikli callback önce çalışır.

Bu, ileride birden fazla mod aynı satış veya hasar işlemini değiştirdiğinde sıralı ve kontrol edilebilir davranış sağlar.

#### Hata izolasyonu

Bir modun callback’i exception verirse:

1. hata loglanır,
2. ilgili mod callback’i başarısız kabul edilir,
3. ana oyunun tamamı durdurulmaz.

Bu özellikle mobilde önemlidir. Bir mod yüzünden doğrudan oyunun ana loop’unun kırılması engellenmiştir.

#### İptal edilebilir işlemler

Bazı işlemler yalnızca bilgi vermek için değil, sonucu değiştirmek için açılmıştır.

Örneğin:

```csharp
context.Cancelled = true;
```

ile şu işlemler iptal edilebilir:

- satış,
- düşman spawn’ı,
- müşteri spawn’ı,
- gece başlangıcı,
- faz değişimi,
- hasar.

#### Mod temizliği

Her callback sahibine ait bir `owner` ile kaydedilebilir. Mod kaldırılırken:

```csharp
ModEventBus.UnsubscribeOwner("my_mod");
```

kullanılarak o modun event bağlantıları topluca temizlenebilir.

---

### 3.3 `ModRuntime.cs`

Mod sisteminin ana yöneticisidir.

#### Görevleri

- Mod klasörünü oluşturmak,
- `manifest.json` dosyalarını bulmak,
- mod manifestlerini okumak,
- API sürümünü kontrol etmek,
- yinelenen mod kimliklerini engellemek,
- modları yükleme sırasına koymak,
- mod kayıt verilerini tutmak,
- mod modüllerini kaydetmek ve kaldırmak.

#### Mod klasörü

Runtime mod klasörü:

```text
Application.persistentDataPath/Mods/
```

Unity projesinin içindeki `Assets` klasörüne bağlı değildir. Böylece oyun build edildikten sonra da mod klasörü kullanılabilir.

#### Başlatma sırası

`GameInitializer` başlarken şu servisler oluşturulur:

```text
ModRuntime
ModWorldService
```

Bunlar mevcut manager’ların yerine geçmez. Mevcut sistemin üzerine eklenir.

#### Varsayılan davranış

Hiç mod yoksa:

- mod klasörü oluşturulur,
- mod listesi boş kalır,
- hiçbir event callback’i çalışmaz,
- mevcut oyun akışı normal şekilde devam eder.

---

### 3.4 `ModContentRegistry.cs`

Mod içeriklerini kimlik üzerinden kaydetmek için kullanılır.

İleride şu içerikler registry’ye alınabilir:

- kitaplar,
- düşmanlar,
- müşteriler,
- silahlar,
- görevler,
- diyaloglar,
- prefablar,
- özel sistemler.

Örnek kullanım şekli:

```csharp
ModContentRegistry.Register("my_mod.special_enemy", enemyData);
```

Daha sonra:

```csharp
EnemyData data;
ModContentRegistry.TryGet("my_mod.special_enemy", out data);
```

Bu yapı, içeriklerin yalnızca `Resources.LoadAll()` ile önceden build içine gömülmesine olan bağımlılığı azaltmak için eklendi.

---

### 3.5 `ModWorldService.cs`

Harita veya dünya değişikliklerinin ana oyun sisteminden ayrılmasını sağlar.

Desteklenen temel işlem:

```csharp
ModWorldService.Instance.TryLoadSceneWorld(
    "my_world",
    "MyModWorld",
    false
);
```

Bu işlemden önce `WorldLoadContext` yayınlanır. Başka bir mod veya güvenlik katmanı yükleme isteğini iptal edebilir.

Desteklenen dünya işlemleri:

- tek sahne olarak dünya yükleme,
- additive sahne yükleme,
- aktif dünya kimliğini tutma,
- aktif sahne adını tutma,
- dünya yüklendiğinde bildirim verme.

Bu servis, yeni haritanın içindeki gameplay objeleri için ileride bir dünya bootstrap sistemiyle genişletilebilir.

---

### 3.6 `ModWorldAnchors.cs`

Haritadaki konumların sabit koordinatlar yerine isimle bulunmasını sağlar.

Bir GameObject’e `ModWorldAnchor` eklendiğinde, örneğin:

```text
customer_spawn
boss_spawn
shop_counter
night_exit
```

gibi kimliklerle kaydedilebilir.

Modlar böylece doğrudan:

```text
new Vector3(12.5f, 4.2f, 0f)
```

gibi sabit koordinatlara güvenmek yerine:

```csharp
ModWorldRegistry.TryGetPosition("boss_spawn", out position);
```

şeklinde konum alabilir.

Harita tamamen değiştiğinde yeni harita aynı anchor kimliklerini sağlarsa mekanikler yeniden koordinat değiştirmeden çalışabilir.

---

### 3.7 `Assets/Scripts/Mods/README.md`

Mod API’sinin kısa teknik notlarını içerir. İleride gerçek mod yazım kılavuzunun temel dosyası olacaktır.

---

## 4. Mevcut oyun kodlarında yapılan bağlantılar

Yeni sistemin yalnızca klasörde durması yeterli olmadığı için mevcut gameplay scriptlerine kontrollü bağlantılar eklendi.

### 4.1 `GameInitializer.cs`

Oyun başlangıcında:

```text
ModRuntime oluşturulur
ModWorldService oluşturulur
```

Bunlar `DontDestroyOnLoad` ile sahne değişimlerinde korunur.

Mevcut singleton başlatma sırası kaldırılmadı.

---

### 4.2 `GameManager.cs`

#### Gün başlangıcı

`StartNewDay()` ve `StartFirstDay()` öncesinde `DayContext` yayınlanır.

Modlar:

- gün süresini değiştirebilir,
- gün başlangıcını iptal edebilir,
- gün numarasına göre özel davranış başlatabilir.

Örnek kullanım hedefi:

```text
Her 5. gün süresi yarıya insin.
```

#### Faz değişimi

`SetPhase()` artık önce `PhaseChangedContext` yayınlar.

Modlar:

- geceye geçişi engelleyebilir,
- fazı başka bir faza yönlendirebilir,
- yeni bir faz mimarisine hazırlık yapabilir.

Mevcut `OnPhaseChanged` event’i korunmuştur.

---

### 4.3 `EconomyManager.cs`

#### Para harcama

`TrySpendGold()` öncesinde `GoldContext` yayınlanır.

Modlar:

- harcanacak miktarı değiştirebilir,
- alışverişi iptal edebilir,
- işlem sebebine göre farklı kural uygulayabilir.

#### Para kazanma

`AddGold()` öncesinde `GoldContext` yayınlanır.

Modlar:

- kazanılan parayı artırabilir,
- azaltabilir,
- tamamen iptal edebilir.

Mevcut `OnGoldChanged` event’i ve günlük istatistik güncellemesi korunmuştur.

---

### 4.4 `CounterController.cs`

Satış sırasında fiyat hesaplandıktan ve müşteri kişilik katsayısı uygulandıktan sonra `SaleContext` yayınlanır.

Modlar:

- son fiyatı değiştirebilir,
- satışı iptal edebilir,
- müşteriye veya kitaba göre özel fiyat verebilir.

Satış iptal edilirse müşteri mevcut oyunun güvenli çıkış akışıyla memnuniyetsiz şekilde gönderilir; işlem yarım bırakılıp sahnede kilitli müşteri bırakılmaz.

Satış onaylanırsa mevcut sistem korunur:

```text
Para eklenir
Floating text gösterilir
Satış istatistiği güncellenir
Özel kitap sayaçları güncellenir
Ses oynatılır
Müşteri tamamlanır
```

---

### 4.5 `CustomerSpawner.cs`

Müşteri instantiate edilmeden önce `CustomerSpawnContext` yayınlanır.

Modlar:

- müşteri prefabını değiştirebilir,
- müşteriyi iptal edebilir,
- spawn konumunu değiştirebilir,
- müşteri profilini seçebilir.

Mevcut müşteri spawn sistemi korunmuştur. Mod müdahalesi olmadığı zaman aynı prefab ve aynı kapı noktası kullanılır.

---

### 4.6 `WaveManager.cs`

#### Gece başlangıcı

`StartNight()` içinde `NightContext` yayınlanır.

Modlar:

- geceyi iptal edebilir,
- gece türünü boss gecesine veya normal geceye çevirebilir,
- düşman havuzunu artırabilir/azaltabilir,
- spawn aralığını değiştirebilir.

#### Düşman üretimi

Her düşman üretiminden önce `EnemySpawnContext` yayınlanır.

Modlar:

- düşman tipini değiştirebilir,
- doğma konumunu değiştirebilir,
- spawn işlemini iptal edebilir,
- can çarpanı verebilir,
- hasar çarpanı verebilir.

Önemli güvenlik sınırı:

```text
HealthMultiplier en az 0 olabilir.
DamageMultiplier en az 0 olabilir.
Spawn interval çarpanı minimum 0.05 ile sınırlandırılır.
```

Böylece hatalı bir modun negatif değerlerle temel oyun matematiğini bozması azaltılmıştır.

---

### 4.7 `EnemyController.cs`

Düşman hasar aldığında `DamageContext` yayınlanır.

Modlar:

- düşman hasarını azaltabilir,
- artırabilir,
- hasarı tamamen iptal edebilir,
- belirli düşmanlara bağışıklık verebilir.

Mevcut ölüm ve havuza geri gönderme akışı korunur.

---

### 4.8 `BossController.cs`

Boss hasarı da `DamageContext` üzerinden geçirilir.

Bu sayede modlar:

- boss’un belirli fazlarda hasar almamasını,
- boss’a kritik vuruş uygulanmasını,
- hasar değerlerinin değişmesini,
- özel boss kurallarını

uygulayabilecek.

Mevcut boss faz değişimleri, ödül ve ölüm akışı kaldırılmamıştır.

---

### 4.9 `PlayerStats.cs`

Oyuncu hasar almadan önce `DamageContext` yayınlanır.

Modlar:

- oyuncuya gelen hasarı değiştirebilir,
- hasarı tamamen engelleyebilir,
- kısa dokunulmazlık kuralını yok sayabilir veya koruyabilir.

Mevcut can azaltma, UI, animasyon, ses ve ölüm akışları korunur.

---

### 4.10 `SaveData.cs` ve `SaveSystem.cs`

`SaveData` içine şu alan eklendi:

```csharp
public List<ModSaveEntry> modStates;
```

`SaveSystem` iki noktada mod runtime’a bağlandı:

```text
ApplyToScene()
GatherFromScene()
```

Yükleme sırasında:

```text
SaveData içindeki modStates → ModRuntime
```

Kayıt sırasında:

```text
ModRuntime içindeki mod state’leri → SaveData
```

Mevcut oyuncu, ekonomi, silah, müşteri ve ilerleme kayıtları değiştirilmedi.

---

## 5. Mevcut projede özellikle değiştirilmeyen şeyler

Uyumluluk ve risk azaltma amacıyla şu büyük değişiklikler yapılmadı:

### Singleton sistemi kaldırılmadı

`GameManager.Instance`, `EconomyManager.Instance`, `WaveManager.Instance` gibi mevcut erişimler korunmuştur.

Bunları bir anda dependency injection sistemine taşımak, sahne referanslarını ve prefab bağlantılarını bozabilirdi.

### Mevcut ScriptableObject asset’leri dönüştürülmedi

`BookData`, `EnemyData`, `CustomerData`, `WeaponData` gibi asset’ler silinmedi veya yeniden formatlanmadı.

### Mevcut sahne bağlantıları değiştirilmedi

Inspector’daki mevcut prefab, Transform, UI ve manager referansları değiştirilmedi.

### Mevcut kayıt alanları yeniden adlandırılmadı

Eski kayıt dosyalarının bozulmaması için var olan alanlar korunup yalnızca yeni `modStates` alanı eklendi.

### Mod kodu henüz çalıştırılmıyor

Bu aşamada dış klasörden gerçek Lua, C# DLL veya başka bir script dosyası otomatik çalıştırılmıyor.

Bu bilinçli bir karardır. Önce API sınırları oluşturuldu; sonraki aşamada mod kodlarının hangi dilde ve hangi izinlerle çalışacağı ayrıca uygulanmalıdır.

---

## 6. Harita değiştirme konusunda mevcut durum

Harita değişimi için ilk altyapı hazırdır, ancak tam anlamıyla otomatik harita modlama sistemi henüz tamamlanmış değildir.

Hazır olanlar:

- world/sahne yükleme context’i,
- additive veya single scene yükleme,
- aktif world kimliği,
- world loaded bildirimi,
- isimli anchor registry’si.

Bir sonraki harita modlama aşamasında ayrıca şu sistemler bağlanmalıdır:

- mod sahnesi veya AssetBundle/Addressables yükleme,
- mod sahnesindeki manager bootstrap kurulumu,
- NavMesh/runtime navigation hazırlığı,
- müşteri ve düşman spawn anchor’larının otomatik eşlenmesi,
- kamera sınırlarının dünya tarafından belirlenmesi,
- UI ve etkileşim noktalarının world registry üzerinden çözülmesi,
- harita kaldırılırken mod nesnelerinin temizlenmesi.

Bu parçalar, mevcut sahne sistemini rastgele değiştirmemek için henüz otomatikleştirilmemiştir.

---

## 7. Hata ve güvenlik yaklaşımı

Şu anki çekirdekteki temel korumalar:

- Mod klasörü yoksa otomatik oluşturulur.
- Bozuk manifest atlanır.
- Kimliksiz manifest atlanır.
- Aynı mod kimliği ikinci kez yüklenmez.
- API sürümü uyumsuz mod atlanır.
- Mod callback hataları ana oyuna taşınmaz.
- Event callback listesi snapshot üzerinden çalışır.
- Mod state ana kayıt alanından ayrıdır.
- Hasar, spawn ve zaman çarpanlarında temel alt sınırlar bulunur.
- Dünya yükleme isteği context üzerinden iptal edilebilir.

---

## 8. Doğrulama durumu

Yapılan doğrulamalar:

- Projedeki C# dosyaları statik olarak tarandı.
- Parantez dengesi kontrol edildi.
- Yeni mod dosyaları kontrol edildi.
- Unity proje sürümü doğrulandı: `6000.4.6f1`.
- ZIP arşivi test edildi.
- Orijinal proje ile değişen dosyalar karşılaştırıldı.

Unity Editor bu çalışma ortamında bulunmadığı için şu testler burada yapılamadı:

- Unity Editor import testi,
- gerçek C# compile testi,
- Play Mode testi,
- Android build testi,
- sahne referanslarının Inspector üzerinden açılış testi.

Bu yüzden projeyi Unity 6000.4.6f1 ile açarken Console penceresi kontrol edilmelidir.

---

## 9. Bundan sonraki mod sistemi aşaması

Mod ekleme kodlarına geçildiğinde önerilen sıra şöyledir:

1. Mod klasörü ve manifest yükleme ayarları
2. Mod etkin/devre dışı yönetimi
3. İlk örnek mod modülü
4. Event hook üzerinden mekanik değiştirme
5. Mod state kayıt/yükleme örneği
6. Runtime sprite/audio/content yükleme
7. Yeni mekanik ekleme API’leri
8. Harita modülü ve world bootstrap
9. Mod yönetim arayüzü
10. Hata raporlama ve mod uyumluluk kontrolü

İlk test modu olarak tüm katmanları doğrulayacak küçük bir mod önerilir:

```text
Nightmare Mode
- gün süresini değiştirir,
- satış fiyatını değiştirir,
- gece spawn sayısını artırır,
- düşman canını artırır,
- mod state kaydeder,
- bir dünya anchor’ı kullanır.
```

---

## 10. Sonuç

Bu aşamada proje doğrudan mod çalıştıran tamamlanmış bir mod platformu değildir. Proje, mod platformunun güvenli ve geriye dönük uyumlu çekirdeğine dönüştürülmüştür.

En önemli kazanım şudur:

```text
Modlar artık mevcut gameplay sınıflarının private alanlarına doğrudan girmek zorunda kalmadan,
oyunun belirli karar noktalarına kontrollü şekilde bağlanabilecek.
```

Mevcut oyun mod yokken eski davranışını sürdürür. Mod ekleme kodları ise bu raporda tanımlanan `ModRuntime`, `ModEventBus`, context’ler, content registry, save state ve world service üzerine kurulmalıdır.

**Bu rapordan sonra mod kodlarına geçilmemiştir. Kullanıcının sonraki komutu beklenmektedir.**

---

## 11. Cihazdan mod dosyası ekleme

Bu sürümde ana menüye runtime olarak **MOD DOSYASI EKLE** butonu eklenmiştir. Buton mevcut sahneye elle bağlanmayı zorunlu kılmaz; `MainMenuController` başlarken aktif Canvas üzerinde oluşturulur. Böylece mevcut sahne/prefab bağlantıları değiştirilmemiştir.

### Android akışı

```text
Ana Menü
  ↓
MOD DOSYASI EKLE
  ↓
Android ACTION_OPEN_DOCUMENT dosya seçici
  ↓
Seçilen content:// dosyası uygulama cache alanına kopyalanır
  ↓
Unity C# callback'i
  ↓
ModPackageImporter
  ↓
Güvenli staging klasörü
  ↓
manifest.json doğrulaması
  ↓
Application.persistentDataPath/Mods/<mod-id>/
  ↓
ModRuntime.RescanMods()
```

Android tarafında `MadnessFilePicker` isimli küçük bir ara Activity kullanılır. Bunun nedeni Android dosya seçicisinin döndürdüğü `content://` URI'larının Unity C# tarafında her cihazda doğrudan dosya yolu olmamasıdır. Ara Activity seçilen dosyayı uygulama cache klasörüne kopyalar ve Unity'nin polling callback'ine gerçek geçici yolu verir.

### Desteklenen paketler

- `.zip`
- `.mod`
- `.madnessmod`

Paket içinde tek bir `manifest.json` bulunmalıdır. Manifestin `apiVersion` değeri mevcut `ModRuntime.CurrentApiVersion` değeriyle aynı olmalıdır.

### Güvenlik kontrolleri

- Tek manifest zorunluluğu
- Mod id doğrulaması
- `../` ve mutlak path engeli
- Staging klasörü dışına yazma engeli
- En fazla 4096 arşiv girdisi
- Açılmış toplam içerik için 256 MB sınırı
- Aynı mod güncellenirken yedekleyerek değiştirme
- İçe aktarma yarıda kalırsa eski mod sürümünü geri alma

### Önemli kapsam sınırı

Bu aşamada dosya aktarımı, manifest keşfi ve mod paketinin oyuna güvenli biçimde alınması tamamlanmıştır. `entryScript` alanındaki dış mod kodunu otomatik çalıştırma henüz etkin değildir. Bu, sonraki mod kodu aşamasında kontrollü API/sandbox tasarımıyla ayrıca uygulanacaktır.
