using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Target Marker")]
    public class BodycamTargetMarker : MonoBehaviour
    {
        [Header("LABEL")]
        public string targetName = "TARGET";
        public bool showLabel = true;

        [Header("BOUNDS")]
        [Tooltip("Leave empty to automatically use all Renderers inside this target.")]
        public Renderer[] targetRenderers = new Renderer[0];

        [Min(0f)]
        [Tooltip("Extra world-space space added around the detected target.")]
        public float boundsPadding = 0.02f;

        public bool TryGetWorldBounds(out Bounds bounds)
        {
            Renderer[] renderers = targetRenderers;

            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();

            bool foundRenderer = false;
            bounds = new Bounds(transform.position, Vector3.zero);

            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null || !targetRenderer.enabled)
                    continue;

                if (!foundRenderer)
                {
                    bounds = targetRenderer.bounds;
                    foundRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(targetRenderer.bounds);
                }
            }

            if (!foundRenderer)
                return false;

            bounds.Expand(boundsPadding * 2f);
            return true;
        }
    }
}
