using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Weapon Motion With Lag")]
    public class BodycamWeaponMotionWithLag : MonoBehaviour
    {
        [Header("AIM SYSTEM")]
        public bool enableAim = true;
        public Camera playerCamera;
        public BodycamFPSController bodycamController;
        [Tooltip("Keeps the weapon facing forward while the camera is in Free Look.")]
        public bool freezeMouseMotionDuringFreeLook = true;
        public Vector3 aimLocalPosition = new Vector3(0f, -0.08f, 0.18f);
        public Vector3 aimLocalRotation;
        [Range(1f, 30f)] public float aimSpeed = 12f;
        public float normalFOV = 75f;
        public float aimFOV = 55f;

        [Header("AIM RELEASE WOBBLE")]
        public bool enableAimReleaseWobble = true;
        [Range(0.1f, 1.5f)] public float aimReleaseWobbleDuration = 0.48f;
        public Vector3 aimReleasePositionAmount = new Vector3(0.008f, 0.006f, 0.012f);
        public Vector3 aimReleaseRotationAmount = new Vector3(1.6f, 2.2f, 2.8f);
        [Range(1f, 30f)] public float aimReleaseWobbleSpeed = 11f;
        [Range(0.5f, 5f)] public float aimReleaseDamping = 2.2f;

        [Header("BODYCAM LAG")]
        [Range(0f, 0.2f)] public float turnLagPositionAmount = 0.055f;
        [Range(0f, 12f)] public float turnLagRotationAmount = 5f;
        [Range(1f, 25f)] public float lagSmoothness = 9f;

        [Header("SWAY")]
        [Range(0f, 0.08f)] public float swayAmount = 0.018f;
        [Range(0f, 0.08f)] public float aimSwayAmount = 0.005f;

        [Header("BOB")]
        public float walkBobSpeed = 7f;
        public float walkBobAmount = 0.025f;
        public float runBobSpeed = 11f;
        public float runBobAmount = 0.055f;

        [Header("IDLE")]
        public float idleAmount = 0.01f;
        public float aimIdleAmount = 0.003f;
        public float idleSpeed = 1.2f;

        [Header("ROTATION")]
        public float rotationAmount = 2.5f;
        public float aimRotationMultiplier = 0.25f;

        [Header("MOUSE FOLLOW ROTATION")]
        public bool enableMouseFollowRotation = true;
        public float horizontalFollowRotation = 8f;
        public float verticalFollowRotation = 6f;
        public float followRollAmount = 1.5f;
        [Range(1f, 60f)] public float followRotationSmoothness = 30f;

        [Range(1f, 60f)]
        [Tooltip("How quickly the weapon rotation responds independently from positional lag.")]
        public float rotationResponseSpeed = 30f;

        [Range(0f, 1f)] public float aimFollowMultiplier = 0.35f;

        [Header("HEAVY MOUSE WEIGHT")]
        [Tooltip("Adds physical inertia and overshoot when the mouse moves.")]
        public bool enableHeavyMouseWeight = true;
        [Range(0f, 0.08f)] public float mouseWeightPositionImpulse = 0.022f;
        [Range(0f, 40f)] public float mouseWeightRotationImpulse = 18f;
        [Range(5f, 120f)] public float mouseWeightSpring = 42f;
        [Range(1f, 30f)] public float mouseWeightDamping = 7.5f;
        [Range(0.01f, 0.2f)] public float maxMouseWeightPosition = 0.09f;
        [Range(1f, 30f)] public float maxMouseWeightRotation = 13f;
        [Range(0f, 1f)] public float aimMouseWeightMultiplier = 0.28f;

        [Header("MOVEMENT INERTIA")]
        public float strafePositionAmount = 0.045f;
        public float forwardPositionAmount = 0.025f;
        public float sprintLowerAmount = 0.11f;
        public float sprintRotationAmount = 7f;
        public float movementRotationAmount = 3f;
        [Range(1f, 25f)] public float movementInertiaSmoothness = 6f;

        Vector3 startLocalPosition;
        Quaternion startLocalRotation;

        float bobTimer;
        float idleTimer;
        bool isAiming;
        bool wasAiming;

        float aimReleaseWobbleTimer;
        float aimReleaseWobblePhase;

        Vector3 smoothLagPos;
        Vector3 smoothLagRot;
        Vector3 smoothMovementPos;
        Vector3 smoothMovementRot;
        Vector3 smoothFollowRotation;
        Vector3 heavyMousePosition;
        Vector3 heavyMousePositionVelocity;
        Vector3 heavyMouseRotation;
        Vector3 heavyMouseRotationVelocity;

        void Awake()
        {
            startLocalPosition = transform.localPosition;
            startLocalRotation = transform.localRotation;
        }

        void Start()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            if (bodycamController == null)
                bodycamController = GetComponentInParent<BodycamFPSController>();

            if (playerCamera != null)
                normalFOV = playerCamera.fieldOfView;

            isAiming = enableAim && Input.GetMouseButton(1);
            wasAiming = isAiming;
        }

        void Update()
        {
            isAiming = enableAim && Input.GetMouseButton(1);

            if (wasAiming && !isAiming)
                StartAimReleaseWobble();
            else if (isAiming)
                aimReleaseWobbleTimer = 0f;

            ApplyFOV();
            ApplyWeaponMotion();

            wasAiming = isAiming;
        }

        void StartAimReleaseWobble()
        {
            if (!enableAimReleaseWobble)
                return;

            aimReleaseWobbleTimer = aimReleaseWobbleDuration;
            aimReleaseWobblePhase = Random.Range(0f, Mathf.PI * 2f);
        }

        void CalculateAimReleaseWobble(out Vector3 positionOffset, out Vector3 rotationOffset)
        {
            positionOffset = Vector3.zero;
            rotationOffset = Vector3.zero;

            if (!enableAimReleaseWobble || aimReleaseWobbleTimer <= 0f || aimReleaseWobbleDuration <= 0f)
                return;

            float elapsed = aimReleaseWobbleDuration - aimReleaseWobbleTimer;
            float normalizedTime = Mathf.Clamp01(elapsed / aimReleaseWobbleDuration);
            float envelope = Mathf.Pow(1f - normalizedTime, aimReleaseDamping);
            float waveTime = elapsed * aimReleaseWobbleSpeed;

            Vector3 wave = new Vector3(
                Mathf.Sin(waveTime * 1.07f + aimReleaseWobblePhase),
                Mathf.Sin(waveTime * 0.83f + aimReleaseWobblePhase * 1.37f),
                Mathf.Sin(waveTime * 1.31f + aimReleaseWobblePhase * 0.71f)
            ) * envelope;

            positionOffset = Vector3.Scale(wave, aimReleasePositionAmount);
            rotationOffset = Vector3.Scale(wave, aimReleaseRotationAmount);

            aimReleaseWobbleTimer = Mathf.Max(0f, aimReleaseWobbleTimer - Time.deltaTime);
        }

        void UpdateHeavyMouseWeight(float mouseX, float mouseY)
        {
            if (!enableHeavyMouseWeight)
            {
                heavyMousePosition = Vector3.zero;
                heavyMousePositionVelocity = Vector3.zero;
                heavyMouseRotation = Vector3.zero;
                heavyMouseRotationVelocity = Vector3.zero;
                return;
            }

            float weightMultiplier = isAiming ? aimMouseWeightMultiplier : 1f;

            Vector3 positionImpulse = new Vector3(
                -mouseX,
                -mouseY,
                -(Mathf.Abs(mouseX) + Mathf.Abs(mouseY)) * 0.12f
            ) * mouseWeightPositionImpulse * weightMultiplier;

            Vector3 rotationImpulse = new Vector3(
                -mouseY,
                mouseX,
                -mouseX * 0.7f
            ) * mouseWeightRotationImpulse * weightMultiplier;

            heavyMousePositionVelocity += positionImpulse;
            heavyMouseRotationVelocity += rotationImpulse;

            float deltaTime = Time.deltaTime;

            Vector3 positionAcceleration = -heavyMousePosition * mouseWeightSpring -
                                           heavyMousePositionVelocity * mouseWeightDamping;
            heavyMousePositionVelocity += positionAcceleration * deltaTime;
            heavyMousePosition += heavyMousePositionVelocity * deltaTime;
            heavyMousePosition = Vector3.ClampMagnitude(heavyMousePosition, maxMouseWeightPosition);

            Vector3 rotationAcceleration = -heavyMouseRotation * mouseWeightSpring -
                                           heavyMouseRotationVelocity * mouseWeightDamping;
            heavyMouseRotationVelocity += rotationAcceleration * deltaTime;
            heavyMouseRotation += heavyMouseRotationVelocity * deltaTime;
            heavyMouseRotation = Vector3.ClampMagnitude(heavyMouseRotation, maxMouseWeightRotation);
        }

        void ApplyFOV()
        {
            if (playerCamera == null) return;

            float targetFOV = isAiming ? aimFOV : normalFOV;

            playerCamera.fieldOfView = Mathf.Lerp(
                playerCamera.fieldOfView,
                targetFOV,
                Time.deltaTime * aimSpeed
            );
        }

        void ApplyWeaponMotion()
        {
            bool freezeMouseMotion = freezeMouseMotionDuringFreeLook &&
                                     bodycamController != null &&
                                     bodycamController.IsFreeLooking;

            float mouseX = freezeMouseMotion ? 0f : Input.GetAxis("Mouse X");
            float mouseY = freezeMouseMotion ? 0f : Input.GetAxis("Mouse Y");

            if (freezeMouseMotion)
            {
                smoothLagPos = Vector3.zero;
                smoothLagRot = Vector3.zero;
                smoothFollowRotation = Vector3.zero;
                heavyMousePosition = Vector3.zero;
                heavyMousePositionVelocity = Vector3.zero;
                heavyMouseRotation = Vector3.zero;
                heavyMouseRotationVelocity = Vector3.zero;
            }

            UpdateHeavyMouseWeight(mouseX, mouseY);

            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            bool moving = Mathf.Abs(moveX) + Mathf.Abs(moveZ) > 0.1f;
            bool running = Input.GetKey(KeyCode.LeftShift) && moveZ > 0.1f && !isAiming;

            float currentSway = isAiming ? aimSwayAmount : swayAmount;
            float currentIdle = isAiming ? aimIdleAmount : idleAmount;
            float aimMultiplier = isAiming ? aimRotationMultiplier : 1f;

            Vector3 basePos = isAiming ? aimLocalPosition : startLocalPosition;
            Quaternion baseRot = isAiming ? Quaternion.Euler(aimLocalRotation) : startLocalRotation;

            Vector3 targetLagPos = new Vector3(
                -mouseX * turnLagPositionAmount,
                -mouseY * turnLagPositionAmount,
                0f
            );

            Vector3 targetLagRot = new Vector3(
                mouseY * turnLagRotationAmount,
                -mouseX * turnLagRotationAmount,
                -mouseX * turnLagRotationAmount
            );

            smoothLagPos = Vector3.Lerp(
                smoothLagPos,
                targetLagPos,
                Time.deltaTime * lagSmoothness
            );

            smoothLagRot = Vector3.Lerp(
                smoothLagRot,
                targetLagRot,
                Time.deltaTime * lagSmoothness
            );

            float movementMultiplier = isAiming ? 0.25f : 1f;

            Vector3 targetMovementPos = new Vector3(
                -moveX * strafePositionAmount,
                running ? -sprintLowerAmount : 0f,
                -Mathf.Max(0f, moveZ) * forwardPositionAmount
            ) * movementMultiplier;

            Vector3 targetMovementRot = new Vector3(
                running ? sprintRotationAmount : 0f,
                -moveX * movementRotationAmount,
                -moveX * movementRotationAmount
            ) * movementMultiplier;

            smoothMovementPos = Vector3.Lerp(
                smoothMovementPos,
                targetMovementPos,
                Time.deltaTime * movementInertiaSmoothness
            );

            smoothMovementRot = Vector3.Lerp(
                smoothMovementRot,
                targetMovementRot,
                Time.deltaTime * movementInertiaSmoothness
            );

            Vector3 targetFollowRotation = Vector3.zero;

            if (enableMouseFollowRotation)
            {
                float followMultiplier = isAiming ? aimFollowMultiplier : 1f;

                targetFollowRotation = new Vector3(
                    -mouseY * verticalFollowRotation,
                    mouseX * horizontalFollowRotation,
                    -mouseX * followRollAmount
                ) * followMultiplier;
            }

            smoothFollowRotation = Vector3.Lerp(
                smoothFollowRotation,
                targetFollowRotation,
                Time.deltaTime * followRotationSmoothness
            );

            Vector3 swayOffset = new Vector3(
                -mouseX * currentSway,
                -mouseY * currentSway,
                0f
            );

            Vector3 bobOffset = Vector3.zero;

            if (moving)
            {
                float speed = running ? runBobSpeed : walkBobSpeed;
                float amount = running ? runBobAmount : walkBobAmount;

                if (isAiming)
                    amount *= 0.2f;

                bobTimer += Time.deltaTime * speed;

                bobOffset = new Vector3(
                    Mathf.Sin(bobTimer) * amount,
                    Mathf.Abs(Mathf.Cos(bobTimer)) * amount,
                    0f
                );
            }
            else
            {
                idleTimer += Time.deltaTime * idleSpeed;

                bobOffset = new Vector3(
                    Mathf.Sin(idleTimer * 0.7f) * currentIdle,
                    Mathf.Cos(idleTimer) * currentIdle,
                    0f
                );
            }

            CalculateAimReleaseWobble(out Vector3 aimReleasePosition, out Vector3 aimReleaseRotation);

            Vector3 targetPos = basePos + smoothLagPos + smoothMovementPos + swayOffset + bobOffset +
                                aimReleasePosition + heavyMousePosition;

            Quaternion targetRot = baseRot * Quaternion.Euler(
                smoothLagRot.x + smoothMovementRot.x + smoothFollowRotation.x - mouseY * rotationAmount * aimMultiplier + aimReleaseRotation.x + heavyMouseRotation.x,
                smoothLagRot.y + smoothMovementRot.y + smoothFollowRotation.y + mouseX * rotationAmount * aimMultiplier + aimReleaseRotation.y + heavyMouseRotation.y,
                smoothLagRot.z + smoothMovementRot.z + smoothFollowRotation.z - moveX * rotationAmount * aimMultiplier + aimReleaseRotation.z + heavyMouseRotation.z
            );

            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPos,
                Time.deltaTime * lagSmoothness
            );

            float rotationResponse = 1f - Mathf.Exp(-rotationResponseSpeed * Time.deltaTime);

            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRot,
                rotationResponse
            );
        }
    }
}
