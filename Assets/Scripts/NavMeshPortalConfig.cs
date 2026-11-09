using UnityEngine;

/// <summary>
/// NavMesh geçit ışınlama koordinatları.
/// Haritayı düzeltince veya Off-Mesh Link ekleyince bu sınıfı kaldırabilirsin.
/// </summary>
[CreateAssetMenu(fileName = "NavMeshPortalConfig", menuName = "Madness/NavMesh Portal Config")]
public class NavMeshPortalConfig : ScriptableObject
{
    [Header("Geçit 1")]
    public float gecit1MinY = 44.9f;
    public float gecit1MaxY = 45.1f;
    public float gecit1HedefY = 41f;

    [Header("Geçit 2")]
    public float gecit2MinY = 41.9f;
    public float gecit2MaxY = 42.1f;
    public float gecit2HedefY = 46f;

    [Header("Genel")]
    public float partialPathIsinlamaSaniye = 5f;
}
