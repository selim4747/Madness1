# Madness / Delilik — Mod Oluşturma Rehberi

Bu rehber, oyuna **mevcut mekanikleri değiştiren**, **yeni harita/karakter/diyalog ekleyen** veya **tamamen yeni bir oyun** haline getiren modların nasıl yapılacağını anlatır.

---

## 1. Mod Paketi Temel Yapısı

Bir mod şu dosyalardan oluşur:

```
benim_modum.zip
├── manifest.json          ← zorunlu
├── config.json            ← isteğe bağlı (kodsuz basit değişiklikler)
├── content.bundle         ← isteğe bağlı (sahne, prefab, diyalog, sprite)
└── Mod.dll                ← isteğe bağlı (C# mantık)
```

Paketi `.zip`, `.mod` veya `.madnessmod` uzantısıyla kaydedebilirsin.

---

## 2. manifest.json (Zorunlu)

Her modun kökünde tek bir `manifest.json` olmalıdır.

### En basit örnek
```json
{
  "id": "kolay_gece",
  "name": "Kolay Gece",
  "version": "1.0.0",
  "apiVersion": "1",
  "author": "Sen",
  "enabled": true,
  "loadOrder": 100
}
```

### Tam dönüşüm örneği (yeni oyun)
```json
{
  "id": "yeni_oyun",
  "name": "Yepyeni Bir Macera",
  "version": "1.0.0",
  "apiVersion": "1",
  "author": "Sen",
  "enabled": true,
  "totalConversion": true,
  "mainScene": "ModAnaSahne",
  "assetBundle": "content.bundle",
  "entryScript": "Mod.dll",
  "loadOrder": 10
}
```

### Alan açıklamaları

| Alan | Zorunlu | Açıklama |
|------|---------|----------|
| `id` | Evet | Benzersiz kimlik (boşluk ve özel karakter yok) |
| `name` | Evet | Görünen isim |
| `version` | Evet | Mod sürümü |
| `apiVersion` | Evet | Şu an `"1"` olmalı |
| `enabled` | Hayır | `false` ise yüklenmez |
| `loadOrder` | Hayır | Küçük sayı önce yüklenir |
| `entryScript` | Hayır | C# DLL dosya adı (örn. `"Mod.dll"`) |
| `assetBundle` | Hayır | AssetBundle dosya adı |
| `mainScene` | Hayır | Total conversion ana sahne adı |
| `worldScene` | Hayır | Alternatif sahne adı |
| `totalConversion` | Hayır | `true` ise oyun açılışında bu modun sahnesi yüklenir |

---

## 3. Kodsuz Basit Mod (config.json)

Kod yazmadan sayıları değiştirmek için `config.json` yeter.

```json
{
  "nightPoolMultiplier": 2.0,
  "spawnIntervalMultiplier": 0.6,
  "goldGainMultiplier": 1.5,
  "goldSpendMultiplier": 0.8,
  "playerDamageTakenMultiplier": 0.5,
  "enemyDamageTakenMultiplier": 1.3,
  "enemyHealthMultiplier": 1.4,
  "enemyDamageMultiplier": 1.2,
  "dayDurationMultiplier": 1.3
}
```

| Alan | Etki |
|------|------|
| `nightPoolMultiplier` | Gece düşman sayısı çarpanı |
| `spawnIntervalMultiplier` | Düşman doğma aralığı (küçük = daha sık) |
| `goldGainMultiplier` | Kazanılan altın çarpanı |
| `goldSpendMultiplier` | Harcanan altın çarpanı |
| `playerDamageTakenMultiplier` | Oyuncunun aldığı hasar |
| `enemyDamageTakenMultiplier` | Düşman/boss'un aldığı hasar |
| `enemyHealthMultiplier` | Düşman can çarpanı |
| `enemyDamageMultiplier` | Düşman vuruş hasarı çarpanı |
| `dayDurationMultiplier` | Gün süresi çarpanı |

**Örnek paket:**
```
kolay_gece/
├── manifest.json
└── config.json
```

---

## 4. AssetBundle ile Harita / Karakter / Diyalog

Sahne, prefab, sprite, ses ve ScriptableObject’leri modun içine koymak için **AssetBundle** kullan.

### Unity’de AssetBundle oluşturma

