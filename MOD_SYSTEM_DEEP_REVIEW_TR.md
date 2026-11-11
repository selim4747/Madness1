# Madness mod sistemi — derinlemesine inceleme

## Kapsam ve sınır

İnceleme; `ModRuntime`, `ModScriptLoader`, `ModConfigApplier`, `ModSettingsStore`, `ModManagerUI`, `ModPanelController`, eski `BuiltInModSelectionUI`, `ModPackageImporter`, `ModAPI`, örnek manifestler ve `AnaMenu.unity` bağlantılarını kapsıyor. Statik kaynak taraması ve sahne/manifest tutarlılığı kontrol edildi. Bu sandbox ortamında Unity 6 Editor/CLI olmadığı için gerçek import, compile, UI etkileşimi, Mono DLL yükleme, Android IL2CPP ve runtime testi yapılamadı.

## Genel değerlendirme

Mod sistemi artık önceki hâline göre belirgin biçimde daha yetenekli: mod tarama, sahiplik bazlı event temizliği, DLL ve config modları, ayar deposu, bağımlılık kontrolü, güvenli silme için `.trash`, mod önceliği ve tek panel yaklaşımı mevcut. Ancak yeni yönetim paneli şu an prototip seviyesinde. Özellikle dinamik Unity UI bileşenlerinin eksik alt nesnelerle oluşturulması ve DLL/mod yaşam döngüsü durumlarının yalnızca kısmen raporlanması, ilk düzeltilmesi gereken konular.

## Kritik bulgular

### 1. Dinamik ayar kontrolleri görsel olarak eksik — yüksek öncelik

`ModManagerUI` içinde `Toggle`, `Slider`, `Dropdown` ve `InputField` yalnızca component olarak oluşturuluyor. Toggle için `graphic`/arka plan/checkmark; slider için fill/handle; dropdown için caption/template; input field için text component’i oluşturulmuyor. Unity UI component’leri bu alt nesneler olmadan bazen tıklanabilir olsa bile görünmez, boş veya kullanılabilir olmayan kontroller hâline gelir.

**Öneri:** Projede hazırlanmış prefab’lar kullanılsın veya kod içinde standart görsel hiyerarşi oluşturulsun. En azından her ayar satırında görünen bir değer metni, arka plan Image, handle/checkmark ve doğru raycast ayarları olmalı. En güvenlisi `ModSettingRow` prefab’ı hazırlayıp tipine göre mevcut kontrolü etkinleştirmektir.

### 2. Mod listesinde ScrollRect yok — yüksek öncelik

`ModManagerUI.BuildPanel()` yalnızca `VerticalLayoutGroup` ve `ContentSizeFitter` oluşturuyor. Mod sayısı ve ayar sayısı arttığında liste panelin dışına taşar. Eski çift panel sorunu kapatılmış olsa da çok sayıda modda aynı sınıfın yeni bir taşma problemi oluşacak.

**Öneri:** Panel içine `ScrollRect -> Viewport -> Content` hiyerarşisi eklenmeli. `Content` üzerinde `VerticalLayoutGroup` ve `ContentSizeFitter`, Viewport üzerinde `RectMask2D` kullanılmalı. Mobilde dokunmatik kaydırma etkinleştirilmeli.

### 3. DLL’in dosyada bulunması “Hazır” durumuyla karıştırılıyor — yüksek öncelik

`GetModStatus()` DLL dosyasının varlığını kontrol ediyor; ancak DLL içinde doğru `IModModule` bulunup bulunmadığını, modülün `Register()` çağrısının başarılı olup olmadığını veya modülün çakışma nedeniyle atlandığını status hesabına katmıyor. Panelde satır “Hazır” görünürken ayrıntı metninde hata bulunabilir.

**Öneri:** Mod başına gerçek bir durum tutulmalı: `Disabled`, `Ready`, `Loaded`, `MissingDependency`, `MissingDll`, `InvalidModule`, `LoadFailed`, `Conflict`, `IL2CPPUnsupported`. `ModScriptLoader` başarılı kaydı ve hata durumunu `ModRuntime`a bildirmeli. Panel status ve renk doğrudan bu durumdan gelmeli.

### 4. DLL modül ID doğrulaması gevşek — yüksek öncelik

`FindModuleType()` beklenen ID’yi bulamazsa ilk uygun `IModModule` tipini `best` olarak döndürüyor. Bu, DLL içinde yanlış ID’li bir modül varsa manifestteki mod ID’sine ait olmayan modülün yüklenebilmesine yol açabilir.

**Öneri:** Beklenen ID eşleşmesi bulunmadığında `null` döndürülmeli ve mod kesinlikle yüklenmemeli. “DLL içinde manifest ID’siyle eşleşen IModModule bulunamadı” hatası gösterilmeli.

### 5. Manifest yazımı atomik değil — yüksek öncelik

