using UnityEngine;

public class Z_Sabitleyici : MonoBehaviour
{
    void LateUpdate()
    {
        // Pozisyonu al, Z eksenini acımadan 0 yap ve geri ata!
        Vector3 simdikiPozisyon = transform.position;

        if (simdikiPozisyon.z != 0f)
        {
            simdikiPozisyon.z = 0f;
            transform.position = simdikiPozisyon;
        }
    }
}