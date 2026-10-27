using UnityEngine;

namespace RealisticBodycam
{
    /// <summary>
    /// Feeds the full-body model's Animator from the player's actual movement.
    /// Put this on the model (the object with the Animator).
    /// Animator parameters needed (Float): Speed, MoveX, MoveZ
    /// MoveX / MoveZ are in metres per second, so blend tree positions
    /// should use your runSpeed (5.8 by default) as the maximum.
    ///
    /// Note: CharacterController.velocity only reports the LAST Move() call of the frame.
    /// BodycamFPSController calls Move() twice (horizontal, then gravity), so velocity
    /// would only show the vertical part. This script measures real movement instead.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class BodyAnimatorDriver : MonoBehaviour
    {
        public CharacterController controller;
        public Animator animator;

        [Tooltip("Smoothing time for the animator parameters.")]
        public float damping = 0.1f;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int MoveXHash = Animator.StringToHash("MoveX");
        static readonly int MoveZHash = Animator.StringToHash("MoveZ");

        Vector3 lastPosition;

        void Reset()
        {
            controller = GetComponentInParent<CharacterController>();
            animator = GetComponent<Animator>();
        }

        void Awake()
        {
            if (controller == null)
                controller = GetComponentInParent<CharacterController>();

            if (animator == null)
                animator = GetComponent<Animator>();
        }

        void Start()
        {
            if (controller != null)
                lastPosition = controller.transform.position;
        }

        void Update()
        {
            if (controller == null || animator == null || Time.deltaTime <= 0f)
                return;

            Transform body = controller.transform;

            Vector3 delta = body.position - lastPosition;
            lastPosition = body.position;

            Vector3 velocity = delta / Time.deltaTime;
            velocity.y = 0f;

            // Velocity relative to the direction the player is facing.
            Vector3 local = body.InverseTransformDirection(velocity);

            animator.SetFloat(SpeedHash, velocity.magnitude, damping, Time.deltaTime);
            animator.SetFloat(MoveXHash, local.x, damping, Time.deltaTime);
            animator.SetFloat(MoveZHash, local.z, damping, Time.deltaTime);
        }
    }
}