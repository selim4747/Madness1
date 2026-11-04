// BossAreaDamage.cs — Boss'un yerde patlayan alan hasari
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections;

public class BossAreaDamage : MonoBehaviour
{
    [Header("Gorsel")]
    [SerializeField] private SpriteRenderer warningCircle;
    [SerializeField] private SpriteRenderer explosionCircle;
    [SerializeField] private AudioClip explosionSound;

    public void Trigger(float radius, float damage, float warningTime)
    {
        transform.localScale = Vector3.one * radius * 2f;

        if (explosionCircle != null) explosionCircle.enabled = false;
        if (warningCircle != null) warningCircle.enabled = true;

        StartCoroutine(Sequence(radius, damage, warningTime));
    }

    private IEnumerator Sequence(float radius, float damage, float warningTime)
    {
        // Uyari fazi — halka yanip soner, oyuncu kacabilir
        float t = 0f;
        while (t < warningTime)
        {
            t += Time.deltaTime;

            if (warningCircle != null)
            {
                float alpha = Mathf.PingPong(t * 4f, 1f) * 0.6f + 0.2f;
                Color c = warningCircle.color;
                warningCircle.color = new Color(c.r, c.g, c.b, alpha);
            }
            yield return null;
        }

        // Patlama
        if (warningCircle != null) warningCircle.enabled = false;
        if (explosionCircle != null) explosionCircle.enabled = true;

        AudioManager.Instance.PlaySfx(explosionSound);

        if (PlayerController.Instance != null)
        {
            float d = Vector2.Distance(transform.position, PlayerController.Instance.transform.position);
            if (d <= radius) PlayerStats.Instance.TakeDamage(damage);
        }

        yield return new WaitForSeconds(0.35f);
        Destroy(gameObject);
    }
}
