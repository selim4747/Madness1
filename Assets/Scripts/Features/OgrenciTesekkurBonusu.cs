using UnityEngine;

/// <summary>
/// Öğrenciye kitap teslim edilince küçük bonus. StudentDeskController'dan çağrılabilir.
/// </summary>
public static class OgrenciTesekkurBonusu
{
    public static void Uygula(int ekstraMemnuniyet = 1, int ekstraAltin = 10)
    {
        if (SatisfactionTracker.Instance != null)
            SatisfactionTracker.Instance.RecordPositiveEvent();
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.AddGold(ekstraAltin);
        if (ToastNotification.Instance != null)
            ToastNotification.Instance.Show("Öğrenci teşekkür etti! +" + ekstraAltin, 2f);
    }
}
