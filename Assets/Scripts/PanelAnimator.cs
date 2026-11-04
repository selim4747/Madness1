// PanelAnimator.cs — Panel her aktif oldugunda hafif bir "buyuyerek acilma" animasyonu oynatir.
// Herhangi bir panelin KOK objesine Add Component ile eklenir, baska hicbir kod degisikligi
// GEREKMEZ — mevcut SetActive(true) cagrilarinin hepsiyle otomatik calisir (OnEnable ile tetiklenir).
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections;

public class PanelAnimator : MonoBehaviour
{
    [SerializeField] private float sure = 0.18f;
    [SerializeField] private float baslangicScale = 0.85f;

    private Vector3 hedefScale;
    private bool hedefAlindi;
    private Coroutine calisan;

    private void OnEnable()
    {
        if (!hedefAlindi)
        {
            hedefScale = transform.localScale;
            hedefAlindi = true;
        }

        if (calisan != null) StopCoroutine(calisan);
        calisan = StartCoroutine(AcilisAnimasyonu());
    }

    private IEnumerator AcilisAnimasyonu()
    {
        float t = 0f;
        transform.localScale = hedefScale * baslangicScale;

        while (t < sure)
        {
            // unscaledDeltaTime: panel Time.timeScale=0 durumunda (orn. olum ekrani) acilsa bile calisir.
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / sure);
            float eased = 1f - Mathf.Pow(1f - p, 3f); // hafif "yumusak firlama" hissi
            transform.localScale = Vector3.LerpUnclamped(hedefScale * baslangicScale, hedefScale, eased);
            yield return null;
        }

        transform.localScale = hedefScale;
    }
}