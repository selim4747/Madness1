using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ana menü kilit sistemi.
/// İstatistikler + Başarımlar kilitsiz.
/// Çok Oyunculu 5. günde açılır.
/// Mod Oluştur 24. gece.
/// </summary>
public class MenuUnlockController : MonoBehaviour
{
    [System.Serializable]
    public class MenuButtonLock
    {
        public string id;
        public Button button;
        public GameObject lockOverlay;
        public TextMeshProUGUI lockLabel;
        public int requiredDay;
        public bool alwaysUnlocked = false;
    }

    [Header("Butonlar")]
    [SerializeField] private MenuButtonLock[] buttons;

    private void Start() => Refresh();

    public void Refresh()
    {
        int highest = PlayerProgressStats.HighestDayReached;

        foreach (var b in buttons)
        {
            if (b.button == null) continue;

            bool unlocked = b.alwaysUnlocked || highest >= b.requiredDay;

            b.button.interactable = unlocked;

            if (b.lockOverlay != null)
                b.lockOverlay.SetActive(!unlocked);

            if (b.lockLabel != null)
            {
                b.lockLabel.gameObject.SetActive(!unlocked);
                if (!unlocked)
                    b.lockLabel.text = b.requiredDay + ". Gece";
            }
        }
    }
}
