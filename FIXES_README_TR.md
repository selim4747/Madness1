# Madness1 mod sistemi düzeltmeleri

Bu paket, mod sistemi ve ilgili oyun akışlarında yapılan düzeltmeleri içerir.

## Önemli düzeltmeler

- `MyCSharpMod` satış fiyatı çarpanı `29.0f` olarak gerçek satış akışına bağlandı. Satış fiyatı context'i üzerinden çalışan built-in örnek mod, normal fiyatı 29 ile çarpar.
- `ExampleMods/csharp_example/MyMod.cs` aynı `29.0f` çarpanını içerir.
- Kaynak `.cs` dosyasını değiştirmek mevcut `Mod.dll` dosyasını otomatik olarak değiştirmez. Harici Mono DLL modunun gerçekten güncellenmesi için `MyMod.cs`, Unity/Mono referanslarıyla yeniden derlenip aynı mod klasöründeki `Mod.dll` üzerine kopyalanmalıdır.
- Built-in ve DLL modlarında aynı ID çakışması güvenli biçimde reddedilir; başarısız mod kaydı takip edilmez.
- Mod event/content kayıtları mod sahibine göre temizlenir.
- Total conversion başlatma akışı `GameInitializer` üzerinden bağlandı.
- Diyalog override'ı merkezi `DialogueBox.Show` akışına bağlandı.
- Müşteri prefabı, müşteri hedef pozisyonu ve hikâye NPC prefabı mod override sistemini kullanır.
- Müşteri mod hedefi artık gerçek `MoveTo` hedefini etkiler; `(0, 0, 0)` da geçerli mod hedefidir.
- Başarısız düşman spawn'ları dalga başarı sayacını artırmaz.
- Save slot doğrulaması, mod save callback'leri ve geçici dosya/yedekli save yazımı eklendi. Android'te `File.Replace` desteklenmezse fallback kullanılır.
- Mod manifest'i total conversion, API ve entry script yol kontrolleriyle doğrulanır.

## Test notu

Bu çalışma ortamında Unity Editor/Unity CLI bulunmadığı için gerçek Unity import, C# compile, Android IL2CPP build ve runtime testi çalıştırılamadı. Paketleme öncesinde statik C# kontrolleri, parantez dengesi, kritik referans kontrolleri ve ZIP bütünlük testi yapılmıştır.
