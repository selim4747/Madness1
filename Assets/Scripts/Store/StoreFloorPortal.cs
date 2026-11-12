using UnityEngine;

namespace Madness.Store
{
    /// <summary>
    /// İki kat arasındaki merdiven, asansör veya başka geçiş bağlantısıdır.
    /// Henüz müşteri hareketine bağlanmaz; yalnızca rota altyapısını kaydeder.
    /// </summary>
    [AddComponentMenu("Madness/Store/Floor Portal")]
    public sealed class StoreFloorPortal : MonoBehaviour
    {
        [SerializeField] private string portalId = "stairs_0_1";
        [SerializeField] private StoreFloor fromFloor;
        [SerializeField] private StoreFloor toFloor;
        [SerializeField] private Transform fromPoint;
        [SerializeField] private Transform toPoint;
        [SerializeField] private bool bidirectional = true;
        [SerializeField] private bool enabledForGameplay = true;
        [SerializeField] private int traversalPriority;

        public string PortalId { get { return portalId; } }
        public StoreFloor FromFloor { get { return fromFloor; } }
        public StoreFloor ToFloor { get { return toFloor; } }
        public Transform FromPoint { get { return fromPoint != null ? fromPoint : transform; } }
        public Transform ToPoint { get { return toPoint != null ? toPoint : transform; } }
        public bool Bidirectional { get { return bidirectional; } }
        public bool IsEnabledForGameplay { get { return enabledForGameplay && isActiveAndEnabled; } }
        public int TraversalPriority { get { return traversalPriority; } }

        private void OnEnable()
        {
            MultiFloorStoreService.EnsureExists().RegisterPortal(this);
        }

        private void OnDisable()
        {
            if (MultiFloorStoreService.Instance != null)
                MultiFloorStoreService.Instance.UnregisterPortal(this);
        }

        public bool CanTravel(StoreFloor from, StoreFloor to)
        {
            if (!IsEnabledForGameplay || from == null || to == null) return false;
            if (from == fromFloor && to == toFloor) return true;
            return bidirectional && from == toFloor && to == fromFloor;
        }

        public Transform PointOn(StoreFloor floor)
        {
            if (floor == fromFloor) return FromPoint;
            if (floor == toFloor) return ToPoint;
            return null;
        }

        public StoreFloor OtherFloor(StoreFloor floor)
        {
            if (floor == fromFloor) return toFloor;
            if (bidirectional && floor == toFloor) return fromFloor;
            return null;
        }

        private void OnValidate()
        {
            portalId = (portalId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(portalId)) portalId = gameObject.name.ToLowerInvariant();
        }
    }
}
