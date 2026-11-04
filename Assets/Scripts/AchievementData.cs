// AchievementData.cs — Tek bir basarimin tanimi (ScriptableObject)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

[CreateAssetMenu(fileName = "Achievement_", menuName = "Delilik/Achievement Data")]
public class AchievementData : ScriptableObject
{
    [Header("Kimlik")]
    public string achievementId = "ach_001";
    public string title = "Yeni Basarim";
    [TextArea(2, 3)] public string description = "Aciklama";
    public Sprite icon;

    [Header("Kategori")]
    public AchievementCategory category = AchievementCategory.Economy;

    [Header("Gizli Basarim")]
    [Tooltip("ISARETLI: acilana kadar basligin/aciklamasinin yerine '???' gosterilir — surpriz basarimlar icin (orn. beklenmedik bir olay, sakli bir eylem).")]
    public bool gizli = false;

    [Header("Kosul")]
    public TrackedStat trackedStat = TrackedStat.BooksSold;
    public int targetValue = 10;

    [Header("Odul")]
    public int goldReward = 250;
}