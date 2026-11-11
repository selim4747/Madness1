# Mod Sistemi ve Inspector UI Risk İncelemesi

Tarih: 2026-11-11  
Proje: Unity 6.0.4.6f1 / Madness  
İncelenen ana dosyalar: `ModManagerUI.cs`, `ModPanelController.cs`, `ModRuntime.cs`, `ModSettingsStore.cs`, `AnaMenu.unity`

## Kısa sonuç

Inspector yapılandırması ve yeni panel sahneye bağlanmış durumda. C# dosyalarında parantez dengesi ve sahne script referansı statik olarak doğrulandı. Ancak Unity Editor/Play Mode bu ortamda bulunmadığından gerçek UI oluşturma, prefab davranışı, compile ve Android IL2CPP testi yapılamadı.

En yüksek riskli bulgu, runtime oluşturulan Dropdown'un açılır liste item template'inde `Toggle` ve item text bulunmamasıdır. Dropdown component'i sahnede oluşturulabilir fakat açıldığında seçenek listesi düzgün oluşmayabilir. İkinci önemli bulgu, mod satırının `preferredHeight` değerinin sabit kalmasıdır; bir modun birden fazla ayarı varsa ayarlar satırın dışına taşabilir veya ScrollRect içinde üst üste binebilir.

## P0 — Kullanıcıya doğrudan görünen veya sistemi bozabilecek sorunlar

### 1. Dropdown template eksik

`CreateDropdown()` içinde Template, Viewport ve Content oluşturuluyor; fakat Unity UI Dropdown'un item üretmesi için template altında bir `Toggle` ve bu Toggle altında `Text`/item text bulunmuyor. `Dropdown.Show()` sırasında template'ten Toggle arandığı için seçenekler görünmeyebilir veya null reference/log hatası oluşabilir.

**Düzeltme:** Template altına gerçek bir `Toggle` item, onun altına bir `Text` (`itemText`) ve gerekli LayoutElement eklenmeli. Template'in RectTransform ölçüleri ve viewport/content anchor'ları da atanmalı.

### 2. Mod satır yüksekliği ayar sayısını hesaba katmıyor

Her satıra `preferredHeight = rowHeight` atanıyor. Ancak satırın içine başlık, açıklama ve sıfır veya daha fazla setting satırı ekleniyor. `rowHeight` sabit 88 kaldığında çok ayarlı modlarda içerik kırpılabilir veya sonraki modla çakışabilir.

**Düzeltme:** `rowHeight` minimum yükseklik olarak kullanılmalı; preferred height ayar sayısına göre hesaplanmalı veya satırda `ContentSizeFitter`/daha kontrollü layout kullanılmalı.

### 3. Runtime ScrollRect sahne düzenini değiştirebilir

`createScrollRectWhenMissing` açık olduğunda `ModManagerScrollRect` ve `ModManagerViewport` runtime'da oluşturuluyor, mevcut `listRoot` yeni viewport altına taşınıyor. Bu işlem:

- Panelde önceden hazırlanmış başka child sıralarını değiştirebilir.
- ScrollRect'in tam genişlik/yükseklik ölçüsünü parent layout'a göre yanlış alabilir.
- `listRoot` üzerinde zaten ScrollRect/Mask düzeni varsa ikinci bir sarmalayıcı oluşmasına yol açabilir.
- Panel arka planı ve dimmer ile sibling sırası nedeniyle görsel katmanlama sorunu çıkarabilir.

**Öneri:** Production sahnesinde ScrollRect, Viewport ve Content hiyerarşisini Inspector'dan önceden kurup `scrollRect` alanına atamak; runtime otomatik kurulumunu yalnızca fallback olarak kullanmak.

### 4. Inspector'da `allowRuntimeBuild = false` güvenli ama eksik UI ile sessizce çalışabilir

Bu seçenek kapatıldığında `listRoot` boş, `ScrollRect` atanmamış veya gerekli layout component'leri eksikse panel boş kalabilir. Kullanıcıya Inspector uyarısı verilmediği için hata yalnızca runtime'da fark edilir.

**Öneri:** `OnValidate()` içinde eksik `listRoot`, ScrollRect ve gerekli component'ler için Inspector warning üretmek; mümkünse custom editor ile alanları zorunlu göstermek.

## P1 — Orta/yüksek öncelikli runtime sorunları

### 5. Setting tipi null veya geçersizse hata riski

