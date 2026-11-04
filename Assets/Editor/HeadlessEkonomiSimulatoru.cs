// HeadlessEkonomiSimulatoru.cs — Editor araci: Play Mode'a HIC girmeden, projedeki
// CustomerData asset'lerinden (karakterHavuzu, sabirCarpani, odemeCarpani, preferredRarity)
// beslenen basit bir Monte Carlo simulasyonuyla N gunluk altin akisini TAHMIN eder.
//
// ONEMLI: bu GERCEK ShopManager/EconomyManager mantiginin BIREBIR AYNISI DEGILDIR — o kod
// bu araca dahil edilmedi (bu dosyada goremiyoruz). Amac, DENGE denemelerini (fiyatlar,
// musteri sabri, gunluk musteri sayisi) PLAYTEST ETMEDEN hizlica gormek. Sonuclari "kesin"
// degil "yaklasik/gorece" olarak oku — iki farkli ayar setini KARSILASTIRMAK icin cok daha
// guvenilirdir, tek bir sayiya guvenmek icin degil.
//
// KULLANIM: Delilik > Ekonomi > Headless Simülatör
// Bu dosya Assets/Editor/ klasorune konulmali (Unity otomatik olarak build'e dahil etmez).
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class HeadlessEkonomiSimulatoru : EditorWindow
{
    private class VeriKaynagi
    {
        public CustomerData veri;
        public bool secili = true;
    }

    private List<VeriKaynagi> kaynaklar = new List<VeriKaynagi>();

    private int gunSayisi = 30;
    private int tekrarSayisi = 20;
    private int gunlukMusteriSayisi = 15;
    private float temelSatinAlmaSansi = 0.55f;

    private Dictionary<Rarity, float> ortalamaFiyatlar = new Dictionary<Rarity, float>();

    private float[] ortalamaGunlukGelir;
    private float[] kumulatifOrtalamaGelir;
    private float ortalamaKayipSatisOrani;
    private bool sonucVarMi = false;

    [MenuItem("Delilik/Ekonomi/Headless Simülatör")]
    public static void PencereyiAc()
    {
        GetWindow<HeadlessEkonomiSimulatoru>("Ekonomi Simülatörü");
    }

    private void OnEnable()
    {
        KaynaklariYukle();
        if (ortalamaFiyatlar.Count == 0) FiyatlariVarsayilanaAyarla();
    }

    private void KaynaklariYukle()
    {
        kaynaklar.Clear();
        string[] guidler = AssetDatabase.FindAssets("t:CustomerData");
        foreach (string guid in guidler)
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            CustomerData veri = AssetDatabase.LoadAssetAtPath<CustomerData>(yol);
            if (veri != null) kaynaklar.Add(new VeriKaynagi { veri = veri, secili = true });
        }
    }

    private void FiyatlariVarsayilanaAyarla()
    {
        ortalamaFiyatlar.Clear();
        foreach (Rarity r in Enum.GetValues(typeof(Rarity)))
        {
            // Basit varsayilan egri: enum sirasi arttikca (daha nadir oldugu varsayilarak)
            // fiyat artar — kendi ekonomine gore asagidan elle degistir.
            ortalamaFiyatlar[r] = 20f + (int)r * 30f;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Headless Ekonomi Simülatörü", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "TAHMINI bir modeldir — ShopManager/EconomyManager'in gerçek mantığıyla birebir aynı " +
            "DEĞİLDİR. Play Mode'a girmeden denge denemeleri için hızlı bir yaklaşım sağlar; iki " +
            "farklı ayarı KARŞILAŞTIRMAK için güvenilir, tek bir sayıya güvenmek için değil.",
            MessageType.Warning);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Müşteri Kaynakları (CustomerData)", EditorStyles.boldLabel);
        if (GUILayout.Button("Yenile", GUILayout.Width(70))) KaynaklariYukle();
        EditorGUILayout.EndHorizontal();

        if (kaynaklar.Count == 0)
        {
            EditorGUILayout.HelpBox("Projede CustomerData asset'i bulunamadı.", MessageType.Warning);
        }
        else
        {
            foreach (var k in kaynaklar)
                k.secili = EditorGUILayout.ToggleLeft(k.veri.name + $"  ({(k.veri.karakterHavuzu != null ? k.veri.karakterHavuzu.Count : 0)} karakter)", k.secili);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Simülasyon Ayarları", EditorStyles.boldLabel);
        gunSayisi = EditorGUILayout.IntSlider("Gün Sayısı", gunSayisi, 1, 200);
        tekrarSayisi = EditorGUILayout.IntSlider("Tekrar (Monte Carlo)", tekrarSayisi, 1, 200);
        gunlukMusteriSayisi = EditorGUILayout.IntSlider("Günlük Müşteri Sayısı", gunlukMusteriSayisi, 1, 100);
        temelSatinAlmaSansi = EditorGUILayout.Slider("Temel Satın Alma Şansı", temelSatinAlmaSansi, 0f, 1f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Nadirliğe Göre Ortalama Fiyat", EditorStyles.boldLabel);
        foreach (Rarity r in ortalamaFiyatlar.Keys.ToList())
            ortalamaFiyatlar[r] = EditorGUILayout.FloatField(r.ToString(), ortalamaFiyatlar[r]);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(kaynaklar.Count == 0 || !kaynaklar.Any(k => k.secili)))
        {
            if (GUILayout.Button("ÇALIŞTIR", GUILayout.Height(32))) SimulasyonuCalistir();
        }

        if (!sonucVarMi) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sonuçlar", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Ortalama günlük gelir: {ortalamaGunlukGelir.Average():F0} altın");
        EditorGUILayout.LabelField($"{gunSayisi} günde tahmini toplam: {kumulatifOrtalamaGelir[kumulatifOrtalamaGelir.Length - 1]:F0} altın");
        EditorGUILayout.LabelField($"Ortalama kayıp satış oranı: %{ortalamaKayipSatisOrani * 100f:F1}  (satın almadan giden müşteriler)");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Kümülatif Gelir Eğrisi", EditorStyles.miniBoldLabel);
        Rect grafikRect1 = GUILayoutUtility.GetRect(position.width - 20, 120);
        GrafikCiz(grafikRect1, kumulatifOrtalamaGelir, "Kümülatif Altın");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Günlük Gelir (gün gün)", EditorStyles.miniBoldLabel);
        Rect grafikRect2 = GUILayoutUtility.GetRect(position.width - 20, 100);
        GrafikCiz(grafikRect2, ortalamaGunlukGelir, "Günlük Altın");
    }

    private void SimulasyonuCalistir()
    {
        var havuz = new List<NPCKimligi>();
        var havuzSahibi = new List<CustomerData>();

        foreach (var k in kaynaklar)
        {
            if (!k.secili || k.veri.karakterHavuzu == null) continue;
            foreach (var kimlik in k.veri.karakterHavuzu)
            {
                if (kimlik == null) continue;
                havuz.Add(kimlik);
                havuzSahibi.Add(k.veri);
            }
        }

        if (havuz.Count == 0)
        {
            EditorUtility.DisplayDialog("Boş Havuz", "Seçili CustomerData'larda hiç karakter bulunamadı.", "Tamam");
            return;
        }

        var random = new System.Random();
        var gunlukGelirToplam = new float[gunSayisi];
        float kayipSatisToplam = 0f;
        int toplamMusteriSayisi = 0;

        for (int tekrar = 0; tekrar < tekrarSayisi; tekrar++)
        {
            for (int gun = 0; gun < gunSayisi; gun++)
            {
                float gunlukGelir = 0f;
                for (int m = 0; m < gunlukMusteriSayisi; m++)
                {
                    int index = random.Next(havuz.Count);
                    NPCKimligi kimlik = havuz[index];
                    CustomerData sahibi = havuzSahibi[index];

                    // Basit model: yuksek sabirCarpani -> daha yuksek satin alma sansi.
                    float satinAlmaSansi = Mathf.Clamp01(temelSatinAlmaSansi * (0.5f + 0.5f * kimlik.sabirCarpani));
                    toplamMusteriSayisi++;

                    if (random.NextDouble() <= satinAlmaSansi)
                    {
                        Rarity secilenRarity = (random.NextDouble() <= 0.5) ? sahibi.preferredRarity : RastgeleRarity(random);
                        float fiyat = ortalamaFiyatlar.TryGetValue(secilenRarity, out float f) ? f : 20f;
                        gunlukGelir += fiyat * kimlik.odemeCarpani;
                    }
                    else
                    {
                        kayipSatisToplam += 1f;
                    }
                }

                gunlukGelirToplam[gun] += gunlukGelir;
            }
        }

        ortalamaGunlukGelir = new float[gunSayisi];
        kumulatifOrtalamaGelir = new float[gunSayisi];
        float kumulatif = 0f;
        for (int gun = 0; gun < gunSayisi; gun++)
        {
            ortalamaGunlukGelir[gun] = gunlukGelirToplam[gun] / tekrarSayisi;
            kumulatif += ortalamaGunlukGelir[gun];
            kumulatifOrtalamaGelir[gun] = kumulatif;
        }

        ortalamaKayipSatisOrani = toplamMusteriSayisi > 0 ? kayipSatisToplam / toplamMusteriSayisi : 0f;
        sonucVarMi = true;
    }

    private static Rarity RastgeleRarity(System.Random random)
    {
        var degerler = (Rarity[])Enum.GetValues(typeof(Rarity));
        return degerler[random.Next(degerler.Length)];
    }

    private void GrafikCiz(Rect rect, float[] veriler, string baslik)
    {
        GUI.Box(rect, GUIContent.none);
        if (veriler == null || veriler.Length < 2) return;

        float minY = veriler.Min();
        float maxY = veriler.Max();
        if (Mathf.Approximately(minY, maxY)) maxY = minY + 1f;

        Handles.BeginGUI();
        Handles.color = new Color(0.3f, 0.8f, 0.45f);

        Vector2 oncekiNokta = Vector2.zero;
        for (int i = 0; i < veriler.Length; i++)
        {
            float xOran = i / (float)(veriler.Length - 1);
            float yOran = (veriler[i] - minY) / (maxY - minY);

            float x = rect.x + 8 + xOran * (rect.width - 16);
            float y = rect.y + rect.height - 8 - yOran * (rect.height - 16);

            Vector2 nokta = new Vector2(x, y);
            if (i > 0) Handles.DrawLine(oncekiNokta, nokta);
            oncekiNokta = nokta;
        }
        Handles.EndGUI();

        GUI.Label(new Rect(rect.x + 4, rect.y + 2, 280, 16), $"{baslik} — min:{minY:F0}  maks:{maxY:F0}", EditorStyles.miniLabel);
    }
}
