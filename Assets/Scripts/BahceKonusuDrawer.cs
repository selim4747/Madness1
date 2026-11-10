// BahceKonusuDrawer.cs — [BahceKonusu] isaretli alanlari (CustomerData.bahceDiyaloglari icindeki
// cumlelerin "konular" listesi dahil) ACILIR LISTE olarak cizer. Liste, BahceKonuVerisi.tanimliKonular'dan gelir.
// Listenin sonundaki "+ Yeni konu ekle..." ile YENI konu tanimlanabilir; tanimliKonular'a eklenir ve secilir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(BahceKonusuAttribute))]
public class BahceKonusuDrawer : PropertyDrawer
{
    private const string SecilmediEtiketi = "(konu seç…)";
    private const string YeniEtiketi = "+ Yeni konu ekle…";
    private static BahceKonuVerisi onbellek;

    internal static BahceKonuVerisi VeriyiBul()
    {
        if (onbellek != null) return onbellek;
        string[] guidler = AssetDatabase.FindAssets("t:BahceKonuVerisi");
        if (guidler.Length == 0) return null;

        // Oyunun gercekten okudugu (Resources icindeki) asset varsa onu tercih et.
        string secilen = guidler[0];
        for (int i = 0; i < guidler.Length; i++)
        {
            if (AssetDatabase.GUIDToAssetPath(guidler[i]).Contains("/Resources/")) { secilen = guidler[i]; break; }
        }
        onbellek = AssetDatabase.LoadAssetAtPath<BahceKonuVerisi>(AssetDatabase.GUIDToAssetPath(secilen));
        return onbellek;
    }

    internal static void KonuyuListeyeEkle(BahceKonuVerisi veri, string konu)
    {
        if (veri == null) return;
        konu = (konu ?? "").Trim();
        if (konu.Length == 0) return;
        if (veri.tanimliKonular == null) veri.tanimliKonular = new List<string>();
        if (veri.tanimliKonular.Contains(konu)) return;

        Undo.RecordObject(veri, "Bahçe konusu ekle: " + konu);
        veri.tanimliKonular.Add(konu);
        EditorUtility.SetDirty(veri);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        BahceKonuVerisi veri = property.propertyType == SerializedPropertyType.String ? VeriyiBul() : null;
        if (veri == null)
        {
            // Asset yoksa eski davranis: duz metin alani (hicbir sey kilitlenmez).
            EditorGUI.PropertyField(position, property, label);
            return;
        }
        if (veri.tanimliKonular == null) veri.tanimliKonular = new List<string>();

        string mevcut = property.stringValue ?? "";
        bool tanimsiz = mevcut.Length > 0 && !veri.tanimliKonular.Contains(mevcut);

        List<string> secenekler = new List<string>();
        secenekler.Add(SecilmediEtiketi);                 // index 0
        secenekler.AddRange(veri.tanimliKonular);         // index 1..n

        int tanimsizIndex = -1;
        if (tanimsiz)
        {
            // Eskiden elle yazilmis ve listede olmayan konu: kaybolmaz, uyari ile gorunur.
            tanimsizIndex = secenekler.Count;
            secenekler.Add("⚠ " + mevcut + " (listede yok — seçersen listeye eklenir)");
        }
        int yeniIndex = secenekler.Count;
        secenekler.Add(YeniEtiketi);

        int suAnki = 0;
        if (tanimsiz) suAnki = tanimsizIndex;
        else if (mevcut.Length > 0) suAnki = veri.tanimliKonular.IndexOf(mevcut) + 1;

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        int secim = EditorGUI.Popup(position, label.text, suAnki, secenekler.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            if (secim == yeniIndex)
                YeniKonuPenceresi.Ac(property.serializedObject.targetObject, property.propertyPath, veri);
            else if (secim == 0)
                property.stringValue = "";
            else if (secim == tanimsizIndex)
                KonuyuListeyeEkle(veri, mevcut);
            else
                property.stringValue = veri.tanimliKonular[secim - 1];
        }
        EditorGUI.EndProperty();
    }
}

public class YeniKonuPenceresi : EditorWindow
{
    [SerializeField] private Object hedef;
    [SerializeField] private string yol;
    [SerializeField] private BahceKonuVerisi veri;
    private string ad = "";
    private bool odaklandi;

    public static void Ac(Object hedef, string yol, BahceKonuVerisi veri)
    {
        YeniKonuPenceresi pencere = CreateInstance<YeniKonuPenceresi>();
        pencere.titleContent = new GUIContent("Yeni Bahçe Konusu");
        pencere.hedef = hedef;
        pencere.yol = yol;
        pencere.veri = veri;
        pencere.minSize = new Vector2(340f, 84f);
        pencere.maxSize = new Vector2(340f, 84f);
        pencere.ShowUtility();
    }

    private void OnGUI()
    {
        GUILayout.Label("Yeni konu adı (boşluklar _ olur, örn: deniz_hikayeleri)", EditorStyles.wordWrappedMiniLabel);
        GUI.SetNextControlName("yeniKonuAdi");
        ad = EditorGUILayout.TextField(ad);
        if (!odaklandi) { EditorGUI.FocusTextInControl("yeniKonuAdi"); odaklandi = true; }

        bool enterBasildi = Event.current.type == EventType.KeyDown &&
            (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("İptal")) { Close(); GUIUtility.ExitGUI(); }
        bool gecerli = !string.IsNullOrWhiteSpace(ad);
        GUI.enabled = gecerli;
        bool ekleBasildi = GUILayout.Button("Ekle ve Seç");
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (gecerli && (ekleBasildi || enterBasildi)) Uygula();
    }

    private void Uygula()
    {
        string temiz = ad.Trim().Replace(' ', '_');
        BahceKonusuDrawer.KonuyuListeyeEkle(veri, temiz);

        if (hedef != null && !string.IsNullOrEmpty(yol))
        {
            SerializedObject so = new SerializedObject(hedef);
            SerializedProperty prop = so.FindProperty(yol);
            if (prop != null)
            {
                prop.stringValue = temiz;
                so.ApplyModifiedProperties();
            }
        }
        Close();
        GUIUtility.ExitGUI();
    }
}
#endif
