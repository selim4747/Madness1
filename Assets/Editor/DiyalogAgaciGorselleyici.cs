// DiyalogAgaciGorselleyici.cs — Editor araci: OzelMusteriHikayesiVerisi (hikaye konugu
// gunluk segmentleri) VEYA BahceKonuVerisi (bahce konu agi) asset'lerini kutu+ok seklinde
// GORSEL bir agac/graf olarak cizer. Inspector'da liste liste acip "hangi secenek nereye
// gidiyor" takip etmek yerine, tum dallanmayi TEK BAKISTA gormek icindir.
//
// KULLANIM: Delilik > Diyalog > Agac Gorselleyici. Ust kisimdaki alana bir
// OzelMusteriHikayesiVerisi ya da BahceKonuVerisi asset'i surukle. Bos alana tiklayip
// SURUKLEYEREK gorunumu kaydirabilirsin, bir DUGUME tiklayinca sag panelde detaylari gorursun.
// Zoom YOK (bilerek) — cok fazla dugum varsa "Yerlesimi Sifirla" ile basa don, pencereyi
// buyut.
//
// Bu dosya Assets/Editor/ klasorune konulmali (Unity otomatik olarak build'e dahil etmez).
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class DiyalogAgaciGorselleyici : EditorWindow
{
    private class DugumVerisi
    {
        public string id;
        public string baslikMetni;
        public string detayMetni;
        public List<(string hedefId, string etiket)> baglantilar = new List<(string hedefId, string etiket)>();
        public Rect rect;
    }

    private const string BITIS_DUGUM_ID = "__BITIS__";
    private const string YAPRAK_DUGUM_ID = "__YAPRAK__";

    private const float DUGUM_GENISLIK = 220f;
    private const float DUGUM_YUKSEKLIK = 70f;
    private const float KATMAN_ARALIK = 280f;
    private const float SATIR_ARALIK = 100f;
    private const float PANEL_GENISLIK = 280f;

    private ScriptableObject secilenAsset;
    private Dictionary<string, DugumVerisi> dugumSozlugu;
    private Vector2 kaydirma = new Vector2(20, 20);
    private bool suruklemeAktif = false;
    private DugumVerisi seciliDugum;

    [MenuItem("Delilik/Diyalog/Agac Gorselleyici")]
    public static void PencereyiAc()
    {
        GetWindow<DiyalogAgaciGorselleyici>("Diyalog Agaci");
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUI.BeginChangeCheck();
        secilenAsset = (ScriptableObject)EditorGUILayout.ObjectField(secilenAsset, typeof(ScriptableObject), false, GUILayout.Width(260));
        if (EditorGUI.EndChangeCheck()) GrafiOlustur();

        if (GUILayout.Button("Yenile", EditorStyles.toolbarButton, GUILayout.Width(70))) GrafiOlustur();
        if (GUILayout.Button("Yerlesimi Sifirla", EditorStyles.toolbarButton, GUILayout.Width(130))) kaydirma = new Vector2(20, 20);

        EditorGUILayout.EndHorizontal();

        if (secilenAsset != null && !(secilenAsset is OzelMusteriHikayesiVerisi) && !(secilenAsset is BahceKonuVerisi))
        {
            EditorGUILayout.HelpBox("Bu asset turu desteklenmiyor. Sadece OzelMusteriHikayesiVerisi ve BahceKonuVerisi gosterilebilir.", MessageType.Warning);
            return;
        }

        if (secilenAsset == null)
        {
            EditorGUILayout.HelpBox("Bir OzelMusteriHikayesiVerisi (hikaye konugu) ya da BahceKonuVerisi (bahce konu agi) asset'i buraya surukle.", MessageType.Info);
            return;
        }

        if (dugumSozlugu == null) GrafiOlustur();

        float toolbarYuksekligi = EditorStyles.toolbar.fixedHeight;
        Rect canvasRect = new Rect(0, toolbarYuksekligi, position.width - PANEL_GENISLIK, position.height - toolbarYuksekligi);
        Rect panelRect = new Rect(canvasRect.width, toolbarYuksekligi, PANEL_GENISLIK, position.height - toolbarYuksekligi);

        GUI.Box(canvasRect, GUIContent.none);
        CanvasCiz(canvasRect);

        GUI.Box(panelRect, GUIContent.none);
        DetayPaneliCiz(panelRect);
    }

    // ------------------------------------------------------------------
    // GRAF OLUSTURMA
    // ------------------------------------------------------------------

    private void GrafiOlustur()
    {
        seciliDugum = null;
        dugumSozlugu = null;

        if (secilenAsset is OzelMusteriHikayesiVerisi hikaye)
        {
            OzelMusteriHikayesindenOlustur(hikaye);
        }
        else if (secilenAsset is BahceKonuVerisi bahce)
        {
            BahceKonuVerisindenOlustur(bahce);
        }
    }

    private void OzelMusteriHikayesindenOlustur(OzelMusteriHikayesiVerisi hikaye)
    {
        var ham = new Dictionary<string, DugumVerisi>();

        if (hikaye.segmentler != null)
        {
            foreach (var seg in hikaye.segmentler)
            {
                if (seg == null || string.IsNullOrEmpty(seg.id)) continue;

                var d = new DugumVerisi
                {
                    id = seg.id,
                    baslikMetni = Kisalt(seg.sonSatir, 60),
                    detayMetni = DetaySegment(seg)
                };

                if (seg.secenekler != null)
                {
                    foreach (var sec in seg.secenekler)
                    {
                        string hedef = string.IsNullOrEmpty(sec.hedefSegmentId) ? BITIS_DUGUM_ID : sec.hedefSegmentId;
                        d.baglantilar.Add((hedef, Kisalt(sec.secenekMetni, 24)));
                    }
                }

                ham[seg.id] = d;
            }
        }

        if (ham.Values.Any(d => d.baglantilar.Any(b => b.hedefId == BITIS_DUGUM_ID)))
        {
            ham[BITIS_DUGUM_ID] = new DugumVerisi
            {
                id = BITIS_DUGUM_ID,
                baslikMetni = "🏁 Hikaye Biter",
                detayMetni = "Bu segmentten sonra hikaye biter — konuk ertesi gunden itibaren normal musteri olarak gelmeye baslar."
            };
        }

        dugumSozlugu = ham;

        string kok = (!string.IsNullOrEmpty(hikaye.ilkSegmentId) && ham.ContainsKey(hikaye.ilkSegmentId))
            ? hikaye.ilkSegmentId
            : ham.Keys.FirstOrDefault();

        KatmanlaYerlestir(string.IsNullOrEmpty(kok) ? new List<string>() : new List<string> { kok });
    }

    private void BahceKonuVerisindenOlustur(BahceKonuVerisi bahce)
    {
        var ham = new Dictionary<string, DugumVerisi>();

        if (bahce.tumCumleler != null)
        {
            foreach (var c in bahce.tumCumleler)
            {
                if (c == null || string.IsNullOrEmpty(c.id)) continue;

                var d = new DugumVerisi
                {
                    id = c.id,
                    baslikMetni = Kisalt(c.metin, 60),
                    detayMetni = DetayCumle(c)
                };

                if (c.secenekler != null)
                {
                    foreach (var sec in c.secenekler)
                    {
                        string hedef = string.IsNullOrEmpty(sec.hedefCumleId) ? YAPRAK_DUGUM_ID : sec.hedefCumleId;
                        d.baglantilar.Add((hedef, Kisalt(sec.secenekMetni, 24)));
                    }
                }

                ham[c.id] = d;
            }
        }

        if (ham.Values.Any(d => d.baglantilar.Any(b => b.hedefId == YAPRAK_DUGUM_ID)))
        {
            ham[YAPRAK_DUGUM_ID] = new DugumVerisi
            {
                id = YAPRAK_DUGUM_ID,
                baslikMetni = "🍂 Konusma Biter",
                detayMetni = "Bu secenek herhangi bir cumleye baglanmiyor — konusma burada dogal olarak biter (ya da baska bir 'ilgili konu' aramasina duser)."
            };
        }

        dugumSozlugu = ham;

        // Kok(ler): hicbir cumlenin secenegi tarafindan HEDEF ALINMAMIS dugumler — bahce
        // sohbeti tek bir baslangic noktasindan degil, konu-eslesmesine gore HERHANGI bir
        // cumleden baslayabildigi icin BIRDEN FAZLA kok olmasi normaldir.
        var hedefAlinanlar = new HashSet<string>();
        foreach (var d in ham.Values)
            foreach (var b in d.baglantilar)
                hedefAlinanlar.Add(b.hedefId);

        var kokler = ham.Keys.Where(k => !hedefAlinanlar.Contains(k)).ToList();
        if (kokler.Count == 0) kokler = ham.Keys.ToList(); // hepsi birbirine bagliysa (dongu), hepsini kok say

        KatmanlaYerlestir(kokler);
    }

    /// <summary>Kokten (ya da koklerden) BFS ile katman (sutun) atar, her katmanda sirayla
    /// satir numarasi verir ve buna gore Rect hesaplar. Ulasilamayan (kopuk) dugumler son
    /// katmandan sonra ayrica gosterilir — en azindan gorunmez olmasinlar.</summary>
    private void KatmanlaYerlestir(List<string> kokler)
    {
        if (dugumSozlugu == null || dugumSozlugu.Count == 0) return;

        var katmanSozluk = new Dictionary<string, int>();
        var ziyaretSirasi = new List<string>();
        var kuyruk = new Queue<string>();

        foreach (var k in kokler)
        {
            if (!dugumSozlugu.ContainsKey(k) || katmanSozluk.ContainsKey(k)) continue;
            katmanSozluk[k] = 0;
            kuyruk.Enqueue(k);
        }

        while (kuyruk.Count > 0)
        {
            string simdi = kuyruk.Dequeue();
            ziyaretSirasi.Add(simdi);
            if (!dugumSozlugu.TryGetValue(simdi, out var d)) continue;

            int katman = katmanSozluk[simdi];
            foreach (var b in d.baglantilar)
            {
                if (!dugumSozlugu.ContainsKey(b.hedefId)) continue;
                if (!katmanSozluk.ContainsKey(b.hedefId))
                {
                    katmanSozluk[b.hedefId] = katman + 1;
                    kuyruk.Enqueue(b.hedefId);
                }
            }
        }

        int maxKatman = katmanSozluk.Count > 0 ? katmanSozluk.Values.Max() : 0;
        foreach (var id in dugumSozlugu.Keys)
        {
            if (!katmanSozluk.ContainsKey(id))
            {
                katmanSozluk[id] = maxKatman + 1;
                ziyaretSirasi.Add(id);
            }
        }

        var katmaninSayaci = new Dictionary<int, int>();
        foreach (var id in ziyaretSirasi)
        {
            int katman = katmanSozluk[id];
            int satir = katmaninSayaci.TryGetValue(katman, out int s) ? s : 0;
            katmaninSayaci[katman] = satir + 1;

            var d = dugumSozlugu[id];
            d.rect = new Rect(katman * KATMAN_ARALIK + 20, satir * SATIR_ARALIK + 20, DUGUM_GENISLIK, DUGUM_YUKSEKLIK);
        }
    }

    // ------------------------------------------------------------------
    // CIZIM
    // ------------------------------------------------------------------

    private void CanvasCiz(Rect canvasRect)
    {
        GUI.BeginGroup(canvasRect);

        if (dugumSozlugu != null)
        {
            Handles.BeginGUI();
            foreach (var d in dugumSozlugu.Values)
            {
                foreach (var b in d.baglantilar)
                {
                    if (!dugumSozlugu.TryGetValue(b.hedefId, out var hedefDugum)) continue;

                    Vector2 baslangic = d.rect.position + kaydirma + new Vector2(d.rect.width, d.rect.height / 2f);
                    Vector2 bitis = hedefDugum.rect.position + kaydirma + new Vector2(0, hedefDugum.rect.height / 2f);

                    bool bitisDugumuMu = hedefDugum.id == BITIS_DUGUM_ID || hedefDugum.id == YAPRAK_DUGUM_ID;
                    Handles.color = bitisDugumuMu ? new Color(0.8f, 0.5f, 0.3f, 0.8f) : new Color(0.4f, 0.75f, 1f, 0.85f);

                    Handles.DrawBezier(baslangic, bitis, baslangic + Vector2.right * 40f, bitis + Vector2.left * 40f, Handles.color, null, 2.5f);

                    Vector2 ortaNokta = (baslangic + bitis) / 2f;
                    GUI.Label(new Rect(ortaNokta.x - 65, ortaNokta.y - 8, 130, 16), b.etiket, EditorStyles.miniLabel);
                }
            }
            Handles.EndGUI();

            foreach (var d in dugumSozlugu.Values)
            {
                Rect ekranRect = new Rect(d.rect.position + kaydirma, d.rect.size);
                bool ozelDugumMu = d.id == BITIS_DUGUM_ID || d.id == YAPRAK_DUGUM_ID;

                Color eskiRenk = GUI.color;
                GUI.color = (d == seciliDugum) ? new Color(1f, 0.85f, 0.3f) : (ozelDugumMu ? new Color(0.8f, 0.8f, 0.8f) : Color.white);
                GUI.Box(ekranRect, GUIContent.none);
                GUI.color = eskiRenk;

                GUI.Label(new Rect(ekranRect.x + 6, ekranRect.y + 2, ekranRect.width - 12, 16), d.id, EditorStyles.boldLabel);
                GUI.Label(new Rect(ekranRect.x + 6, ekranRect.y + 20, ekranRect.width - 12, ekranRect.height - 24), d.baslikMetni, EditorStyles.wordWrappedMiniLabel);
            }
        }

        HandleEvents();

        GUI.EndGroup();
    }

    private void HandleEvents()
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            DugumVerisi tiklanan = null;
            if (dugumSozlugu != null)
            {
                foreach (var d in dugumSozlugu.Values)
                {
                    Rect ekranRect = new Rect(d.rect.position + kaydirma, d.rect.size);
                    if (ekranRect.Contains(e.mousePosition)) { tiklanan = d; break; }
                }
            }

            seciliDugum = tiklanan;
            suruklemeAktif = (tiklanan == null);
            Repaint();
        }
        else if (e.type == EventType.MouseDrag && suruklemeAktif)
        {
            kaydirma += e.delta;
            Repaint();
        }
        else if (e.type == EventType.MouseUp)
        {
            suruklemeAktif = false;
        }
    }

    private void DetayPaneliCiz(Rect panelRect)
    {
        GUILayout.BeginArea(panelRect, GUIContent.none, GUIStyle.none);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Detay", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (seciliDugum == null)
        {
            EditorGUILayout.HelpBox("Detaylarini gormek icin bir dugume tikla.\n\nBos alana tiklayip surukleyerek gorunumu kaydirabilirsin.", MessageType.None);
        }
        else
        {
            EditorGUILayout.LabelField("ID", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(seciliDugum.id, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(seciliDugum.detayMetni, EditorStyles.wordWrappedLabel);

            if (seciliDugum.baglantilar.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Secenekler", EditorStyles.miniBoldLabel);
                foreach (var b in seciliDugum.baglantilar)
                    EditorGUILayout.LabelField("• " + b.etiket + "  →  " + b.hedefId, EditorStyles.wordWrappedMiniLabel);
            }
        }

        GUILayout.EndArea();
    }

    // ------------------------------------------------------------------
    // YARDIMCILAR
    // ------------------------------------------------------------------

    private static string Kisalt(string metin, int uzunluk)
    {
        if (string.IsNullOrEmpty(metin)) return "(bos)";
        string tekSatir = metin.Replace("\n", " ").Replace("\r", "");
        return tekSatir.Length <= uzunluk ? tekSatir : tekSatir.Substring(0, uzunluk) + "...";
    }

    private static string DetaySegment(HikayeGunSegmenti seg)
    {
        int onSatirSayisi = seg.onSatirlar != null ? seg.onSatirlar.Count : 0;
        int ekDilSayisi = seg.sonSatirEkDiller != null ? seg.sonSatirEkDiller.Count : 0;
        int secenekSayisi = seg.secenekler != null ? seg.secenekler.Count : 0;

        return $"On satir sayisi: {onSatirSayisi} (Turkce-only)\n" +
               $"Son satir: {seg.sonSatir}\n" +
               $"Son satir ek dil sayisi: {ekDilSayisi}\n" +
               $"Secenek sayisi: {secenekSayisi}";
    }

    private static string DetayCumle(BahceCumlesi c)
    {
        string konular = (c.konular != null && c.konular.Count > 0) ? string.Join(", ", c.konular) : "(konu etiketi yok)";
        int ekDilSayisi = c.ekDiller != null ? c.ekDiller.Count : 0;
        string sesDurumu = c.ses.BosMu ? "yok (varsayilan blabla calar)" : "ozel klip var";
        int secenekSayisi = c.secenekler != null ? c.secenekler.Count : 0;

        return $"Metin: {c.metin}\n" +
               $"Konular: {konular}\n" +
               $"Ek dil sayisi: {ekDilSayisi}\n" +
               $"Ozel seslendirme: {sesDurumu}\n" +
               $"Secenek sayisi: {secenekSayisi}";
    }
}
