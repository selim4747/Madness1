// TextPunch.cs — Bagli oldugu TextMeshPro yazisinin ICERIGI degistiginde kisa bir "punch"
// (buyup kuculme) animasyonu oynatir. Can/Altin gibi surekli guncellenen yazilara eklenir,
// otomatik calisir — ekstra kod cagrisi gerekmez.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextPunch : MonoBehaviour
{
    [SerializeField] private float punchScale = 1.25f;
    [SerializeField] private float sure = 0.15f;

    private TextMeshProUGUI tmp;
    private string oncekiMetin;
    private Vector3 orijinalScale;
    private Coroutine calisan;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        orijinalScale = transform.localScale;
        oncekiMetin = tmp.text;
    }

    private void LateUpdate()
    {
        if (tmp.text != oncekiMetin)
        {
            oncekiMetin = tmp.text;
            Punch();
        }
    }

    private void Punch()
    {
        if (calisan != null) StopCoroutine(calisan);
        calisan = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        float t = 0f;
        while (t < sure)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / sure);
            float scale = Mathf.Lerp(punchScale, 1f, p);
            transform.localScale = orijinalScale * scale;
            yield return null;
        }
        transform.localScale = orijinalScale;
    }
}