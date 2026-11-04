// StoryCutsceneData.cs — "Video + anlatim diyalogu" hikaye anlari (ScriptableObject)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class CutsceneCaption
{
    [Tooltip("Videonun kacinci saniyesinde bu altyazi ekrana gelsin.")]
    public float showAtSeconds = 0f;

    [Tooltip("Bos birakilabilir (orn. sadece bir 'Anlatici' varsa hep ayni isim olur, tekrar yazmana gerek yok).")]
    public string speakerName = "Anlatici";

    [TextArea(2, 4)]
    public string text;
}

[CreateAssetMenu(fileName = "Story_Cutscene_", menuName = "Delilik/Story Cutscene Data")]
public class StoryCutsceneData : ScriptableObject
{
    [Header("Kimlik")]
    public string eventId = "cutscene_001";

    [Tooltip("Bu cutscene hangi gunun sonunda tetiklenecek. 0 = gun bazli degil, elle tetiklenir.")]
    public int triggerDay = 1;

    [Tooltip("Bu cutscene oyunun finalini tetikliyorsa isaretle.")]
    public bool isFinale = false;

    [Header("Video")]
    public VideoClip videoClip;

    [Tooltip("Videonun kendi ses parcasi yoksa (orn. sessiz/render goruntu) ayri bir seslendirme dosyasi burada video ile birlikte calar.")]
    public AudioClip narratorVoiceOver;

    [Header("Altyazi / Anlatim Satirlari")]
    [Tooltip("Video oynarken belirlenen saniyelerde ekranda beliren altyazilar. Tiklamayla ilerlemez, video akisina gore otomatik gecer.")]
    public CutsceneCaption[] captions;

    [Header("Akis Ayarlari")]
    [Tooltip("Oyuncu bu cutsceneyi atlayabilsin mi?")]
    public bool canSkip = true;

    [Tooltip("Video basinda/sonunda kararma suresi (saniye). 0 = kararma yok.")]
    public float fadeInSeconds = 0.5f;
    public float fadeOutSeconds = 0.5f;
}
