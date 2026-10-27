using UnityEngine;
using UnityEngine.UI;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class Minimap : MonoBehaviour
    {
        public static Minimap Instance;

        [Header("SETUP")]
        public Camera MinimapCamera;
        [Tooltip("Usually the hero. Left empty, the minimap finds HeroController at runtime.")]
        public Transform Target;
        public RawImage DisplayImage;

        [Header("VIEW")]
        [Tooltip("How much of the world fits in the map. Larger means zoomed out.")]
        public float ViewSize = 25f;
        public float MinViewSize = 10f;
        public float MaxViewSize = 60f;
        public float CameraHeight = 40f;
        [Tooltip("On, the map turns with the player. Off, north is always up.")]
        public bool RotateWithPlayer = false;

        [Header("BLIPS")]
        public Material BlipMaterial;
        public Color Color_Player = new Color(0.25f, 0.85f, 1f);
        public Color Color_Enemy = new Color(0.95f, 0.25f, 0.25f);
        public Color Color_Item = new Color(1f, 0.85f, 0.3f);
        public Color Color_Door = new Color(0.7f, 0.55f, 0.35f);
        public Color Color_Chest = new Color(1f, 0.6f, 0.15f);
        public Color Color_Objective = new Color(0.6f, 1f, 0.4f);

        [Header("FULL SCREEN MAP")]
        public GameObject FullMapPanel;

        private Transform blipRoot;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (Target == null && HeroController.instance != null)
            {
                Target = HeroController.instance.transform;
            }

            if (MinimapCamera == null)
            {
                Debug.LogError("[ARPS] Minimap has no camera assigned. Assign a camera in the inspector.", this);
                enabled = false;
                return;
            }

            MinimapCamera.orthographic = true;
            MinimapCamera.orthographicSize = ViewSize;

            var rootGo = new GameObject("Minimap Blips");
            blipRoot = rootGo.transform;

            if (FullMapPanel != null) FullMapPanel.SetActive(false);
        }

        private void LateUpdate()
        {
            if (Target == null || MinimapCamera == null) return;

            MinimapCamera.transform.position = new Vector3(Target.position.x, Target.position.y + CameraHeight, Target.position.z);
            MinimapCamera.transform.rotation = Quaternion.Euler(90f, RotateWithPlayer ? Target.eulerAngles.y : 0f, 0f);
            MinimapCamera.orthographicSize = ViewSize;

            if (GameManager.Instance != null && GameManager.Instance.mapAction != null
                && GameManager.Instance.mapAction.WasReleasedThisFrame())
            {
                ToggleFullMap();
            }
        }

        public void Zoom(float delta)
        {
            ViewSize = Mathf.Clamp(ViewSize + delta, MinViewSize, MaxViewSize);
        }

        public void Click_ZoomIn() { Zoom(-5f); }
        public void Click_ZoomOut() { Zoom(5f); }

        public void ToggleFullMap()
        {
            if (FullMapPanel == null) return;

            bool opening = !FullMapPanel.activeSelf;
            FullMapPanel.SetActive(opening);

            ViewSize = opening ? Mathf.Min(MaxViewSize, ViewSize * 2.5f) : Mathf.Max(MinViewSize, ViewSize / 2.5f);
        }

        public Color GetColor(MinimapIconType type)
        {
            switch (type)
            {
                case MinimapIconType.Player: return Color_Player;
                case MinimapIconType.Enemy: return Color_Enemy;
                case MinimapIconType.Door: return Color_Door;
                case MinimapIconType.Chest: return Color_Chest;
                case MinimapIconType.Objective: return Color_Objective;
                default: return Color_Item;
            }
        }

        public Transform CreateBlip(MinimapIcon icon)
        {
            if (icon == null) return null;

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Blip_" + icon.IconType + "_" + icon.name;
            quad.layer = MinimapLayer;

            Collider quadCollider = quad.GetComponent<Collider>();
            if (quadCollider != null) Destroy(quadCollider);

            quad.transform.SetParent(blipRoot, false);
            quad.transform.localScale = Vector3.one * icon.Size;

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (BlipMaterial != null)
            {
                renderer.material = new Material(BlipMaterial);
            }
            renderer.material.color = GetColor(icon.IconType);

            return quad.transform;
        }

        public static int MinimapLayer
        {
            get
            {
                int layer = LayerMask.NameToLayer("Minimap");
                return layer >= 0 ? layer : 0;
            }
        }
    }
}
