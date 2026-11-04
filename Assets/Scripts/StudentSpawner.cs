// StudentSpawner.cs — Gunluk ogrenci talebi zamanlayicisi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections;

public class StudentSpawner : MonoBehaviour
{
    [Header("Zamanlama")]
    [SerializeField] private int minPerDay = 1;
    [SerializeField] private int maxPerDay = 2;
    [SerializeField] private float minDelay = 25f;
    [SerializeField] private float maxDelay = 70f;

    private Coroutine routine;

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
        routine = StartCoroutine(DailyStudentLoop());
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

    private IEnumerator DailyStudentLoop()
    {
        int count = Random.Range(minPerDay, maxPerDay + 1);

        for (int i = 0; i < count; i++)
        {
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

            if (GameManager.Instance.CurrentPhase != GamePhase.Day) yield break;
            if (GameManager.Instance.IsDayTimeUp) yield break;

            // Musait (acik + bos) rastgele bir masa bulup talep olusturmayi dener.
            // 2 masa acikken ayni anda 2 farkli ogrenci bekleyebilir — bu yuzden burada
            // eski sistemdeki gibi "tek talep bitene kadar bekle" mantigi YOK.
            // Musait masa yoksa bu ogrencinin sirasi atlanir, oyun akmaya devam eder.
            StudentDeskManager.Instance.TryCreateRequestOnRandomDesk();
        }
        routine = null;
    }
}
