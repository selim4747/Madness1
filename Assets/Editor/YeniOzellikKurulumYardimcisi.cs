using UnityEditor;
using UnityEngine;

/// <summary>
/// Yeni eklenen özelliklerin sahneye nasıl bağlanacağını listeler.
/// KayipReferansBulucu penceresinden de açılır.
/// </summary>
public class YeniOzellikKurulumYardimcisi : EditorWindow
{
    [MenuItem("Delilik/Denetim/Yeni Özellik Kurulum")]
    public static void Ac()
    {
        GetWindow<YeniOzellikKurulumYardimcisi>("Yeni Özellik Kurulum");
    }

    private Vector2 scroll;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Yeni Özellikler — Yapman Gerekenler", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Aşağıdaki script'ler oyunu bozmaz. Sahnede yoksa hiçbir şey olmaz. " +
            "İstediğini ekle, istemediğini atla.",
            MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        Ciz("1. Günlük Hedef",
            "Boş GameObject oluştur → GunlukHedefYoneticisi ekle.\n" +
            "İsteğe bağlı: hedefKitapSat / hedefSohbet / hedefHirsiz değerlerini ayarla.\n" +
            "Ödül için EconomyManager ve ToastNotification sahnede olmalı.");

        Ciz("2. Raf Doluluk Göstergesi",
            "Her raf objesine RafDolulukGostergesi ekle.\n" +
            "Shelf alanına aynı objedeki ShelfController'ı sürükle.\n" +
            "Gösterge için bir child SpriteRenderer oluştur ve ata.");

        Ciz("3. Gece Öncesi Özet",
            "Boş GameObject → GeceOncesiOzet ekle.\n" +
            "GameManager.OnPhaseChanged otomatik dinlenir.\n" +
            "ToastNotification varsa mesaj oradan çıkar.");

        Ciz("4. Günlük Olay",
            "Boş GameObject → GunlukOlayYoneticisi ekle.\n" +
            "Yeni gün başlarken GunlukOlayYoneticisi.Instance.YeniGun(gun) çağır " +
            "(GameManager gün değişiminde veya manuel).");

        Ciz("5. Müşteri Tekrar Hoş Geldin",
            "Boş GameObject → MusteriTekrarHosgeldin ekle.\n" +
            "Müşteri spawn olunca MusteriTekrarHosgeldin.Instance.MusteriGeldi(isim) çağır " +
            "(CustomerController veya Spawner içinden).");

        Ciz("6. Öğrenci Teşekkür Bonusu",
            "Kod tarafı hazır (OgrenciTesekkurBonusu.Uygula()).\n" +
            "StudentDeskController teslim başarılı olunca bu metodu çağır.");

        Ciz("7. Kasa Sabır Barı",
            "UI Slider oluştur → KasaSabirBarı ekle.\n" +
            "Bar alanına Slider'ı ata.\n" +
            "Not: CustomerController.patienceTimer private. Gerçek bar için " +
            "CustomerController'a public float PatienceNormalized eklemen gerekir.");

        Ciz("8. Hırsız / Bahçe / İsyan varyasyonları",
            "Bunlar mevcut sistemlere küçük eklemelerdir.\n" +
            "Şimdilik ayrı script olarak eklenmedi; istersen sonra ThiefController ve " +
            "ShelfController üzerine varyant ekleyebiliriz.");

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Hepsi opsiyonel. Eklemessen oyun eskisi gibi çalışır.",
            MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void Ciz(string baslik, string aciklama)
    {
        EditorGUILayout.LabelField(baslik, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(aciklama, MessageType.None);
        EditorGUILayout.Space(4);
    }
}
