// DeathPuffEffect.cs — Dusman olunce dagilan kucuk renkli parcalar efekti.
// Hicbir sprite/prefab GEREKTIRMEZ — 1x1 beyaz doku kod icinde uretilir, renklendirilir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public static class DeathPuffEffect
{
    private static Sprite kareSprite;

    private static Sprite GetKareSprite()
    {
        if (kareSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            kareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return kareSprite;
    }

    public static void Spawn(Vector3 konum, Color renk)
    {
        const int parcaSayisi = 6;

        for (int i = 0; i < parcaSayisi; i++)
        {
            GameObject go = new GameObject("DeathPuffParca");
            go.transform.position = konum;
            go.transform.localScale = Vector3.one * 0.15f;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetKareSprite();
            sr.color = renk;
            sr.sortingOrder = 50;

            float aci = (360f / parcaSayisi) * i + UnityEngine.Random.Range(-15f, 15f);
            Vector2 yon = new Vector2(Mathf.Cos(aci * Mathf.Deg2Rad), Mathf.Sin(aci * Mathf.Deg2Rad));

            DeathPuffParca parca = go.AddComponent<DeathPuffParca>();
            parca.Baslat(yon * UnityEngine.Random.Range(1f, 2f));
        }
    }
}

public class DeathPuffParca : MonoBehaviour
{
    private Vector2 hiz;
    private float omur = 0.4f;
    private float gecenSure;
    private SpriteRenderer sr;

    public void Baslat(Vector2 baslangicHiz)
    {
        hiz = baslangicHiz;
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        gecenSure += Time.deltaTime;
        float t = gecenSure / omur;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(hiz * Time.deltaTime);
        hiz *= 0.9f; // yavaslar

        transform.localScale = Vector3.one * 0.15f * (1f - t);

        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f - t;
            sr.color = c;
        }
    }
}