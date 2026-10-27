using UnityEngine;

namespace RealisticBodycam
{
    /// <summary>
    /// Controls what the first-person camera sees of the full-body model.
    /// Put this on the model (the object with the Animator).
    ///
    ///  - Melee equipped:  the whole body is visible EXCEPT the head.
    ///  - Melee not equipped: the head and the arms are hidden (your gun viewmodel arms are used instead).
    ///  - While melee is equipped the chest tilts with the camera pitch, so swings follow where you look.
    ///
    /// Runs in LateUpdate so it applies after the Animator.
    /// Empty bone fields are found once in Awake by name (works on the pack's models).
    /// </summary>
    [AddComponentMenu("Realistic Bodycam/Body Visibility")]
    public class BodycamBodyVisibility : MonoBehaviour
    {
        public BodycamBodyMelee melee;
        public Camera playerCamera;

        [Header("BONES")]
        public Transform head;

        [Tooltip("Both upper-arm bones (B-upperArm.L and B-upperArm.R). Hidden when melee is not equipped.")]
        public Transform[] armRoots;

        [Tooltip("B-chest. Used for the pitch tilt during melee.")]
        public Transform chest;

        [Header("PITCH (melee only)")]
        public bool followCameraPitch = true;

        [Range(0f, 1f)]
        public float pitchWeight = 0.4f;

        public float maxLookUp = 30f;
        public float maxLookDown = 20f;

        Transform selfTransform;
        Transform camTransform;

        void Awake()
        {
            selfTransform = transform;

            if (playerCamera == null)
                playerCamera = Camera.main;

            if (playerCamera != null)
                camTransform = playerCamera.transform;

            if (melee == null)
                melee = GetComponent<BodycamBodyMelee>();

            if (head == null)
                head = FindDeep(transform, "B-head");

            if (chest == null)
                chest = FindDeep(transform, "B-chest");

            if (armRoots == null || armRoots.Length == 0)
            {
                Transform left = FindDeep(transform, "B-upperArm.L");
                Transform right = FindDeep(transform, "B-upperArm.R");
                armRoots = new Transform[] { left, right };
            }
        }

        void LateUpdate()
        {
            bool showArms = melee != null && melee.ShowArms;

            // Head is always hidden.
            if (head != null)
                head.localScale = Vector3.zero;

            // Arms are hidden unless melee is equipped.
            Vector3 armScale = showArms ? Vector3.one : Vector3.zero;

            for (int i = 0; i < armRoots.Length; i++)
            {
                if (armRoots[i] != null)
                    armRoots[i].localScale = armScale;
            }

            if (showArms)
                ApplyChestPitch();
        }

        void ApplyChestPitch()
        {
            if (!followCameraPitch || chest == null || camTransform == null)
                return;

            float pitch = camTransform.localEulerAngles.x;

            if (pitch > 180f)
                pitch -= 360f;

            // Negative = looking up, positive = looking down.
            pitch = Mathf.Clamp(pitch, -maxLookUp, maxLookDown);

            chest.rotation =
                Quaternion.AngleAxis(pitch * pitchWeight, selfTransform.right) * chest.rotation;
        }

        static Transform FindDeep(Transform root, string boneName)
        {
            if (root.name == boneName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), boneName);

                if (found != null)
                    return found;
            }

            return null;
        }
    }
}

/*using UnityEngine;

namespace RealisticBodycam
{
    /// <summary>
    /// Controls what the first-person camera sees of the full-body model.
    /// Put this on the model (the object with the Animator).
    ///  - Always hides the head (scales the head bone to zero).
    ///  - Hides the arms unless melee is equipped.
    ///  - Tilts the chest to follow the camera pitch so swings follow where you look.
    /// Runs in LateUpdate so it applies after the Animator.
    /// </summary>
    [AddComponentMenu("Realistic Bodycam/Body Visibility")]
    public class BodycamBodyVisibility : MonoBehaviour
    {
        public BodycamBodyMelee melee;
        public Camera playerCamera;

        [Header("BONES")]
        public Transform head;

        [Tooltip("Both shoulder / upper-arm bones. Scaled to zero when melee is not equipped.")]
        public Transform[] armRoots;

        [Tooltip("Chest bone used for pitch. Use the chest, not the spine bone driven by SpineProxy.")]
        public Transform chest;

        [Header("PITCH")]
        public bool followCameraPitch = true;

        [Range(0f, 1f)]
        public float pitchWeight = 0.7f;

        void Awake()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            if (melee == null)
                melee = GetComponent<BodycamBodyMelee>();
        }

        void LateUpdate()
        {
            if (head != null)
                head.localScale = Vector3.zero;

            float armScale = (melee != null && melee.ShowArms) ? 1f : 0f;

            if (armRoots != null)
            {
                for (int i = 0; i < armRoots.Length; i++)
                {
                    if (armRoots[i] != null)
                        armRoots[i].localScale = Vector3.one * armScale;
                }
            }

            if (followCameraPitch && chest != null && playerCamera != null
                && melee != null && melee.ShowArms)
            {
                float pitch = playerCamera.transform.localEulerAngles.x;

                if (pitch > 180f)
                    pitch -= 360f;

                pitch = Mathf.Clamp(pitch, -30f, 20f);   // -30 up, +20 down

                chest.rotation =
                    Quaternion.AngleAxis(pitch * pitchWeight, transform.right) * chest.rotation;
            }
        }
    }
}*/