1. Yeni bir klasör aç: `Assets/ModContent/`
2. İçine sahne, prefab, diyalog asset’i vb. koy.
3. Her asset’i seç → Inspector altındaki **AssetBundle** alanına bir isim yaz (örn. `content`).
4. `Assets → Build AssetBundles` (veya kendi build script’in).
5. Çıkan `.bundle` dosyasını mod klasörüne `content.bundle` olarak koy.

### Desteklenen içerikler
- Sahneler (`.unity`) → tamamen yeni harita
- Karakter prefabları
- `StoryDialogueData`, `CustomerData`, `BookData` vb.
- Sprite, ses, animasyon

### Karakter değiştirme (C# tarafı)
```csharp
var yeniKarakter = ModAssetLoader.LoadAsset<GameObject>("yeni_oyun", "YeniOyuncu");
ModContentOverride.Register("player_prefab", yeniKarakter);

var yeniMusteri = ModAssetLoader.LoadAsset<GameObject>("yeni_oyun", "YeniMusteri");
ModContentOverride.Register("customer_normal", yeniMusteri);
```

### Diyalog değiştirme
```csharp
ModEventBus.Subscribe<DialogueContext>(ctx =>
{
    if (ctx.EventId == "story_001")
    {
        ctx.Lines.Clear();
        ctx.Lines.Add(new DialogueLine 
        { 
            speaker = "Yeni Karakter", 
            text = "Bu tamamen yeni bir hikaye satırı." 
        });
    }
}, priority: 0, owner: "yeni_oyun");
```

---

## 5. C# DLL Modu (Tam Kontrol)

### Adımlar
1. Visual Studio veya Rider’da **Class Library** (.NET Standard 2.1) oluştur.
2. Oyunun `Assets/Scripts/Mods/` klasöründeki dosyaları referans olarak ekle (veya derlenmiş DLL).
3. `ModModuleBase`’den türeyen bir sınıf yaz.
4. Projeyi derle → `Mod.dll` çıktısını mod klasörüne koy.
5. `manifest.json` içinde `"entryScript": "Mod.dll"` yaz.

### Örnek mod kodu
```csharp
using Madness.Mods;
using UnityEngine;

public class BenimModum : ModModuleBase
{
    public override string Id => "benim_modum";

    protected override void OnRegister(ModRuntime runtime)
    {
        // Oyuncu %50 daha az hasar alır
        Subscribe<DamageContext>(ctx =>
        {
            if (ctx.Target is PlayerStats)
                ctx.Amount *= 0.5f;
        });

        // Satışlar %30 daha karlı
        Subscribe<SaleContext>(ctx =>
        {
            ctx.Price = Mathf.RoundToInt(ctx.Price * 1.3f);
        });

        // Gece daha kalabalık
        Subscribe<NightContext>(ctx =>
        {
            ctx.PoolMultiplier *= 1.8f;
            ctx.SpawnIntervalMultiplier *= 0.7f;
        });

        Log("Mod yüklendi");
    }
}
```

### Kullanabileceğin kancalar

| Context | Ne değiştirebilirsin |
|---------|----------------------|
| `DamageContext` | Hasar miktarı, iptal, invulnerability |
| `SaleContext` | Satış fiyatı, satışı iptal |
| `GoldContext` | Para ekleme / harcama |
| `DayContext` | Gün süresi |
| `NightContext` | Düşman havuzu, spawn aralığı, boss gecesi |
| `PhaseChangedContext` | Faz geçişleri |
| `EnemySpawnContext` | Düşman tipi, konum, can/hasar çarpanı, spawn iptali |
| `CustomerSpawnContext` | Müşteri prefabı, profil, konum |
| `DialogueContext` | Diyalog satırları, konuşmacı, iptal |
| `WorldLoadContext` | Sahne yüklemeyi iptal veya değiştir |

---

## 6. Tamamen Yeni Oyun (Total Conversion)

1. Kendi sahnelerini ve içeriklerini AssetBundle’a koy.
2. İstersen C# DLL ile yeni sistemler yaz.
3. `manifest.json`:

```json
{
  "id": "yeni_dunya",
  "name": "Yeni Dünya",
  "version": "1.0.0",
  "apiVersion": "1",
  "enabled": true,
  "totalConversion": true,
  "mainScene": "BenimAnaSahne",
  "assetBundle": "content.bundle",
  "entryScript": "Mod.dll"
}
```

Oyun açıldığında normal akış atlanır, senin sahnen yüklenir.  
Artık harita, karakter, diyalog ve oyun işleyişi tamamen sana aittir.