`WriteManifest()` önce geçici dosyayı yazıyor, sonra mevcut `manifest.json` dosyasını siliyor ve geçiciyi taşıyor. Uygulama bu iki işlem arasında kapanırsa manifest kaybolabilir.

**Öneri:** SaveSystem’deki yaklaşım burada da kullanılmalı: `.tmp` yaz, mümkünse `File.Replace`, desteklenmiyorsa `.bak` kopyası alıp kontrollü fallback uygula. Başarısızlıkta eski dosya korunmalı.

## Orta öncelikli bulgular

### 6. Bağımlılık kontrolü yalnızca doğrudan ve yüzeysel

`GetDependencyError()` manifestin varlığını ve `enabled` değerini kontrol ediyor. Bağımlılığın gerçekten yüklenmiş/başarılı olup olmadığını kontrol etmiyor. A ayrıca bağımlılık zincirleri, döngüler ve sürüm aralıkları desteklenmiyor.

**Öneri:** Manifest modeline bağımlılık sürümü eklenmeli; DFS/topolojik sıralama ile zincirler çözülmeli; döngü tespit edilmeli; bağımlılık ancak `Loaded` durumundaysa başarılı kabul edilmeli.

### 7. Aynı ID’li manifestler sessizce birini seçiyor

`ScanManifests()` aynı ID tekrar ederse ilk gördüğünü tutuyor. Dosya sistemi sırası deterministik olmak zorunda değil ve panelde hangi modun seçildiği anlaşılmayabilir.

**Öneri:** Duplicate ID açık hata olarak kaydedilmeli, iki manifest de yüklenmemeli ve panelde iki klasör yolu gösterilmeli.

### 8. `entryScript` güvenlik kontrolü yalnızca import sırasında uygulanıyor

İçe aktarma sırasında `..` kontrolü var; ancak kullanıcı `persistentDataPath/Mods` içine doğrudan mod klasörü koyarsa `ScanManifests()` ve `ResolveEntryPath()` aynı doğrulamayı tekrar etmiyor. Bu hem güvenlik hem de yanlış dosya yükleme riski oluşturur.

**Öneri:** Tüm mod tarama/yükleme yollarında normalize edilmiş yolun mod klasörü içinde kaldığı doğrulanmalı. `entryScript` yalnızca `.dll` olmalı ve sembolik/parent segmentler reddedilmeli.

### 9. Ayar tanımlarının değerleri doğrulanmıyor

Importer ayar tipini, tekrar eden anahtarı ve min/max sırasını kontrol ediyor; ancak `defaultValue` gerçekten bool/int/float mı, dropdown default seçenekler arasında mı, float NaN/Infinity içeriyor mu kontrol edilmiyor.

**Öneri:** Import sırasında ayar değerleri parse edilmeli, aralığa clamp edilmeli ve geçersiz manifest reddedilmeli. Runtime’da da ayar kaydedilirken aynı doğrulama yapılmalı.

### 10. Ayar deposunda güvenli yol doğrulaması eksik

`ModSettingsStore.PathFor()` mod ID’sini doğrudan dosya adına ekliyor. Normal manifest importu ID’yi filtreliyor olsa da public API doğrudan çağrılabilir.

**Öneri:** `modId` için tek bir merkezi `IsSafeModId()` doğrulaması kullanılmalı; boşluk, `/`, `\\`, `..`, kolon ve geçersiz dosya karakterleri reddedilmeli.

### 11. Ayar değişimi anında kaydediliyor ama uygulama davranışı net değil

Slider her hareketinde dosyaya yazıyor. Bu gereksiz I/O üretir. Ayrıca bazı DLL ayarları yalnızca `Register()` sırasında okunuyor; slider değişince mod yeniden kaydedilmeden davranış değişmiyor.

**Öneri:** UI değişikliklerini bellekte tutup `Kaydet` butonuyla yazmak; `restartRequired=false` ayarlar için `OnSettingChanged` event’i sağlamak; `restartRequired=true` için açıkça yeniden yükleme uyarısı göstermek.

## UI ve kullanıcı deneyimi önerileri

Tek panel çözümü doğru yönde. Sahnedeki eski `BuiltInModSelectionUI` kapatıldı ve `ModPanelController` açılışta eski component’i de devre dışı bırakıyor. Buna rağmen eski script proje içinde duruyor; bakım karmaşasını azaltmak için kullanılmayan eski panel script’i ya kaldırılmalı ya da açıkça “legacy” olarak işaretlenmeli.

Yeni panelde mod satırı için önerilen düzen: sol tarafta mod ikonu ve ad, altında yapımcı/sürüm; sağda durum rozeti ve toggle; alt bölümde ayarlar; en altta `Uygula`, `Sıfırla`, `Yükleme sırası`, `Sil`. Hata metni satırı büyütmek yerine ayrı bir ayrıntı popup’ında gösterilmeli.

