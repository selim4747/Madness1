// KonuSenkronizasyonuDoldurucusu.cs — Editor araci: BahceKonuVerisi asset'lerindeki
// "tumCumleler[].konular" alanlarinda GECEN ama "tanimliKonular" (Inspector'daki 'Konu Ekle'
// dropdown'unun besledigi kayit listesi) icinde TANIMLI OLMAYAN etiketleri bulur ve otomatik
// ekler.
//
// NEDEN GEREKLI: "konular" alani duz bir List<string> oldugu icin, Inspector'da yeni bir
// cumle yazarken konu adini DOGRUDAN yazip (Konu Ekle butonunu kullanmadan) gecebilirsin —
// bu durumda o konu cumleye baglanir ama "tanimliKonular" listesine KAYDEDILMEZ, yani dropdown
// listesinde gorunmez ve baska bir cumleyi AYNI konuya baglamak istedigin zaman elle
// hatirlaman/yazman gerekir (yazim hatasi riski). Bu arac bu senkronizasyon BOSLUGUNU kapatir.
//
// TAMAMEN MEKANIK — hicbir YENI konu adi UYDURULMAZ, sadece cumlelerde ZATEN yazili olan
// etiketler kayit listesine KOPYALANIR. Onay sormaz, "DOLDUR" butonuna basinca direkt uygular.
//
// KULLANIM: Delilik > Diyalog > Konu Senkronizasyonu Doldurucusu
// Bu dosya Assets/Editor/ klasorune konulmali (Unity otomatik olarak build'e dahil etmez).
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class KonuSenkronizasyonuDoldurucusu : EditorWindow
{
    private class VeriKaynagi
    {
        public BahceKonuVerisi veri;
        public bool secili = true;
        public List<string> eksikKonular = new List<string>();
    }

    private List<VeriKaynagi> kaynaklar = new List<VeriKaynagi>();
    private bool tarandiMi = false;

    [MenuItem("Delilik/Diyalog/Konu Senkronizasyonu Doldurucusu")]
    public static void PencereyiAc()
    {
        GetWindow<KonuSenkronizasyonuDoldurucusu>("Konu Senkronizasyonu");
    }

    private void OnEnable()
    {
        KaynaklariYukle();
    }

    private void KaynaklariYukle()
    {
        kaynaklar.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:BahceKonuVerisi"))
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            BahceKonuVerisi veri = AssetDatabase.LoadAssetAtPath<BahceKonuVerisi>(yol);
            if (veri != null) kaynaklar.Add(new VeriKaynagi { veri = veri, secili = true });
        }
        tarandiMi = false;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Konu Senkronizasyonu Doldurucusu", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "'tumCumleler[].konular' içinde geçen ama 'tanımlı konular' listesinde kayıtlı " +
            "olmayan etiketleri bulur ve ekler. Tamamen mekanik — hiçbir yeni isim uydurulmaz, " +
            "sadece zaten yazılmış etiketler kayıt listesine kopyalanır. Onay sormaz.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("BahceKonuVerisi Asset'leri", EditorStyles.boldLabel);
        if (GUILayout.Button("Yenile", GUILayout.Width(70))) KaynaklariYukle();
        EditorGUILayout.EndHorizontal();

        if (kaynaklar.Count == 0)
        {
            EditorGUILayout.HelpBox("Projede BahceKonuVerisi asset'i bulunamadı.", MessageType.Warning);
            return;
        }

        foreach (var k in kaynaklar)
            k.secili = EditorGUILayout.ToggleLeft(k.veri.name, k.secili);

        EditorGUILayout.Space();
        if (GUILayout.Button("Tara", GUILayout.Height(28))) Tara();

        if (!tarandiMi) return;

        EditorGUILayout.Space();
        int toplamEksik = kaynaklar.Where(k => k.secili).Sum(k => k.eksikKonular.Count);
        EditorGUILayout.LabelField($"Toplam eksik konu: {toplamEksik}", EditorStyles.boldLabel);

        foreach (var k in kaynaklar)
        {
            if (!k.secili || k.eksikKonular.Count == 0) continue;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{k.veri.name}  ({k.eksikKonular.Count} eksik)", EditorStyles.miniBoldLabel);
            foreach (string konu in k.eksikKonular)
                EditorGUILayout.LabelField("   • " + konu);
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(toplamEksik == 0))
        {
            if (GUILayout.Button("DOLDUR (onay sorulmaz)", GUILayout.Height(32))) Doldur();
        }
    }

    private void Tara()
    {
        foreach (var k in kaynaklar)
        {
            k.eksikKonular.Clear();
            if (!k.secili || k.veri.tumCumleler == null) continue;

            var tanimliSet = new HashSet<string>(k.veri.tanimliKonular ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var kullanilanSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var c in k.veri.tumCumleler)
            {
                if (c == null || c.konular == null) continue;
                foreach (string konu in c.konular)
                {
                    if (!string.IsNullOrWhiteSpace(konu)) kullanilanSet.Add(konu.Trim());
                }
            }

            foreach (string konu in kullanilanSet)
            {
                if (!tanimliSet.Contains(konu)) k.eksikKonular.Add(konu);
            }

            k.eksikKonular.Sort(StringComparer.OrdinalIgnoreCase);
        }

        tarandiMi = true;
    }

    private void Doldur()
    {
        int eklenenToplam = 0;
        int guncellenenAssetSayisi = 0;

        foreach (var k in kaynaklar)
        {
            if (!k.secili || k.eksikKonular.Count == 0) continue;

            SerializedObject so = new SerializedObject(k.veri);
            SerializedProperty listeProp = so.FindProperty("tanimliKonular");
            if (listeProp == null) continue;

            foreach (string konu in k.eksikKonular)
            {
                listeProp.InsertArrayElementAtIndex(listeProp.arraySize);
                listeProp.GetArrayElementAtIndex(listeProp.arraySize - 1).stringValue = konu;
                eklenenToplam++;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(k.veri);
            guncellenenAssetSayisi++;
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Tamamlandı",
            $"{eklenenToplam} konu etiketi, {guncellenenAssetSayisi} asset'in 'tanımlı konular' listesine eklendi.",
            "Tamam");

        Tara(); // sonucu tazele
    }
}
