// ThiefSpawner.cs — Gunde 1-2 hirsiz uretir
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections;

public class ThiefSpawner : MonoBehaviour
{
    public static ThiefSpawner Instance { get; private set; }

    [Header("Referanslar")]
    [SerializeField] private GameObject thiefPrefab;
    [SerializeField] private Transform doorPoint;
    [SerializeField] private Transform gecitNoktasi;

    [Header("Zamanlama")]
    [SerializeField] private float minGapSeconds = 60f;
    [SerializeField] private int minPerDay = 1;
    [SerializeField] private int maxPerDay = 2;

    private Coroutine routine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GameManager.Instance.OnDayChanged += HandleDayChanged;
        GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnDayChanged -= HandleDayChanged;
        GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandleDayChanged(int day)
    {
        StopRoutine();
        routine = StartCoroutine(DailyThiefLoop());
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase != GamePhase.Day) StopRoutine();
    }

    private void StopRoutine()
    {
        if (routine == null) return;

        StopCoroutine(routine);
        routine = null;
    }

    private IEnumerator DailyThiefLoop()
    {
        int count = Random.Range(minPerDay, maxPerDay + 1);

        for (int i = 0; i < count; i++)
        {
            // En az 60 saniye ara — GDD kurali
            yield return new WaitForSeconds(Random.Range(minGapSeconds, minGapSeconds * 1.8f));

            if (GameManager.Instance.CurrentPhase != GamePhase.Day) yield break;
            if (GameManager.Instance.IsDayTimeUp) yield break;

            SpawnThief();
        }
        routine = null;
    }

    private void SpawnThief()
    {
        ShelfController shelf = ShopManager.Instance.FindRandomStockedShelf();
        if (shelf == null) return;   // calacak kitap yok

        GameObject go = Instantiate(thiefPrefab, doorPoint.position, Quaternion.identity);
        go.GetComponent<ThiefController>().Initialize(doorPoint, shelf, gecitNoktasi);
    }
}
