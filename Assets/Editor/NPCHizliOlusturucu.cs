// NpcHizliOlusturucu.cs — Isim/soyad/meslek/diyalog HAVUZLARINDAN rastgele kombinasyonlarla
// hizlica birden fazla NPCKimligi uretir ve secilen CustomerData'nin karakterHavuzu'na ekler.
//
// HAVUZLAR ARTIK KALICI: NpcHavuzuAyarlari adinda bir ScriptableObject asset'inde saklanir.
// Metin havuzlari (isim/soyad/meslek/diyalog) BU PENCEREDEN hizlica duzenlenir. Kiyafet
// sprite havuzlari ISE asset'in KENDI Inspector'inden surukle-birak ile doldurulur.
//
// ONEMLI: NPCKimligi bir ScriptableObject DEGIL, duz bir C# sinifi — sadece bir CustomerData
// asset'inin ICINDE (karakterHavuzu listesinde) YASAR. Bu yuzden uretilen NPC'ler dogrudan
// HEDEF CustomerData'nin listesine EKLENIR (ayri .asset dosyasi olarak degil).
//
// URETILEN ISIM CAKISMASI: rastgele isim+soyad kombinasyonu, PROJEDEKI TUM CustomerData
// asset'lerindeki MEVCUT isimlerle karsilastirilir — "bulunan isimlerden olusmasin" sarti.
//
// Menu: Delilik > Hizli NPC Olustur
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class NpcHizliOlusturucu : EditorWindow
{
    private const string PrefsKey = "Delilik_NpcHavuzuAyarlariYolu";

    private NpcHavuzuAyarlari ayarlarAsset;
    private CustomerData hedefData;

    // Metin kutulari, ayarlarAsset'in ICERIGININ gorunumu — "Kaydet"e basana kadar asset'e
    // YAZILMAZ, sadece "Yenile" ile asset'ten tekrar okunur.
    private string kadinIsimlerText = "";
    private string erkekIsimlerText = "";
    private string soyadlarText = "";
    private string meslekText = "";
    private string diyalogText = "";
    private string meslekTurText = "";

    private int kacTaneUretilsin = 5;
    private int npcBasinaDiyalogSayisi = 3;
    private int npcBasinaSevilenTurSayisi = 2;

    private Vector2 scroll;

    [MenuItem("Delilik/Hizli NPC Olustur")]
    public static void PenceresiniAc()
    {
        GetWindow<NpcHizliOlusturucu>("Hizli NPC Olustur");
    }

    private void OnEnable()
    {
        string kayitliYol = EditorPrefs.GetString(PrefsKey, "");
        if (!string.IsNullOrEmpty(kayitliYol))
        {
            ayarlarAsset = AssetDatabase.LoadAssetAtPath<NpcHavuzuAyarlari>(kayitliYol);
            if (ayarlarAsset != null) AssetTenYukle();
        }
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("1) Havuz Dosyası (kalıcı — her açılışta hatırlanır)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        ayarlarAsset = (NpcHavuzuAyarlari)EditorGUILayout.ObjectField(
            new GUIContent("Havuz Ayarları", "Tum havuzlarin KALICI olarak saklandigi asset."),
            ayarlarAsset, typeof(NpcHavuzuAyarlari), false);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(PrefsKey, ayarlarAsset != null ? AssetDatabase.GetAssetPath(ayarlarAsset) : "");
            if (ayarlarAsset != null) AssetTenYukle();
        }

        if (ayarlarAsset == null)
        {
            EditorGUILayout.HelpBox("Henüz bir Havuz Ayarları dosyan yok. Aşağıdaki butonla yeni bir tane oluştur.", MessageType.Info);
            if (GUILayout.Button("Yeni Havuz Ayarları Dosyası Oluştur"))
            {
                YeniAyarDosyasiOlustur();
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("2) Metin Havuzları (her satıra bir tane)", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Kadın İsimleri:");
        kadinIsimlerText = EditorGUILayout.TextArea(kadinIsimlerText, GUILayout.Height(60));

        EditorGUILayout.LabelField("Erkek İsimleri:");
        erkekIsimlerText = EditorGUILayout.TextArea(erkekIsimlerText, GUILayout.Height(60));

        EditorGUILayout.LabelField("Soyadlar:");
        soyadlarText = EditorGUILayout.TextArea(soyadlarText, GUILayout.Height(50));

        EditorGUILayout.LabelField("Meslekler:");
        meslekText = EditorGUILayout.TextArea(meslekText, GUILayout.Height(50));

        EditorGUILayout.LabelField("Mağaza İçi Diyalog Havuzu:");
        diyalogText = EditorGUILayout.TextArea(diyalogText, GUILayout.Height(60));

        EditorGUILayout.LabelField("Meslek → Tür Eşleşmesi (format: \"Meslek: Tür1, Tür2\"):");
        meslekTurText = EditorGUILayout.TextArea(meslekTurText, GUILayout.Height(60));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("💾 Kaydet (asset'e yaz)")) AssetEKaydet();
        if (GUILayout.Button("↻ Yenile (asset'ten oku)")) AssetTenYukle();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("3) Kişilik Çarpanı Aralıkları", EditorStyles.boldLabel);
        FloatAraligiCiz("Sabır Çarpanı", ref ayarlarAsset.sabirCarpaniMin, ref ayarlarAsset.sabirCarpaniMax);
        FloatAraligiCiz("Ödeme Çarpanı", ref ayarlarAsset.odemeCarpaniMin, ref ayarlarAsset.odemeCarpaniMax);
        FloatAraligiCiz("İnceleme Süresi Çarpanı", ref ayarlarAsset.incelemeCarpaniMin, ref ayarlarAsset.incelemeCarpaniMax);
        EditorGUILayout.HelpBox("Kıyafet sprite havuzları (Kadın/Erkek) bu pencerede değil, Havuz Ayarları asset'inin KENDİ Inspector'ında sürükle-bırakla doldurulur.", MessageType.Info);

        GUILayout.Space(16);
        EditorGUILayout.LabelField("4) Hedef CustomerData", EditorStyles.boldLabel);
        hedefData = (CustomerData)EditorGUILayout.ObjectField(
            new GUIContent("Hedef Data", "Uretilen NPC'ler bunun karakterHavuzu'na eklenir."),
            hedefData, typeof(CustomerData), false);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("5) Üretim Ayarları", EditorStyles.boldLabel);
        kacTaneUretilsin = EditorGUILayout.IntField("Kaç NPC üretilsin:", kacTaneUretilsin);
        npcBasinaDiyalogSayisi = EditorGUILayout.IntField("NPC başına mağaza diyalog sayısı:", npcBasinaDiyalogSayisi);
        npcBasinaSevilenTurSayisi = EditorGUILayout.IntField("NPC başına sevilen tür sayısı:", npcBasinaSevilenTurSayisi);

        GUILayout.Space(16);

        GUI.enabled = hedefData != null;
        if (GUILayout.Button("Üret ve Ekle", GUILayout.Height(36)))
        {
            AssetEKaydet();
            Uret();
        }
        GUI.enabled = true;

        if (hedefData == null)
            EditorGUILayout.HelpBox("Önce bir CustomerData sürükle.", MessageType.Info);

        GUILayout.Space(24);
        MevcutKaraktereDiyalogEkleBolumunuCiz();

        EditorGUILayout.EndScrollView();
    }

    // ══════════ MEVCUT KARAKTERE DIYALOG EKLEME ══════════

    private CustomerData duzenlenecekData;
    private int seciliKarakterIndex = 0;
    private string yeniOzelDiyalogMetni = "";
    private bool havuzGoster = false;
    private Vector2 havuzScroll;

    private void MevcutKaraktereDiyalogEkleBolumunuCiz()
    {
        EditorGUILayout.LabelField("6) Mevcut Bir Karaktere Diyalog Ekle", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Yeni NPC uretmek yerine, ZATEN VAR OLAN birine (magaza) diyalog satiri eklemek icin. " +
            "Bahce sohbeti artik BahceKonuVerisi/BahceSohbetYoneticisi'nden geliyor, burada eklenmez.",
            MessageType.Info);

        duzenlenecekData = (CustomerData)EditorGUILayout.ObjectField(
            new GUIContent("Duzenlenecek CustomerData"), duzenlenecekData, typeof(CustomerData), false);

        if (duzenlenecekData == null || duzenlenecekData.karakterHavuzu == null || duzenlenecekData.karakterHavuzu.Count == 0)
        {
            if (duzenlenecekData != null)
                EditorGUILayout.HelpBox("Bu CustomerData'nin karakterHavuzu bos.", MessageType.Info);
            return;
        }

        List<NPCKimligi> havuz = duzenlenecekData.karakterHavuzu;
        string[] isimler = new string[havuz.Count];
        for (int i = 0; i < havuz.Count; i++)
            isimler[i] = havuz[i] != null && !string.IsNullOrEmpty(havuz[i].musteriIsmi) ? havuz[i].musteriIsmi : "(isimsiz #" + i + ")";

        seciliKarakterIndex = Mathf.Clamp(seciliKarakterIndex, 0, havuz.Count - 1);
        seciliKarakterIndex = EditorGUILayout.Popup("Karakter Sec", seciliKarakterIndex, isimler);

        NPCKimligi secili = havuz[seciliKarakterIndex];
        if (secili == null) return;

        int mevcutSayi = secili.ozelDiyaloglar != null ? secili.ozelDiyaloglar.Count : 0;
        EditorGUILayout.LabelField("Su anki mağaza diyalog sayısı: " + mevcutSayi);

        // HAVUZDAN SEC: ayarlarAsset.diyaloglar'daki (senin asset'e elle eklediklerin dahil)
        // her satiri burada gosterir. Yanindaki "+" butonuna basinca, o cumleyi asagidaki
        // metin kutusuna EKLER — Inspector'a gecip kopyala-yapistir yapmana gerek kalmaz.
        if (ayarlarAsset != null && ayarlarAsset.diyaloglar != null && ayarlarAsset.diyaloglar.Count > 0)
        {
            EditorGUILayout.LabelField("Havuzdan Seç (tıkla, aşağıdaki kutuya eklenir):", EditorStyles.boldLabel);

            havuzGoster = EditorGUILayout.Foldout(havuzGoster, "Genel Diyalog Havuzu (" + ayarlarAsset.diyaloglar.Count + " satır)");
            if (havuzGoster)
            {
                havuzScroll = EditorGUILayout.BeginScrollView(havuzScroll, GUILayout.Height(150));
                for (int i = 0; i < ayarlarAsset.diyaloglar.Count; i++)
                {
                    string satir = ayarlarAsset.diyaloglar[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(satir, EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button("+", GUILayout.Width(28)))
                    {
                        yeniOzelDiyalogMetni = string.IsNullOrEmpty(yeniOzelDiyalogMetni)
                            ? satir
                            : yeniOzelDiyalogMetni.TrimEnd('\n') + "\n" + satir;
                        GUI.FocusControl(null); // TextArea'nin hemen guncellenmesi icin
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        EditorGUILayout.LabelField("Eklenecek Yeni Diyalog(lar) (her satıra bir tane — yukarıdan '+' ile de doldurabilirsin):");
        yeniOzelDiyalogMetni = EditorGUILayout.TextArea(yeniOzelDiyalogMetni, GUILayout.Height(60));

        GUI.enabled = !string.IsNullOrWhiteSpace(yeniOzelDiyalogMetni);
        if (GUILayout.Button("'" + isimler[seciliKarakterIndex] + "' Karakterine Ekle", GUILayout.Height(30)))
        {
            List<string> yeniSatirlar = SatirlaraAyir(yeniOzelDiyalogMetni);
            if (yeniSatirlar.Count > 0)
            {
                Undo.RecordObject(duzenlenecekData, "Diyalog Ekle: " + isimler[seciliKarakterIndex]);

                if (secili.ozelDiyaloglar == null) secili.ozelDiyaloglar = new List<string>();
                secili.ozelDiyaloglar.AddRange(yeniSatirlar);

                EditorUtility.SetDirty(duzenlenecekData);
                AssetDatabase.SaveAssets();

                yeniOzelDiyalogMetni = "";
                EditorUtility.DisplayDialog("Eklendi",
                    yeniSatirlar.Count + " yeni diyalog '" + isimler[seciliKarakterIndex] + "' karakterine eklendi.",
                    "Tamam");
            }
        }
        GUI.enabled = true;
    }

    private void FloatAraligiCiz(string etiket, ref float min, ref float max)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(etiket, GUILayout.Width(180));
        min = EditorGUILayout.FloatField(min, GUILayout.Width(60));
        EditorGUILayout.LabelField("–", GUILayout.Width(12));
        max = EditorGUILayout.FloatField(max, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
    }

    private void YeniAyarDosyasiOlustur()
    {
        NpcHavuzuAyarlari yeni = ScriptableObject.CreateInstance<NpcHavuzuAyarlari>();
        yeni.kadinIsimleri = new List<string> { "Ayşe", "Fatma", "Elif", "Zeynep", "Merve", "Selin" };
        yeni.erkekIsimleri = new List<string> { "Ahmet", "Mehmet", "Can", "Burak", "Emre", "Kerem" };
        yeni.soyadlar = new List<string> { "Yılmaz", "Kaya", "Demir", "Çelik", "Arslan" };
        yeni.meslekler = new List<string> { "Öğretmen", "Mühendis", "Doktor", "Esnaf" };
        yeni.diyaloglar = new List<string> { "Bugün hava çok güzel.", "Bu dükkanı çok seviyorum.", "Yeni bir kitap arıyorum." };
        yeni.meslekTurEslesmeleri = new List<string> { "Öğretmen: Tarih, Felsefe", "Doktor: Bilim" };

        string yol = "Assets/Resources/NpcHavuzuAyarlari.asset";
        AssetDatabase.CreateAsset(yeni, yol);
        AssetDatabase.SaveAssets();

        ayarlarAsset = yeni;
        EditorPrefs.SetString(PrefsKey, yol);
        AssetTenYukle();

        Selection.activeObject = yeni;
    }

    private void AssetTenYukle()
    {
        if (ayarlarAsset == null) return;
        kadinIsimlerText = string.Join("\n", ayarlarAsset.kadinIsimleri);
        erkekIsimlerText = string.Join("\n", ayarlarAsset.erkekIsimleri);
        soyadlarText = string.Join("\n", ayarlarAsset.soyadlar);
        meslekText = string.Join("\n", ayarlarAsset.meslekler);
        diyalogText = string.Join("\n", ayarlarAsset.diyaloglar);
        meslekTurText = string.Join("\n", ayarlarAsset.meslekTurEslesmeleri);
    }

    private void AssetEKaydet()
    {
        if (ayarlarAsset == null) return;

        Undo.RecordObject(ayarlarAsset, "NPC Havuzlarini Guncelle");

        ayarlarAsset.kadinIsimleri = SatirlaraAyir(kadinIsimlerText);
        ayarlarAsset.erkekIsimleri = SatirlaraAyir(erkekIsimlerText);
        ayarlarAsset.soyadlar = SatirlaraAyir(soyadlarText);
        ayarlarAsset.meslekler = SatirlaraAyir(meslekText);
        ayarlarAsset.diyaloglar = SatirlaraAyir(diyalogText);
        ayarlarAsset.meslekTurEslesmeleri = SatirlaraAyir(meslekTurText);

        EditorUtility.SetDirty(ayarlarAsset);
        AssetDatabase.SaveAssets();
    }

    private void Uret()
    {
        List<string> kadinIsimleri = ayarlarAsset.kadinIsimleri;
        List<string> erkekIsimleri = ayarlarAsset.erkekIsimleri;
        List<string> soyadlar = ayarlarAsset.soyadlar;
        List<string> meslekler = ayarlarAsset.meslekler;
        List<string> diyaloglar = ayarlarAsset.diyaloglar;

        bool kadinVar = kadinIsimleri.Count > 0;
        bool erkekVar = erkekIsimleri.Count > 0;

        if ((!kadinVar && !erkekVar) || soyadlar.Count == 0)
        {
            EditorUtility.DisplayDialog("Eksik Havuz", "En az bir cinsiyette isim ve soyad havuzu dolu olmalı.", "Tamam");
            return;
        }

        // "Meslek: Tur1, Tur2" satirlarini onceden parse et — meslek adina gore hizli erisim.
        Dictionary<string, List<KitapTuru>> meslekTurHaritasi = MeslekTurHaritasiOlustur(ayarlarAsset.meslekTurEslesmeleri);

        // PROJEDEKI TUM CustomerData asset'lerindeki MEVCUT isimleri topla — cakisma kontrolu icin.
        HashSet<string> mevcutIsimler = new HashSet<string>();
        string[] tumDataGuidleri = AssetDatabase.FindAssets("t:CustomerData");
        foreach (string guid in tumDataGuidleri)
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            CustomerData data = AssetDatabase.LoadAssetAtPath<CustomerData>(yol);
            if (data == null || data.karakterHavuzu == null) continue;

            foreach (NPCKimligi k in data.karakterHavuzu)
                if (k != null && !string.IsNullOrEmpty(k.musteriIsmi)) mevcutIsimler.Add(k.musteriIsmi);
        }

        Undo.RecordObject(hedefData, "Hizli NPC Uret: " + kacTaneUretilsin + " adet");

        int basariylaUretilen = 0;
        int denemeSiniri = kacTaneUretilsin * 50;
        int denemeSayaci = 0;

        while (basariylaUretilen < kacTaneUretilsin && denemeSayaci < denemeSiniri)
        {
            denemeSayaci++;

            bool buSeferKadin = kadinVar && erkekVar ? Random.value < 0.5f : kadinVar;
            List<string> aktifIsimHavuzu = buSeferKadin ? kadinIsimleri : erkekIsimleri;

            string isim = aktifIsimHavuzu[Random.Range(0, aktifIsimHavuzu.Count)];
            string soyad = soyadlar[Random.Range(0, soyadlar.Count)];
            string tamIsim = isim + " " + soyad;

            if (mevcutIsimler.Contains(tamIsim)) continue;
            mevcutIsimler.Add(tamIsim);

            string secilenMeslek = meslekler.Count > 0 ? meslekler[Random.Range(0, meslekler.Count)] : "";

            NPCKimligi yeni = new NPCKimligi();
            yeni.musteriIsmi = tamIsim;
            yeni.cinsiyet = buSeferKadin ? Cinsiyet.Kadin : Cinsiyet.Erkek;
            yeni.meslek = secilenMeslek;
            yeni.loreHikayesi = "Evrende sıradan bir vatandaş.";
            yeni.ozelDiyaloglar = KaristirVeIlkNTaneyiAl(diyaloglar, npcBasinaDiyalogSayisi);
            // NOT: bahceDiyaloglari artik kullanilmiyor -- bahce sohbeti artik
            // BahceKonuVerisi/BahceSohbetYoneticisi (paylasimli konu grafigi) sisteminden geliyor.

            // #7: Meslek-Tur tematik eslesmesi — bu meslek icin tanimli tur(ler) varsa,
            // ONCELIKLE oradan bir tane eklenir, kalan slotlar tamamen rastgele doldurulur.
            yeni.sevilenTurler = TematikTurlerSec(secilenMeslek, meslekTurHaritasi, npcBasinaSevilenTurSayisi);

            // #1: Kisilik carpanlari, ayarlanan araliktan rastgele secilir.
            yeni.sabirCarpani = Random.Range(ayarlarAsset.sabirCarpaniMin, ayarlarAsset.sabirCarpaniMax);
            yeni.odemeCarpani = Random.Range(ayarlarAsset.odemeCarpaniMin, ayarlarAsset.odemeCarpaniMax);
            yeni.incelemeSuresiCarpani = Random.Range(ayarlarAsset.incelemeCarpaniMin, ayarlarAsset.incelemeCarpaniMax);

            // #3: Cinsiyete uygun kiyafet havuzundan rastgele bir sprite (varsa).
            List<Sprite> kiyafetHavuzu = buSeferKadin ? ayarlarAsset.kadinKiyafetHavuzu : ayarlarAsset.erkekKiyafetHavuzu;
            if (kiyafetHavuzu != null && kiyafetHavuzu.Count > 0)
                yeni.tisortSprite = kiyafetHavuzu[Random.Range(0, kiyafetHavuzu.Count)];

            hedefData.karakterHavuzu.Add(yeni);
            basariylaUretilen++;
        }

        EditorUtility.SetDirty(hedefData);
        AssetDatabase.SaveAssets();

        string mesaj = basariylaUretilen + " yeni NPC '" + hedefData.name + "' içine eklendi.";
        if (basariylaUretilen < kacTaneUretilsin)
            mesaj += "\n\nUYARI: " + (kacTaneUretilsin - basariylaUretilen) + " tanesi üretilemedi — isim/soyad havuzu, istenen benzersiz kombinasyon sayısı için yetersiz olabilir. Havuzları büyüt.";

        EditorUtility.DisplayDialog("Tamamlandı", mesaj, "Tamam");
    }

    /// <summary>"Meslek: Tur1, Tur2" formatindaki satirlari ayristirip meslek-adi -> tur-listesi
    /// sozlugune cevirir. Gecersiz/parse edilemeyen turler sessizce atlanir.</summary>
    private static Dictionary<string, List<KitapTuru>> MeslekTurHaritasiOlustur(List<string> satirlar)
    {
        Dictionary<string, List<KitapTuru>> harita = new Dictionary<string, List<KitapTuru>>();
        if (satirlar == null) return harita;

        foreach (string satir in satirlar)
        {
            int ikiNoktaIndex = satir.IndexOf(':');
            if (ikiNoktaIndex < 0) continue;

            string meslek = satir.Substring(0, ikiNoktaIndex).Trim();
            string turlerKismi = satir.Substring(ikiNoktaIndex + 1).Trim();
            if (meslek.Length == 0 || turlerKismi.Length == 0) continue;

            List<KitapTuru> turler = new List<KitapTuru>();
            foreach (string turAdi in turlerKismi.Split(','))
            {
                KitapTuru turDegeri;
                if (System.Enum.TryParse(turAdi.Trim(), true, out turDegeri))
                    turler.Add(turDegeri);
            }

            if (turler.Count > 0) harita[meslek] = turler;
        }

        return harita;
    }

    /// <summary>Meslege gore hafif temali sevilenTurler secimi: eslesme varsa ilk slot
    /// ONCELIKLE oradan gelir, kalanlar tum turlerden rastgele (tekrarsiz) doldurulur.</summary>
    private static List<KitapTuru> TematikTurlerSec(string meslek, Dictionary<string, List<KitapTuru>> harita, int adet)
    {
        List<KitapTuru> sonuc = new List<KitapTuru>();

        List<KitapTuru> temaliListe;
        if (!string.IsNullOrEmpty(meslek) && harita.TryGetValue(meslek, out temaliListe) && temaliListe.Count > 0)
        {
            sonuc.Add(temaliListe[Random.Range(0, temaliListe.Count)]);
        }

        System.Array tumTurlerArray = System.Enum.GetValues(typeof(KitapTuru));
        List<KitapTuru> tumTurler = new List<KitapTuru>();
        foreach (KitapTuru t in tumTurlerArray) tumTurler.Add(t);

        // Karistir, sirayla ekle — zaten listede olanlari (tema slotu) tekrar ekleme.
        for (int i = tumTurler.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            KitapTuru gecici = tumTurler[i]; tumTurler[i] = tumTurler[j]; tumTurler[j] = gecici;
        }

        foreach (KitapTuru t in tumTurler)
        {
            if (sonuc.Count >= adet) break;
            if (!sonuc.Contains(t)) sonuc.Add(t);
        }

        return sonuc;
    }

    private static List<string> SatirlaraAyir(string metin)
    {
        if (string.IsNullOrEmpty(metin)) return new List<string>();

        return metin.Split('\n')
                     .Select(s => s.Trim())
                     .Where(s => s.Length > 0)
                     .Distinct()
                     .ToList();
    }

    private static List<string> KaristirVeIlkNTaneyiAl(List<string> kaynak, int adet)
    {
        if (kaynak == null || kaynak.Count == 0) return new List<string>();

        List<string> kopya = new List<string>(kaynak);
        for (int i = kopya.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string gecici = kopya[i]; kopya[i] = kopya[j]; kopya[j] = gecici;
        }
        return kopya.Take(Mathf.Min(adet, kopya.Count)).ToList();
    }
}
#endif