using System.Collections.Generic;
using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Low FPS Target Boxes")]
    public class BodycamLowFPSTargetBoxes : MonoBehaviour
    {
        class TrackedTarget
        {
            public BodycamTargetMarker marker;
            public Rect screenRect;
            public bool visible;
        }

        [Header("REFERENCES")]
        public Camera detectionCamera;

        [Header("DETECTION")]
        [Min(0.1f)]
        public float maximumDistance = 60f;

        public bool requireLineOfSight = true;
        public LayerMask occlusionMask = ~0;

        [Min(0.1f)]
        [Tooltip("How often the target boxes update. Lower values create a low-FPS bodycam look.")]
        public float trackingFPS = 8f;

        [Min(0.1f)]
        public float searchForTargetsInterval = 2f;

        [Header("BOX APPEARANCE")]
        public Color boxColor = new Color(0.1f, 1f, 0.2f, 0.9f);

        [Min(1f)]
        public float lineThickness = 2f;

        [Min(0f)]
        public float screenPadding = 5f;

        [Min(1f)]
        [Tooltip("Rounds box positions to larger pixel steps for a digital low-FPS appearance.")]
        public float pixelSnap = 2f;

        public int labelFontSize = 13;

        readonly List<TrackedTarget> trackedTargets = new List<TrackedTarget>();

        Texture2D whiteTexture;
        GUIStyle labelStyle;
        float nextTrackingUpdate;
        float nextTargetSearch;

        void Awake()
        {
            if (detectionCamera == null)
                detectionCamera = GetComponent<Camera>();

            if (detectionCamera == null)
                detectionCamera = Camera.main;

            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();

            labelStyle = new GUIStyle
            {
                fontSize = labelFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerLeft
            };

            FindTargets();
        }

        void Update()
        {
            if (Time.unscaledTime >= nextTargetSearch)
            {
                FindTargets();
                nextTargetSearch = Time.unscaledTime + searchForTargetsInterval;
            }

            if (Time.unscaledTime < nextTrackingUpdate)
                return;

            UpdateTargetBoxes();
            nextTrackingUpdate = Time.unscaledTime + 1f / trackingFPS;
        }

        void FindTargets()
        {
            BodycamTargetMarker[] markers = FindObjectsOfType<BodycamTargetMarker>();
            trackedTargets.Clear();

            foreach (BodycamTargetMarker marker in markers)
            {
                trackedTargets.Add(new TrackedTarget
                {
                    marker = marker,
                    visible = false
                });
            }
        }

        void UpdateTargetBoxes()
        {
            if (detectionCamera == null)
                return;

            foreach (TrackedTarget trackedTarget in trackedTargets)
            {
                trackedTarget.visible = TryCalculateTargetRect(
                    trackedTarget.marker,
                    out trackedTarget.screenRect
                );
            }
        }

        bool TryCalculateTargetRect(BodycamTargetMarker marker, out Rect result)
        {
            result = default;

            if (marker == null || !marker.isActiveAndEnabled)
                return false;

            if (!marker.TryGetWorldBounds(out Bounds bounds))
                return false;

            Vector3 cameraPosition = detectionCamera.transform.position;
            float distance = Vector3.Distance(cameraPosition, bounds.center);

            if (distance > maximumDistance)
                return false;

            Vector3 centerViewport = detectionCamera.WorldToViewportPoint(bounds.center);

            if (centerViewport.z <= 0f ||
                centerViewport.x < 0f || centerViewport.x > 1f ||
                centerViewport.y < 0f || centerViewport.y > 1f)
            {
                return false;
            }

            if (requireLineOfSight && !HasLineOfSight(marker, bounds.center, distance))
                return false;

            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, max.y, max.z)
            };

            float minimumX = float.MaxValue;
            float minimumY = float.MaxValue;
            float maximumX = float.MinValue;
            float maximumY = float.MinValue;

            foreach (Vector3 corner in corners)
            {
                Vector3 screenPoint = detectionCamera.WorldToScreenPoint(corner);

                if (screenPoint.z <= 0f)
                    return false;

                float guiY = Screen.height - screenPoint.y;
                minimumX = Mathf.Min(minimumX, screenPoint.x);
                minimumY = Mathf.Min(minimumY, guiY);
                maximumX = Mathf.Max(maximumX, screenPoint.x);
                maximumY = Mathf.Max(maximumY, guiY);
            }

            minimumX = Snap(minimumX - screenPadding);
            minimumY = Snap(minimumY - screenPadding);
            maximumX = Snap(maximumX + screenPadding);
            maximumY = Snap(maximumY + screenPadding);

            result = Rect.MinMaxRect(
                Mathf.Clamp(minimumX, 0f, Screen.width),
                Mathf.Clamp(minimumY, 0f, Screen.height),
                Mathf.Clamp(maximumX, 0f, Screen.width),
                Mathf.Clamp(maximumY, 0f, Screen.height)
            );

            return result.width > 2f && result.height > 2f;
        }

        bool HasLineOfSight(
            BodycamTargetMarker marker,
            Vector3 targetCenter,
            float targetDistance
        )
        {
            Vector3 direction = (targetCenter - detectionCamera.transform.position).normalized;

            if (!Physics.Raycast(
                    detectionCamera.transform.position,
                    direction,
                    out RaycastHit hit,
                    targetDistance + 0.1f,
                    occlusionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.transform == marker.transform ||
                   hit.transform.IsChildOf(marker.transform);
        }

        float Snap(float value)
        {
            return Mathf.Round(value / pixelSnap) * pixelSnap;
        }

        void OnGUI()
        {
            if (whiteTexture == null)
                return;

            GUI.color = boxColor;
            labelStyle.normal.textColor = boxColor;
            labelStyle.fontSize = labelFontSize;

            foreach (TrackedTarget trackedTarget in trackedTargets)
            {
                if (!trackedTarget.visible || trackedTarget.marker == null)
                    continue;

                DrawBox(trackedTarget.screenRect);

                if (trackedTarget.marker.showLabel &&
                    !string.IsNullOrEmpty(trackedTarget.marker.targetName))
                {
                    Rect labelRect = new Rect(
                        trackedTarget.screenRect.x,
                        trackedTarget.screenRect.y - labelFontSize - 4f,
                        trackedTarget.screenRect.width,
                        labelFontSize + 4f
                    );

                    GUI.Label(labelRect, trackedTarget.marker.targetName, labelStyle);
                }
            }

            GUI.color = Color.white;
        }

        void DrawBox(Rect rect)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, lineThickness), whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - lineThickness, rect.width, lineThickness), whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, lineThickness, rect.height), whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - lineThickness, rect.y, lineThickness, rect.height), whiteTexture);
        }

        void OnDestroy()
        {
            if (whiteTexture != null)
                Destroy(whiteTexture);
        }
    }
}
