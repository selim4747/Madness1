// ShopRoomController.cs — Dukkan odalarinin gorunurlugu
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;
using NavMeshPlus.Components;

public class ShopRoomController : MonoBehaviour
{
    public static ShopRoomController Instance { get; private set; }

    [System.Serializable]
    public class ShopRoom
    {
        [Tooltip("Bu odanin dukkan numarasi (1-6)")]
        public int shopNumber = 1;

        [Tooltip("Odanin tum gorsellerini iceren kok obje (duvarlar, zemin, hali, raflar)")]
        public GameObject roomRoot;

        [Tooltip("Bu oda acikken kameranin gidebilecegi sinirlar")]
        public Vector2 cameraMin;
        public Vector2 cameraMax;
    }

    [Header("Odalar (6 adet)")]
    [SerializeField] private List<ShopRoom> rooms = new List<ShopRoom>();

    [Header("Referanslar")]
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private CameraFollow cameraFollow;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        EconomyManager.Instance.OnShopsUnlockedChanged += HandleShopsChanged;
        HandleShopsChanged(EconomyManager.Instance.ShopsUnlocked);
    }

    private void OnDestroy()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnShopsUnlockedChanged -= HandleShopsChanged;
    }

    private void HandleShopsChanged(int shopsUnlocked)
    {
        // Odalari ac/kapat
        for (int i = 0; i < rooms.Count; i++)
        {
            ShopRoom room = rooms[i];
            if (room.roomRoot == null) continue;

            bool visible = room.shopNumber <= shopsUnlocked;
            room.roomRoot.SetActive(visible);
        }

        UpdateCameraBounds(shopsUnlocked);
        RebakeNavMesh();
    }

    /// <summary>Yeni oda acilinca NavMesh'i yeniden hesapla.</summary>
    private void RebakeNavMesh()
    {
        if (navMeshSurface == null) return;

        // Runtime bake — yeni odanin zemini ve haliya erisim eklenir
        navMeshSurface.BuildNavMesh();
    }

    private void UpdateCameraBounds(int shopsUnlocked)
    {
        if (cameraFollow == null) return;

        // En son acik odanin sinirlarini kullan
        for (int i = rooms.Count - 1; i >= 0; i--)
        {
            if (rooms[i].shopNumber != shopsUnlocked) continue;

            cameraFollow.SetBounds(rooms[i].cameraMin, rooms[i].cameraMax);
            return;
        }
    }

    /// <summary>Bir dukkan acik mi? (ShelfSetupTool ve UI kontrolleri icin)</summary>
    public bool IsShopOpen(int shopNumber)
    {
        return shopNumber <= EconomyManager.Instance.ShopsUnlocked;
    }
}
