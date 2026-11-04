// AnaMenuyeDonButonu.cs — Oyunu o SANIYEDE dondurup tam durumu kaydeder ve Ana Menu'ye doner.
// Geri girildiginde ayni yerden, ayni gun/para/konum/musterilerle devam edilir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.SceneManagement;

public class AnaMenuyeDonButonu : MonoBehaviour
{
    [Header("Ana Menu sahnesinin adi (Build Settings'teki isimle AYNI olmali)")]
    [SerializeField] private string anaMenuSahneAdi = "AnaMenu";

    /// <summary>Butona baglanir. Tam oturum durumunu kaydedip Ana Menu'ye doner.</summary>
    public void AnaMenuyeDon()
    {
        if (SaveSystem.Instance != null && GameManager.Instance != null)
        {
            // O ANIN durumunu (konum, sayaclar, faz, musteriler) kayit verisine yaz.
            // NOT: SaveSnapshot() BILEREK cagrilmiyor — o, "olunce gunun basina don"
            // ozelligi icin gun BASINDA alinan ayri bir kayit; burada cagirirsak onu ezip
            // olum sonrasi geri donusu bozardik.
            GameManager.Instance.OturumDurumunuYaz(SaveSystem.Instance.Current);

            // SaveToSlot zaten GatherFromScene() cagirip para/gun/stat vs. hepsini toplar.
            // ONEMLI DUZELTME: eskiden ActiveSlot -1 ise (hicbir slot secilmemisse) bu adim
            // SESSIZCE atlanip HICBIR SEY diske yazilmiyordu — "menuye donup devam edince
            // ayni yerden devam etmiyor" sikayetinin olasi sebebiydi. Artik PauseMenuController
            // ile tutarli sekilde, slot secilmemisse 0'a duser, HER ZAMAN bir yere kaydeder.
            int slot = SaveSystem.Instance.ActiveSlot;
            if (slot < 0)
            {
                slot = 0;
                Debug.LogWarning("[AnaMenuyeDonButonu] ActiveSlot -1 idi (hicbir slot secilmemisti), 0. slota kaydediliyor.");
            }
            SaveSystem.Instance.SaveToSlot(slot);
        }

        Time.timeScale = 1f; // bir panel/cutscene ortasinda basilmis olabilir
        SceneManager.LoadScene(anaMenuSahneAdi);
    }
}