// BoslukTaramaPenceresi.cs — Editor araci: projedeki TUM ScriptableObject veri asset'lerini
// (hikaye, bahce sohbeti, musteri, dedikodu vb.) REFLECTION ile gezip su seyleri arar VE
// bir kismini OTOMATIK DOLDURUR:
//
//  1) SUPHELI VARSAYILAN CINSIYET/YAS — bir nesnenin ayni anda BIR Cinsiyet alani ve BIR
//     YasGrubu alani varsa ve ikisi de enum'un SIFIRINCI degerindeyse (Cinsiyet.Kadin +
//     YasGrubu.Cocuk), bu ELLE ATANMAMIS olabilir. SADECE RAPORLANIR, OTOMATIK DUZELTILMEZ —
//     dogru cinsiyet/yas TAHMIN EDILEMEZ (yanlis tahmin, tam da duzeltmeye calistigimiz
//     hatanin AYNISINI, farkli bir sekilde, tekrar uretir). Elle bakman gerekiyor.
//
//  2) CEVRILMEMIS METINLER — List<DilMetniGirisi> tipinde bir alan bos/null ise, RAPORLANIR
//     VE "İngilizce Taslak Ekle" butonuyla OTOMATIK DOLDURULABILIR: Turkce metnin
//     '[ÇEVİR] ' onekli bir kopyasi Ingilizce girisi olarak eklenir — GERCEK CEVIRI DEGIL,
//     sadece bos liste yerine elle duzeltilecek bir BASLANGIC NOKTASI (ceviri kalitesi
//     gerektirdigi icin gercek metni TAHMIN ETMEYIZ, sadece BOSLUGU dolduruyoruz).
//
//  3) LOKALIZASYON ANAHTARI EKSIKLIGI — kod icinde LocalizationManager.Instance.Get("...")
//     veya LokalizasyonYardimcisi.SistemMetniCoz("...", "...") ile kullanilan anahtarlar
//     taranir, CSV'deki anahtarlarla karsilastirilir. "CSV'ye Ekle" butonuyla OTOMATIK
//     DOLDURULABILIR: SistemMetniCoz cagirilarinda ikinci parametre olarak verilen Turkce
//     yedek metin GERCEK deger olarak yazilir (bu bir tahmin degil, kodun kendisinde zaten
//     yazili olan metin); sade Get() cagirilarinda yedek metin bulunamadigi icin "TODO_"
//     onekiyle isaretlenir.
//
// BILEREK GENEL (reflection tabanli) YAZILDI: hangi veri sinifinin hangi alanlara sahip
// oldugu HARDCODE EDILMEDI — ileride eklenecek yeni diyalog/hikaye veri turleri de (ayni
// DilMetniGirisi/SesKlipSeti/Cinsiyet+YasGrubu desenini kullandigi surece) otomatik taranir.
//
// KULLANIM: Delilik > Denetim > Boşluk Tarayıcısı
// Bu dosya Assets/Editor/ klasorune konulmali (Unity otomatik olarak build'e dahil etmez).
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

public class BoslukTaramaPenceresi : EditorWindow
{
    private const string PREF_CSV = "Delilik_BoslukTarayici_CsvYolu";
    private const string PREF_KOD = "Delilik_BoslukTarayici_KodKlasoru";
    private const int MAX_ZIYARET = 50000; // guvenlik supabi — pathological buyuk grafiklerde sonsuz taramayi onler
    private const int MAX_DERINLIK = 8;

    private class CeviriBulgusu { public string assetAdi; public string baglam; public UnityEngine.Object asset; }
    private class CinsiyetYasBulgusu { public string assetAdi; public string baglam; public UnityEngine.Object asset; }
    private class SesKapsamOzeti { public string assetAdi; public int toplam; public int sesVar; public UnityEngine.Object asset; }

    private class TaramaSonuclari
    {
        public List<CeviriBulgusu> cevrilmemisler = new List<CeviriBulgusu>();
        public List<CinsiyetYasBulgusu> supheliVarsayilanlar = new List<CinsiyetYasBulgusu>();
        public List<SesKapsamOzeti> sesKapsami = new List<SesKapsamOzeti>();
        public List<string> eksikAnahtarlar = new List<string>();
        public List<string> kullanilmayanAnahtarlar = new List<string>();
        public Dictionary<string, string> anahtarYedekleri = new Dictionary<string, string>();
        public int taranankAssetSayisi;
    }

