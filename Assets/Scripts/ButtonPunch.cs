// ButtonPunch.cs — Butona basinca kisa bir "punch" (hafifce buyuyup eski haline donme) hissi ekler.
// Herhangi bir Button objesine Add Component ile eklemen yeterli — ekstra Inspector ayari gerekmez.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button))]
public class ButtonPunch : MonoBehaviour
{
    [SerializeField] private float punchScale = 1.15f;
    [SerializeField] private float sure = 0.12f;

    private Vector3 orijinalScale;
    private Coroutine calisan;

    private void Awake()
    {
        orijinalScale = transform.localScale;
        Button btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(Punch);
    }

    private void Punch()
    {
        if (!gameObject.activeInHierarchy) return;
        if (calisan != null) StopCoroutine(calisan);
        calisan = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        // unscaledDeltaTime: oyun duraklatilmisken (Time.timeScale=0, orn. olum ekrani) bile calisir.
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