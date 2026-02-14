using UnityEngine;

namespace DodgingAgent.Scripts.Core
{
    public enum BoundsMode { Box, Mesh }

    /// <summary>
    /// Provides random spawn positions within bounds, keeping distance from walls.
    /// </summary>
    public class RandomizePosition : MonoBehaviour
    {
        [Header("Bounds")]
        [SerializeField] private BoundsMode mode = BoundsMode.Box;
        [SerializeField] private Vector3 boxSize = new Vector3(10f, 5f, 10f);
        [SerializeField] private Vector3 boxOffset = Vector3.zero;
        [SerializeField] private Collider meshCollider;

        [Header("Wall Avoidance")]
        [SerializeField] private float wallOffset = 2f;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        public Vector3 GetResetPosition()
        {
            Vector3 position = mode == BoundsMode.Box ? GetBoxPosition() : GetMeshPosition();
            return PushInside(position);
        }

        private Vector3 GetBoxPosition()
        {
            Vector3 halfSize = boxSize * 0.5f - Vector3.one * wallOffset;
            Vector3 localPos = new Vector3(
                Random.Range(-halfSize.x, halfSize.x),
                Random.Range(-halfSize.y, halfSize.y),
                Random.Range(-halfSize.z, halfSize.z)
            );
            return transform.TransformPoint(localPos + boxOffset);
        }

        private Vector3 GetMeshPosition()
        {
            if (!meshCollider)
            {
                Debug.LogWarning("[RandomizePosition] No mesh collider assigned");
                return transform.position;
            }

            Bounds bounds = meshCollider.bounds;
            bounds.Expand(-wallOffset * 2f);
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        private static readonly Vector3[] Directions =
            { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };

        private Vector3 PushInside(Vector3 position)
        {
            if (mode == BoundsMode.Box || !meshCollider || wallOffset <= 0f)
                return position;

            int layer = 1 << meshCollider.gameObject.layer;

            // 6-direction raycast - push away from any walls within wallOffset
            foreach (var dir in Directions)
            {
                if (Physics.Raycast(position, dir, out RaycastHit hit, wallOffset, layer))
                {
                    position -= dir * (wallOffset - hit.distance);
                }
            }

            return position;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);

            MeshCollider mc = meshCollider as MeshCollider;
            if (mode == BoundsMode.Mesh && mc && mc.sharedMesh)
            {
                Gizmos.matrix = mc.transform.localToWorldMatrix;
                Gizmos.DrawMesh(mc.sharedMesh);
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
                Gizmos.DrawWireMesh(mc.sharedMesh);
            }
            else
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxOffset, boxSize);
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
                Gizmos.DrawWireCube(boxOffset, boxSize);
            }
        }
    }
}
