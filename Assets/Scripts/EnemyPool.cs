// EnemyPool.cs — Dusman nesne havuzu (mobil performans)
// NJG Games | Delilik | Unity 6 | UTF-8 with BOM
using UnityEngine;
using System.Collections.Generic;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    [System.Serializable]
    public class PoolEntry
    {
        public EnemyType type;
        public GameObject prefab;
        public int prewarmCount = 8;
    }

    [Header("Havuz tanimlari (4 canavar tipi)")]
    [SerializeField] private List<PoolEntry> entries = new List<PoolEntry>();

    private readonly Dictionary<EnemyType, Queue<GameObject>> pools =
        new Dictionary<EnemyType, Queue<GameObject>>();
    private readonly Dictionary<EnemyType, GameObject> prefabLookup =
        new Dictionary<EnemyType, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildPools();
    }

    private void BuildPools()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            PoolEntry entry = entries[i];
            if (entry.prefab == null) continue;

            prefabLookup[entry.type] = entry.prefab;
            Queue<GameObject> queue = new Queue<GameObject>();

            for (int j = 0; j < entry.prewarmCount; j++)
                queue.Enqueue(CreateNew(entry.prefab));

            pools[entry.type] = queue;
        }
    }

    private GameObject CreateNew(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, transform);
        go.SetActive(false);
        return go;
    }

    /// <summary>Havuzdan bir dusman alir. Havuz bossa yenisini uretir.</summary>
    public GameObject Get(EnemyType type, Vector3 position)
    {
        Queue<GameObject> queue;
        if (!pools.TryGetValue(type, out queue))
        {
            Debug.LogError("[EnemyPool] Tanimsiz dusman tipi: " + type);
            return null;
        }

        GameObject go = queue.Count > 0 ? queue.Dequeue() : CreateNew(prefabLookup[type]);

        go.transform.position = position;
        go.SetActive(true);

        // NavMeshAgent'i dogru konuma isinla. EnemyPool havuzu isitirken (prewarm) NavMesh
        // disinda bir noktada Instantiate ettiyse agent HIC NavMesh'e baglanamamis olabilir
        // ("Failed to create agent because it is not close enough to the NavMesh" uyarisi).
        // Eskiden burada "agent.isOnNavMesh ise Warp" diyorduk — ama isOnNavMesh zaten hep
        // false kalirsa asla duzelme sansi olmuyordu. Warp'i KOSULSUZ deniyoruz; dogru
        // spawn noktasinda (NavMesh uzerinde) cagirinca agent'i oraya yeniden baglar.
        UnityEngine.AI.NavMeshAgent agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.Warp(position);

        return go;
    }

    /// <summary>Olen dusman havuza geri doner (Destroy edilmez).</summary>
    public void Return(EnemyType type, GameObject go)
    {
        if (go == null) return;

        UnityEngine.AI.NavMeshAgent agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();

        go.SetActive(false);
        go.transform.SetParent(transform);

        Queue<GameObject> queue;
        if (!pools.TryGetValue(type, out queue))
        {
            queue = new Queue<GameObject>();
            pools[type] = queue;
        }
        queue.Enqueue(go);
    }
}