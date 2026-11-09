// KayipReferansBulucu.cs
// 1) Missing Script temizler
// 2) Tüm iç içe / liste Object referanslarındaki null ve MISSING'leri listeler
// Prefab bulgularında "Göster" prefabı seçer.
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class KayipReferansBulucu : EditorWindow
{
    private class ScriptBulgu
    {
        public Object hedef;
        public string konum;
        public int kayipSayisi;
    }

    private class NullBulgu
    {
        public Object hedef;
        public string konum;
        public string bilesen;
        public string alan;
        public bool missing;
    }

    private List<ScriptBulgu> scriptBulgulari = new List<ScriptBulgu>();
    private List<NullBulgu> nullBulgulari = new List<NullBulgu>();
    private bool tarandiMi;
    private Vector2 scroll;
    private bool sadeceMissing;

    [MenuItem("Delilik/Denetim/Kayıp Script Bulucu")]
    public static void PencereyiAc() => GetWindow<KayipReferansBulucu>("Kayıp Referans Bulucu");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Kayıp Script + Null Referans Bulucu", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Missing Script + tüm Object referansları (iç içe class, List, array dahil).\n" +
            "MISSING = bağlantı kopmuş. null = alan boş (bilerek de olabilir).",
            MessageType.Info);

        if (GUILayout.Button("Yeni Özellik Kurulum Rehberi", GUILayout.Height(24)))
            YeniOzellikKurulumYardimcisi.Ac();

        if (GUILayout.Button("Tara", GUILayout.Height(30))) Tara();
        if (!tarandiMi) return;

        sadeceMissing = EditorGUILayout.Toggle("Sadece MISSING göster (boş null'ları gizle)", sadeceMissing);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Missing Script: " + scriptBulgulari.Count, EditorStyles.boldLabel);
        foreach (var b in scriptBulgulari)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(b.konum + " (" + b.kayipSayisi + ")");
            if (b.hedef != null && GUILayout.Button("Göster", GUILayout.Width(60)))
                Ping(b.hedef);
            EditorGUILayout.EndHorizontal();
        }
        if (scriptBulgulari.Count > 0 && GUILayout.Button("Kayıp Scriptleri Temizle"))
            Temizle();

        EditorGUILayout.Space(6);
        int gosterilen = 0;
        EditorGUILayout.LabelField("Null / Missing Referans", EditorStyles.boldLabel);
        foreach (var n in nullBulgulari)
        {
            if (sadeceMissing && !n.missing) continue;
            gosterilen++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(n.konum + " → " + n.bilesen + "." + n.alan);
            if (n.hedef != null && GUILayout.Button("Göster", GUILayout.Width(60)))
                Ping(n.hedef);
            EditorGUILayout.EndHorizontal();
        }
        if (gosterilen == 0)
            EditorGUILayout.HelpBox(sadeceMissing ? "MISSING referans yok." : "Null referans yok.", MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private static void Ping(Object o)
    {
        Selection.activeObject = o;
        EditorGUIUtility.PingObject(o);
    }

    private void Tara()
    {
        scriptBulgulari.Clear();
        nullBulgulari.Clear();

        Scene sahne = SceneManager.GetActiveScene();
        foreach (GameObject kok in sahne.GetRootGameObjects())
            TaraObje(kok, "Sahne/" + sahne.name, kok);

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(yol);
            try { TaraObje(root, "Prefab/" + yol, AssetDatabase.LoadAssetAtPath<Object>(yol)); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        tarandiMi = true;
    }

    private void TaraObje(GameObject nesne, string konum, Object pingHedef)
    {
        int kayip = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(nesne);
        if (kayip > 0)
            scriptBulgulari.Add(new ScriptBulgu { hedef = pingHedef, konum = konum + "/" + Hiyerarsi(nesne), kayipSayisi = kayip });

        foreach (var b in nesne.GetComponents<MonoBehaviour>())
        {
            if (b == null) continue;
            var so = new SerializedObject(b);
            var prop = so.GetIterator();
            // Tüm görünür property'leri (iç içe dahil) dolaş
            while (prop.NextVisible(true))
            {
                if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (prop.name == "m_Script") continue;

                bool degerNull = prop.objectReferenceValue == null;
                bool missing = degerNull && prop.objectReferenceInstanceIDValue != 0;
                if (!degerNull) continue;

                nullBulgulari.Add(new NullBulgu
                {
                    hedef = pingHedef,
                    konum = konum + "/" + Hiyerarsi(nesne),
                    bilesen = b.GetType().Name,
                    alan = prop.propertyPath + (missing ? " [MISSING]" : " [null]"),
                    missing = missing
                });
            }
        }

        foreach (Transform c in nesne.transform)
            TaraObje(c.gameObject, konum, pingHedef);
    }

    private void Temizle()
    {
        int n = 0, b = 0;
        Scene sahne = SceneManager.GetActiveScene();
        foreach (GameObject kok in sahne.GetRootGameObjects())
            TemizleAgac(kok, ref n, ref b);
        if (n > 0) EditorSceneManager.MarkSceneDirty(sahne);

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string yol = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(yol);
            int once = n;
            TemizleAgac(root, ref n, ref b);
            if (n > once) PrefabUtility.SaveAsPrefabAsset(root, yol);
            PrefabUtility.UnloadPrefabContents(root);
        }
        EditorUtility.DisplayDialog("Tamam", b + " script temizlendi.", "OK");
        Tara();
    }

    private void TemizleAgac(GameObject o, ref int nesne, ref int bilesen)
    {
        int k = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(o);
        if (k > 0)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(o);
            bilesen += k; nesne++;
        }
        foreach (Transform c in o.transform) TemizleAgac(c.gameObject, ref nesne, ref bilesen);
    }

    private static string Hiyerarsi(GameObject o)
    {
        string s = o.name;
        var p = o.transform.parent;
        while (p != null) { s = p.name + "/" + s; p = p.parent; }
        return s;
    }
}
