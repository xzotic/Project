using UnityEngine;

namespace PurgeFPS
{
    /// <summary>
    /// Quick dodge / dash. Put this on the player root (the object with the CharacterController
    /// and BodycamFPSController). Press the dodge key (Left Control by default) to dash in the
    /// direction you are holding (WASD). With no direction held, the player steps backward.
    ///
    /// The camera tilts toward the dodge: it rolls toward the side you dodge to, and pitches
    /// down for a forward dodge / up for a backward dodge. This needs the two small
    /// externalRoll / externalPitch additions in BodycamFPSController.
    ///
    /// The dash is added on top of normal movement with CharacterController.Move, so walls
    /// still stop it. Other scripts can read IsDodging (for example for brief invulnerability).
    /// </summary>
    [AddComponentMenu("Realistic Bodycam/Dodge")]
    [RequireComponent(typeof(CharacterController))]
    public class BodycamDodge : MonoBehaviour
    {
        [Header("INPUT")]
        public KeyCode dodgeKey = KeyCode.LeftControl;

        [Header("DODGE")]
        [Tooltip("Total distance travelled by one dodge, in metres.")]
        public float dodgeDistance = 3.5f;

        [Tooltip("How long one dodge lasts, in seconds.")]
        public float dodgeDuration = 0.25f;

        [Tooltip("Seconds before another dodge is allowed (counted from the end of the dodge).")]
        public float cooldown = 0.8f;

        [Tooltip("Only allow dodging while standing on the ground.")]
        public bool requireGrounded = true;

        [Tooltip("With no direction held, dodge backward. Off = dodge forward.")]
        public bool dodgeBackwardWhenIdle = true;

        [Header("CAMERA TILT")]
        public bool enableTilt = true;

        [Tooltip("Degrees of roll toward the side you dodge to.")]
        public float tiltRoll = 6f;

        [Tooltip("Degrees of pitch: down for a forward dodge, up for a backward dodge.")]
        public float tiltPitch = 3f;

        [Tooltip("How fast the tilt builds up (higher = snappier).")]
        public float tiltInSpeed = 10f;

        [Tooltip("How fast the tilt returns to normal after the dodge (higher = snappier).")]
        public float tiltOutSpeed = 5f;

        [Header("FEEL (optional)")]
        public BodycamFPSController bodycamController;
        public float shakeDuration = 0.15f;
        public float shakeIntensity = 0.3f;
        public float shakeFrequency = 20f;

        public AudioSource audioSource;
        public AudioClip dodgeSound;

        [Tooltip("Optional. Found automatically on the player root. The dodge costs stamina.dodgeCost.")]
        public BodycamStamina stamina;

        CharacterController controller;

        bool dodging;
        float dodgeTimer;
        float nextDodgeTime;
        float previousProgress;
        Vector3 dodgeDirection;

        float tiltSide;      // -1 = dodging left, +1 = dodging right (relative to the player)
        float tiltForward;   // +1 = dodging forward, -1 = backward
        float tilt;          // 0..1 blend

        public bool IsDodging => dodging;

        void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (bodycamController == null)
                bodycamController = GetComponent<BodycamFPSController>();

            if (stamina == null)
                stamina = GetComponent<BodycamStamina>();
        }

        void OnDisable()
        {
            dodging = false;
            tilt = 0f;
            ApplyTilt();
        }

        void Update()
        {
            if (!dodging)
            {
                if (Input.GetKeyDown(dodgeKey) && Time.time >= nextDodgeTime)
                    TryStartDodge();
            }
            else
            {
                UpdateDodge();
            }

            UpdateTilt();
        }

        void TryStartDodge()
        {
            if (requireGrounded && !controller.isGrounded)
                return;

            if (stamina != null && !stamina.TryConsume(stamina.dodgeCost))
                return;

            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            Vector3 direction = transform.right * x + transform.forward * z;

            if (direction.sqrMagnitude < 0.01f)
                direction = dodgeBackwardWhenIdle ? -transform.forward : transform.forward;

            direction.y = 0f;
            dodgeDirection = direction.normalized;

            tiltSide = Vector3.Dot(dodgeDirection, transform.right);
            tiltForward = Vector3.Dot(dodgeDirection, transform.forward);

            dodging = true;
            dodgeTimer = 0f;
            previousProgress = 0f;

            if (bodycamController != null && shakeIntensity > 0f)
                bodycamController.AddCameraShake(shakeDuration, shakeIntensity, shakeFrequency);

            if (audioSource != null && dodgeSound != null)
                audioSource.PlayOneShot(dodgeSound);
        }

        void UpdateDodge()
        {
            dodgeTimer += Time.deltaTime;

            float t = Mathf.Clamp01(dodgeTimer / Mathf.Max(0.01f, dodgeDuration));

            // Ease-out: fast start that slows toward the end.
            float progress = 1f - (1f - t) * (1f - t);

            float step = (progress - previousProgress) * dodgeDistance;
            previousProgress = progress;

            controller.Move(dodgeDirection * step);

            if (t >= 1f)
            {
                dodging = false;
                nextDodgeTime = Time.time + cooldown;
            }
        }

        // Builds the tilt while dodging and eases it back afterward.
        void UpdateTilt()
        {
            if (!enableTilt || bodycamController == null)
                return;

            // Nothing to do while idle and fully recovered.
            if (!dodging && tilt <= 0f)
                return;

            float target = dodging ? 1f : 0f;
            float speed = dodging ? tiltInSpeed : tiltOutSpeed;

            tilt = Mathf.MoveTowards(tilt, target, speed * Time.deltaTime);
            ApplyTilt();
        }

        void ApplyTilt()
        {
            if (bodycamController == null)
                return;

            // Negative roll leans the camera to the right (same sign as the strafe lean).
            bodycamController.externalRoll = -tiltSide * tiltRoll * tilt;

            // Positive pitch looks down.
            bodycamController.externalPitch = tiltForward * tiltPitch * tilt;
        }
    }
}