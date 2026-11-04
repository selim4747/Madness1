// SpawnPop.cs — Obje sahneye/aktif hale gelince kucuk bir "pop" (siftirarak buyume) ile belirir.
// Herhangi bir NPC/dusman objesine Add Component ile eklenir, otomatik OnEnable'da tetiklenir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections;

public class SpawnPop : MonoBehaviour
{
    [SerializeField] private float sure = 0.25f;

    private Vector3 hedefScale;
    private Coroutine calisan;

    private void OnEnable()
    {
        if (hedefScale == Vector3.zero) hedefScale = transform.localScale;
        if (calisan != null) StopCoroutine(calisan);
        calisan = StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        float t = 0f;
        transform.localScale = Vector3.zero;

        while (t < sure)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / sure);
            // "overshoot" (hafif tasip geri gelme) hissi icin basit bir back-ease
            float overshoot = 1.4f;
            float eased = 1f + (overshoot + 1f) * Mathf.Pow(p - 1f, 3f) + overshoot * Mathf.Pow(p - 1f, 2f);
            transform.localScale = hedefScale * Mathf.Max(0f, eased);
            yield return null;
        }

        transform.localScale = hedefScale;
    }
}