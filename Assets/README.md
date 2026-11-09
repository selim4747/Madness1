# Madness1

Unity 6 (6000.4.6f1) 2D kitapçı / kütüphane temalı oyun.

## Özellikler
- Gündüz: müşteri servisi, raf yönetimi, bahçe sohbeti, özel raflar, öğrenci sistemi
- Gece: dalga / boss savaşı
- Kapsamlı save sistemi (3 slot + gün başı snapshot + oturum)
- NPC kimlikleri, çoklu kişilik, diyalog, dedikodu, başarımlar

## Kurulum
1. Unity 6000.4.6f1 (veya uyumlu) ile aç.
2. NavMeshPlus paketi Packages/manifest.json içinde zaten tanımlı.
3. Ana sahne: `Assets/Scenes/AnaMenu.unity`

## Önemli Sistemler
- `ShelfRegistry` — rafları merkezi tutar (FindObjectsOfType yerine)
- `CustomerController` — müşteri AI state machine
- `CounterController` — kasa kuyruğu
- `SaveSystem` — kayıt / yükleme
- `EconomyManager`, `GameManager`, `BahceYoneticisi` vb. singleton'lar

## Geliştirme Notları
- Debug log'ları `DevLog` üzerinden gider (Editor / Development Build dışında sessiz).
- NavMesh geçit ışınlaması için `NavMeshPortalConfig` ScriptableObject oluşturup CustomerController'a atayabilirsin.
- Build klasörleri ve `.DS_Store` `.gitignore`'da.

## Not
Bu README otomatik oluşturulmuştur. Daha fazla detay için script yorumlarına bak.
