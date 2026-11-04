using UnityEngine;
using TMPro;

public class HataYakalayici : MonoBehaviour
{
    public TextMeshProUGUI hataEkrani;

    void OnEnable()
    {
        Application.logMessageReceived += LogYazdir;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= LogYazdir;
    }

    void LogYazdir(string mesaj, string stackTrace, LogType tur)
    {
        // Sadece ölümcül hataları (kırmızıları) ekrana basar!
        if (tur == LogType.Error || tur == LogType.Exception)
        {
            if (hataEkrani != null)
            {
                hataEkrani.text += $"\n<color=red>HATA: {mesaj}</color>";
            }
        }
    }
}