Silme işleminin iki kez aynı butona basma ile yapılması çalışabilir ancak yanlış anlaşılabilir. Onay popup’ı ve “Mod klasörünü geri dönüşüme taşı” açıklaması daha iyi olur. `.trash` klasörü için panelden geri yükleme ve kalıcı temizleme de eklenebilir.

## Mod yaşam döngüsü önerileri

Şu anda `ReloadModSelection()` bütün DLL modlarını kaldırıp built-in modları, config hook’larını ve DLL modlarını baştan kuruyor. Bu basit ve güvenli bir temel, ancak event callback’i sırasında UI yenilemek, DLL modül durumunu sıfırlamak ve sahnedeki mod tarafından oluşturulmuş GameObject’leri temizlemek gibi yan etkiler olabilir.

Önerilen yaşam döngüsü:

1. Manifestleri tara ve doğrula.
2. Duplicate ID ve bağımlılık grafiğini çöz.
3. Devre dışı/bozuk modları yükleme planından çıkar.
4. Eski modların `Unregister` ve owner cleanup işlemlerini tamamla.
5. AssetBundle ve mod tarafından oluşturulan runtime objeleri temizle.
6. Öncelik sırasına göre yükle.
7. Her mod için gerçek `ModLoadState` kaydet.
8. UI’ye tek bir `OnModsChanged` bildirimi gönder.

`OnModsChanged` çağrısında subscriber exception’ları da ayrı ayrı yakalanmalı; bir UI listener hatası diğer listener’ların çalışmasını engellememeli.

## Config modları ve DLL modları arasındaki ayrım

`config.json` modları IL2CPP’de çalışabilir; DLL modları Android IL2CPP’de çalışmaz. Panelde bu fark açıkça gösterilmeli. Şu an `entryScript` doluysa “DLL devre dışı” durumu üretilebiliyor, fakat bunun yanında “Bu platformda yalnızca config/asset modları destekleniyor” açıklaması gösterilmeli.

Config sistemi şu anda sabit dokuz alanı destekliyor. Bu, hızlı başlangıç için iyi fakat gerçek dinamik ayar sistemi değil. Uzun vadede config hook’ları da manifest `settings` tanımlarından üretilmeli veya config alanları bir schema sistemiyle tanımlanmalı.

## Save ve veri bütünlüğü

Mod ayarlarının save dosyasından ayrı tutulması doğru. Mod ilerleme verilerinin `IModSaveAware` üzerinden save içinde tutulması da doğru. Ancak mod kaldırıldığında mod save state’i otomatik silinmiyor; bu genellikle istenen davranış olabilir fakat panelde “Mod verisini de sil” seçeneği bulunmalı.

Mod tekrar yüklendiğinde API veya schema sürümü değişmişse `schemaVersion` migration sistemi gerekiyor. Şu an `schemaVersion` saklanıyor fakat otomatik migration callback’i bulunmuyor.

## Test matrisi

Unity Editor’da en az şu senaryolar çalıştırılmalı:

- Hiç mod yokken ana menü açılışı.
- Yalnızca built-in mod açık.
- Yalnızca config mod açık.
- Geçerli DLL mod açık.
- DLL eksik.
- DLL’de yanlış `IModModule.Id`.
- Aynı ID’ye sahip iki mod.
- Eksik bağımlılık.
- Döngüsel bağımlılık.
- Bir ayarı minimum, maksimum ve geçersiz değere çekme.
- On adet veya daha fazla modla ScrollRect taşma testi.
- Mod aç/kapat sonrası sahne değişimi.
- Mod silme sonrası yeniden tarama.
- Android IL2CPP build’de DLL modun güvenli biçimde atlanması.
- Save alıp mod kapatma, tekrar açma ve mod verisi migration testi.

## Önceliklendirilmiş uygulama planı

**P0:** UI kontrol prefab/alt nesneleri; ScrollRect; DLL modül ID’sinde gevşek fallback’in kaldırılması; atomik manifest yazımı; gerçek mod yükleme durumları.

**P1:** bağımlılık grafiği ve döngü kontrolü; duplicate ID hatası; tarama sırasında entryScript yol doğrulaması; ayar değer doğrulaması; `OnModsChanged` subscriber izolasyonu.

**P2:** ayar değişiklik event’i; mod ayarlarını sıfırlama; mod save state silme/koruma seçimi; schema migration; `.trash` geri yükleme; mod log popup’ı; ikon ve kategori sistemi.

## Sonuç

Mevcut sistem bir “çalışan prototip mod yöneticisi” seviyesinde. Mod aç/kapat ve temel yönetim akışı mevcut; fakat UI’nin gerçek Unity runtime davranışı ve DLL yükleme durumlarının doğruluğu henüz üretim kalitesinde değil. En acil iki konu, ayar kontrollerinin gerçek görsel prefablarla değiştirilmesi ve modların `Loaded/Failed` durumunun dosya varlığından bağımsız izlenmesidir. Bu iki konu çözülmeden kullanıcıya “ayarlar kesin çalışıyor” veya “mod hazır” geri bildirimi verilmemeli.
