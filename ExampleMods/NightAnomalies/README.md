# Night Anomalies — Mono C# DLL mod örneği

Bu kaynak mod Madness projesinin **itch.io masaüstü Mono** sürümü için hazırlanmıştır. Play Store/IL2CPP sürümünde dışarıdan DLL olarak yüklenemez. Oyun derlemesinde yapılmış API uzantılarını da gerektirir; önce proje kaynaklarında `EnemySpawnContext.IsElite`, `EnemyKilledContext`, `EnemyController.SetElite` ve WaveManager bağlantısı derlenmelidir.

## İçerik

- Her gece rastgele üç olaydan biri: **Kanlı Ay** (daha büyük/sık dalga, artan risk), **Altın Avı** (daha az düşman, yüksek elit ödülü), **Tutulma** (daha tehlikeli düşmanlar ve daha dayanıklı elitler).
- Gecede en fazla beş elit düşman. Elitler normalden daha dayanıklı ve güçlüdür; kırmızı/turuncu görünür ve öldürüldüklerinde altın verir.
- Gündüz satın alınan **Kan Yemini**: 150 altın karşılığında sonraki iki gece oyuncu hasarını %40 artırır; alınan hasarı %25 ve gece düşman havuzunu artırır. Elit çıkma ve ödül ihtimali de artar.
- Gece **Şok Dalgası**: 35 altın, 18 saniye bekleme, oyuncunun çevresindeki 3.25 birimlik alanda `DamageStat × 2.5` hasar.
- UI için ek sprite/prefab gerektirmeyen IMGUI düğmeleri. Desktop itch.io prototipi içindir; mobil görünüm tasarlanmamıştır.

## Derleme

1. Projeyi Unity 6000.4.6f1'de açıp önce değiştirilen oyun scriptlerinin derlendiğini doğrulayın.
2. Bir **Mono standalone** oyun derlemesi alın.
3. Oyunun `<OyunAdı>_Data/Managed/` klasörünü bulun. Bu klasördeki `Assembly-CSharp.dll` ve Unity modül DLL'leri mod API'si için referans alınır.
4. `NightAnomalies.csproj` içindeki `GameManagedDir` değerini bu klasörün mutlak yoluna ayarlayın. Örnek: `dotnet build NightAnomalies.csproj -p:GameManagedDir="C:/Games/Madness/Madness_Data/Managed" -c Release`.
5. Oluşan `bin/Release/NightAnomalies.dll` dosyasını mod paketinin köküne, `manifest.json` ile aynı seviyeye koyun.
6. `manifest.json` ve `NightAnomalies.dll` dosyalarını birlikte ZIP'leyin. Oyundaki Mod Ekle işleviyle içe aktarın veya masaüstü sürümünün persistent `Mods/night_anomalies/` klasörüne yerleştirin.

`Assembly-CSharp.dll` oyuna ve API sürümüne bağlıdır. Modu dağıttığınız her oyun sürümünün aynı API ile derlendiğini doğrulayın; oyun içindeki `apiVersion` şu an `1`.

## Güvenlik ve bilinen sınırlamalar

DLL çalıştırmak mod yazarına oyun sürecinde C# kodu çalıştırma imkânı verir. Yalnızca güvendiğiniz kaynaktan gelen modları yükleyin; bu örnek güvenlik sandbox'ı değildir. Unity/oyun dosyaları olmadan taşınabilir DLL üretilemez. Yeni düşman ayrı bir prefab değil, mevcut düşman tipinin güçlendirilmiş/renklenmiş elit varyantıdır. UI, `OnGUI` tabanlı prototiptir. Oyun içinde test edilmeden yayımlanmamalıdır.
