// BahceKonuVerisiEditor.cs — BahceKonuVerisi.asset'e Project penceresinde TIKLADIGINDA
// otomatik devreye giren OZEL Inspector. Varsayilan Unity Inspector'da "konular" duz bir
// metin listesiydi (yazim hatasi riski) — burada her cumlenin konulari, tanimliKonular'dan
// DROPDOWN ile eklenir/kaldirilir. Ayri bir arac penceresi ACMANA gerek yok, asset'e
// tikladigin an bu gorunur.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BahceKonuVerisi))]
public class BahceKonuVerisiEditor : Editor
{
    private SerializedProperty tumCumlelerProp;
    private SerializedProperty tanimliKonularProp;
    private Vector2 scroll;

    private void OnEnable()
    {
        tumCumlelerProp = serializedObject.FindProperty("tumCumleler");
        tanimliKonularProp = serializedObject.FindProperty("tanimliKonular");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Tanımlı Konular", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(tanimliKonularProp, true);

        GUILayout.Space(12);
        EditorGUILayout.LabelField("Tüm Cümleler (" + tumCumlelerProp.arraySize + ")", EditorStyles.boldLabel);

        // Konu adlarini diziden diziye (string[]) cikar — Popup bunu ister.
        string[] konuIsimleri = new string[tanimliKonularProp.arraySize];
        for (int i = 0; i < tanimliKonularProp.arraySize; i++)
            konuIsimleri[i] = tanimliKonularProp.GetArrayElementAtIndex(i).stringValue;

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < tumCumlelerProp.arraySize; i++)
        {
            SerializedProperty cumleProp = tumCumlelerProp.GetArrayElementAtIndex(i);
            SerializedProperty idProp = cumleProp.FindPropertyRelative("id");
            SerializedProperty metinProp = cumleProp.FindPropertyRelative("metin");
            SerializedProperty konularProp = cumleProp.FindPropertyRelative("konular");
            SerializedProperty seceneklerProp = cumleProp.FindPropertyRelative("secenekler");

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(idProp, new GUIContent("Id"));
            if (GUILayout.Button("Cümleyi Sil", GUILayout.Width(100)))
            {
                tumCumlelerProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break; // dizi boyutu degisti, bu karede devam etme
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(metinProp, new GUIContent("Metin"));

            // ── KONULAR: dropdown ile ekle, tek tikla kaldir ──
            EditorGUILayout.LabelField("Konular:", EditorStyles.miniBoldLabel);
            for (int k = 0; k < konularProp.arraySize; k++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("• " + konularProp.GetArrayElementAtIndex(k).stringValue);
                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    konularProp.DeleteArrayElementAtIndex(k);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (konuIsimleri.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                int secim = EditorGUILayout.Popup("+ Konu Ekle", -1, konuIsimleri);
                EditorGUILayout.EndHorizontal();

                if (secim >= 0)
                {
                    string secilenKonu = konuIsimleri[secim];
                    bool zatenVar = false;
                    for (int k = 0; k < konularProp.arraySize; k++)
                        if (konularProp.GetArrayElementAtIndex(k).stringValue == secilenKonu) { zatenVar = true; break; }

                    if (!zatenVar)
                    {
                        konularProp.InsertArrayElementAtIndex(konularProp.arraySize);
                        konularProp.GetArrayElementAtIndex(konularProp.arraySize - 1).stringValue = secilenKonu;
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Önce yukarıdan 'Tanımlı Konular' listesine birkaç konu ekle.", MessageType.Info);
            }

            // ── SEÇENEKLER: varsayilan Unity listesi (id/metin gibi basit degil, ayrica
            // ozellestirmeye gerek yok — burada sadece acilir/kapanir bir liste olarak) ──
            EditorGUILayout.PropertyField(seceneklerProp, new GUIContent("Seçenekler"), true);

            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(8);
        if (GUILayout.Button("+ Yeni Boş Cümle Ekle", GUILayout.Height(28)))
        {
            tumCumlelerProp.InsertArrayElementAtIndex(tumCumlelerProp.arraySize);
            SerializedProperty yeni = tumCumlelerProp.GetArrayElementAtIndex(tumCumlelerProp.arraySize - 1);
            yeni.FindPropertyRelative("id").stringValue = "yeni_" + tumCumlelerProp.arraySize;
            yeni.FindPropertyRelative("metin").stringValue = "";
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif