// ISohbetDuraklatilabilir.cs — Konuşma sırasında hareketi geçici durdurulabilen hedefler
// NJG Games | Delilik | Unity 6 | UTF-8

/// <summary>
/// PlayerInteraction, bu arayüzü uygulayan konuşma hedeflerini otomatik olarak
/// konuşma başında durdurur ve DialogueBox kapanınca devam ettirir.
/// Yeni öğrenci/NPC sohbet hedefleri bu arayüzü uygulayabilir.
/// </summary>
public interface ISohbetDuraklatilabilir
{
    void SohbetIcinDuraklat(float minimumSure = 2f);
    void SohbetiBitir();
}
