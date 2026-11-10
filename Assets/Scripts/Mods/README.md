# Madness Mod API çekirdeği

Bu klasör, mevcut oyunun varsayılan davranışını değiştirmeden modların bağlanacağı çekirdektir.

## İlk sürümdeki kancalar

- `DayContext`: gün süresi ve gün başlangıcı
- `PhaseChangedContext`: faz değişimleri
- `GoldContext`: para ekleme/harcama
- `SaleContext`: satış fiyatı ve satış iptali
- `CustomerSpawnContext`: müşteri prefabı, profili ve spawn konumu
- `EnemySpawnContext`: düşman tipi, konumu ve can/hasar çarpanları
- `DamageContext`: hasar miktarı, hasar iptali ve dokunulmazlık davranışı
- `WorldLoadContext`: harita/sahne yükleme isteği

## Modların kayıt alanı

`ModRuntime.SetState(modId, json, schemaVersion)` ile mod verisi ana kayıt şemasından ayrılır. `SaveData` içine tek tek mod alanı eklenmez.

## Harita ve konumlar

Yeni veya değişen haritalar `ModWorldService` üzerinden yüklenebilir. Sahne içindeki kritik konumlar `ModWorldAnchor` ile isimlendirilirse modlar sabit koordinatlara bağlı kalmadan konum çözebilir:

```csharp
Vector3 position;
ModWorldRegistry.TryGetPosition("customer_spawn", out position);
```

## Güvenlik ve uyumluluk

Bu çekirdek henüz dışarıdan C# DLL çalıştırmaz. Mod kodları ileride yalnızca bu API üzerinden bağlanmalıdır. Event callback hataları ana oyuna taşınmaz; bir modun exception'ı ilgili callback'te yakalanır.

## Cihazdan mod ekleme

Ana menüde oluşturulan **MOD DOSYASI EKLE** butonu Android dosya seçicisini açar. Seçilen `.zip`, `.mod` veya `.madnessmod` arşivi önce geçici alana kopyalanır, sonra güvenli biçimde açılır. Paket kökünde tek bir `manifest.json` bulunmalıdır:

```text
my_mod.zip
└── manifest.json
```

Örnek manifest:

```json
{
  "id": "my_mod",
  "name": "My Mod",
  "version": "1.0.0",
  "apiVersion": "1",
  "enabled": true
}
```

Arşiv `Application.persistentDataPath/Mods/<id>/` altına alınır. Path traversal (`../`) ve 256 MB üzerindeki açılmış paketler reddedilir. Aynı id yeniden aktarılırsa eski sürüm yedeklenip yalnızca yeni paket başarıyla taşındığında değiştirilir.