## Örnek DLL mod: Night Anomalies
`ExampleMods/NightAnomalies/` klasöründe kaynak kodu, manifest, `.csproj` ve kurulum talimatları bulunur. Örnek; rastgele gece olayları, güçlendirilmiş elitler, risk karşılığı güç veren Kan Yemini ve Shockwave yeteneği ekler.

Bu örneğin elit sistemi oyun API'sine eklenen `EnemySpawnContext.IsElite`, `EnemyKilledContext` ve WaveManager/EnemyController bağlantılarını kullanır. Elitler mevcut prefabların varyantıdır; yeni prefab değildir. Kaynak klasörde hazır DLL yoktur: DLL, hedef Mono oyununun `Assembly-CSharp.dll` dosyasına göre derlenmelidir. Ayrıntılar için `ExampleMods/NightAnomalies/README.md` dosyasına bak.

---

## 7. Modu Oyuna Ekleme

### Yöntem A — Buton (önerilen)
1. Ana menüye bir Button ekle.
2. Bir GameObject’e `ModImportUI` component’i ekle.
3. Inspector’da **Import Button** alanına butonu sürükle.
4. Oyunda butona bas → dosya seç → mod yüklenir.

### Yöntem B — Elle
Mod klasörünü şu yere kopyala:
```
Application.persistentDataPath/Mods/<mod-id>/
```
(Android’de genellikle `/storage/emulated/0/Android/data/<paket>/files/Mods/`)

---

## 8. Yeni Sahne Butonu

1. Buton ekle.
2. `ModSceneButton` component’i ekle.
3. Inspector:
   - **Load Button** → buton
   - **From Mod Id** → modunun `id`’si
   - **Scene Name** → sahne adı (boşsa bundle’daki ilk sahne)
   - **Load From Mod Bundle** → işaretli

Sahne Build Settings’te olmak zorunda değil.

---

## 9. İpuçları ve Sınırlar

- `apiVersion` mutlaka `"1"` olmalı.
- Bir mod hata verirse diğerlerini ve ana oyunu durdurmaz.
- `id` benzersiz olmalı.
- AssetBundle içindeki sahne adları `mainScene` / `Scene Name` ile eşleşmeli.
- Vanilla (hiç mod yokken) oyun eski haliyle çalışır.
- IL2CPP derlemede dinamik DLL yükleme sınırlı olabilir; Mono derlemede tam çalışır.

---

## 10. Hızlı Başlangıç Şablonları

### Şablon 1 — Sadece sayıları değiştir
```
kolay_mod/
├── manifest.json
└── config.json
```

### Şablon 2 — Yeni harita
```
yeni_harita/
├── manifest.json
└── content.bundle
```

### Şablon 3 — Tam yeni oyun
```
yeni_oyun/
├── manifest.json
├── content.bundle
└── Mod.dll
```

Bu üçünden biriyle başlayıp ihtiyacına göre genişletebilirsin.


---

## 11. IL2CPP ve Mono

| Özellik | IL2CPP | Mono |
|---------|--------|------|
| config.json (sayı değiştirme) | Çalışır | Çalışır |
| AssetBundle (sahne, prefab, diyalog, sprite) | Çalışır | Çalışır |
| totalConversion (yeni oyun) | Çalışır | Çalışır |
| ModSceneButton ile sahne yükleme | Çalışır | Çalışır |
| Dinamik C# DLL (`Mod.dll`) | **Çalışmaz** | Çalışır |

IL2CPP ile derlersen:
- Oyun **çökmez**.
- DLL modları sessizce atlanır.
- Basit modlar ve AssetBundle modları tam çalışır.
- Tam kod kontrolü (yeni mekanik yazmak) istiyorsan Mono’ya geç veya mantığı AssetBundle + mevcut kancalarla çöz.

**Öneri:** Önce IL2CPP ile dene. Sadece “kendi C# kodumu çalıştırayım” dersen Mono’ya geç.


---

## Mono'da Tam Güç (Sınır Yok)

Mono derlemede `Mod.dll` yüklenir ve modlar **tüm sınırları aşabilir**:
- Yeni MonoBehaviour / sistem
- Her mekaniği değiştirme
- AssetBundle + kendi kodun
- `ModAPI` ve `ModModuleBase` kullan

IL2CPP'de DLL yüklenmez; sadece `config.json` + AssetBundle çalışır.
Play Store için IL2CPP, tam mod desteği için Mono sürümünü ayrıca dağıt.
