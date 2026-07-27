using UnityEngine;

namespace WuxiaRoguelite.Architecture.Spawning
{
    [DisallowMultipleComponent]
    public sealed class SpawnRegion : MonoBehaviour
    {
        [SerializeField] private string regionId = "main_map";
        [SerializeField] private Vector3 size = new Vector3(10f, 0f, 10f);

        public string RegionId => regionId;

        public Vector3 RandomPoint()
        {
            Vector3 local = new Vector3(
                UnityEngine.Random.Range(-size.x * 0.5f, size.x * 0.5f),
                UnityEngine.Random.Range(-size.y * 0.5f, size.y * 0.5f),
                UnityEngine.Random.Range(-size.z * 0.5f, size.z * 0.5f));
            return transform.TransformPoint(local);
        }

        private void OnDrawGizmosSelected()
        {
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.8f, 0.6f, 0.35f);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.color = new Color(0.2f, 0.8f, 0.6f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = previous;
        }
    }
}