    private static readonly Dictionary<Type, List<FieldInfo>> alanCache = new Dictionary<Type, List<FieldInfo>>();

    private string csvYolu;
    private string kodKlasoru;
    private TaramaSonuclari sonuclar;
    private bool tarandiMi = false;
    private Vector2 scroll;

    private bool foldCinsiyetYas = true, foldCeviri = true, foldEksikAnahtar = true, foldKullanilmayanAnahtar = false, foldSes = false;

    [MenuItem("Delilik/Denetim/Boşluk Tarayıcısı")]
    public static void PencereyiAc()
    {
        GetWindow<BoslukTaramaPenceresi>("Boşluk Tarayıcısı");
    }

    private void OnEnable()
    {
        csvYolu = EditorPrefs.GetString(PREF_CSV, "");
        kodKlasoru = EditorPrefs.GetString(PREF_KOD, "Assets/Scripts");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Boşluk Tarayıcısı", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Projedeki TUM veri asset'lerini tarar. Bazı bulgular sadece RAPORLANIR (tahmin " +
            "riskli olduğu için), bazıları 'Otomatik Doldur' butonuyla mekanik olarak doldurulabilir " +
            "— aşağıda her kategori altında hangisinin hangisi olduğu yazıyor.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();
        csvYolu = EditorGUILayout.TextField("Lokalizasyon CSV", csvYolu);
        if (GUILayout.Button("Seç", GUILayout.Width(50)))
        {
            string secilen = EditorUtility.OpenFilePanel("Lokalizasyon CSV Dosyasını Seç", "Assets", "csv");
            if (!string.IsNullOrEmpty(secilen)) csvYolu = secilen;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        kodKlasoru = EditorGUILayout.TextField("Kod Klasörü", kodKlasoru);
        if (GUILayout.Button("Seç", GUILayout.Width(50)))
        {
            string secilen = EditorUtility.OpenFolderPanel("Kod Klasörünü Seç", "Assets", "");
            if (!string.IsNullOrEmpty(secilen) && secilen.StartsWith(Application.dataPath))
                kodKlasoru = "Assets" + secilen.Substring(Application.dataPath.Length);
        }
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(PREF_CSV, csvYolu);
            EditorPrefs.SetString(PREF_KOD, kodKlasoru);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("TARA", GUILayout.Height(30))) TaramaBaslat();

        if (!tarandiMi) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"{sonuclar.taranankAssetSayisi} ScriptableObject asset'i tarandı.", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        // --- Şüpheli varsayılan cinsiyet/yaş — SADECE RAPOR ---
        foldCinsiyetYas = EditorGUILayout.Foldout(foldCinsiyetYas, $"⚠️  Şüpheli Varsayılan Cinsiyet/Yaş ({sonuclar.supheliVarsayilanlar.Count}) — sadece rapor, otomatik düzeltilmez", true);
        if (foldCinsiyetYas)
        {
            EditorGUILayout.HelpBox("Burası OTOMATİK DÜZELTİLMEZ: doğru cinsiyet/yaşı tahmin etmek, tam da düzeltmeye çalıştığımız hatayı başka bir şekilde tekrar üretir. Elle kontrol et.", MessageType.None);
            foreach (var b in sonuclar.supheliVarsayilanlar)
                SonucSatiriCiz($"{b.assetAdi} → {b.baglam}", b.asset);
        }

        // --- Çevrilmemiş metinler — OTOMATIK DOLDURULABİLİR ---
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        foldCeviri = EditorGUILayout.Foldout(foldCeviri, $"🌐  Çevrilmemiş Metinler ({sonuclar.cevrilmemisler.Count})", true);
        using (new EditorGUI.DisabledScope(sonuclar.cevrilmemisler.Count == 0))
        {
            if (GUILayout.Button("İngilizce Taslak Ekle (Otomatik)", GUILayout.Width(220)))
                CevrilmemisleriDoldur();
        }
        EditorGUILayout.EndHorizontal();
        if (foldCeviri)
        {
            EditorGUILayout.HelpBox("'İngilizce Taslak Ekle': her boş listeye, Türkçe metnin '[ÇEVİR] ' önekli kopyasını İngilizce girişi olarak ekler — GERÇEK ÇEVİRİ DEĞİLDİR, sadece elle düzelteceğin bir başlangıç noktası verir.", MessageType.None);
            foreach (var b in sonuclar.cevrilmemisler)
                SonucSatiriCiz($"{b.assetAdi} → {b.baglam}", b.asset);
        }

        // --- Eksik lokalizasyon anahtarları — OTOMATIK DOLDURULABİLİR ---
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        foldEksikAnahtar = EditorGUILayout.Foldout(foldEksikAnahtar, $"🔑  Kodda Var, CSV'de Yok ({sonuclar.eksikAnahtarlar.Count})", true);
        using (new EditorGUI.DisabledScope(sonuclar.eksikAnahtarlar.Count == 0 || string.IsNullOrEmpty(csvYolu)))
        {
            if (GUILayout.Button("CSV'ye Ekle (Otomatik)", GUILayout.Width(220)))
                EksikAnahtarlariCsvyeEkle();
        }
        EditorGUILayout.EndHorizontal();
        if (foldEksikAnahtar)
        {
            EditorGUILayout.HelpBox("'CSV'ye Ekle': SistemMetniCoz(...) çağrılarındaki Türkçe yedek metni GERÇEK değer olarak yazar (bu bir tahmin değil — kodda zaten yazılı). Düz Get(...) çağrılarında yedek metin bulunamadığı için 'TODO_' önekiyle eklenir, elle tamamlaman gerekir.", MessageType.None);
            foreach (var k in sonuclar.eksikAnahtarlar)
                EditorGUILayout.LabelField("   " + k);
        }

        EditorGUILayout.Space();
        foldKullanilmayanAnahtar = EditorGUILayout.Foldout(foldKullanilmayanAnahtar, $"🗑  CSV'de Var, Kodda Kullanılmıyor ({sonuclar.kullanilmayanAnahtarlar.Count})", true);
        if (foldKullanilmayanAnahtar)
            foreach (var k in sonuclar.kullanilmayanAnahtarlar)
                EditorGUILayout.LabelField("   " + k);

        EditorGUILayout.Space();
        foldSes = EditorGUILayout.Foldout(foldSes, "🔊  Seslendirme Kapsamı (bilgi amaçlı — boş olması BEKLENEN durum, hata değil)", true);
        if (foldSes)
            foreach (var o in sonuclar.sesKapsami)
            {
                float yuzde = o.toplam > 0 ? (o.sesVar * 100f / o.toplam) : 0f;
                SonucSatiriCiz($"{o.assetAdi}: {o.sesVar}/{o.toplam} satırda özel klip (%{yuzde:F0}) — kalanı blabla çalar", o.asset);
            }

        EditorGUILayout.EndScrollView();
    }

