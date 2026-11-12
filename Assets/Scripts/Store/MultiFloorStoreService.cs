using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madness.Store
{
    /// <summary>
    /// Oda sisteminden bağımsız mağaza kat registry'si.
    /// Kat indeksleri 0-9 arasındadır; 0 zemin kat olarak önerilir.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Madness/Store/Multi-Floor Store Service")]
    public sealed class MultiFloorStoreService : MonoBehaviour
    {
        public const int MaxFloorCount = 10;
        public const int MaxFloorIndex = MaxFloorCount - 1;

        public static MultiFloorStoreService Instance { get; private set; }

        [SerializeField] private StoreFloor configuredGroundFloor;
        [SerializeField] private List<StoreFloor> configuredFloors = new List<StoreFloor>();
        [SerializeField] private List<StoreFloorPortal> configuredPortals = new List<StoreFloorPortal>();

        private readonly Dictionary<string, StoreFloor> floorsById = new Dictionary<string, StoreFloor>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, StoreFloor> floorsByIndex = new Dictionary<int, StoreFloor>();
        private readonly List<StoreFloorPortal> portals = new List<StoreFloorPortal>();

        public StoreFloor GroundFloor
        {
            get
            {
                if (configuredGroundFloor != null && configuredGroundFloor.IsEnabledForGameplay) return configuredGroundFloor;
                StoreFloor floor;
                return floorsByIndex.TryGetValue(0, out floor) ? floor : null;
            }
        }

        public IReadOnlyCollection<StoreFloor> Floors { get { return floorsById.Values; } }
        public IReadOnlyList<StoreFloorPortal> Portals { get { return portals; } }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            RegisterConfiguredObjects();
        }

        private void Start()
        {
            // Kat ve portal objeleri servis objesinden sonra etkinleşmiş olabilir.
            StoreFloor[] sceneFloors = FindObjectsByType<StoreFloor>();
            for (int i = 0; i < sceneFloors.Length; i++) RegisterFloor(sceneFloors[i]);
            StoreFloorPortal[] scenePortals = FindObjectsByType<StoreFloorPortal>();
            for (int i = 0; i < scenePortals.Length; i++) RegisterPortal(scenePortals[i]);
            ValidateInfrastructure(true);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static MultiFloorStoreService EnsureExists()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("MultiFloorStoreService");
            return go.AddComponent<MultiFloorStoreService>();
        }

        private void RegisterConfiguredObjects()
        {
            if (configuredFloors != null)
                for (int i = 0; i < configuredFloors.Count; i++) RegisterFloor(configuredFloors[i]);
            if (configuredPortals != null)
                for (int i = 0; i < configuredPortals.Count; i++) RegisterPortal(configuredPortals[i]);
        }

        public bool RegisterFloor(StoreFloor floor)
        {
            if (floor == null || string.IsNullOrWhiteSpace(floor.FloorId)) return false;
            if (floor.FloorIndex < 0 || floor.FloorIndex > MaxFloorIndex)
            {
                Debug.LogError("[MultiFloorStore] Kat indeksi 0-9 arasında olmalı: " + floor.name);
                return false;
            }

            StoreFloor existing;
            if (floorsById.TryGetValue(floor.FloorId, out existing) && existing != floor)
            {
                Debug.LogError("[MultiFloorStore] Tekrarlanan floorId: " + floor.FloorId);
                return false;
            }
            if (floorsByIndex.TryGetValue(floor.FloorIndex, out existing) && existing != floor)
            {
                Debug.LogError("[MultiFloorStore] Tekrarlanan floorIndex: " + floor.FloorIndex);
                return false;
            }

            floorsById[floor.FloorId] = floor;
            floorsByIndex[floor.FloorIndex] = floor;
            return true;
        }

        public void UnregisterFloor(StoreFloor floor)
        {
            if (floor == null) return;
            StoreFloor current;
            if (floorsById.TryGetValue(floor.FloorId, out current) && current == floor) floorsById.Remove(floor.FloorId);
            if (floorsByIndex.TryGetValue(floor.FloorIndex, out current) && current == floor) floorsByIndex.Remove(floor.FloorIndex);
        }

        public bool RegisterPortal(StoreFloorPortal portal)
        {
            if (portal == null || portals.Contains(portal)) return portal != null;
            if (portal.FromFloor == null || portal.ToFloor == null || portal.FromFloor == portal.ToFloor)
            {
                Debug.LogError("[MultiFloorStore] Portal iki farklı kat arasında olmalı: " + portal.name);
                return false;
            }
            portals.Add(portal);
            return true;
        }

        public void UnregisterPortal(StoreFloorPortal portal)
        {
            if (portal != null) portals.Remove(portal);
        }

        public bool TryGetFloor(string floorId, out StoreFloor floor)
        {
            return floorsById.TryGetValue(floorId ?? string.Empty, out floor) && floor != null;
        }

        public bool TryGetFloor(int floorIndex, out StoreFloor floor)
        {
            return floorsByIndex.TryGetValue(floorIndex, out floor) && floor != null;
        }

        public bool TryFindRoute(StoreFloor start, StoreFloor destination, out StoreFloorRoute route)
        {
            route = null;
            if (start == null || destination == null || !start.IsEnabledForGameplay || !destination.IsEnabledForGameplay) return false;
            if (start == destination)
            {
                route = new StoreFloorRoute(start, destination, new List<StoreFloorPortal>());
                return true;
            }

            Queue<StoreFloor> queue = new Queue<StoreFloor>();
            Dictionary<StoreFloor, StoreFloor> previousFloor = new Dictionary<StoreFloor, StoreFloor>();
            Dictionary<StoreFloor, StoreFloorPortal> previousPortal = new Dictionary<StoreFloor, StoreFloorPortal>();
            HashSet<StoreFloor> visited = new HashSet<StoreFloor>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                StoreFloor current = queue.Dequeue();
                for (int i = 0; i < portals.Count; i++)
                {
                    StoreFloorPortal portal = portals[i];
                    StoreFloor next = portal != null ? portal.OtherFloor(current) : null;
                    if (next == null || !next.IsEnabledForGameplay || visited.Contains(next)) continue;
                    visited.Add(next);
                    previousFloor[next] = current;
                    previousPortal[next] = portal;
                    if (next == destination)
                    {
                        List<StoreFloorPortal> path = new List<StoreFloorPortal>();
                        StoreFloor cursor = destination;
                        while (cursor != start)
                        {
                            path.Add(previousPortal[cursor]);
                            cursor = previousFloor[cursor];
                        }
                        path.Reverse();
                        route = new StoreFloorRoute(start, destination, path);
                        return true;
                    }
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        public bool TryFindRoute(string startFloorId, string destinationFloorId, out StoreFloorRoute route)
        {
            StoreFloor start;
            StoreFloor destination;
            route = null;
            return TryGetFloor(startFloorId, out start) && TryGetFloor(destinationFloorId, out destination) && TryFindRoute(start, destination, out route);
        }

        public string ValidateInfrastructure(bool logErrors)
        {
            if (floorsById.Count == 0) return "Hiç kat kayıtlı değil.";
            if (GroundFloor == null) return "Zemin kat bulunamadı. floorIndex=0 olan bir kat ekleyin.";
            foreach (StoreFloorPortal portal in portals)
            {
                if (portal == null || portal.FromFloor == null || portal.ToFloor == null)
                {
                    if (logErrors) Debug.LogError("[MultiFloorStore] Eksik portal bağlantısı.");
                    return "Eksik portal bağlantısı var.";
                }
                if (!floorsById.ContainsKey(portal.FromFloor.FloorId) || !floorsById.ContainsKey(portal.ToFloor.FloorId))
                {
                    if (logErrors) Debug.LogError("[MultiFloorStore] Portal kayıtlı olmayan kata bağlanıyor: " + portal.name);
                    return "Portal kayıtlı olmayan kata bağlanıyor.";
                }
            }
            return null;
        }
    }

    [Serializable]
    public sealed class StoreFloorRoute
    {
        public StoreFloor Start { get; private set; }
        public StoreFloor Destination { get; private set; }
        public IReadOnlyList<StoreFloorPortal> Portals { get; private set; }

        public StoreFloorRoute(StoreFloor start, StoreFloor destination, List<StoreFloorPortal> portals)
        {
            Start = start;
            Destination = destination;
            Portals = portals ?? new List<StoreFloorPortal>();
        }
    }
}
