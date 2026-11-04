// ISohbetHedefi.cs — MusteriSohbetRadari (magaza) ve BahceSohbetRadari (bahce) icin
// ORTAK arayuz. PlayerInteraction, ikisini de AYNI listede tutup ayni sekilde
// cagirabilsin diye — hangi tip oldugunu bilmesine gerek kalmaz.
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;

public interface ISohbetHedefi
{
    void SohbetEt();
    Transform SohbetTransformuAl();
}