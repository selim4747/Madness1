// DebugRestartButton.cs — Test icin: kaydi silip sahneyi bastan yukler
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugRestartButton : MonoBehaviour
{
    /// <summary>Market panelindeki test butonuna baglanir. Aktif kayit slotunu siler
    /// ve sahneyi sifirdan yukler — para, gun, envanter, dusman havuzu, HER SEY bastan baslar.</summary>
    public void DEBUG_OyunuBastanBaslat()
    {
        if (SaveSystem.Instance != null)
        {
            int slot = SaveSystem.Instance.ActiveSlot;
            if (slot >= 0) SaveSystem.Instance.DeleteSlot(slot);
        }

        Time.timeScale = 1f;   // bir cutscene ortasinda basilirsa donuk kalmasin
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}