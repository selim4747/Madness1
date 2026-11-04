// HikayeKonuguData.cs — Ozel gunlerde bahce kosesinde beliren, kendi gorunumu ve kamera
// odakli diyalogu olan hikaye konugu (gonderme karakterleri, turistler vs.)
// Tanisildiktan SONRA istege bagli olarak duzenli musteri havuzuna katilabilir.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Konuk_", menuName = "Delilik/Hikaye Konugu")]
public class HikayeKonuguData : ScriptableObject
{
    [Header("Kimlik")]
    [Tooltip("Kayit sisteminde bu konugu benzersiz tanimlayan sabit id (orn. 'konuk_gezgin').")]
    public string konukId = "konuk_001";
    public string isim = "Gezgin";

    [Header("Gorunum")]
    [Tooltip("Bu konuga OZEL prefab — farkli gorunmesini saglar. StoryNPCController icermeli.")]
    public GameObject konukPrefab;

    [Header("Hangi Gunlerde Gelir")]
    [Tooltip("Tam olarak bu gunlerde gelir (orn. 12, 33). Birden fazla yazabilirsin.")]
    public List<int> gorunecegiGunler = new List<int>();
    [Tooltip("Her KAC gunde bir gelsin (0 = kullanma).")]
    public int herKacGundeBir = 0;
    [Tooltip("Bu gun araliginda gelebilir (ikisi de 0 ise kullanilmaz).")]
    public int enErkenGun = 0;
    public int enGecGun = 0;

    [Header("Diyalog (kamera bu konuga odaklanir)")]
    public bool focusCamera = true;
    public Vector2 cameraFocusOffset = Vector2.zero;
    [Tooltip("Konusurken sag-sol gidip gelsin mi (hareketli anlatim).")]
    public bool hareketliAnlatim = false;
    public float hareketMesafesi = 2f;
    public DialogueLine[] lines;

    [Header("Tanisildiktan Sonra")]
    [Tooltip("ISARETLI: bu konukla bir kez konusunca, artik NORMAL musteri olarak da gelmeye baslar. ISARETSIZ: sadece kendi ozel gunlerinde gelmeye devam eder.")]
    public bool tanisincaDuzenliGelir = false;

    [Tooltip("Duzenli gelmeye basladiginda kullanilacak musteri kimligi (sevdigi turler, diyaloglar, kisilik vs.). tanisincaDuzenliGelir ISARETLIYSE doldurulmali.")]
    public NPCKimligi duzenliKimlik;

    [Tooltip("Duzenli gelmeye basladiginda hangi musteri tipi havuzuna katilacagi.")]
    public MusteriTipi katilacagiTip = MusteriTipi.Normal;

    /// <summary>Bu konuk belirtilen gunde gelir mi?</summary>
    public bool BuGunGelirMi(int gun)
    {
        if (gorunecegiGunler != null && gorunecegiGunler.Contains(gun)) return true;
        if (herKacGundeBir > 0 && gun % herKacGundeBir == 0) return true;
        if (enErkenGun > 0 && enGecGun > 0 && gun >= enErkenGun && gun <= enGecGun) return true;
        return false;
    }
}