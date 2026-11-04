// BahceKonuOrnekIcerikUretici.cs — Bahce Konu Verisi icin ZENGIN bir icerik seti (20+ konu,
// 55+ cumle, HER dallanma noktasinda oyuncuya sunulacak SECENEK ETIKETLI) uretir/yeniden
// uretir. Artik tam bir EditorWindow — sadece tek-seferlik uretim degil, mevcut BahceKonuVerisi
// asset'ine YENI TEK CUMLE de ekleyebilirsin (form doldurup "Ekle"ye basarak, Inspector'in
// nested-liste tiklama karmasasina girmeden).
//
// Menu: Delilik > Bahce Konu Verisi Yoneticisi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class BahceKonuOrnekIcerikUretici : EditorWindow
{
    private BahceKonuVerisi hedefAsset;

    private string yeniId = "";
    private string yeniMetin = "";
    private readonly HashSet<string> seciliKonular = new HashSet<string>();
    private string yeniKonuAdi = "";
    private int seciliAnaKonuIndex = 0;
    private readonly List<string> yeniSecenekMetinleri = new List<string> { "", "", "", "" };
    private readonly List<string> yeniSecenekHedefleri = new List<string> { "", "", "", "" };

    private Vector2 scroll;
    private Vector2 mevcutCumlelerScroll;
    private bool mevcutCumleleriGoster = false;

    [MenuItem("Delilik/Bahce Konu Verisi Yoneticisi")]
    public static void PenceresiniAc()
    {
        GetWindow<BahceKonuOrnekIcerikUretici>("Bahçe Konu Verisi");
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("1) Hedef Asset", EditorStyles.boldLabel);
        hedefAsset = (BahceKonuVerisi)EditorGUILayout.ObjectField(
            new GUIContent("Bahce Konu Verisi", "Bos ise, asagidaki 'Zengin Baslangic Seti Uret' butonu YENI bir tane olusturur."),
            hedefAsset, typeof(BahceKonuVerisi), false);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("2) Zengin Başlangıç Seti", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "20+ konu, 55+ cumle, HER dallanma noktasi oyuncu-secenegi etiketli olarak uretir. " +
            "Eger 'Hedef Asset' zaten doluysa UZERINE YAZAR (elle eklediklerin kaybolur) — bos ise YENI olusturur.",
            MessageType.Info);

        if (GUILayout.Button("Zengin Başlangıç Seti Üret / Üzerine Yaz", GUILayout.Height(32)))
        {
            ZenginSetUret();
        }

        GUILayout.Space(20);
        EditorGUILayout.LabelField("3) Tek Bir Yeni Cümle Ekle", EditorStyles.boldLabel);

        if (hedefAsset == null)
        {
            EditorGUILayout.HelpBox("Önce bir Bahçe Konu Verisi asset'i seç ya da yukarıdan yeni bir tane üret.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        yeniId = EditorGUILayout.TextField(new GUIContent("Kimlik (id)", "Benzersiz olmali, orn. 'muzik_03'."), yeniId);
        EditorGUILayout.LabelField("Cümle Metni:");
        yeniMetin = EditorGUILayout.TextArea(yeniMetin, GUILayout.Height(40));
        GUILayout.Space(6);
        EditorGUILayout.LabelField("Konular:", EditorStyles.boldLabel);

        // KONU EKLE — tanimli konu listesine yeni bir ad ekler (yazim hatasi riskini azaltir,
        // ayni konu her yerde AYNI sekilde yazilir).
        EditorGUILayout.BeginHorizontal();
        yeniKonuAdi = EditorGUILayout.TextField(yeniKonuAdi);
        GUI.enabled = !string.IsNullOrWhiteSpace(yeniKonuAdi);
        if (GUILayout.Button("Konu Ekle", GUILayout.Width(90)))
        {
            string temiz = yeniKonuAdi.Trim();
            if (!hedefAsset.tanimliKonular.Contains(temiz))
            {
                Undo.RecordObject(hedefAsset, "Konu Ekle: " + temiz);
                hedefAsset.tanimliKonular.Add(temiz);
                EditorUtility.SetDirty(hedefAsset);
                AssetDatabase.SaveAssets();
            }
            seciliKonular.Add(temiz); // yeni eklenen konu otomatik secili gelsin
            seciliAnaKonuIndex = hedefAsset.tanimliKonular.IndexOf(temiz);
            yeniKonuAdi = "";
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (hedefAsset.tanimliKonular == null || hedefAsset.tanimliKonular.Count == 0)
        {
            EditorGUILayout.HelpBox("Henüz tanımlı konu yok. Yukarıdan birkaç tane ekle (ya da 'Zengin Başlangıç Seti Üret' ile otomatik doldur).", MessageType.Info);
        }
        else
        {
            // ANA KONU — kitap türü seçer gibi TEK, temiz bir açılır menü. Seçtiğin an
            // otomatik olarak "seçili konular"a eklenir VE o konudaki mevcut cümleleri
            // aşağıda referans olarak gösterir.
            seciliAnaKonuIndex = Mathf.Clamp(seciliAnaKonuIndex, 0, hedefAsset.tanimliKonular.Count - 1);
            int yeniIndex = EditorGUILayout.Popup("Ana Konu", seciliAnaKonuIndex, hedefAsset.tanimliKonular.ToArray());
            if (yeniIndex != seciliAnaKonuIndex || !seciliKonular.Contains(hedefAsset.tanimliKonular[yeniIndex]))
            {
                seciliAnaKonuIndex = yeniIndex;
                seciliKonular.Add(hedefAsset.tanimliKonular[seciliAnaKonuIndex]);
            }

            string anaKonu = hedefAsset.tanimliKonular[seciliAnaKonuIndex];

            // Bu konudaki MEVCUT cumleleri referans olarak goster — boylece "bu konuya
            // zaten ne yazilmis" gorup ona gore yeni cumle/hedef id kurabilirsin.
            List<BahceCumlesi> buKonudakiler = hedefAsset.tumCumleler.FindAll(
                c => c != null && c.konular != null && c.konular.Contains(anaKonu));

            EditorGUILayout.LabelField("'" + anaKonu + "' konusundaki mevcut cümleler (" + buKonudakiler.Count + "):", EditorStyles.miniBoldLabel);
            foreach (BahceCumlesi c in buKonudakiler)
                EditorGUILayout.LabelField("  • [" + c.id + "] " + c.metin, EditorStyles.wordWrappedMiniLabel);

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Ek konular (köprü — birden fazla konuya bağlamak istersen):", EditorStyles.miniLabel);
            int sutun = 0;
            EditorGUILayout.BeginHorizontal();
            foreach (string konu in hedefAsset.tanimliKonular)
            {
                if (konu == anaKonu) continue; // ana konu zaten dahil, tekrar gosterme
                bool secili = seciliKonular.Contains(konu);
                bool yeniDeger = EditorGUILayout.ToggleLeft(konu, secili, GUILayout.Width(140));
                if (yeniDeger && !secili) seciliKonular.Add(konu);
                else if (!yeniDeger && secili) seciliKonular.Remove(konu);

                sutun++;
                if (sutun % 4 == 0) { EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal(); }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(6);
        EditorGUILayout.LabelField("Seçenekler (oyuncuya gösterilecek KISA metin + hedef cümle id — boş bırakılan slot yok sayılır):", EditorStyles.wordWrappedLabel);
        for (int i = 0; i < 4; i++)
        {
            EditorGUILayout.BeginHorizontal();
            yeniSecenekMetinleri[i] = EditorGUILayout.TextField("Seçenek " + (i + 1), yeniSecenekMetinleri[i], GUILayout.Width(300));
            yeniSecenekHedefleri[i] = EditorGUILayout.TextField(yeniSecenekHedefleri[i], GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
        }

        GUI.enabled = !string.IsNullOrWhiteSpace(yeniId) && !string.IsNullOrWhiteSpace(yeniMetin);
        if (GUILayout.Button("Bu Cümleyi Asset'e Ekle", GUILayout.Height(28)))
        {
            YeniCumleyiEkle();
        }
        GUI.enabled = true;

        GUILayout.Space(20);
        mevcutCumleleriGoster = EditorGUILayout.Foldout(mevcutCumleleriGoster,
            "Mevcut Cümleler (" + (hedefAsset.tumCumleler != null ? hedefAsset.tumCumleler.Count : 0) + " tane) — referans için");
        if (mevcutCumleleriGoster && hedefAsset.tumCumleler != null)
        {
            mevcutCumlelerScroll = EditorGUILayout.BeginScrollView(mevcutCumlelerScroll, GUILayout.Height(200));
            foreach (BahceCumlesi c in hedefAsset.tumCumleler)
            {
                if (c == null) continue;
                EditorGUILayout.LabelField("[" + c.id + "] " + c.metin, EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndScrollView();
    }

    private void YeniCumleyiEkle()
    {
        if (hedefAsset.tumCumleler.Any(c => c != null && c.id == yeniId))
        {
            EditorUtility.DisplayDialog("Aynı Kimlik", "'" + yeniId + "' id'si zaten kullanılıyor. Farklı bir id seç.", "Tamam");
            return;
        }

        BahceCumlesi yeni = new BahceCumlesi();
        yeni.id = yeniId.Trim();
        yeni.metin = yeniMetin.Trim();
        yeni.konular = new List<string>(seciliKonular);
        yeni.secenekler = new List<BahceSecenegi>();

        for (int i = 0; i < 4; i++)
        {
            if (string.IsNullOrWhiteSpace(yeniSecenekMetinleri[i]) || string.IsNullOrWhiteSpace(yeniSecenekHedefleri[i])) continue;
            BahceSecenegi s = new BahceSecenegi();
            s.secenekMetni = yeniSecenekMetinleri[i].Trim();
            s.hedefCumleId = yeniSecenekHedefleri[i].Trim();
            yeni.secenekler.Add(s);
        }

        Undo.RecordObject(hedefAsset, "Bahce Cumlesi Ekle: " + yeni.id);
        hedefAsset.tumCumleler.Add(yeni);
        EditorUtility.SetDirty(hedefAsset);
        AssetDatabase.SaveAssets();

        yeniId = ""; yeniMetin = ""; seciliKonular.Clear();
        for (int i = 0; i < 4; i++) { yeniSecenekMetinleri[i] = ""; yeniSecenekHedefleri[i] = ""; }

        EditorUtility.DisplayDialog("Eklendi", "'" + yeni.id + "' asset'e eklendi.", "Tamam");
    }

    private void ZenginSetUret()
    {
        BahceKonuVerisi veri = hedefAsset;
        string yol = null;

        if (veri == null)
        {
            veri = ScriptableObject.CreateInstance<BahceKonuVerisi>();
            yol = "Assets/Resources/BahceKonuVerisi.asset";
            if (System.IO.File.Exists(yol))
            {
                if (!EditorUtility.DisplayDialog("Zaten Var", yol + " zaten var. Üzerine yazılsın mı?", "Üzerine Yaz", "İptal"))
                    return;
                AssetDatabase.DeleteAsset(yol);
            }
        }
        else
        {
            if (!EditorUtility.DisplayDialog("Emin misin?", "'" + veri.name + "' asset'inin İÇERİĞİ TAMAMEN değiştirilecek. Elle eklediklerin kaybolur.", "Üzerine Yaz", "İptal"))
                return;
        }

        veri.tumCumleler = TumCumleleriUret();

        // Uretilen icerikteki TUM konu etiketlerini tanimliKonular'a da ekle — boylece
        // "Konu Ekle" listesi bastan bos kalmaz, hemen secime hazir olur.
        HashSet<string> tumKonular = new HashSet<string>();
        foreach (BahceCumlesi c in veri.tumCumleler)
            if (c.konular != null)
                foreach (string k in c.konular) tumKonular.Add(k);
        veri.tanimliKonular = tumKonular.OrderBy(k => k).ToList();

        if (yol != null)
        {
            AssetDatabase.CreateAsset(veri, yol);
            hedefAsset = veri;
        }

        EditorUtility.SetDirty(veri);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = veri;
        EditorUtility.DisplayDialog("Tamamlandı", veri.tumCumleler.Count + " cümle üretildi.", "Tamam");
    }

    private static BahceCumlesi C(string id, string metin, string[] konular, params (string secenek, string hedef)[] secenekler)
    {
        BahceCumlesi c = new BahceCumlesi();
        c.id = id;
        c.metin = metin;
        c.konular = new List<string>(konular);
        c.secenekler = new List<BahceSecenegi>();
        foreach (var s in secenekler)
        {
            BahceSecenegi sec = new BahceSecenegi();
            sec.secenekMetni = s.secenek;
            sec.hedefCumleId = s.hedef;
            c.secenekler.Add(sec);
        }
        return c;
    }

    private static List<BahceCumlesi> TumCumleleriUret()
    {
        List<BahceCumlesi> l = new List<BahceCumlesi>();

        // ══════════ KİTAPLAR ══════════
        l.Add(C("kitap_01", "Bu dükkandaki kitap seçkisi gerçekten etkileyici.", new[] { "kitaplar", "dukkan" },
            ("Dükkan hakkında ne düşünüyorsun?", "dukkan_01"), ("Sen nasıl okursun?", "kitap_02")));
        l.Add(C("kitap_02", "Bir kitaba başlayınca bitirmeden bırakamıyorum.", new[] { "kitaplar" },
            ("En sevdiğin tür ne?", "kitap_03"), ("Başka alışkanlıkların var mı?", "aliskanlik_01")));
        l.Add(C("kitap_03", "En sevdiğim tür hangisi mi? Söylesem şaşırırsın.", new[] { "kitaplar", "bilimkurgu" },
            ("Neden bilim kurgu?", "bilimkurgu_01")));
        l.Add(C("kitap_04", "Eski baskı kitapların kokusunu özlüyorum.", new[] { "kitaplar", "anilar" },
            ("Ne gibi anıların var?", "anilar_01")));

        // ══════════ DÜKKAN ══════════
        l.Add(C("dukkan_01", "Buraya her geldiğimde kendimi evimde gibi hissediyorum.", new[] { "dukkan", "mahalle" },
            ("Mahalle hakkında ne düşünüyorsun?", "mahalle_01")));
        l.Add(C("dukkan_02", "Bahçe kısmı gerçekten huzur verici bir köşe olmuş.", new[] { "dukkan", "doga" },
            ("Doğayı sever misin?", "doga_01")));

        // ══════════ BİLİM KURGU / UZAY ══════════
        l.Add(C("bilimkurgu_01", "Uzaylı medeniyetleri hakkında yazılanları hep merak etmişimdir.", new[] { "bilimkurgu", "uzay" },
            ("Uzay hakkında ne düşünüyorsun?", "uzay_01")));
        l.Add(C("uzay_01", "Gökyüzüne bakarken hep 'acaba orada da birileri bize bakıyor mu' diye düşünürüm.", new[] { "uzay", "geceler" },
            ("Geceleri gökyüzü nasıl?", "geceler_01")));
        l.Add(C("geceler_01", "Gece gökyüzü çok net görünüyor bu mevsimde.", new[] { "geceler", "mevsimler" },
            ("Bu mevsimi seviyor musun?", "mevsimler_01")));

        // ══════════ HAVA / MEVSİM ══════════
        l.Add(C("hava_01", "Bugün hava tam da kitap okumaya uygun.", new[] { "hava_durumu", "kitaplar" },
            ("Kitap konusunu açalım mı?", "kitap_02")));
        l.Add(C("hava_02", "Yağmur yağarken bir şeyler okumak ayrı güzel.", new[] { "hava_durumu", "kitaplar" }));
        l.Add(C("mevsimler_01", "Bu mevsimi diğerlerine tercih ederim, bir sebebi var mı bilmiyorum.", new[] { "mevsimler" },
            ("Eski günleri özlüyor musun?", "anilar_02")));

        // ══════════ DOĞA ══════════
        l.Add(C("doga_01", "Bitkilerin arasında oturmak insanı sakinleştiriyor.", new[] { "doga", "hobiler" },
            ("Başka nelerle uğraşırsın?", "hobiler_01")));
        l.Add(C("doga_02", "Çocukken bahçede saatlerce oynardım.", new[] { "doga", "anilar" },
            ("Çocukluğunu özlüyor musun?", "anilar_02")));

        // ══════════ ANILAR ══════════
        l.Add(C("anilar_01", "Küçükken büyükannemin bana okuduğu masalları hâlâ hatırlıyorum.", new[] { "anilar", "aile" },
            ("Ailen hakkında ne düşünüyorsun?", "aile_01")));
        l.Add(C("anilar_02", "Eski günler her zaman daha güzel görünüyor, değil mi?", new[] { "anilar" },
            ("Kardeşinle aranız nasıl?", "aile_02")));

        // ══════════ AİLE ══════════
        l.Add(C("aile_01", "Ailem her zaman okumayı teşvik etmiştir.", new[] { "aile", "kitaplar" }));
        l.Add(C("aile_02", "Kardeşimle hâlâ eski günleri konuşuruz bazen.", new[] { "aile", "arkadaslik" },
            ("Arkadaşlık hakkında ne düşünüyorsun?", "arkadaslik_01")));

        // ══════════ ARKADAŞLIK ══════════
        l.Add(C("arkadaslik_01", "İyi bir dost bulmak, iyi bir kitap bulmaktan daha zor bence.", new[] { "arkadaslik", "kitaplar" }));
        l.Add(C("arkadaslik_02", "Eski arkadaşlarla bağ kurmak zamanla zorlaşıyor.", new[] { "arkadaslik" },
            ("İşin nasıl gidiyor?", "meslek_01")));

        // ══════════ MESLEK ══════════
        l.Add(C("meslek_01", "İşim beni bazen çok yoruyor ama şikayet edecek değilim.", new[] { "meslek" },
            ("Gelecek planların var mı?", "gelecek_01")));
        l.Add(C("meslek_02", "Meslektaşlarımla aramız gayet iyi, şanslıyım bu konuda.", new[] { "meslek", "arkadaslik" }));

        // ══════════ GELECEK ══════════
        l.Add(C("gelecek_01", "Bir gün kendi işimi kurmayı hayal ediyorum.", new[] { "gelecek", "meslek" }));
        l.Add(C("gelecek_02", "Nereye gideceğimi bilmiyorum ama bir yerlere gideceğimi biliyorum.", new[] { "gelecek", "seyahat" },
            ("Seyahat etmeyi düşünüyor musun?", "seyahat_01")));

        // ══════════ SEYAHAT ══════════
        l.Add(C("seyahat_01", "Hiç gitmediğim bir yere gitmek istiyorum bu yıl.", new[] { "seyahat" },
            ("Buradan ayrılmayı düşünür müsün?", "mahalle_02")));
        l.Add(C("seyahat_02", "Seyahat ederken hep yanımda bir kitap taşırım.", new[] { "seyahat", "kitaplar" }));

        // ══════════ MAHALLE ══════════
        l.Add(C("mahalle_01", "Bu mahalle eskisi kadar sessiz değil ama yine de seviyorum.", new[] { "mahalle" }));
        l.Add(C("mahalle_02", "Başka şehirleri gördükçe burasının kıymetini daha iyi anlıyorum.", new[] { "mahalle", "seyahat" }));

        // ══════════ YEMEK ══════════
        l.Add(C("yemek_01", "Bir fincan sıcak içecek eşliğinde okumak gibisi yok.", new[] { "yemek", "kitaplar" }));
        l.Add(C("yemek_02", "Annemin yemeklerini hâlâ özlüyorum açıkçası.", new[] { "yemek", "aile" },
            ("Ailenle ilgili başka anın var mı?", "aile_01")));

        // ══════════ MÜZİK ══════════
        l.Add(C("muzik_01", "Arka planda müzik olmadan pek çalışamıyorum.", new[] { "muzik", "hobiler" }));
        l.Add(C("muzik_02", "Eski şarkılar bana hep bir şeyleri hatırlatıyor.", new[] { "muzik", "anilar" }));

        // ══════════ HOBİLER ══════════
        l.Add(C("hobiler_01", "Boş zamanımda ellerimle bir şeyler yapmayı seviyorum.", new[] { "hobiler" }));
        l.Add(C("hobiler_02", "Yeni bir hobiye başlamak hiç geç değil derler.", new[] { "hobiler", "gelecek" }));

        // ══════════ HAYVANLAR ══════════
        l.Add(C("hayvan_01", "Bir hayvanım olsa hayatım daha renkli olurdu sanırım.", new[] { "hayvanlar" },
            ("Doğayı sever misin?", "doga_01")));
        l.Add(C("hayvan_02", "Sokak hayvanlarına yemek vermeyi hiç ihmal etmem.", new[] { "hayvanlar", "mahalle" }));

        // ══════════ RÜYALAR ══════════
        l.Add(C("ruya_01", "Dün gece çok garip bir rüya gördüm, hâlâ aklımdan çıkmıyor.", new[] { "ruyalar" },
            ("Başka neler görüyorsun rüyanda?", "ruya_02")));
        l.Add(C("ruya_02", "Rüyalarımda genelde uçtuğumu görürüm, tuhaf değil mi?", new[] { "ruyalar", "uzay" }));

        // ══════════ SAĞLIK ══════════
        l.Add(C("saglik_01", "Son zamanlarda daha çok yürümeye özen gösteriyorum.", new[] { "saglik" },
            ("Doğada vakit geçirir misin?", "doga_01")));

        // ══════════ ALIŞKANLIKLAR ══════════
        l.Add(C("aliskanlik_01", "Her sabah aynı rutini takip etmeden güne başlayamıyorum.", new[] { "aliskanliklar" }));
        l.Add(C("aliskanlik_02", "Bazı alışkanlıklarımdan vazgeçmek gerçekten zor.", new[] { "aliskanliklar", "gelecek" },
            ("Gelecekte değişmek ister misin?", "gelecek_01")));

        // ══════════ L25 (baska evrende hapis, yeri BILINMIYOR) ══════════
        l.Add(C("l25_01", "L25'in ortadan kaybolduğunu duydun mu? Aylardır kimse ondan haber alamıyor.", new[] { "l25" },
            ("Nereye gittiğini biliyor musun?", "l25_02")));
        l.Add(C("l25_02", "Bazıları başka bir evrende mahsur kaldığını söylüyor ama kimse tam olarak nerede olduğunu bilmiyor.", new[] { "l25" },
            ("Onunla tanışmış mıydın hiç?", "l25_03")));
        l.Add(C("l25_03", "Keşke L25 ile bir kez olsun konuşabilseydim, sorası çok şeyim vardı.", new[] { "l25" }));

        // ══════════ ESKİ GÖREVLİ (Tarı) ══════════
        l.Add(C("eskigorevli_01", "Buradaki eski görevliyi hatırlıyor musun? Onu düşünmeden edemiyorum bazen.", new[] { "eski_gorevli", "anilar" },
            ("Gittiğinden beri neler değişti?", "eskigorevli_02")));
        l.Add(C("eskigorevli_02", "Gittiğinden beri burası bir tuhaf, ama belki de ben öyle hissediyorumdur.", new[] { "eski_gorevli", "dukkan" },
            ("Onu nasıl hatırlıyorsun?", "eskigorevli_03")));
        l.Add(C("eskigorevli_03", "Onun her zaman oturduğu köşeye hâlâ oturmaya kıyamıyorum, garip gelecek ama.", new[] { "eski_gorevli" }));

        // ══════════ KÜTÜPHANENİN TARİHİ ══════════
        l.Add(C("kutuphanetarihi_01", "Bu kütüphanenin bin yıllık olduğunu biliyor muydun?", new[] { "kutuphane_tarihi", "dukkan" },
            ("Bu kadar eski bir yerde neler olmuştur?", "kutuphanetarihi_02")));
        l.Add(C("kutuphanetarihi_02", "Bu kadar eski bir yerde kaç insanın hikâyesi saklıdır kim bilir.", new[] { "kutuphane_tarihi", "anilar" },
            ("Eski görevliyi tanıyor muydun?", "eskigorevli_01")));

        // ══════════ BUZ OKYANUSU (Tömye'nin okyanusu) ══════════
        l.Add(C("buzokyanusu_01", "Tömye'nin okyanusunun tamamen buzla kaplı olduğunu biliyor muydun?", new[] { "buz_okyanusu", "uzay" },
            ("Altında ne olduğunu hiç merak ettin mi?", "buzokyanusu_02")));
        l.Add(C("buzokyanusu_02", "Buzun altında ne olduğunu kimse bilmiyor, hiç merak etmedin mi?", new[] { "buz_okyanusu" },
            ("Sen oraya gider miydin?", "buzokyanusu_03")));
        l.Add(C("buzokyanusu_03", "Bazı geceler oraya gidip bakmayı hayal ederim, korkak olmasam tabii.", new[] { "buz_okyanusu", "geceler" }));

        // ══════════ KAELAN'IN AİLESİNİN ÖLÜMÜ ══════════
        l.Add(C("kaelan_ailesi_01", "Kaelan'ın ailesini genç yaşta kaybettiğini duymuştum, ne kadar zor olmalı.", new[] { "kaelan_ailesi", "anilar" },
            ("Nasıl atlatmış bunu?", "kaelan_ailesi_02")));
        l.Add(C("kaelan_ailesi_02", "Bazı insanlar böyle şeyleri hiç atlatamıyor, sadece üstünü örtüyor.", new[] { "kaelan_ailesi" },
            ("Onu nasıl biri olarak tanırsın?", "kaelan_ailesi_03")));
        l.Add(C("kaelan_ailesi_03", "Duyduğuma göre onlar ölmeden çok önce bile hep kendi dünyasındaymış, kimse tam anlayamamış onu.", new[] { "kaelan_ailesi" }));

        return l;
    }
}
#endif