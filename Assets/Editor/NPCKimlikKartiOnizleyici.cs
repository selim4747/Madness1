// NPCKimlikKartiOnizleyici.cs — Editor araci: CustomerData.karakterHavuzu listesindeki her
// NPCKimligi elemanini, 20+ alanlik duz Inspector listesi yerine KUCUK BIR KART olarak
// gosterir (portre + isim + cinsiyet/yas rozeti + sever/sevmez turler + bayraklar). Karti
// ACMADAN karaktere bir bakista "kim bu" anlarsin; altindaki "Detaylar" foldout'unu acinca
// TUM alanlar normal sekilde (tamamen duzenlenebilir) gorunur — hicbir alan KAYBOLMAZ,
// sadece varsayilan gorunumun ONUNE bir ozet eklenir.
//
// Bu dosya Assets/Editor/ klasorune konulmali (Unity otomatik olarak build'e dahil etmez).
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(NPCKimligi))]
public class NPCKimlikKartiOnizleyici : PropertyDrawer
{
    private const float KART_YUKSEKLIK = 58f;
    private const float PORTRE_BOYUT = 48f;
    private const float FOLDOUT_YUKSEKLIK = 18f;

    // Foldout (Detaylar acik/kapali) durumu, PropertyDrawer ornekleri Unity tarafindan
    // PAYLASILABILDIGI icin static bir sozlukte, her property'nin kendi "yolu" ile tutulur.
    private static readonly Dictionary<string, bool> genisletmeDurumu = new Dictionary<string, bool>();

    // Bu alanlar karti basliginda ZATEN gosterildigi icin, "Detaylar" acildiginda TEKRAR
    // cizilmez (gereksiz tekrar olmasin diye) — istersen bu HashSet'i bosaltip hepsini
    // tekrar gorebilirsin.
    private static readonly HashSet<string> basliktaGosterilenAlanlar = new HashSet<string> { "musteriIsmi", "tisortSprite", "cinsiyet", "yas" };

