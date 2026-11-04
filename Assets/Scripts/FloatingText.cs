// FloatingText.cs — Havada yukselip kaybolan sayi/yazi efekti (hasar, altin, iyilesme vs.)
// Prefab GEREKTIRMEZ — TextMeshPro objesini kod icinde kendi olusturur, hicbir Inspector
// baglantisi gerektirmeden herhangi bir script'ten FloatingText.Spawn(...) ile cagrilabilir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public enum Renk { Hasar, Iyilesme, Altin, Notr }

    private TextMeshPro tmp;
    private Vector3 baslangicKonum;
    private float gecenSure;

    private const float SURE = 0.8f;
    private const float YUKSELME_MESAFESI = 1f;

    public static void Spawn(Vector3 dunyaKonumu, string metin, Renk renk)
    {
        GameObject go = new GameObject("FloatingText");
        // Z=-1: kamera Z=-10'da oyuncuya bakiyor, bu yuzden -1 sprite'larin (Z=0) ONUNDE gorunur.
        go.transform.position = dunyaKonumu + new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0.3f, -1f);

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = metin;
        tmp.fontSize = 3.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = RenkSec(renk);
        tmp.fontStyle = FontStyles.Bold;
        tmp.sortingOrder = 100;

        FloatingText ft = go.AddComponent<FloatingText>();
        ft.tmp = tmp;
        ft.baslangicKonum = go.transform.position;
    }

    private static Color RenkSec(Renk renk)
    {
        switch (renk)
        {
            case Renk.Hasar: return new Color(1f, 0.25f, 0.25f);
            case Renk.Iyilesme: return new Color(0.35f, 1f, 0.35f);
            case Renk.Altin: return new Color(1f, 0.85f, 0.2f);
            default: return Color.white;
        }
    }

    private void Update()
    {
        gecenSure += Time.deltaTime;
        float t = gecenSure / SURE;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = baslangicKonum + Vector3.up * (YUKSELME_MESAFESI * t);

        if (tmp != null)
        {
            Color c = tmp.color;
            c.a = 1f - t;
            tmp.color = c;
        }
    }
}