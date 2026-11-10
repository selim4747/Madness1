# Tam Dönüşüm Modları (Yeni Oyun / Harita / Karakter / Diyalog)

Mod artık kendi içinde **sahne, karakter, diyalog ve oyun akışı** taşıyabilir.

## Mod Paketi Yapısı

```
my_total_mod.zip
├── manifest.json
├── content.bundle          ← AssetBundle (sahne + prefab + SO + sprite)
├── Mod.dll                 ← isteğe bağlı C# mantık
└── config.json             ← isteğe bağlı basit ayarlar
```

## manifest.json (tam dönüşüm örneği)

```json
{
  "id": "yeni_oyun",
  "name": "Yepyeni Bir Oyun",
  "version": "1.0.0",
  "apiVersion": "1",
  "enabled": true,
  "totalConversion": true,
  "mainScene": "ModAnaSahne",
  "assetBundle": "content.bundle",
  "entryScript": "Mod.dll"
}
```

`totalConversion: true` ise oyun açılışında normal akış atlanır, modun sahnesi yüklenir.

## AssetBundle İçeriği

Unity'de bir AssetBundle oluşturup içine koyabilirsin:

- Sahneler (`.unity`) → harita tamamen değişir
- Karakter prefabları
- `StoryDialogueData`, `CustomerData`, `BookData` vb. ScriptableObject'ler
- Sprite, ses, animasyon

### Karakter değiştirme
Mod DLL'inde veya yükleme anında:

```csharp
var prefab = ModAssetLoader.LoadAsset<GameObject>("yeni_oyun", "YeniKarakter");
ModContentOverride.Register("player_prefab", prefab);
ModContentOverride.Register("customer_normal", baskaPrefab);
```

Mevcut spawn kancaları (`CustomerSpawnContext`, `EnemySpawnContext`) bu override'ları kullanabilir.

### Diyalog değiştirme
```csharp
ModEventBus.Subscribe<DialogueContext>(ctx =>
{
    if (ctx.EventId == "story_001")
    {
        ctx.Lines.Clear();
        ctx.Lines.Add(new DialogueLine { speaker = "Yeni Karakter", text = "Bu tamamen yeni bir hikaye." });
    }
});
```

Veya AssetBundle'daki `StoryDialogueData` asset'ini `ModContentOverride` ile kaydet.

## Buton ile Sahne Yükleme

1. Buton ekle.
2. `ModSceneButton` component'i ekle.
3. Inspector:
   - **Load Button** → buton
   - **From Mod Id** → `yeni_oyun`
   - **Scene Name** → `ModAnaSahne` (boş bırakılırsa bundle'daki ilk sahne)
   - **Load From Mod Bundle** → işaretli

Sahne Build Settings'te olmak zorunda değil.

## Ne Değişebilir?

| Şey | Nasıl |
|-----|-------|
| Harita / Sahne | AssetBundle içindeki sahne |
| Karakter prefabı | `ModContentOverride` + spawn context |
| Tüm diyaloglar | `DialogueContext` veya SO override |
| Oyun akışı | `totalConversion` + kendi sahne + C# DLL |
| Mekanikler | Mevcut event kancaları + yeni kod |

Vanilla mod yokken eski oyun aynen çalışır.
