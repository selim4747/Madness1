// StoryDialogueData.cs — "Kameranin bir NPC'ye odaklandigi" hikaye anlari (ScriptableObject)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

[CreateAssetMenu(fileName = "Story_Dialogue_", menuName = "Delilik/Story Dialogue Data")]
public class StoryDialogueData : ScriptableObject
{
    [Header("Kimlik")]
    [Tooltip("Kayit sisteminde bu olayi benzersiz tanimlayan sabit id. Sonradan gun numarasi disi tetikleyiciler eklersek (orn. 'ilk kez X yapinca') bu id lazim olacak.")]
    public string eventId = "story_001";

    [Tooltip("Bu diyalog hangi gunun sonunda tetiklenecek. 0 = gun bazli degil, elle tetiklenir.")]
    public int triggerDay = 1;

    [Tooltip("Bu diyalog oyunun finalini tetikliyorsa isaretle (StoryManager.OnFinaleCompleted cagrilir).")]
    public bool isFinale = false;

    [Header("Konusan Karakter")]
    public string speakerName = "Afor";
    [Tooltip("Diyalog kutusunda gosterilecek portre (opsiyonel, DialogueBox portre alani eklenince kullanilir).")]
    public Sprite speakerPortrait;

    [Tooltip("Sahnede canlandirilacak NPC prefabi. Bos birakilirsa StoryManager'daki varsayilan lore NPC prefabi kullanilir.")]
    public GameObject npcPrefabOverride;

    [Header("Kamera Odagi")]
    [Tooltip("Diyalog baslarken kamera bu NPC'ye kaysin mi? Kapatilirsa kamera oyuncuda kalir.")]
    public bool focusCamera = true;

    [Tooltip("Kameranin NPC'ye odaklanirken uygulayacagi ek kaydirma (orn. NPC'yi ekranin solunda tutmak icin).")]
    public Vector2 cameraFocusOffset = Vector2.zero;

    [Header("Hareketli Anlatim (ozel karakterler icin — orn. Afor)")]
    [Tooltip("Isaretlenirse, konusma boyunca NPC kendi kendine sag-sol gidip gelir. Kamera zaten NPC'yi takip ettigi icin ekstra bir sey yapman gerekmez, otomatik takip eder.")]
    public bool hareketliAnlatim = false;

    [Tooltip("Baslangic noktasindan ne kadar saga/sola gidecegi (birim).")]
    public float hareketMesafesi = 2f;

    [Header("Diyalog Satirlari")]
    [Tooltip("Sirayla oynatilacak satirlar. Her satirin kendi konusmacisi olabilir — karsilikli konusma (Afor + Oyuncu) icin kullanisli.")]
    public DialogueLine[] lines;
}