`BuildSettings()` doğrudan `setting.type.Equals(...)` çağırıyor. Doğrudan Mods klasörüne koyulan veya eski bir manifestte `type` null ise `NullReferenceException` oluşabilir. `min > max`, NaN/infinity veya geçersiz default değerler de slider davranışını bozabilir.

**Düzeltme:** Tipi normalize etmek, bilinmeyen tipi `string` olarak ele almak, min/max sırasını düzeltmek ve sayısal değerleri finite/range doğrulamasından geçirmek.

### 6. Slider her değişiklikte diske yazıyor

Slider sürüklenirken `onValueChanged` her frame benzeri sıklıkta çalışabilir. Her değişiklikte `ModSettingsStore.Save()` çağrılması mobil cihazlarda gereksiz I/O, takılma ve dosya yıpranması oluşturabilir.

**Düzeltme:** Değeri bellekte tutup `onEndDrag`, Uygula butonu veya debounce ile kaydetmek.

### 7. Dinamik kontroller görsel olarak temel seviyede olsa da tam Unity UI template'i değil

Toggle ve Slider artık görsel alt nesnelere sahip. Ancak kontrol RectTransform anchor/pivot/size değerleri sınırlı ayarlanıyor. Farklı çözünürlük, Canvas Scaler ve dar Android ekranlarında başlık ile butonlar yatay alana sığmayabilir.

**Öneri:** Mobil için satırın yatay yapısını responsive yapmak; kontrol gruplarını ikinci satıra geçirmek veya `HorizontalLayoutGroup` child alignment/overflow davranışını Inspector'dan ayarlamak.

### 8. Runtime script component'i sahneye manuel eklendiği için duplicate component riski var

`ModManagerUI` artık `AnaMenu.unity` içinde mevcut. `ModPanelController.Attach()` mevcut component'i bulduğu için normal akışta duplicate oluşmaz. Ancak başka bir sahnede aynı panel prefab'ı tekrar kullanılırsa veya bir script ayrıca `AddComponent<ModManagerUI>()` çağırırsa iki UI listener'ı ve iki Refresh döngüsü oluşabilir.

**Öneri:** `Awake()` içinde singleton değil, aynı GameObject üzerindeki duplicate component kontrolü veya `RequireComponent`/prefab standardı kullanmak.

## P1 — Mod runtime güvenlik ve veri bütünlüğü sorunları

### 9. Manifest yazımı hâlâ tam atomik değil

`ModRuntime.WriteManifest()` ve `ModSettingsStore.Save()` önce mevcut dosyayı siliyor, sonra temp dosyasını taşıyor. Oyun veya cihaz yazma sırasında kapanırsa manifest/ayar dosyası kaybolabilir.

**Düzeltme:** `File.Replace` ve `.bak` fallback kullanmak; temp dosyasını flush edip replace etmek.

### 10. Mod ID dosya yolu için güvenli değil

`ModSettingsStore.PathFor()` mod ID'yi doğrudan dosya adına ekliyor. Import dışından gelen veya kötü niyetli manifestlerde path karakterleri sorun çıkarabilir.

**Düzeltme:** Tek merkezî Mod ID doğrulaması; yalnızca `[a-z0-9._-]` gibi güvenli karakterlere izin vermek ve ID'yi canonicalize etmek.

### 11. Duplicate manifest ID sessizce atlanıyor

`ScanManifests()` aynı ID'ye sahip ikinci manifesti hata olarak raporlamadan ilk manifesti kullanıyor. Kullanıcı yanlış mod sürümünün neden seçildiğini anlayamayabilir.

**Düzeltme:** Duplicate ID'yi diagnostic error olarak raporlamak ve iki manifesti de etkin olmayan/çakışmalı duruma almak.

### 12. Bağımlılık kontrolü yalnızca doğrudan ve manifest seviyesinde

Şu anda eksik veya kapalı doğrudan bağımlılık kontrol ediliyor; ancak:

- A -> B -> C zincirinin tümü doğrulanmıyor.
- A -> B -> A döngüsü tespit edilmiyor.
- Bağımlılığın gerçekten register edildiği kontrol edilmiyor.
- Sürüm aralığı desteği bulunmuyor.

**Düzeltme:** DFS tabanlı dependency graph, cycle detection ve yükleme sonucu kontrolü.

### 13. DLL varlığı “Hazır” ile karıştırılıyor

