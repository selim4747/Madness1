using UnityEngine;

namespace Madness.Store
{
    /// <summary>
    /// Mevcut ShelfController'ı kat altyapısına bağlayan hafif adaptördür.
    /// Mevcut raf/kitap/stok akışını değiştirmez.
    /// </summary>
    [AddComponentMenu("Madness/Store/Floor Shelf Anchor")]
    public sealed class StoreFloorShelfAnchor : MonoBehaviour
    {
        [SerializeField] private string shelfId = "shelf_001";
        [SerializeField] private StoreFloor floor;
        [SerializeField] private Transform approachPoint;
        [SerializeField] private bool availableForCustomers = true;

        public string ShelfId { get { return shelfId; } }
        public StoreFloor Floor { get { return floor; } }
        public Transform ApproachPoint { get { return approachPoint != null ? approachPoint : transform; } }
        public bool AvailableForCustomers { get { return availableForCustomers && isActiveAndEnabled; } }
        public ShelfController ExistingShelf { get { return GetComponentInChildren<ShelfController>(true); } }

        private void Awake()
        {
            if (floor == null) floor = GetComponentInParent<StoreFloor>();
        }

        private void OnEnable()
        {
            if (floor == null) floor = GetComponentInParent<StoreFloor>();
            if (floor != null) floor.RegisterShelf(this);
        }

        private void OnDisable()
        {
            if (floor != null) floor.UnregisterShelf(this);
        }

        private void OnValidate()
        {
            shelfId = (shelfId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(shelfId)) shelfId = gameObject.name.ToLowerInvariant();
            if (floor == null) floor = GetComponentInParent<StoreFloor>();
        }
    }
}
