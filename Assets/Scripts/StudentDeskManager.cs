// StudentDeskManager.cs — Tum ogrenci masalarinin merkezi yoneticisi
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class StudentDeskManager : MonoBehaviour
{
    public static StudentDeskManager Instance { get; private set; }

    [Header("Tum Ogrenci Masalari")]
    [Tooltip("Sahnedeki her masa objesini buraya surukle. Sira UI listesindeki sirayla ayni olur.")]
    [SerializeField] private List<StudentDeskController> tumMasalar = new List<StudentDeskController>();

    public List<StudentDeskController> AllDesks { get { return tumMasalar; } }
    public int TotalCount { get { return tumMasalar.Count; } }

    public List<StudentDeskController> UnlockedDesks
    {
        get
        {
            List<StudentDeskController> list = new List<StudentDeskController>();
            for (int i = 0; i < tumMasalar.Count; i++)
                if (tumMasalar[i] != null && tumMasalar[i].IsUnlocked) list.Add(tumMasalar[i]);
            return list;
        }
    }

    public int UnlockedCount { get { return UnlockedDesks.Count; } }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>StudentSpawner tarafindan cagrilir: acik ve bos (talebi olmayan) rastgele bir masada talep olusturmayi dener.
    /// Musait masa yoksa false doner (o "ogrenci sirasi" atlanir, hata degildir).</summary>
    public bool TryCreateRequestOnRandomDesk()
    {
        List<StudentDeskController> musaitler = new List<StudentDeskController>();

        for (int i = 0; i < tumMasalar.Count; i++)
        {
            StudentDeskController masa = tumMasalar[i];
            if (masa != null && masa.IsUnlocked && !masa.HasActiveRequest) musaitler.Add(masa);
        }

        if (musaitler.Count == 0) return false;

        StudentDeskController secilen = musaitler[Random.Range(0, musaitler.Count)];
        secilen.CreateRequest();
        return true;
    }

    /// <summary>Herhangi bir masanin aktif talebi var mi?</summary>
    public bool AnyDeskHasActiveRequest()
    {
        for (int i = 0; i < tumMasalar.Count; i++)
            if (tumMasalar[i] != null && tumMasalar[i].HasActiveRequest) return true;
        return false;
    }

    /// <summary>PlayerInteraction tarafindan cagrilir: oyuncuya en yakin, menzildeki, aktif talebi olan masa.</summary>
    public StudentDeskController FindNearestDeskWithRequest(Vector3 pos, float maxRange)
    {
        StudentDeskController nearest = null;
        float best = maxRange;

        for (int i = 0; i < tumMasalar.Count; i++)
        {
            StudentDeskController masa = tumMasalar[i];
            if (masa == null || !masa.HasActiveRequest) continue;

            float d = Vector2.Distance(pos, masa.transform.position);
            if (d <= best) { best = d; nearest = masa; }
        }
        return nearest;
    }

    /// <summary>Tum masalarin su an istedigi raflarin birlesik listesi (oyuncunun en yakin parlayan rafi bulmasi icin).</summary>
    public List<ShelfController> GetAllActiveRequestedShelves()
    {
        List<ShelfController> list = new List<ShelfController>();
        for (int i = 0; i < tumMasalar.Count; i++)
        {
            StudentDeskController masa = tumMasalar[i];
            if (masa == null || !masa.HasActiveRequest) continue;
            list.AddRange(masa.RequestedShelves);
        }
        return list;
    }

    /// <summary>Bir raf su an HERHANGI bir masa tarafindan isteniyor mu? (iki masa ayni rafi ayni anda istemesin diye)</summary>
    public bool IsShelfRequestedByAnyDesk(ShelfController shelf)
    {
        for (int i = 0; i < tumMasalar.Count; i++)
        {
            StudentDeskController masa = tumMasalar[i];
            if (masa != null && masa.RequestedShelves.Contains(shelf)) return true;
        }
        return false;
    }

    // ── Kayit koprusu ──

    public void ApplySave(SaveData data)
    {
        for (int i = 0; i < tumMasalar.Count; i++)
        {
            if (tumMasalar[i] == null) continue;

            bool unlocked = data.unlockedDeskIndices != null && data.unlockedDeskIndices.Contains(tumMasalar[i].DeskIndex);
            tumMasalar[i].ApplyUnlockState(unlocked);
        }
    }

    public void WriteToSave(SaveData data)
    {
        data.unlockedDeskIndices = new List<int>();
        for (int i = 0; i < tumMasalar.Count; i++)
            if (tumMasalar[i] != null && tumMasalar[i].IsUnlocked) data.unlockedDeskIndices.Add(tumMasalar[i].DeskIndex);
    }
}
