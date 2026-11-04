// OtomatikBlablaDoldurucu.cs — Editor araci: bir klasordeki "blabla" (mirildanma) ses
// dosyalarini dosya ADINDAN cinsiyet+yas grubunu OTOMATIK cikarip, SeslendirmeYoneticisi'nin
// blablaHavuzlari listesine TEK TIKLA doldurur. ONEMLI: bu BIR ONAY DIYALOGU ZINCIRI DEGIL —
// "bunu mu istedin?" diye sormaz, TARA -> DOLDUR akisiyla dogrudan atar (istege gore
// "Mevcutlari koru" acikken var olan klipleri SILMEZ, sadece eksik olanlari EKLER).
//
// DOSYA ADI KURALI: dosya adinda (buyuk/kucuk harf onemsiz) HEM bir cinsiyet etiketi
// ("erkek" / "kadin") HEM bir yas grubu etiketi ("cocuk","genc","gencyetiskin","yetiskin",
// "ortayas","yasli") gecmesi gerekir. Orn: "blabla_erkek_yetiskin_03.wav",
// "kadin_genc-01.mp3". Ikisinden biri eksikse dosya "Eslestirilemedi" listesine duser,
// ATLANMAZ ama otomatik atanmaz — sen manuel bakarsin.
//
// KULLANIM: Delilik > Seslendirme > Otomatik Blabla Doldurucu
// Bu dosya Assets/Editor/ klasorune konulmali (Unity otomatik olarak build'e dahil etmez).
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class OtomatikBlablaDoldurucu : EditorWindow
{
    private SeslendirmeYoneticisi hedefYonetici;
    private string klipKlasoru = "Assets/Audio/Blabla";
    private bool mevcutlariKoru = true;

    // Tarama sonucu (Tara butonuna basilinca doldurulur, Doldur butonu bunu kullanir).
    private Dictionary<(Cinsiyet, YasGrubu), List<AudioClip>> taramaSonucu;
    private List<string> eslestirilemeyenler;
    private bool tarandiMi = false;

    private static readonly (string etiket, YasGrubu grup)[] YasEtiketleri =
    {
        ("gencyetiskin", YasGrubu.GencYetiskin),
        ("genc_yetiskin", YasGrubu.GencYetiskin),
        ("ortayas", YasGrubu.OrtaYas),
        ("orta_yas", YasGrubu.OrtaYas),
        ("yetiskin", YasGrubu.Yetiskin),
        ("yasli", YasGrubu.Yasli),
        ("genc", YasGrubu.Genc),
        ("cocuk", YasGrubu.Cocuk),
    };

    private static readonly (string etiket, Cinsiyet degeri)[] CinsiyetEtiketleri =
    {
        ("kadin", Cinsiyet.Kadin),
        ("erkek", Cinsiyet.Erkek),
    };

    [MenuItem("Delilik/Seslendirme/Otomatik Blabla Doldurucu")]
    public static void PencereyiAc()
    {
        GetWindow<OtomatikBlablaDoldurucu>("Blabla Doldurucu");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Klasordeki ses dosyalarini adlarina gore (cinsiyet+yas etiketi) otomatik gruplar ve " +
            "SeslendirmeYoneticisi.blablaHavuzlari listesine TEK TIKLA yazar. Onay sormaz — 'Tara' ile " +
            "sonucu gor, 'Doldur' ile uygula.", MessageType.Info);

        EditorGUILayout.Space();

        hedefYonetici = (SeslendirmeYoneticisi)EditorGUILayout.ObjectField(
            "Hedef SeslendirmeYoneticisi", hedefYonetici, typeof(SeslendirmeYoneticisi), true);

        if (GUILayout.Button("Sahnede Bul"))
        {
            hedefYonetici = FindFirstObjectByType<SeslendirmeYoneticisi>();
            if (hedefYonetici == null)
                EditorUtility.DisplayDialog("Bulunamadi", "Sahnede SeslendirmeYoneticisi bulunamadi.", "Tamam");
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        klipKlasoru = EditorGUILayout.TextField("Klip Klasoru", klipKlasoru);
        if (GUILayout.Button("Klasor Sec", GUILayout.Width(90)))
        {
            string secilen = EditorUtility.OpenFolderPanel("Blabla Ses Klasorunu Sec", "Assets", "");
            if (!string.IsNullOrEmpty(secilen))
            {
                if (secilen.StartsWith(Application.dataPath))
                    klipKlasoru = "Assets" + secilen.Substring(Application.dataPath.Length);
                else
                    EditorUtility.DisplayDialog("Gecersiz Klasor", "Klasor bu projenin Assets klasoru icinde olmali.", "Tamam");
            }
        }
        EditorGUILayout.EndHorizontal();

        mevcutlariKoru = EditorGUILayout.ToggleLeft(
            "Mevcut klipleri koru (sadece eksik olanlari ekle — kapatirsan tam DEGISTIRME yapar)",
            mevcutlariKoru);

        EditorGUILayout.Space();

        if (GUILayout.Button("Tara", GUILayout.Height(28)))
        {
            KlasoruTara();
        }

        if (!tarandiMi) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tarama Sonucu", EditorStyles.boldLabel);

        if (taramaSonucu.Count == 0)
        {
            EditorGUILayout.HelpBox("Eslesen klip bulunamadi.", MessageType.Warning);
        }
        else
        {
            foreach (var kv in taramaSonucu.OrderBy(k => k.Key.Item2).ThenBy(k => k.Key.Item1))
            {
                EditorGUILayout.LabelField($"  {kv.Key.Item2} + {kv.Key.Item1} : {kv.Value.Count} klip");
            }
        }

        if (eslestirilemeyenler != null && eslestirilemeyenler.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Eslestirilemedi ({eslestirilemeyenler.Count}) — dosya adinda cinsiyet/yas etiketi bulunamadi:", EditorStyles.boldLabel);
            foreach (string s in eslestirilemeyenler)
                EditorGUILayout.LabelField("  ⚠ " + s);
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(hedefYonetici == null || taramaSonucu.Count == 0))
        {
            if (GUILayout.Button("DOLDUR (onay sorulmaz)", GUILayout.Height(32)))
            {
                Doldur();
            }
        }
    }

    private void KlasoruTara()
    {
        taramaSonucu = new Dictionary<(Cinsiyet, YasGrubu), List<AudioClip>>();
        eslestirilemeyenler = new List<string>();
        tarandiMi = true;

        if (!AssetDatabase.IsValidFolder(klipKlasoru))
        {
            EditorUtility.DisplayDialog("Klasor Bulunamadi", $"'{klipKlasoru}' gecerli bir Assets klasoru degil.", "Tamam");
            tarandiMi = false;
            return;
        }

        string[] guidler = AssetDatabase.FindAssets("t:AudioClip", new[] { klipKlasoru });
        foreach (string guid in guidler)
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip klip = AssetDatabase.LoadAssetAtPath<AudioClip>(yol);
            if (klip == null) continue;

            string adKucuk = Path.GetFileNameWithoutExtension(yol).ToLowerInvariant();

            Cinsiyet? cinsiyet = null;
            foreach (var e in CinsiyetEtiketleri)
                if (adKucuk.Contains(e.etiket)) { cinsiyet = e.degeri; break; }

            YasGrubu? yasGrubu = null;
            foreach (var e in YasEtiketleri)
                if (adKucuk.Contains(e.etiket)) { yasGrubu = e.grup; break; }

            if (cinsiyet.HasValue && yasGrubu.HasValue)
            {
                var anahtar = (cinsiyet.Value, yasGrubu.Value);
                if (!taramaSonucu.ContainsKey(anahtar)) taramaSonucu[anahtar] = new List<AudioClip>();
                taramaSonucu[anahtar].Add(klip);
            }
            else
            {
                eslestirilemeyenler.Add(Path.GetFileName(yol));
            }
        }
    }

    private void Doldur()
    {
        if (hedefYonetici == null || taramaSonucu == null || taramaSonucu.Count == 0) return;

        SerializedObject so = new SerializedObject(hedefYonetici);
        SerializedProperty liste = so.FindProperty("blablaHavuzlari");
        if (liste == null)
        {
            EditorUtility.DisplayDialog("Hata", "blablaHavuzlari alani bulunamadi — SeslendirmeYoneticisi.cs degismis olabilir.", "Tamam");
            return;
        }

        int eklenenHavuz = 0, guncellenenHavuz = 0, toplamYeniKlip = 0;

        foreach (var kv in taramaSonucu)
        {
            Cinsiyet cinsiyet = kv.Key.Item1;
            YasGrubu yasGrubu = kv.Key.Item2;
            List<AudioClip> yeniKlipler = kv.Value;

            int bulunanIndex = -1;
            for (int i = 0; i < liste.arraySize; i++)
            {
                SerializedProperty eleman = liste.GetArrayElementAtIndex(i);
                Cinsiyet c = (Cinsiyet)eleman.FindPropertyRelative("cinsiyet").enumValueIndex;
                YasGrubu y = (YasGrubu)eleman.FindPropertyRelative("yasGrubu").enumValueIndex;
                if (c == cinsiyet && y == yasGrubu) { bulunanIndex = i; break; }
            }

            SerializedProperty hedefEleman;
            if (bulunanIndex >= 0)
            {
                hedefEleman = liste.GetArrayElementAtIndex(bulunanIndex);
                guncellenenHavuz++;
            }
            else
            {
                liste.InsertArrayElementAtIndex(liste.arraySize);
                hedefEleman = liste.GetArrayElementAtIndex(liste.arraySize - 1);
                hedefEleman.FindPropertyRelative("cinsiyet").enumValueIndex = (int)cinsiyet;
                hedefEleman.FindPropertyRelative("yasGrubu").enumValueIndex = (int)yasGrubu;
                hedefEleman.FindPropertyRelative("sesler").arraySize = 0;
                eklenenHavuz++;
            }

            SerializedProperty seslerProp = hedefEleman.FindPropertyRelative("sesler");
            List<AudioClip> sonuncListe = new List<AudioClip>();

            if (mevcutlariKoru)
            {
                for (int i = 0; i < seslerProp.arraySize; i++)
                    sonuncListe.Add(seslerProp.GetArrayElementAtIndex(i).objectReferenceValue as AudioClip);
            }

            int oncekiSayi = mevcutlariKoru ? sonuncListe.Count : 0;
            foreach (AudioClip k in yeniKlipler)
                if (!sonuncListe.Contains(k)) sonuncListe.Add(k);

            // NOT: "replace" modunda (mevcutlariKoru=false) sonuncListe zaten bos baslar, o
            // yuzden eklenen tum klipler "yeni" sayilir — sayaç sadece bilgilendirme amaçli.
            toplamYeniKlip += sonuncListe.Count - oncekiSayi;

            seslerProp.arraySize = sonuncListe.Count;
            for (int i = 0; i < sonuncListe.Count; i++)
                seslerProp.GetArrayElementAtIndex(i).objectReferenceValue = sonuncListe[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(hedefYonetici);
        if (hedefYonetici.gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hedefYonetici.gameObject.scene);

        EditorUtility.DisplayDialog("Tamamlandi",
            $"{eklenenHavuz} yeni havuz eklendi, {guncellenenHavuz} mevcut havuz guncellendi, " +
            $"{toplamYeniKlip} yeni klip yazildi.\nSahneyi kaydetmeyi unutma (Ctrl+S).", "Tamam");
    }
}