    private void SonucSatiriCiz(string metin, UnityEngine.Object asset)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("   " + metin, EditorStyles.wordWrappedLabel);
        if (asset != null && GUILayout.Button("Göster", GUILayout.Width(60)))
            EditorGUIUtility.PingObject(asset);
        EditorGUILayout.EndHorizontal();
    }

    // ------------------------------------------------------------------
    // TARAMA (SADECE OKUMA)
    // ------------------------------------------------------------------

    private void TaramaBaslat()
    {
        var sonuc = new TaramaSonuclari();
        var ziyaretEdilen = new HashSet<object>();

        string[] guidler = AssetDatabase.FindAssets("t:ScriptableObject");
        foreach (string guid in guidler)
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(yol);
            if (so == null) continue;

            sonuc.taranankAssetSayisi++;

            try
            {
                NesneyiTara(so, so.name, so, ziyaretEdilen, 0, sonuc);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Boşluk Tarayıcısı] '{so.name}' taranırken hata: {ex.Message}");
            }
        }

        LokalizasyonAnahtarlariniTara(sonuc);

        sonuclar = sonuc;
        tarandiMi = true;
    }

    private void NesneyiTara(object nesne, string assetAdi, UnityEngine.Object assetRef, HashSet<object> ziyaretEdilen, int derinlik, TaramaSonuclari sonuc)
    {
        if (nesne == null || derinlik > MAX_DERINLIK) return;
        if (ziyaretEdilen.Count > MAX_ZIYARET) return;

        Type tip = nesne.GetType();

        if (!tip.IsValueType)
        {
            if (ziyaretEdilen.Contains(nesne)) return; // dongu koruma
            ziyaretEdilen.Add(nesne);
        }

        List<FieldInfo> alanlar = TumAlanlariGetir(tip);

        CinsiyetYasCiftiniKontrolEt(nesne, alanlar, assetAdi, assetRef, sonuc);
        SesKlipSetiIstatistiginiTopla(nesne, alanlar, assetAdi, assetRef, sonuc);

        foreach (FieldInfo alan in alanlar)
        {
            object deger;
            try { deger = alan.GetValue(nesne); }
            catch { continue; }

            Type alanTipi = alan.FieldType;

            if (alanTipi == typeof(List<DilMetniGirisi>))
            {
                var liste = deger as IList;
                if (liste == null || liste.Count == 0)
                {
                    sonuc.cevrilmemisler.Add(new CeviriBulgusu
                    {
                        assetAdi = assetAdi,
                        baglam = BaglamMetniBul(nesne, alanlar, 70),
                        asset = assetRef
                    });
                }
                continue;
            }

            if (deger == null) continue;
            if (alanTipi == typeof(SesKlipSeti)) continue; // yukarida ayrica islendi

            if (deger is IEnumerable enumerable && alanTipi != typeof(string))
            {
                foreach (var eleman in enumerable)
                {
                    if (eleman == null) continue;
                    Type elemanTipi = eleman.GetType();
                    if (KullanicininTipiMi(elemanTipi))
                        NesneyiTara(eleman, assetAdi, assetRef, ziyaretEdilen, derinlik + 1, sonuc);
                }
                continue;
            }

            if (KullanicininTipiMi(alanTipi))
                NesneyiTara(deger, assetAdi, assetRef, ziyaretEdilen, derinlik + 1, sonuc);
        }
    }

    private void CinsiyetYasCiftiniKontrolEt(object nesne, List<FieldInfo> alanlar, string assetAdi, UnityEngine.Object assetRef, TaramaSonuclari sonuc)
    {
        FieldInfo cinsiyetAlani = alanlar.FirstOrDefault(a => a.FieldType == typeof(Cinsiyet));
        FieldInfo yasAlani = alanlar.FirstOrDefault(a => a.FieldType == typeof(YasGrubu));
        if (cinsiyetAlani == null || yasAlani == null) return;

        Cinsiyet cinsiyet;
        YasGrubu yasGrubu;
        try
        {
            cinsiyet = (Cinsiyet)cinsiyetAlani.GetValue(nesne);
            yasGrubu = (YasGrubu)yasAlani.GetValue(nesne);
        }
        catch { return; }

        if ((int)cinsiyet == 0 && (int)yasGrubu == 0)
        {
            sonuc.supheliVarsayilanlar.Add(new CinsiyetYasBulgusu
            {
                assetAdi = assetAdi,
                baglam = BaglamMetniBul(nesne, alanlar, 70),
                asset = assetRef
            });
        }
    }

    private void SesKlipSetiIstatistiginiTopla(object nesne, List<FieldInfo> alanlar, string assetAdi, UnityEngine.Object assetRef, TaramaSonuclari sonuc)
    {
        FieldInfo sesAlani = alanlar.FirstOrDefault(a => a.FieldType == typeof(SesKlipSeti));
        if (sesAlani == null) return;

        SesKlipSeti sesSeti;
        try { sesSeti = (SesKlipSeti)sesAlani.GetValue(nesne); }
        catch { return; }

        SesKapsamOzeti ozet = sonuc.sesKapsami.FirstOrDefault(o => o.assetAdi == assetAdi);
        if (ozet == null)
        {
            ozet = new SesKapsamOzeti { assetAdi = assetAdi, asset = assetRef };
            sonuc.sesKapsami.Add(ozet);
        }

        ozet.toplam++;
        if (!sesSeti.BosMu) ozet.sesVar++;
    }

    private void LokalizasyonAnahtarlariniTara(TaramaSonuclari sonuc)
    {
        var csvAnahtarlari = new HashSet<string>();
        if (!string.IsNullOrEmpty(csvYolu) && File.Exists(csvYolu))
        {
            foreach (string satirHam in File.ReadAllLines(csvYolu))
            {
                string satir = satirHam.Trim();
                if (string.IsNullOrEmpty(satir) || satir.StartsWith("#") || satir.StartsWith("//")) continue;

                int ayirici = satir.IndexOf(';');
                if (ayirici <= 0) continue;

                string anahtar = satir.Substring(0, ayirici).Trim();
                if (anahtar.Equals("anahtar", StringComparison.OrdinalIgnoreCase)) continue; // baslik satiri

                if (!string.IsNullOrEmpty(anahtar)) csvAnahtarlari.Add(anahtar);
            }
        }

        var kodAnahtarlari = new HashSet<string>();
        var yedekler = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(kodKlasoru) && Directory.Exists(kodKlasoru))
        {
            var desenGet = new Regex("LocalizationManager\\s*\\.\\s*Instance\\s*\\.\\s*Get\\s*\\(\\s*\"([^\"]*)\"");
            var desenSistemMetniCoz = new Regex("SistemMetniCoz\\s*\\(\\s*\"([^\"]*)\"\\s*,\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");

            foreach (string dosya in Directory.GetFiles(kodKlasoru, "*.cs", SearchOption.AllDirectories))
            {
                string icerik;
                try { icerik = File.ReadAllText(dosya); }
                catch { continue; }

                foreach (Match m in desenGet.Matches(icerik))
                    kodAnahtarlari.Add(m.Groups[1].Value);

                foreach (Match m in desenSistemMetniCoz.Matches(icerik))
                {
                    string anahtar = m.Groups[1].Value;
                    string yedekKacisli = m.Groups[2].Value;
                    string yedek = yedekKacisli.Replace("\\\"", "\"").Replace("\\\\", "\\");

                    kodAnahtarlari.Add(anahtar);
                    yedekler[anahtar] = yedek;
                }
            }
        }

        sonuc.eksikAnahtarlar = kodAnahtarlari.Where(k => !csvAnahtarlari.Contains(k)).OrderBy(k => k).ToList();
        sonuc.kullanilmayanAnahtarlar = csvAnahtarlari.Where(k => !kodAnahtarlari.Contains(k)).OrderBy(k => k).ToList();
        sonuc.anahtarYedekleri = yedekler;
    }

    // ------------------------------------------------------------------
    // OTOMATIK DOLDURMA (YAZMA)
    // ------------------------------------------------------------------

    /// <summary>Kodda kullanilan ama CSV'de olmayan anahtarlari CSV'nin SONUNA ekler.
    /// SistemMetniCoz cagirilarindaki Turkce yedek metin biliniyorsa GERCEK deger olarak
    /// yazilir (tahmin degil — kodun kendisinde zaten var); bilinmiyorsa 'TODO_' onekiyle
    /// isaretlenir, elle tamamlaman gerekir.</summary>
    private void EksikAnahtarlariCsvyeEkle()
    {
        if (string.IsNullOrEmpty(csvYolu))
        {
            EditorUtility.DisplayDialog("CSV Yolu Yok", "Önce bir Lokalizasyon CSV dosyası seç.", "Tamam");
            return;
        }

        var eklenecekSatirlar = new List<string>();
        int biliniyorSayisi = 0, todoSayisi = 0;

        foreach (string anahtar in sonuclar.eksikAnahtarlar)
        {
            if (sonuclar.anahtarYedekleri.TryGetValue(anahtar, out string yedek))
            {
                eklenecekSatirlar.Add(anahtar + ";" + yedek);
                biliniyorSayisi++;
            }
            else
            {
                eklenecekSatirlar.Add(anahtar + ";TODO_" + anahtar);
                todoSayisi++;
            }
        }

        if (eklenecekSatirlar.Count == 0) return;

        File.AppendAllLines(csvYolu, eklenecekSatirlar);

        EditorUtility.DisplayDialog("Tamamlandı",
            $"{eklenecekSatirlar.Count} anahtar CSV'nin sonuna eklendi ({biliniyorSayisi} gerçek Türkçe yedekle, " +
            $"{todoSayisi} 'TODO_' işaretiyle — bunları elle tamamlaman gerekiyor).", "Tamam");

        TaramaBaslat();
    }

    /// <summary>Bos List&lt;DilMetniGirisi&gt; alanlarina Ingilizce bir TASLAK giris ekler
    /// (Turkce metnin '[ÇEVİR] ' onekli kopyasi) — GERCEK CEVIRI DEGILDIR, sadece elle
    /// duzeltilecek bir baslangic noktasi verir. Cinsiyet/Yas alanlarina KASITLI OLARAK
    /// DOKUNULMAZ (bkz. dosya basindaki aciklama).</summary>
    private void CevrilmemisleriDoldur()
    {
        int eklenenSayisi = 0;
        int degisenAssetSayisi = 0;
        var ziyaretEdilen = new HashSet<object>();

        string[] guidler = AssetDatabase.FindAssets("t:ScriptableObject");
        foreach (string guid in guidler)
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(yol);
            if (so == null) continue;

            try
            {
                bool degisti = NesneyiDoldur(so, ziyaretEdilen, 0, ref eklenenSayisi);
                if (degisti)
                {
                    EditorUtility.SetDirty(so);
                    degisenAssetSayisi++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Boşluk Tarayıcısı] '{so.name}' doldurulurken hata: {ex.Message}");
            }
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Tamamlandı",
            $"{eklenenSayisi} adet çevrilmemiş alana İngilizce TASLAK eklendi ({degisenAssetSayisi} asset güncellendi).\n\n" +
            "ÖNEMLİ: eklenen metinler GERÇEK çeviri DEĞİL — '[ÇEVİR] ' önekli Türkçe kopyasıdır, " +
            "sadece bir başlangıç noktası. Gerçek İngilizce'yi elle yazmalısın.", "Tamam");

        TaramaBaslat();
    }

    /// <summary>NesneyiTara ile AYNI gezinme mantigi ama YAZAR: bos List&lt;DilMetniGirisi&gt;
    /// alanlarina bir Ingilizce taslak giris ekler. DialogueLine gibi STRUCT'lar bir
    /// List/array icinde ELEMAN olarak gelirse, degisiklik struct KOPYASINDA yapilir ve
    /// listeye/array'e GERI YAZILIR (aksi halde degisiklik kaybolur) — bkz. asagidaki
    /// IList[i] = eleman satiri.</summary>
    private bool NesneyiDoldur(object nesne, HashSet<object> ziyaretEdilen, int derinlik, ref int eklenenSayisi)
    {
        if (nesne == null || derinlik > MAX_DERINLIK) return false;
        if (ziyaretEdilen.Count > MAX_ZIYARET) return false;

        Type tip = nesne.GetType();
        bool degisti = false;

        if (!tip.IsValueType)
        {
            if (ziyaretEdilen.Contains(nesne)) return false;
            ziyaretEdilen.Add(nesne);
        }

        List<FieldInfo> alanlar = TumAlanlariGetir(tip);

        foreach (FieldInfo alan in alanlar)
        {
            object deger;
            try { deger = alan.GetValue(nesne); }
            catch { continue; }

            Type alanTipi = alan.FieldType;

            if (alanTipi == typeof(List<DilMetniGirisi>))
            {
                var liste = deger as List<DilMetniGirisi>;
                if (liste == null)
                {
                    liste = new List<DilMetniGirisi>();
                    alan.SetValue(nesne, liste);
                }

                if (liste.Count == 0)
                {
                    string tabanMetin = TamMetinBul(nesne, alanlar);
                    if (!string.IsNullOrEmpty(tabanMetin))
                    {
                        liste.Add(new DilMetniGirisi { dil = GameLanguage.English, metin = "[ÇEVİR] " + tabanMetin });
                        eklenenSayisi++;
                        degisti = true;
                    }
                }
                continue;
            }

            if (deger == null) continue;
            if (alanTipi == typeof(SesKlipSeti)) continue; // kasitli olarak dokunulmuyor

            if (deger is IList ilist && alanTipi != typeof(string))
            {
                for (int i = 0; i < ilist.Count; i++)
                {
                    object eleman = ilist[i];
                    if (eleman == null) continue;

                    Type elemanTipi = eleman.GetType();
                    if (!KullanicininTipiMi(elemanTipi)) continue;

                    bool altDegisti = NesneyiDoldur(eleman, ziyaretEdilen, derinlik + 1, ref eklenenSayisi);
                    if (altDegisti)
                    {
                        degisti = true;
                        if (elemanTipi.IsValueType) ilist[i] = eleman; // struct kopyasini GERI YAZ
                    }
                }
                continue;
            }

            if (deger is IEnumerable enumerableOnly && alanTipi != typeof(string))
            {
                // IList olmayan (nadir) koleksiyonlar — okunur ama geri yazilamaz, atla.
                continue;
            }

            if (KullanicininTipiMi(alanTipi))
            {
                bool altDegisti = NesneyiDoldur(deger, ziyaretEdilen, derinlik + 1, ref eklenenSayisi);
                if (altDegisti)
                {
                    degisti = true;
                    if (alanTipi.IsValueType) alan.SetValue(nesne, deger); // struct alani GERI YAZ
                }
            }
        }

        return degisti;
    }

    // ------------------------------------------------------------------
    // REFLECTION YARDIMCILARI
    // ------------------------------------------------------------------

    private static bool ScalarMi(Type t)
    {
        return t.IsPrimitive || t == typeof(string) || t == typeof(decimal) || t.IsEnum;
    }

    /// <summary>Sadece [Serializable] duz veri siniflarina/struct'larina inilir — Sprite,
    /// AudioClip, GameObject, Transform, ScriptableObject/MonoBehaviour referanslari gibi
    /// UnityEngine.Object turevleri KASITLI olarak DISLANIR.</summary>
    private static bool KullanicininTipiMi(Type t)
    {
        if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return false;
        if (ScalarMi(t)) return false;
        return t.IsClass || (t.IsValueType && !t.IsPrimitive && !t.IsEnum);
    }

    private static List<FieldInfo> TumAlanlariGetir(Type tip)
    {
        if (alanCache.TryGetValue(tip, out var vc)) return vc;

        var sonuc = new List<FieldInfo>();
        var gorulenAdlar = new HashSet<string>();
        Type simdi = tip;

        while (simdi != null && simdi != typeof(object))
        {
            FieldInfo[] alanlar = simdi.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var a in alanlar)
            {
                if (gorulenAdlar.Contains(a.Name)) continue;
                if (a.IsStatic || a.IsNotSerialized) continue;
                if (!a.IsPublic && a.GetCustomAttribute<SerializeField>() == null) continue;

                sonuc.Add(a);
                gorulenAdlar.Add(a.Name);
            }
            simdi = simdi.BaseType;
        }

        alanCache[tip] = sonuc;
        return sonuc;
    }

    /// <summary>Bir bulgunun yaninda gosterilecek KISALTILMIS baglam metnini bulur.</summary>
    private static string BaglamMetniBul(object nesne, List<FieldInfo> alanlar, int uzunluk)
    {
        string tam = TamMetinBul(nesne, alanlar);
        return string.IsNullOrEmpty(tam) ? "(metin bulunamadı)" : Kisalt(tam, uzunluk);
    }

    /// <summary>Nesnenin kendi alanlari arasinda oncelikli isimlerden (metin, sonSatir,
    /// secenekMetni, speakerName vb.) birini arar, bulamazsa ilk dolu string alanina duser.
    /// TAM (kisaltilmamis) metni doner — otomatik doldurmada taslak icin gerekli.</summary>
    private static string TamMetinBul(object nesne, List<FieldInfo> alanlar)
    {
        string[] oncelikliParcalar = { "metin", "sonsatir", "secenekmetni", "speakername", "musteriismi", "text" };

        foreach (string parca in oncelikliParcalar)
        {
            var alan = alanlar.FirstOrDefault(a => a.FieldType == typeof(string) && a.Name.ToLowerInvariant().Contains(parca));
            if (alan == null) continue;

            try
            {
                string deger = alan.GetValue(nesne) as string;
                if (!string.IsNullOrEmpty(deger)) return deger;
            }
            catch { }
        }

        var herhangi = alanlar.FirstOrDefault(a => a.FieldType == typeof(string));
        if (herhangi != null)
        {
            try
            {
                string deger = herhangi.GetValue(nesne) as string;
                if (!string.IsNullOrEmpty(deger)) return deger;
            }
            catch { }
        }

        return "";
    }

    private static string Kisalt(string s, int n)
    {
        string tekSatir = s.Replace("\n", " ").Replace("\r", "");
        return tekSatir.Length <= n ? tekSatir : tekSatir.Substring(0, n) + "...";
    }
}
