// RewardedAdManager.cs — Odullu reklam sistemi (IAP yok)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System;
using System.Collections.Generic;

public class RewardedAdManager : MonoBehaviour
{
    public static RewardedAdManager Instance { get; private set; }

    [Header("Bekleme suresi")]
    [SerializeField] private float cooldownSeconds = 300f;   // 5 dakika

    [Header("Odul degerleri")]
    [SerializeField] private float reviveDamageBonus = 0.20f;  // %20 guc
    [SerializeField] private int customerLimitBonus = 5;

    private readonly Dictionary<RewardType, float> lastUsedTime = new Dictionary<RewardType, float>();

    /// <summary>Odul verildiginde tetiklenir (UI yenilemek icin).</summary>
    public event Action<RewardType> OnRewardGranted;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool IsAvailable(RewardType type)
    {
        float last;
        if (!lastUsedTime.TryGetValue(type, out last)) return true;

        return Time.time - last >= cooldownSeconds;
    }

    /// <summary>Kalan bekleme suresi (saniye). 0 = hazir.</summary>
    public float GetRemainingCooldown(RewardType type)
    {
        float last;
        if (!lastUsedTime.TryGetValue(type, out last)) return 0f;

        return Mathf.Max(0f, cooldownSeconds - (Time.time - last));
    }

    /// <summary>UI butonundan cagrilir.</summary>
    public void RequestAd(RewardType type)
    {
        if (!IsAvailable(type))
        {
            int seconds = Mathf.CeilToInt(GetRemainingCooldown(type));
            ToastNotification.Instance.Show("Bu odul icin " + seconds + " saniye beklemelisin.", 3f);
            return;
        }

        ShowAd(type);
    }

    /// <summary>
    /// GERCEK REKLAM SDK ENTEGRASYONU BURAYA GELIR.
    /// Ornek (Google Mobile Ads):
    ///   rewardedAd.Show((Reward r) =&gt; GrantReward(type));
    /// SDK kurulana kadar odul dogrudan verilir — test edilebilir kalir.
    /// </summary>
    private void ShowAd(RewardType type)
    {
        GrantReward(type);
    }

    private void GrantReward(RewardType type)
    {
        lastUsedTime[type] = Time.time;

        switch (type)
        {
            case RewardType.ExtraStock:
                RestockSystem.Instance.ForceRestockPackage();
                break;

            case RewardType.Revive:
                PlayerStats.Instance.ReviveWithBonus(reviveDamageBonus);
                ToastNotification.Instance.Show("Yeniden dogdun — tam can ve guc bonusu!", 3f);
                break;

            case RewardType.CustomerLimitBoost:
                CustomerSpawner.Instance.AddCapacityBonus(customerLimitBonus);
                ToastNotification.Instance.Show("Bugun " + customerLimitBonus + " ekstra musteri gelecek!", 3f);
                break;
        }

        if (OnRewardGranted != null) OnRewardGranted(type);
    }
}
