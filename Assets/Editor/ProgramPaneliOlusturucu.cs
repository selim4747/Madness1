// ProgramPaneliOlusturucu.cs — Isim gir, tek tikla yeni bir "program" (Katalog/Basarimlar
// tarzi) kurar: {Isim}Panel, {Isim}Button, {Isim}ButtonText, {Isim}CloseButton.
// Otomatik olarak KasaEtkilesim.programlar[] listesine eklenir ve butonlar baglanir.
// Menu: Delilik > Program Paneli Olustur
// Bu script SADECE Unity Editor'de calisir, oyuna dahil edilmez (Assets/Editor klasoru).
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class ProgramPaneliOlusturucu : EditorWindow
{
    private string programAdi = "";
    private RectTransform ikonKonteyneri;   // Ikon+yazi butonlarinin dizildigi yer (orn. pcAnaEkran'in ikon alani)
    private RectTransform panelKonteyneri;  // Icerik panellerinin durdugu yer (orn. Canvas'in kendisi)
    private KasaEtkilesim kasaKontrol;

    [MenuItem("Delilik/Program Paneli Olustur")]
    public static void PenceresiniAc()
    {
        GetWindow<ProgramPaneliOlusturucu>("Program Paneli Olustur");
    }

    private void OnEnable()
    {
        // Acilista otomatik bulmayi dene — bulamazsa elle surukleyebilirsin.
        if (kasaKontrol == null) kasaKontrol = FindFirstObjectByType<KasaEtkilesim>();
    }

    private void OnGUI()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("1) Konteynerler (bir kere ayarla, hatirlar)", EditorStyles.boldLabel);
        ikonKonteyneri = (RectTransform)EditorGUILayout.ObjectField(
            new GUIContent("Ikon Konteyneri", "Katalog/Basarimlar/Oyun ikon butonlarinin dizildigi obje (orn. pcAnaEkran icindeki ikon listesi)."),
            ikonKonteyneri, typeof(RectTransform), true);

        panelKonteyneri = (RectTransform)EditorGUILayout.ObjectField(
            new GUIContent("Panel Konteyneri", "Icerik panellerinin (Katalog paneli gibi) durdugu obje (genelde Canvas'in kendisi)."),
            panelKonteyneri, typeof(RectTransform), true);

        kasaKontrol = (KasaEtkilesim)EditorGUILayout.ObjectField(
            new GUIContent("KasaEtkilesim", "Yeni panel otomatik olarak bunun programlar[] listesine eklenir."),
            kasaKontrol, typeof(KasaEtkilesim), true);

        GUILayout.Space(16);
        EditorGUILayout.LabelField("2) Yeni Programin Ismi", EditorStyles.boldLabel);
        programAdi = EditorGUILayout.TextField("Isim (orn. Selim):", programAdi);

        GUILayout.Space(16);

        GUI.enabled = !string.IsNullOrWhiteSpace(programAdi) && ikonKonteyneri != null && panelKonteyneri != null;
        if (GUILayout.Button("Olustur", GUILayout.Height(36)))
        {
            Olustur(programAdi.Trim());
        }
        GUI.enabled = true;

        if (ikonKonteyneri == null || panelKonteyneri == null)
            EditorGUILayout.HelpBox("Once iki konteyneri de surukle.", MessageType.Info);
    }

    private void Olustur(string isim)
    {
        Undo.SetCurrentGroupName("Program Paneli Olustur: " + isim);
        int undoGrup = Undo.GetCurrentGroup();

        // ── 1) BUTON (ikon) ──
        GameObject buttonGO = YeniUIObjesi(isim + "Button", ikonKonteyneri, typeof(Image), typeof(Button));
        RectTransform buttonRT = buttonGO.GetComponent<RectTransform>();
        buttonRT.sizeDelta = new Vector2(75, 75);

        // Ikon konteynerindeki EN ALTTAKI mevcut butonun hemen altina otomatik yerlesir
        // (Katalog/Basarimlar/Oyun gibi dikey siraya kendiliginden eklenir).
        float enDusukY = float.MaxValue;
        bool herhangiBirCocukVarMi = false;
        for (int i = 0; i < ikonKonteyneri.childCount; i++)
        {
            RectTransform cocuk = ikonKonteyneri.GetChild(i) as RectTransform;
            if (cocuk == null || cocuk == buttonRT) continue;
            herhangiBirCocukVarMi = true;
            if (cocuk.anchoredPosition.y < enDusukY) enDusukY = cocuk.anchoredPosition.y;
        }
        float yeniY = herhangiBirCocukVarMi ? (enDusukY - 80f) : 220f; // 220 = ilk ikonun tipik baslangic Y'si
        buttonRT.anchoredPosition = new Vector2(-420f, yeniY);

        Image buttonImg = buttonGO.GetComponent<Image>();
        buttonImg.color = new Color(0.78f, 0.78f, 0.78f); // diger ikon butonlariyla ayni notr gri

        // ── 2) BUTTON TEXT (butonun cocugu, altinda yazi) ──
        GameObject textGO = YeniUIObjesi(isim + "ButtonText", buttonRT, typeof(TextMeshProUGUI));
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.sizeDelta = new Vector2(120, 75);
        textRT.anchoredPosition = new Vector2(0, -60);

        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = isim;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        // ── 3) PANEL (icerik penceresi) ──
        GameObject panelGO = YeniUIObjesi(isim + "Panel", panelKonteyneri, typeof(Image));
        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(475, 475);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelImg = panelGO.GetComponent<Image>();
        panelImg.color = new Color(0.25f, 0.35f, 0.85f, 0.9f); // katalog panelindeki gibi mavi

        panelGO.SetActive(false); // diger programlar gibi varsayilan KAPALI baslar

        // ── 4) CLOSE BUTTON (panelin cocugu, sag ust "X") ──
        GameObject closeGO = YeniUIObjesi(isim + "CloseButton", panelRT, typeof(Image), typeof(Button));
        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.sizeDelta = new Vector2(40, 40);
        closeRT.anchoredPosition = new Vector2(-10, -10);

        GameObject closeTextGO = YeniUIObjesi(isim + "CloseButtonText", closeRT, typeof(TextMeshProUGUI));
        RectTransform closeTextRT = closeTextGO.GetComponent<RectTransform>();
        closeTextRT.anchorMin = Vector2.zero;
        closeTextRT.anchorMax = Vector2.one;
        closeTextRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI closeTmp = closeTextGO.GetComponent<TextMeshProUGUI>();
        closeTmp.text = "X";
        closeTmp.fontSize = 20;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.color = Color.black;

        // ── 5) KasaEtkilesim.programlar[] listesine ekle + butonlari bagla ──
        if (kasaKontrol != null)
        {
            SerializedObject so = new SerializedObject(kasaKontrol);
            SerializedProperty programlarProp = so.FindProperty("programlar");
            int yeniIndex = programlarProp.arraySize;
            programlarProp.arraySize++;
            programlarProp.GetArrayElementAtIndex(yeniIndex).objectReferenceValue = panelGO;
            so.ApplyModifiedProperties();

            Button btn = buttonGO.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
                btn.onClick, kasaKontrol.ProgramAc, yeniIndex);

            Button closeBtn = closeGO.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                closeBtn.onClick, kasaKontrol.GeriDon);
        }
        else
        {
            Debug.LogWarning("[ProgramPaneliOlusturucu] KasaEtkilesim atanmadigi icin '" + isim +
                              "' programlar[] listesine EKLENMEDI ve butonlar baglanmadi — elle yapman gerekecek.");
        }

        Undo.CollapseUndoOperations(undoGrup);
        Selection.activeGameObject = panelGO;

        EditorUtility.DisplayDialog("Olusturuldu",
            isim + "Panel, " + isim + "Button, " + isim + "ButtonText, " + isim + "CloseButton olusturuldu.\n\n" +
            (kasaKontrol != null ? "KasaEtkilesim.programlar[] listesine eklendi, butonlar baglandi." :
             "KasaEtkilesim atanmadigi icin butonlar baglanmadi — elle yap."),
            "Tamam");
    }

    private static GameObject YeniUIObjesi(string isim, Transform ebeveyn, params System.Type[] ekBilesenler)
    {
        System.Type[] hepsi = new System.Type[ekBilesenler.Length + 1];
        hepsi[0] = typeof(RectTransform);
        ekBilesenler.CopyTo(hepsi, 1);

        GameObject go = ObjectFactory.CreateGameObject(isim, hepsi);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(ebeveyn, false);
        rt.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(go, "Olustur " + isim);
        return go;
    }
}
#endif