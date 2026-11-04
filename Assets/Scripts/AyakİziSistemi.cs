// AyakIziSistemi.cs — Yururken periyodik ayak tozu, ani durunca daha buyuk "fren" tozu.
// DeathPuffEffect gibi kod-uretimli, hicbir sprite/prefab gerektirmez.
// Herhangi bir karakterin KOK objesine eklenir (NavMeshAgent ya da Rigidbody2D olan yere).
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public class AyakIziSistemi : MonoBehaviour
{
    [Header("Ayak Tozu")]
    [SerializeField] private float adimAraligi = 0.35f; // ne kadar sure/mesafede bir toz birak
    [SerializeField] private float minHizEsigi = 0.3f;   // bu hizin altinda toz birakma

    [Header("Fren Tozu (ani durma)")]
    [SerializeField] private float frenIvmeEsigi = 8f;    // bu ivmenin uzerinde ani durma sayilir

    private UnityEngine.AI.NavMeshAgent agent;
    private Rigidbody2D rb;
    private float sonAdimZamani;
    private Vector2 oncekiHiz;
    private bool sagAyak; // adimlar sirayla sag/sol hissi versin diye hafif offset

    private void Awake()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        rb = GetComponent<Rigidbody2D>();
    }

    private Vector2 MevcutHiz()
    {
        if (agent != null) return agent.velocity;
        if (rb != null) return rb.linearVelocity;
        return Vector2.zero;
    }

    private void Update()
    {
        Vector2 hiz = MevcutHiz();
        float hizBuyuklugu = hiz.magnitude;

        // Periyodik ayak tozu (yururken)
        if (hizBuyuklugu > minHizEsigi)
        {
            if (Time.time - sonAdimZamani >= adimAraligi / Mathf.Max(0.5f, hizBuyuklugu))
            {
                sonAdimZamani = Time.time;
                Vector3 ayakOfseti = sagAyak ? new Vector3(0.08f, 0f, 0f) : new Vector3(-0.08f, 0f, 0f);
                sagAyak = !sagAyak;
                TozEfektiUret.Spawn(transform.position + ayakOfseti + Vector3.down * 0.2f, 0.08f, 3, 0.35f);
            }
        }

        // Ani durma tespiti (fren tozu)
        float ivme = Time.deltaTime > 0f ? (hiz - oncekiHiz).magnitude / Time.deltaTime : 0f;
        if (ivme > frenIvmeEsigi && oncekiHiz.magnitude > 1.5f && hizBuyuklugu < 0.5f)
        {
            TozEfektiUret.Spawn(transform.position + Vector3.down * 0.2f, 0.14f, 6, 0.4f);
        }

        oncekiHiz = hiz;
    }
}

/// <summary>Kod-uretimli toz efekti — DeathPuffEffect ile ayni teknik, farkli gorunum
/// (daha kucuk, gri/toprak rengi, daha kisa omurlu).</summary>
public static class TozEfektiUret
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

    public static void Spawn(Vector3 konum, float boyut, int parcaSayisi, float omur)
    {
        Color tozRengi = new Color(0.75f, 0.7f, 0.6f, 0.5f); // toprak/gri, yari saydam

        for (int i = 0; i < parcaSayisi; i++)
        {
            GameObject go = new GameObject("TozParca");
            go.transform.position = konum;
            go.transform.localScale = Vector3.one * boyut;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetKareSprite();
            sr.color = tozRengi;
            sr.sortingOrder = 5; // karakterlerin biraz altinda kalsin

            float aci = UnityEngine.Random.Range(0f, 360f);
            Vector2 yon = new Vector2(Mathf.Cos(aci * Mathf.Deg2Rad), Mathf.Sin(aci * Mathf.Deg2Rad));

            TozParca parca = go.AddComponent<TozParca>();
            parca.Baslat(yon * UnityEngine.Random.Range(0.2f, 0.5f), omur);
        }
    }
}

public class TozParca : MonoBehaviour
{
    private Vector2 hiz;
    private float omur;
    private float gecenSure;
    private SpriteRenderer sr;
    private Vector3 baslangicScale;

    public void Baslat(Vector2 baslangicHiz, float yasamSuresi)
    {
        hiz = baslangicHiz;
        omur = yasamSuresi;
        sr = GetComponent<SpriteRenderer>();
        baslangicScale = transform.localScale;
    }

    private void Update()
    {
        gecenSure += Time.deltaTime;
        float t = gecenSure / omur;

        if (t >= 1f) { Destroy(gameObject); return; }

        transform.position += (Vector3)(hiz * Time.deltaTime);
        hiz *= 0.85f;
        transform.localScale = baslangicScale * (1f + t * 0.5f); // buyuyerek dagilir (toz gibi)

        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(0.5f, 0f, t);
            sr.color = c;
        }
    }
}