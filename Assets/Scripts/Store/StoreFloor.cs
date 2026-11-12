using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madness.Store
{
    /// <summary>
    /// Mağazadaki tek bir katın altyapı tanımıdır. Oda sistemi kullanmaz;
    /// katlar kendi kimliği, sırası, rafları ve servis noktalarıyla kaydolur.
    /// </summary>
    [AddComponentMenu("Madness/Store/Floor")]
    public sealed class StoreFloor : MonoBehaviour
    {
        [SerializeField] private string floorId = "floor_0";
        [SerializeField, Range(0, MultiFloorStoreService.MaxFloorIndex)] private int floorIndex;
        [SerializeField] private bool enabledForGameplay = true;
        [Header("Kat noktaları")]
        [SerializeField] private Transform customerEntryPoint;
        [SerializeField] private Transform customerExitPoint;
        [SerializeField] private Transform servicePoint;
        [Header("Kat rafları")]
        [SerializeField] private List<StoreFloorShelfAnchor> shelves = new List<StoreFloorShelfAnchor>();

        private readonly List<StoreFloorShelfAnchor> registeredShelves = new List<StoreFloorShelfAnchor>();

        public string FloorId { get { return floorId; } }
        public int FloorIndex { get { return floorIndex; } }
        public bool IsEnabledForGameplay { get { return enabledForGameplay && isActiveAndEnabled; } }
        public Transform CustomerEntryPoint { get { return customerEntryPoint; } }
        public Transform CustomerExitPoint { get { return customerExitPoint; } }
        public Transform ServicePoint { get { return servicePoint; } }
        public IReadOnlyList<StoreFloorShelfAnchor> Shelves { get { return registeredShelves; } }

        private void OnEnable()
        {
            MultiFloorStoreService.EnsureExists().RegisterFloor(this);
            RegisterSerializedShelves();
        }

        private void OnDisable()
        {
            UnregisterSerializedShelves();
            if (MultiFloorStoreService.Instance != null)
                MultiFloorStoreService.Instance.UnregisterFloor(this);
        }

        private void RegisterSerializedShelves()
        {
            if (shelves == null) shelves = new List<StoreFloorShelfAnchor>();
            for (int i = 0; i < shelves.Count; i++) RegisterShelf(shelves[i]);
        }

        private void UnregisterSerializedShelves()
        {
            for (int i = registeredShelves.Count - 1; i >= 0; i--)
                UnregisterShelf(registeredShelves[i]);
        }

        public void RegisterShelf(StoreFloorShelfAnchor shelf)
        {
            if (shelf != null && !registeredShelves.Contains(shelf))
                registeredShelves.Add(shelf);
        }

        public void UnregisterShelf(StoreFloorShelfAnchor shelf)
        {
            if (shelf != null) registeredShelves.Remove(shelf);
        }

        public bool ContainsShelf(StoreFloorShelfAnchor shelf)
        {
            return shelf != null && registeredShelves.Contains(shelf);
        }

        public void AddShelfReference(StoreFloorShelfAnchor shelf)
        {
            if (shelves == null) shelves = new List<StoreFloorShelfAnchor>();
            if (shelf != null && !shelves.Contains(shelf)) shelves.Add(shelf);
            RegisterShelf(shelf);
        }

        public void RemoveShelfReference(StoreFloorShelfAnchor shelf)
        {
            if (shelves != null) shelves.Remove(shelf);
            UnregisterShelf(shelf);
        }

        private void OnValidate()
        {
            floorId = (floorId ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(floorId)) floorId = "floor_" + floorIndex;
            floorIndex = Mathf.Clamp(floorIndex, 0, MultiFloorStoreService.MaxFloorIndex);
        }
    }
}