`GetModStatus()` DLL dosyasının varlığını kontrol ediyor; ancak `Assembly.Load`, doğru `IModModule`, ID eşleşmesi, `Register()` sonucu ve gerçek loaded state ayrı tutulmuyor.

**Düzeltme:** `Disabled`, `Ready`, `Loaded`, `LoadFailed`, `InvalidModule`, `MissingDependency`, `Conflict`, `IL2CPPUnsupported` gibi enum tabanlı gerçek durum.

### 14. Yanlış DLL modülü fallback ile yüklenebilir

`ModScriptLoader.FindModuleType()` beklenen ID bulunamazsa ilk uygun `IModModule` tipini seçiyorsa bir DLL başka modun modülü gibi yüklenebilir.

**Düzeltme:** Manifest ID ile module ID eşleşmesi zorunlu olmalı; eşleşme yoksa DLL yüklenmemeli.

## P2 — Kullanılabilirlik ve bakım

- `statusText` sahnede atanmadığı için kullanıcıya hata/uygulama mesajları görünmeyebilir.
- `panelImage` Inspector'dan atanıyor ancak panel rengini uygulayan kod bulunmuyor; alanın adı kullanıcıda etkili olduğu izlenimi oluşturuyor.
- Eski `BuiltInModSelectionUI` sahnede disable edilmiş olsa da proje içinde tutuluyor; ileride yanlışlıkla tekrar açılabilir.
- Mod silindikten sonra `.trash` içinden geri yükleme UI'ı yok.
- Ayarların sıfırlanması için panel butonu yok.
- Save schema migration altyapısı tanımlı değil.
- `OnModsChanged` event listener'larından birinin exception vermesi diğer listener akışını etkileyebilir; listener bazlı izolasyon eklenmeli.
- Mod ID'siyle oluşturulan `GameObject` adları ve text içerikleri normalize edilmediğinden çok uzun/özel karakterli modlar UI düzenini bozabilir.

## Önerilen düzeltme sırası

1. Dropdown template'ini gerçek Toggle + item Text yapısına tamamlamak.
2. Mod satırlarının yüksekliğini dinamik yapmak ve ScrollRect'i sahnede hazır hiyerarşi olarak kurmak.
3. `OnValidate()`/custom editor ile Inspector bağlantılarını doğrulamak.
4. Setting tipi, min/max/default ve mod ID doğrulamasını merkezîleştirmek.
5. Gerçek mod load state enum'u ve zorunlu module ID eşleşmesi eklemek.
6. Manifest ve ayar yazımını atomik `.bak`/replace yöntemiyle değiştirmek.
7. Bağımlılık grafiği, döngü ve duplicate ID kontrollerini eklemek.
8. Unity Editor Play Mode, Windows Mono DLL ve Android IL2CPP test matrisi çalıştırmak.

## Test matrisi

| Test | Beklenen sonuç |
|---|---|
| 0 mod | Panel boş fakat hata vermeden açılır |
| 1 built-in mod | Toggle görünür, silme/sıra butonları görünmez |
| 1 DLL mod | Doğru loaded/failed durumu görünür |
| 5+ setting içeren mod | Satır içeriği taşmaz, ScrollRect kaydırır |
| Dropdown ayarı | Liste açılır, seçenek seçilir ve kaydedilir |
| Dar Android çözünürlüğü | Başlık ve butonlar üst üste binmez |
| Eksik listRoot + runtime build kapalı | Inspector warning oluşur |
| Eksik DLL | Mod hata durumu gösterir, oyun açılmaya devam eder |
| Duplicate ID | Sessizce ilk mod seçilmez; diagnostic error gösterilir |
| A -> B -> A bağımlılığı | Döngü raporlanır, modlar yanlış sırada yüklenmez |
| Oyun manifest yazarken kapanır | Önceki veya yeni manifest kurtarılabilir |
| Yanlış DLL module ID | DLL reddedilir, başka mod olarak yüklenmez |

## Sonuç

Mevcut sürüm temel panel ve mod yönetimi için kullanılabilir bir prototiptir. Ancak özellikle Dropdown template'i, dinamik satır yüksekliği ve gerçek mod yükleme durumu düzeltilmeden üretim sürümü olarak değerlendirilmemelidir. Unity Editor/Play Mode bulunmadığı için bu rapor statik inceleme ve Unity UI yaşam döngüsü risklerine dayanır.
