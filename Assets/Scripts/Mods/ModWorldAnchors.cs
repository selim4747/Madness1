using System.Collections.Generic;
using UnityEngine;

namespace Madness.Mods
{
    [DefaultExecutionOrder(-800)]
    public sealed class ModWorldAnchor : MonoBehaviour
    {
        [SerializeField] private string anchorId = "";
        public string AnchorId { get { return anchorId; } }

        private void OnEnable()
        {
            if (!string.IsNullOrWhiteSpace(anchorId)) ModWorldRegistry.Register(anchorId, transform);
        }

        private void OnDisable()
        {
            if (!string.IsNullOrWhiteSpace(anchorId)) ModWorldRegistry.Unregister(anchorId, transform);
        }
    }

    public static class ModWorldRegistry
    {
        private static readonly Dictionary<string, Transform> anchors = new Dictionary<string, Transform>();

        public static void Register(string id, Transform anchor)
        {
            if (string.IsNullOrWhiteSpace(id) || anchor == null) return;
            anchors[id] = anchor;
        }

        public static void Unregister(string id, Transform anchor)
        {
            Transform current;
            if (anchors.TryGetValue(id, out current) && current == anchor) anchors.Remove(id);
        }

        public static bool TryGet(string id, out Transform anchor)
        {
            if (anchors.TryGetValue(id, out anchor) && anchor != null && anchor.gameObject.activeInHierarchy)
                return true;
            anchor = null;
            return false;
        }

        public static bool TryGetPosition(string id, out Vector3 position)
        {
            Transform anchor;
            if (TryGet(id, out anchor)) { position = anchor.position; return true; }
            position = Vector3.zero;
            return false;
        }

        public static void Clear() { anchors.Clear(); }
    }
}
