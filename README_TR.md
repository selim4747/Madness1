# Madness Mod Creator — İlk Kullanılabilir Sürüm

Bu paket yalnızca yeni/değişen kodları içerir; tüm Unity projesi değildir.

## Projeye kopyalanacak dosyalar

- `Assets/Scripts/Mods/ModCreatorModels.cs`
- `Assets/Scripts/Mods/ModContentPackageLoader.cs`
- `Assets/Scripts/Mods/ModRuntime.cs` — mevcut dosyada ilgili değişiklik vardır
- `Assets/Scripts/Mods/ModAPI.cs` — mevcut dosyada ilgili değişiklik vardır
- `Assets/Editor/Mods/ModCreatorWindow.cs`

## Kullanım

1. Dosyaları aynı yollarla Madness projenize kopyalayın.
2. Unity Editor açıldıktan sonra menüden `Madness > Mod Creator` seçin.
3. `New Mod Creator Project` oluşturun veya mevcut `.asset` projesini seçin.
4. ID, görünen ad, sürüm ve yazarı doldurun.
5. Asset ekleyin. Prefab, Sprite, Material, Audio, ScriptableObject veya Scene seçebilirsiniz.
6. `replaceId` boşsa yeni içerik olarak paketlenir. Doluysa o içerik ID'sinin yerine geçmesi amaçlanır.
7. Mod panelinde görünecek ayarları ekleyin.
8. Event ID verilmiş diyalog patch'leri ekleyin.
9. İsterseniz ana DLL'i seçin.
10. `.madnessmod paketi oluştur` düğmesine basın.
11. Oluşan paketi oyunun `ModImportUI` panelinden içe aktarın.

## DLL güvenliği

DLL modları oyunun yetkileriyle C# kodu çalıştırabilir. Yalnızca kaynağına güvendiğiniz DLL'leri ekleyin.

## Önemli sınır

Bir asset'in `replaceId` ile override edilmesi, oyunun ilgili sistemi o ID'yi `ModAPI.GetModContent<T>` veya mevcut `ModContentOverride` üzerinden okuyorsa etkili olur. Oyunun her sistemini otomatik ve risksiz şekilde değiştirmek için ayrıca oyun sistemlerine özel adapter/hook'lar gerekir. Bu sürüm paketleme, asset bundle, diyalog patch'i, ayar manifesti ve DLL içe aktarma temelini sağlar.

## Doğrulama

Unity Editor bulunmayan ortamda gerçek Play Mode ve AssetBundle build testi yapılamaz. Unity içinde ilk testte boş asset, geçersiz ID, DLL bulunamaması ve toplam dönüşüm sahnesi durumlarını tek tek kontrol edin.
