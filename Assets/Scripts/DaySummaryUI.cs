// DaySummaryUI.cs — Gun sonu ozet ekrani
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DaySummaryUI : MonoBehaviour
{
    public static DaySummaryUI Instance { get; private set; }

    [Header("Referanslar")]
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI booksSoldText;
    [SerializeField] private TextMeshProUGUI goldEarnedText;
    [SerializeField] private TextMeshProUGUI satisfactionText;
    [SerializeField] private TextMeshProUGUI totalGoldText;
    [SerializeField] private Button continueButton;

    private Action onClosed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (root != null) root.SetActive(false);
        if (continueButton != null) continueButton.onClick.AddListener(Close);
    }

    /// <summary>GameManager gece bitince cagirir.</summary>
    public void Show(int day, Action continueCallback)
    {
        onClosed = continueCallback;

        if (titleText != null) titleText.text = day + ". Gun Tamamlandi";
        if (booksSoldText != null) booksSoldText.text = "Satilan kitap: " + PlayerProgressStats.BooksSoldToday;
        if (goldEarnedText != null) goldEarnedText.text = "Kazanc: " + PlayerProgressStats.GoldEarnedToday + " Para";

        if (satisfactionText != null)
        {
            int percent = Mathf.RoundToInt(SatisfactionTracker.Instance.TodayAverage * 100f);
            satisfactionText.text = "Memnuniyet: %" + percent;
        }

        if (totalGoldText != null) totalGoldText.text = "Toplam para: " + EconomyManager.Instance.Gold;

        if (root != null) root.SetActive(true);
    }

    private void Close()
    {
        if (root != null) root.SetActive(false);

        Action callback = onClosed;
        onClosed = null;

        if (callback != null) callback();
    }
}
