// GeciciHikayeKonuguController.cs — GeciciHikayeKonuguYoneticisi tarafindan spawn edilen,
// SADECE O GUNLUGUNE var olan gecici hikaye konugu. Sabit durur (hareket etmez). Oyuncu
// yaklasinca o gunun segmentindeki satirlari sirayla gosterir (ETK ile ilerletilir), en
// sonunda bir SECIM sunar. Gece (gun degisince) YONETICI tarafindan yok edilir — kendi
// kendine hicbir sekilde ayrilmaz/kaybolmaz.
//
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class GeciciHikayeKonuguController : MonoBehaviour
{
    private OzelMusteriHikayesiVerisi hikaye;
    private HikayeGunSegmenti buGunSegmenti;
    private bool dialogueShown = false;

    public void Initialize(OzelMusteriHikayesiVerisi h, HikayeGunSegmenti segment)
    {
        hikaye = h;
        buGunSegmenti = segment;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || dialogueShown || hikaye == null || buGunSegmenti == null) return;
        dialogueShown = true;
        Konus();
    }

    private void Konus()
    {
        List<DialogueLine> satirlar = new List<DialogueLine>();
        if (buGunSegmenti.onSatirlar != null)
        {
            for (int i = 0; i < buGunSegmenti.onSatirlar.Count; i++)
            {
                DialogueLine satir = new DialogueLine();
                satir.speakerName = hikaye.hedefMusteriIsmi;
                satir.text = buGunSegmenti.onSatirlar[i];
                // SESLENDIRME: bu satirlar kod icinde olusturuluyor, cinsiyet/yas grubu ELLE
                // atanmali — atanmazsa struct'in varsayilani (Kadin+Cocuk) kullanilir. Hedef
                // kisinin GERCEK NPCKimligi'siyle ayni olmasi icin hikaye.hedefCinsiyet/
                // hedefYasGrubu kullanilir (asset yazarken elle senkronize edilmis olmali).
                satir.konusanCinsiyeti = hikaye.hedefCinsiyet;
                satir.konusanYasGrubu = hikaye.hedefYasGrubu;
                satirlar.Add(satir);
            }
        }

        if (satirlar.Count > 0)
        {
            if (DialogueBox.Instance != null) DialogueBox.Instance.OnNoMoreLines += HandleOnSatirlarBitti;
            if (DialogueBox.Instance != null) DialogueBox.Instance.Show(satirlar, null, false);
        }
        else
        {
            SecimGoster();
        }
    }

    private void HandleOnSatirlarBitti()
    {
        if (DialogueBox.Instance != null) DialogueBox.Instance.OnNoMoreLines -= HandleOnSatirlarBitti;
        SecimGoster();
    }

    private void SecimGoster()
    {
        // YERELLEŞTIRME: sonSatir + secenek metinleri aktif dile gore cozulur.
        string sonSatirCozulmus = LokalizasyonYardimcisi.MetniCoz(buGunSegmenti.sonSatir, buGunSegmenti.sonSatirEkDiller);

        List<string> secenekMetinleri = new List<string>();
        if (buGunSegmenti.secenekler != null)
            for (int i = 0; i < buGunSegmenti.secenekler.Count; i++)
                secenekMetinleri.Add(LokalizasyonYardimcisi.MetniCoz(buGunSegmenti.secenekler[i].secenekMetni, buGunSegmenti.secenekler[i].ekDiller));

        if (DialogueBox.Instance == null) return;

        DialogueBox.Instance.SecenekliGoster(hikaye.hedefMusteriIsmi, sonSatirCozulmus, secenekMetinleri, (secilenIndex) =>
        {
            if (buGunSegmenti.secenekler == null || secilenIndex < 0 || secilenIndex >= buGunSegmenti.secenekler.Count) return;

            OzelHikayeSecenegi secilen = buGunSegmenti.secenekler[secilenIndex];
            if (GeciciHikayeKonuguYoneticisi.Instance != null)
                GeciciHikayeKonuguYoneticisi.Instance.SecimYapildi(hikaye, secilen);
        }, default(SesKlipSeti), hikaye.hedefCinsiyet, hikaye.hedefYasGrubu);
    }

    private void OnDestroy()
    {
        // Konusma ortasindayken gece basladiysa (yonetici bu objeyi yok ederken) asili
        // kalmis OnNoMoreLines aboneligini temizle — baska bir konusmaya karismasin.
        if (DialogueBox.Instance != null) DialogueBox.Instance.OnNoMoreLines -= HandleOnSatirlarBitti;
    }
}
