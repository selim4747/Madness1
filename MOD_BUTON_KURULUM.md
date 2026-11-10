# Mod Buton Kurulumu (Inspector)

Kod otomatik buton oluşturmaz. Kendi butonlarını ekleyip bağlarsın.

## 1. Mod Dosyası Ekle Butonu

1. AnaMenu sahnesini aç.
2. Mevcut butonların yanına yeni bir **Button** ekle (yazısı: "Mod Ekle").
3. Herhangi bir GameObject'e (veya butonun kendisine) **ModImportUI** component'i ekle.
4. Inspector'da:
   - **Import Button** → yeni butonunu sürükle.
   - **Status Text** (isteğe bağlı) → bir Text sürükle.
5. Play'e bas. Butona basınca dosya seçici açılır.

Alternatif: MainMenuController üzerinde **Mod Import Button** alanına butonu sürükle (ModImportUI olmasa da çalışır).

## 2. Yeni Sahne Yükle Butonu

1. Yeni bir Button ekle (yazısı: "Yeni Harita" veya istediğin isim).
2. Bir GameObject'e **ModSceneButton** component'i ekle.
3. Inspector'da:
   - **Load Button** → butonu sürükle.
   - **Scene Name** → yüklenecek sahnenin adı (Build Settings'e eklenmiş olmalı).
   - **Additive** → işaretlersen mevcut sahnenin üstüne yükler.
   - **From Mod Id** (isteğe bağlı) → bir modun `manifest.json` içindeki `worldScene` alanını kullanır.

Sahne Build Settings'te olmalı:
File → Build Settings → Scenes In Build listesine sahneyi ekle.

## 3. Mod Manifest'te Sahne

Mod paketinin `manifest.json` örneği:

```json
{
  "id": "yeni_harita",
  "name": "Yeni Harita",
  "version": "1.0.0",
  "apiVersion": "1",
  "enabled": true,
  "worldScene": "ModHarita1"
}
```

`ModSceneButton` içinde **From Mod Id** = `yeni_harita` yazarsan, buton otomatik olarak `ModHarita1` sahnesini yükler.

## Notlar
- Buton atamazsan hiçbir şey bozulmaz.
- `createButtonIfMissing` işaretliyse ve buton boşsa eski otomatik buton oluşur (varsayılan kapalı).