    private static string AnahtarUret(SerializedProperty property)
    {
        int id = property.serializedObject.targetObject != null ? property.serializedObject.targetObject.GetInstanceID() : 0;
        return id + "::" + property.propertyPath;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        string anahtar = AnahtarUret(property);
        bool genisletilmis = genisletmeDurumu.TryGetValue(anahtar, out bool v) && v;

        Rect kartRect = new Rect(position.x, position.y, position.width, KART_YUKSEKLIK);
        GUI.Box(kartRect, GUIContent.none);

        SerializedProperty ismiProp = property.FindPropertyRelative("musteriIsmi");
        SerializedProperty cinsiyetProp = property.FindPropertyRelative("cinsiyet");
        SerializedProperty yasProp = property.FindPropertyRelative("yas");
        SerializedProperty portreProp = property.FindPropertyRelative("tisortSprite");
        SerializedProperty sevilenProp = property.FindPropertyRelative("sevilenTurler");
        SerializedProperty sevmeyenProp = property.FindPropertyRelative("sevmedigiTurler");
        SerializedProperty sosyalProp = property.FindPropertyRelative("sosyal");
        SerializedProperty ozelGunProp = property.FindPropertyRelative("ozelGunKarakteri");
        SerializedProperty cokluKisilikProp = property.FindPropertyRelative("cokluKisilikliMi");

        // --- Portre ---
        Rect portreRect = new Rect(kartRect.x + 4, kartRect.y + 5, PORTRE_BOYUT, PORTRE_BOYUT);
        Sprite portre = portreProp != null ? portreProp.objectReferenceValue as Sprite : null;
        if (portre != null)
        {
            Texture2D onizleme = AssetPreview.GetAssetPreview(portre);
            if (onizleme == null) onizleme = AssetPreview.GetMiniThumbnail(portre);
            if (onizleme != null) GUI.DrawTexture(portreRect, onizleme, ScaleMode.ScaleToFit);
            else EditorGUI.LabelField(portreRect, "?");
        }
        else
        {
            GUI.Box(portreRect, "—");
        }

        float metinX = portreRect.xMax + 8;
        float metinGenislik = Mathf.Max(40, kartRect.width - (metinX - kartRect.x) - 96);

        // --- Isim ---
        string isim = ismiProp != null ? ismiProp.stringValue : "";
        Rect isimRect = new Rect(metinX, kartRect.y + 4, metinGenislik, 18);
        EditorGUI.LabelField(isimRect, string.IsNullOrEmpty(isim) ? "(isimsiz)" : isim, EditorStyles.boldLabel);

        // --- Cinsiyet + Yas rozeti ---
        Cinsiyet cinsiyet = cinsiyetProp != null ? (Cinsiyet)cinsiyetProp.enumValueIndex : Cinsiyet.Erkek;
        int yas = yasProp != null ? yasProp.intValue : 30;
        YasGrubu yasGrubu = YasGrubuHesapla(yas);

        Rect rozetRect = new Rect(metinX, kartRect.y + 22, metinGenislik, 16);
        EditorGUI.LabelField(rozetRect, $"{cinsiyet} • {yasGrubu} ({yas} yaş)", EditorStyles.miniLabel);

        // --- Bayraklar ---
        string bayraklar = "";
        if (sosyalProp != null && sosyalProp.boolValue) bayraklar += "🌳 Bahçe  ";
        if (ozelGunProp != null && ozelGunProp.boolValue) bayraklar += "📅 Özel Gün  ";
        if (cokluKisilikProp != null && cokluKisilikProp.boolValue) bayraklar += "🎭 Çoklu Kişilik";

        Rect bayrakRect = new Rect(metinX, kartRect.y + 40, metinGenislik, 16);
        EditorGUI.LabelField(bayrakRect, bayraklar, EditorStyles.miniLabel);

        // --- Sever / Sevmez (sag ust kose) ---
        string sever = TurleriListele(sevilenProp);
        string sevmez = TurleriListele(sevmeyenProp);

        Rect turlerRect = new Rect(kartRect.xMax - 92, kartRect.y + 4, 88, KART_YUKSEKLIK - 8);
        GUIStyle kucukSaga = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { alignment = TextAnchor.UpperRight };
        EditorGUI.LabelField(turlerRect, $"Sever: {sever}\nSevmez: {sevmez}", kucukSaga);

        // --- Detaylar foldout ---
        Rect foldoutRect = new Rect(kartRect.x, kartRect.yMax + 2, position.width, FOLDOUT_YUKSEKLIK);
        bool yeniGenisletilmis = EditorGUI.Foldout(foldoutRect, genisletilmis, "Detaylar (tüm alanlar)", true);
        if (yeniGenisletilmis != genisletilmis) genisletmeDurumu[anahtar] = yeniGenisletilmis;

        if (yeniGenisletilmis)
        {
            float y = foldoutRect.yMax + 2;

            SerializedProperty cocuk = property.Copy();
            SerializedProperty son = property.GetEndProperty();
            bool ilkAlan = true;

            while (cocuk.NextVisible(ilkAlan) && !SerializedProperty.EqualContents(cocuk, son))
            {
                ilkAlan = false;
                if (basliktaGosterilenAlanlar.Contains(cocuk.name)) continue;

                float alanYuksekligi = EditorGUI.GetPropertyHeight(cocuk, true);
                Rect alanRect = new Rect(position.x, y, position.width, alanYuksekligi);
                EditorGUI.PropertyField(alanRect, cocuk, true);
                y += alanYuksekligi + 2;
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        string anahtar = AnahtarUret(property);
        bool genisletilmis = genisletmeDurumu.TryGetValue(anahtar, out bool v) && v;

        float yukseklik = KART_YUKSEKLIK + FOLDOUT_YUKSEKLIK + 2;

        if (genisletilmis)
        {
            SerializedProperty cocuk = property.Copy();
            SerializedProperty son = property.GetEndProperty();
            bool ilkAlan = true;

            while (cocuk.NextVisible(ilkAlan) && !SerializedProperty.EqualContents(cocuk, son))
            {
                ilkAlan = false;
                if (basliktaGosterilenAlanlar.Contains(cocuk.name)) continue;
                yukseklik += EditorGUI.GetPropertyHeight(cocuk, true) + 2;
            }
        }

        return yukseklik;
    }

    /// <summary>NPCKimligi.GetYasGrubu() ile AYNI esikler kullanilir — o metod degisirse
    /// burasi da SENKRON guncellenmeli (bu sadece GORUNTULEME icin ayri bir kopyadir,
    /// gercek oyun mantigi hep NPCKimligi.GetYasGrubu()'nu kullanir).</summary>
    private static YasGrubu YasGrubuHesapla(int yas)
    {
        if (yas <= 15) return YasGrubu.Cocuk;
        if (yas <= 20) return YasGrubu.Genc;
        if (yas <= 30) return YasGrubu.GencYetiskin;
        if (yas <= 45) return YasGrubu.Yetiskin;
        if (yas <= 60) return YasGrubu.OrtaYas;
        return YasGrubu.Yasli;
    }

    private static string TurleriListele(SerializedProperty listeProp)
    {
        if (listeProp == null || !listeProp.isArray || listeProp.arraySize == 0) return "-";

        var adlar = new List<string>();
        int sinir = Mathf.Min(listeProp.arraySize, 3);
        for (int i = 0; i < sinir; i++)
        {
            SerializedProperty eleman = listeProp.GetArrayElementAtIndex(i);
            string ad = (eleman.enumDisplayNames != null && eleman.enumValueIndex >= 0 && eleman.enumValueIndex < eleman.enumDisplayNames.Length)
                ? eleman.enumDisplayNames[eleman.enumValueIndex]
                : eleman.enumValueIndex.ToString();
            adlar.Add(ad);
        }

        string sonuc = string.Join(", ", adlar);
        if (listeProp.arraySize > sinir) sonuc += $" +{listeProp.arraySize - sinir}";
        return sonuc;
    }
}
