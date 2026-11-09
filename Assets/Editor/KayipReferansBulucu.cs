// KayipReferansBulucu.cs — Editor araci: "'X' is missing the class attribute
// 'ExtensionOfNativeClass'!" gibi hatalarin EN YAYGIN sebebi olan "Missing (Mono Script)"
// kalintilarini (eskiden bir sinif MonoBehaviour/Component'ti, sonra duz siniфa cevrildi ama
// sahne/prefab'taki eski bilesen kaydi SILINMEDI) TUM prefab'larda VE su an ACIK olan sahnede
// tarar, listeler ve (onaylarsan) temizler.
//
// GUVENLIK SINIRI: SADECE su an ACIK olan sahneyi tarar, DIGER sahneleri OTOMATIK ACMAZ. Bu
// projede COK sayida Awake() singleton'i oldugu icin (Instance != null ise Destroy(gameObject))
// birden fazla sahneyi ayni anda yuklemek yan etkiler yaratabilir. Baska bir sahneyi kontrol
// etmek icin onu AC, bu taramayi tekrar calistir.
//
// KULLANIM: Delilik > Denetim > Kayıp Script Bulucu
// Bu dosya Assets/Editor/ klasorune konulmali (Unity otomatik olarak build'e dahil etmez).
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class KayipReferansBulucu : EditorWindow
{
    private class Bulgu
    {
        public GameObject sahneNesnesi; // sadece SAHNEDEKI bulgularda dolu (Ping icin)
        public string konum;
        public int kayipSayisi;
    }

    private List<Bulgu> bulgular = new List<Bulgu>();
    private bool tarandiMi = false;

    [MenuItem("Delilik/Denetim/Kayıp Script Bulucu")]
    public static void PencereyiAc()
    {
        GetWindow<KayipReferansBulucu>("Kayıp Script Bulucu");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Kayıp Script/Referans Bulucu", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "'... is missing the class attribute ExtensionOfNativeClass' gibi hatalar genelde " +
            "bir GameObject/Prefab'ta unutulmuş bir 'Missing Script' bileşeninden gelir — eskiden " +
            "bir sınıf Component'ti, sonra düz sınıfa çevrildi ama sahne/prefab'taki eski bileşen " +
            "kaydı silinmedi.\n\n" +
            "GÜVENLİK: bu araç SADECE şu an AÇIK olan sahneyi ve TÜM prefab'ları tarar. Diğer " +
            "sahneleri otomatik AÇMAZ (bu projede çok sayıda Awake() singleton'ı var, birden " +
            "fazla sahneyi aynı anda yüklemek yan etkiler yaratabilir) — başka bir sahneyi " +
            "kontrol etmek için onu AÇIP taramayı tekrar çalıştır.",
            MessageType.Info);

        EditorGUILayout.Space();
        if (GUILayout.Button("Yeni Özellik Kurulum Rehberi", GUILayout.Height(28)))
            YeniOzellikKurulumYardimcisi.Ac();

        EditorGUILayout.Space();
        if (GUILayout.Button("Tara (Açık Sahne + Tüm Prefablar)", GUILayout.Height(30))) Tara();

        if (!tarandiMi) return;

        EditorGUILayout.Space();

        if (bulgular.Count == 0)
        {
            EditorGUILayout.HelpBox("Açık sahnede ve prefab'larda kayıp script bulunamadı. Hata hâlâ çıkıyorsa, sorun BAŞKA bir (şu an kapalı) sahnede olabilir — o sahneyi açıp taramayı tekrar çalıştır.", MessageType.None);
            return;
        }

        EditorGUILayout.LabelField($"{bulgular.Count} nesnede kayıp script bulundu.", EditorStyles.boldLabel);

        foreach (var b in bulgular)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{b.konum}  ({b.kayipSayisi} kayıp bileşen)", EditorStyles.wordWrappedLabel);
            if (b.sahneNesnesi != null && GUILayout.Button("Göster", GUILayout.Width(60)))
                EditorGUIUtility.PingObject(b.sahneNesnesi);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("TÜMÜNÜ TEMİZLE (kayıp bileşenleri kaldır)", GUILayout.Height(32)))
            Temizle();
    }

    private void Tara()
    {
        bulgular.Clear();

        Scene aktifSahne = SceneManager.GetActiveScene();
        foreach (GameObject kok in aktifSahne.GetRootGameObjects())
            TaraGameObjectAgaci(kok, "Sahne '" + aktifSahne.name + "'", sahneIci: true);

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(yol);
            try
            {
                TaraGameObjectAgaci(root, "Prefab '" + yol + "'", sahneIci: false);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        tarandiMi = true;
    }

    private void TaraGameObjectAgaci(GameObject nesne, string konumOnEki, bool sahneIci)
    {
        int kayip = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(nesne);
        if (kayip > 0)
        {
            bulgular.Add(new Bulgu
            {
                sahneNesnesi = sahneIci ? nesne : null, // prefab icerigi disaridan Ping edilemez
                konum = konumOnEki + $" → '{HiyerarsiYolu(nesne)}'",
                kayipSayisi = kayip
            });
        }

        foreach (Transform cocuk in nesne.transform)
            TaraGameObjectAgaci(cocuk.gameObject, konumOnEki, sahneIci);
    }

    private void Temizle()
    {
        int temizlenenNesne = 0, temizlenenBilesen = 0;

        Scene aktifSahne = SceneManager.GetActiveScene();
        foreach (GameObject kok in aktifSahne.GetRootGameObjects())
            TemizleGameObjectAgaci(kok, ref temizlenenNesne, ref temizlenenBilesen);
        if (temizlenenNesne > 0) EditorSceneManager.MarkSceneDirty(aktifSahne);

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(yol);

            int oncekiNesne = temizlenenNesne;
            TemizleGameObjectAgaci(root, ref temizlenenNesne, ref temizlenenBilesen);

            if (temizlenenNesne > oncekiNesne)
                PrefabUtility.SaveAsPrefabAsset(root, yol);

            PrefabUtility.UnloadPrefabContents(root);
        }

        EditorUtility.DisplayDialog("Tamamlandı",
            $"{temizlenenBilesen} kayıp bileşen, {temizlenenNesne} nesneden kaldırıldı.\n" +
            "Açık sahneyi kaydetmeyi unutma (Ctrl+S) — prefab'lar zaten diske kaydedildi.", "Tamam");

        Tara();
    }

    private void TemizleGameObjectAgaci(GameObject nesne, ref int temizlenenNesne, ref int temizlenenBilesen)
    {
        int kayip = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(nesne);
        if (kayip > 0)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(nesne);
            temizlenenBilesen += kayip;
            temizlenenNesne++;
        }

        foreach (Transform cocuk in nesne.transform)
            TemizleGameObjectAgaci(cocuk.gameObject, ref temizlenenNesne, ref temizlenenBilesen);
    }

    private static string HiyerarsiYolu(GameObject nesne)
    {
        string yol = nesne.name;
        Transform ebeveyn = nesne.transform.parent;
        while (ebeveyn != null)
        {
            yol = ebeveyn.name + "/" + yol;
            ebeveyn = ebeveyn.parent;
        }
        return yol;
    }
}
