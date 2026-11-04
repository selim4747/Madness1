// SpriteUzuvBolucu.cs — TEK PARCA bir karakter sprite'ini (Afor2/Zengin0/VIP0/Normal0
// tarzinda), 4 ORNEK gorselin piksel analizinden cikan SABIT ORANLARA gore 6 parcaya boler
// (Bas, Govde, SolKol, SagKol, SolBacak, SagBacak), Unity'nin "Multiple Sprite" texture
// dilimleme sistemini kullanarak GERCEK, KALICI alt-sprite'lar uretir, sonra bunlari kullanan
// TAM bir karakter hiyerarsisi + calisan ProseduralHareketAnimasyonu component'i OTOMATIK kurar.
//
// ORTUSME PAYI: Parcalarin GORSEL KESIM sinirlari, birbirine TAM deginmek yerine birkac
// piksel ORTUSUR — yuvarlama/piksel-hizalama kaynakli "hayalet bosluk" (bas-boyun arasi gibi)
// boylece onlenir. ONEMLI: bu ortusme SADECE KESIM icin uygulanir — EKLEM (pivot/anchor)
// noktalari HER ZAMAN orijinal (ortusmesiz) sinirdan hesaplanir, yoksa omuz/boyun/kalca
// noktalari kayar ve karakter yaninca "carpik" gorunur.
//
// ONEMLI ON KOSUL: Kaynak texture'in Import Settings'inde "Read/Write Enabled" ACIK olmali.
// Tool bunu KONTROL EDER, kapaliysa GECICI acar, isi bitince ESKI HALINE GERI DONDURUR.
//
// SABLON VARSAYIMI (4 ornek gorselden cikarilan oranlar):
//   %0-20   icerik yuksekligi  -> BAS (tam genislik)
//   %20-52  icerik yuksekligi  -> KOL+GOVDE bandi
//   %52-77  icerik yuksekligi  -> GOVDE/KALCA (kolsuz, govde-genisliginde)
//   %77-100 icerik yuksekligi  -> BACAKLAR (ortadan ikiye bolunur)
//
// Menu: Delilik > Sprite Uzuv Bolucu
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SpriteUzuvBolucu : EditorWindow
{
    private Texture2D kaynakTexture;
    private GameObject hedefKok;
    private float pixelsPerUnit = 100f;

    [MenuItem("Delilik/Sprite Uzuv Bolucu")]
    public static void PenceresiniAc()
    {
        GetWindow<SpriteUzuvBolucu>("Sprite Uzuv Bolucu");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Kafa+govde+kol+bacak TEK PARCA cizilmis bir karakter sprite'ini 6 parcaya boler " +
            "ve calisan bir ProseduralHareketAnimasyonu hiyerarsisi kurar. SADECE Afor2/Zengin0/" +
            "VIP0/Normal0 ile AYNI oranli sablona uyan gorsellerde dogru sonuc verir.",
            MessageType.Info);

        GUILayout.Space(10);
        kaynakTexture = (Texture2D)EditorGUILayout.ObjectField(
            new GUIContent("Kaynak Sprite", "Bolunecek, tek parca karakter texture'i."),
            kaynakTexture, typeof(Texture2D), false);

        hedefKok = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Hedef Obje (opsiyonel)", "Uzuv hiyerarsisi buraya EKLENIR. Bos birakilirsa sifirdan yeni bir karakter objesi olusturulur."),
            hedefKok, typeof(GameObject), true);

        pixelsPerUnit = EditorGUILayout.FloatField(
            new GUIContent("Pixels Per Unit", "Sprite'larinin import ayarindaki PPU degeriyle AYNI olmali."),
            pixelsPerUnit);

        GUILayout.Space(16);

        GUI.enabled = kaynakTexture != null;
        if (GUILayout.Button("Böl ve Karakteri Kur", GUILayout.Height(36)))
        {
            BolVeKur();
        }
        GUI.enabled = true;

        if (kaynakTexture == null)
            EditorGUILayout.HelpBox("Önce bölünecek texture'i sürükle.", MessageType.Info);
    }

    private void BolVeKur()
    {
        string yol = AssetDatabase.GetAssetPath(kaynakTexture);
        TextureImporter importer = AssetImporter.GetAtPath(yol) as TextureImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("Hata", "Bu dosyanin TextureImporter'ina erisilemedi.", "Tamam");
            return;
        }

        bool eskiReadWrite = importer.isReadable;
        bool geciciAcildi = false;
        if (!eskiReadWrite)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
            geciciAcildi = true;
        }

        Rect icerikKutusu;
        if (!IcerikKutusuBul(kaynakTexture, out icerikKutusu))
        {
            EditorUtility.DisplayDialog("Hata", "Texture tamamen seffaf görünüyor, içerik bulunamadı.", "Tamam");
            GeriAl(importer, eskiReadWrite, geciciAcildi);
            return;
        }

        float govdeGenisligi, govdeMerkezX;
        GovdeGenisliginiBul(kaynakTexture, icerikKutusu, out govdeGenisligi, out govdeMerkezX);

        // ONEMLI SIRA: ONCE orijinal (ortusmesiz, ANATOMIK DOGRU) sinirlari hesapla —
        // eklem/pivot noktalari HER ZAMAN bunlardan turetilir. SONRA gorsel kesim icin
        // genisletilmis (ortusmeli) versiyonunu uret — o SADECE SpriteMetaData.rect icindir.
        Dictionary<string, Rect> orijinalParcalar = OrijinalParcaSinirlariniHesapla(icerikKutusu, govdeGenisligi, govdeMerkezX);
        Dictionary<string, Rect> kesimParcalari = OrtusmeliKesimSinirlariniHesapla(orijinalParcalar, icerikKutusu, govdeGenisligi);

        List<SpriteMetaData> metaListesi = new List<SpriteMetaData>();
        foreach (string parcaAdi in orijinalParcalar.Keys)
        {
            Rect kesimRect = kesimParcalari[parcaAdi];
            Rect orijinalRect = orijinalParcalar[parcaAdi];

            SpriteMetaData meta = new SpriteMetaData();
            meta.name = kaynakTexture.name + "_" + parcaAdi;
            meta.rect = kesimRect;
            meta.alignment = (int)SpriteAlignment.Custom;
            // Pivot, GENISLETILMIS rect icinde, orijinal (dogru) eklem noktasina denk
            // gelecek sekilde YENIDEN hesaplanir — boylece gorsel kesim buyuse de eklem
            // noktasi KAYMAZ.
            meta.pivot = PivotuGenisletilmisRectIcindeYenidenHesapla(parcaAdi, orijinalRect, kesimRect);
            metaListesi.Add(meta);
        }

        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritesheet = metaListesi.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Dictionary<string, Sprite> uretilenSpritelar = new Dictionary<string, Sprite>();
        Object[] tumVarliklar = AssetDatabase.LoadAllAssetsAtPath(yol);
        foreach (Object o in tumVarliklar)
        {
            Sprite s = o as Sprite;
            if (s == null) continue;
            foreach (string parcaAdi in orijinalParcalar.Keys)
                if (s.name == kaynakTexture.name + "_" + parcaAdi) uretilenSpritelar[parcaAdi] = s;
        }

        if (uretilenSpritelar.Count != 6)
        {
            EditorUtility.DisplayDialog("Hata", "6 parcanin hepsi uretilemedi (" + uretilenSpritelar.Count + "/6). Texture Read/Write ayarini ve icerigini kontrol et.", "Tamam");
            GeriAl(importer, eskiReadWrite, geciciAcildi);
            return;
        }

        GameObject kok = hedefKok != null ? hedefKok : new GameObject(kaynakTexture.name + "_Karakter");
        if (hedefKok == null) Undo.RegisterCreatedObjectUndo(kok, "Karakter Olustur");

        // GameObject POZISYONU her zaman ORIJINAL (ortusmesiz) sinirdaki eklem noktasindan
        // hesaplanir — genisletilmis kesim rect'i SADECE gorsel icin, konum icin degil.
        Transform govdeT = UzuvOlustur(kok.transform, "Govde", uretilenSpritelar["Govde"], icerikKutusu, orijinalParcalar["Govde"], pixelsPerUnit, 10);
        Transform solBacakT = UzuvOlustur(kok.transform, "SolBacak", uretilenSpritelar["SolBacak"], icerikKutusu, orijinalParcalar["SolBacak"], pixelsPerUnit, 9);
        Transform sagBacakT = UzuvOlustur(kok.transform, "SagBacak", uretilenSpritelar["SagBacak"], icerikKutusu, orijinalParcalar["SagBacak"], pixelsPerUnit, 9);
        Transform solKolT = UzuvOlustur(kok.transform, "SolKol", uretilenSpritelar["SolKol"], icerikKutusu, orijinalParcalar["SolKol"], pixelsPerUnit, 11);
        Transform sagKolT = UzuvOlustur(kok.transform, "SagKol", uretilenSpritelar["SagKol"], icerikKutusu, orijinalParcalar["SagKol"], pixelsPerUnit, 11);
        Transform basT = UzuvOlustur(kok.transform, "Bas", uretilenSpritelar["Bas"], icerikKutusu, orijinalParcalar["Bas"], pixelsPerUnit, 12);

        ProseduralHareketAnimasyonu animasyon = kok.GetComponent<ProseduralHareketAnimasyonu>();
        if (animasyon == null) animasyon = Undo.AddComponent<ProseduralHareketAnimasyonu>(kok);

        SerializedObject so = new SerializedObject(animasyon);
        so.FindProperty("govde").objectReferenceValue = govdeT;
        so.FindProperty("solBacak").objectReferenceValue = solBacakT;
        so.FindProperty("sagBacak").objectReferenceValue = sagBacakT;
        so.FindProperty("solKol").objectReferenceValue = solKolT;
        so.FindProperty("sagKol").objectReferenceValue = sagKolT;
        so.FindProperty("bas").objectReferenceValue = basT;
        so.ApplyModifiedProperties();

        GeriAl(importer, eskiReadWrite, geciciAcildi);

        Selection.activeGameObject = kok;
        EditorUtility.DisplayDialog("Tamamlandı",
            "'" + kaynakTexture.name + "' 6 parçaya bölündü ve '" + kok.name + "' karakteri kuruldu.\n\n" +
            "ProseduralHareketAnimasyonu otomatik baglandi. Play'e basip test edebilirsin.",
            "Tamam");
    }

    /// <summary>Texture'daki alpha>0 olan pikselleri tarayip icerik sinirlarini bulur.</summary>
    private static bool IcerikKutusuBul(Texture2D tex, out Rect sonuc)
    {
        int w = tex.width, h = tex.height;
        Color32[] pikseller = tex.GetPixels32();

        int minX = w, maxX = -1, minY = h, maxY = -1;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                byte a = pikseller[y * w + x].a;
                if (a > 10)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX < 0) { sonuc = default; return false; }
        sonuc = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return true;
    }

    /// <summary>%52-77 icerik-yuksekligi bandindaki genisligi ve merkez X'ini bulur.</summary>
    private static void GovdeGenisliginiBul(Texture2D tex, Rect icerik, out float genislik, out float merkezX)
    {
        int w = tex.width;
        Color32[] pikseller = tex.GetPixels32();

        float ustten52 = icerik.y + icerik.height * (1f - 0.52f);
        float ustten77 = icerik.y + icerik.height * (1f - 0.77f);
        int yBaslangic = Mathf.RoundToInt(Mathf.Min(ustten52, ustten77));
        int yBitis = Mathf.RoundToInt(Mathf.Max(ustten52, ustten77));

        int toplamMinX = int.MaxValue, toplamMaxX = int.MinValue;
        int ornekSayisi = 0;

        for (int y = yBaslangic; y <= yBitis; y++)
        {
            int minX = -1, maxX = -1;
            for (int x = (int)icerik.xMin; x <= (int)icerik.xMax; x++)
            {
                if (y < 0 || y >= tex.height || x < 0 || x >= w) continue;
                byte a = pikseller[y * w + x].a;
                if (a > 10)
                {
                    if (minX < 0) minX = x;
                    maxX = x;
                }
            }
            if (minX >= 0)
            {
                toplamMinX = Mathf.Min(toplamMinX, minX);
                toplamMaxX = Mathf.Max(toplamMaxX, maxX);
                ornekSayisi++;
            }
        }

        if (ornekSayisi == 0) { genislik = icerik.width; merkezX = icerik.center.x; return; }

        genislik = toplamMaxX - toplamMinX + 1;
        merkezX = (toplamMinX + toplamMaxX) / 2f;
    }

    /// <summary>6 parcanin ORIJINAL (ortusmesiz) sinirlarini hesaplar — bunlar ANATOMIK
    /// DOGRU eklem noktalarini temsil eder, pozisyon/pivot hesaplari HER ZAMAN bunlardan
    /// turetilir.</summary>
    private static Dictionary<string, Rect> OrijinalParcaSinirlariniHesapla(Rect icerik, float govdeGenisligi, float govdeMerkezX)
    {
        Dictionary<string, Rect> sonuc = new Dictionary<string, Rect>();

        float govdeSol = govdeMerkezX - govdeGenisligi / 2f;
        float govdeSag = govdeMerkezX + govdeGenisligi / 2f;

        float UstYuzdeRectY(float ustYuzde) => icerik.y + icerik.height * (1f - ustYuzde);

        sonuc["Bas"] = Rect.MinMaxRect(icerik.xMin, UstYuzdeRectY(0.20f), icerik.xMax, UstYuzdeRectY(0f));
        sonuc["SolKol"] = Rect.MinMaxRect(icerik.xMin, UstYuzdeRectY(0.52f), govdeSol, UstYuzdeRectY(0.20f));
        sonuc["SagKol"] = Rect.MinMaxRect(govdeSag, UstYuzdeRectY(0.52f), icerik.xMax, UstYuzdeRectY(0.20f));
        sonuc["Govde"] = Rect.MinMaxRect(govdeSol, UstYuzdeRectY(0.77f), govdeSag, UstYuzdeRectY(0.20f));
        sonuc["SolBacak"] = Rect.MinMaxRect(govdeSol, UstYuzdeRectY(1f), govdeMerkezX, UstYuzdeRectY(0.77f));
        sonuc["SagBacak"] = Rect.MinMaxRect(govdeMerkezX, UstYuzdeRectY(1f), govdeSag, UstYuzdeRectY(0.77f));

        return sonuc;
    }

    /// <summary>Orijinal sinirlari, KOMSU parcaya dogru birkac piksel GENISLETEREK gorsel
    /// kesim icin kullanilacak rect'leri uretir. SADECE gorsel kesim icin — konum/pivot
    /// hesabinda KULLANILMAZ.</summary>
    private static Dictionary<string, Rect> OrtusmeliKesimSinirlariniHesapla(Dictionary<string, Rect> orijinal, Rect icerik, float govdeGenisligi)
    {
        float dikeyPay = icerik.height * 0.03f;
        float yatayPay = govdeGenisligi * 0.08f;

        Dictionary<string, Rect> sonuc = new Dictionary<string, Rect>();

        sonuc["Bas"] = Genislet(orijinal["Bas"], sol: 0, sag: 0, alt: dikeyPay, ust: 0);
        sonuc["SolKol"] = Genislet(orijinal["SolKol"], sol: 0, sag: yatayPay, alt: 0, ust: dikeyPay);
        sonuc["SagKol"] = Genislet(orijinal["SagKol"], sol: yatayPay, sag: 0, alt: 0, ust: dikeyPay);
        sonuc["Govde"] = Genislet(orijinal["Govde"], sol: 0, sag: 0, alt: dikeyPay, ust: dikeyPay);
        sonuc["SolBacak"] = Genislet(orijinal["SolBacak"], sol: 0, sag: 0, alt: 0, ust: dikeyPay);
        sonuc["SagBacak"] = Genislet(orijinal["SagBacak"], sol: 0, sag: 0, alt: 0, ust: dikeyPay);

        return sonuc;
    }

    private static Rect Genislet(Rect r, float sol, float sag, float alt, float ust)
    {
        return Rect.MinMaxRect(r.xMin - sol, r.yMin - alt, r.xMax + sag, r.yMax + ust);
    }

    /// <summary>Her uzuv tipinin EKLEM noktasina gore ORIJINAL rect icindeki normalize pivotu.</summary>
    private static Vector2 ParcaninAnatomikPivotu(string parcaAdi)
    {
        switch (parcaAdi)
        {
            case "Bas": return new Vector2(0.5f, 0f);
            case "SolKol": return new Vector2(0.5f, 1f);
            case "SagKol": return new Vector2(0.5f, 1f);
            case "SolBacak": return new Vector2(0.5f, 1f);
            case "SagBacak": return new Vector2(0.5f, 1f);
            default: return new Vector2(0.5f, 0.5f); // Govde
        }
    }

    /// <summary>Orijinal rect'teki anatomik pivotun MUTLAK piksel konumunu, GENISLETILMIS
    /// (kesim) rect'in KENDI normalize (0-1) koordinat sistemine cevirir — boylece sprite'in
    /// gercek pivotu, gorsel kesim buyumus olsa bile DOGRU eklem noktasinda kalir.</summary>
    private static Vector2 PivotuGenisletilmisRectIcindeYenidenHesapla(string parcaAdi, Rect orijinalRect, Rect genisletilmisRect)
    {
        Vector2 orijinalPivotNormalize = ParcaninAnatomikPivotu(parcaAdi);
        Vector2 mutlakPivotNoktasi = new Vector2(
            orijinalRect.xMin + orijinalRect.width * orijinalPivotNormalize.x,
            orijinalRect.yMin + orijinalRect.height * orijinalPivotNormalize.y
        );

        return new Vector2(
            (mutlakPivotNoktasi.x - genisletilmisRect.xMin) / genisletilmisRect.width,
            (mutlakPivotNoktasi.y - genisletilmisRect.yMin) / genisletilmisRect.height
        );
    }

    /// <summary>Bir parcanin ORIJINAL (ortusmesiz) rect'ine gore, o parcanin eklem/capa
    /// noktasinin MUTLAK piksel konumunu bulur — GameObject'in transform pozisyonu buraya
    /// denk gelir.</summary>
    private static Vector2 ParcaninCapaNoktasi(string parcaAdi, Rect orijinalParcaRect)
    {
        Vector2 pivotNormalize = ParcaninAnatomikPivotu(parcaAdi);
        return new Vector2(
            orijinalParcaRect.xMin + orijinalParcaRect.width * pivotNormalize.x,
            orijinalParcaRect.yMin + orijinalParcaRect.height * pivotNormalize.y
        );
    }

    private static Transform UzuvOlustur(Transform ebeveyn, string isim, Sprite sprite,
                                          Rect icerikKutusu, Rect orijinalParcaRect, float ppu, int sortingOrder)
    {
        GameObject go = new GameObject(isim);
        Undo.RegisterCreatedObjectUndo(go, "Uzuv Olustur: " + isim);
        go.transform.SetParent(ebeveyn, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;

        Vector2 icerikMerkezi = icerikKutusu.center;
        Vector2 parcaCapasi = ParcaninCapaNoktasi(isim, orijinalParcaRect);
        Vector2 pikselOfset = parcaCapasi - icerikMerkezi;

        go.transform.localPosition = new Vector3(pikselOfset.x / ppu, pikselOfset.y / ppu, 0f);

        return go.transform;
    }

    private static void GeriAl(TextureImporter importer, bool eskiReadWrite, bool geciciAcildiMi)
    {
        if (geciciAcildiMi && !eskiReadWrite)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
    }
}
#endif