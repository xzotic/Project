using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Bodycam FPS Controller")]
    [RequireComponent(typeof(CharacterController))]
    public class BodycamFPSController : MonoBehaviour
    {
        public enum BodycamPreset
        {
            Realistic,
            Cinematic,
            Extreme,
            Horror,
            Tactical,
            FoundFootage
        }

        [Header("SYSTEM TOGGLES")]
        public bool enableMovement = true;
        public bool enableMouseLook = true;
        public bool enableBodycamMotion = true;
        public bool enableHeadBob = true;
        public bool enableIdleBreathing = true;
        public bool enableBodyWeight = true;
        public bool enableTurnInertia = true;
        public bool enableStepRotation = true;
        public bool enableFreeLook = true;
        public bool enableWeaponCompensation = true;

        [Header("REFERENCES")]
        public Transform cameraTransform;
        public Transform weaponHolder;
        public Transform groundCheck;
        public LayerMask groundMask;

        [Header("MOVEMENT")]
        public float walkSpeed = 3.2f;
        public float runSpeed = 5.8f;
        public float movementSmoothness = 0.18f;

        [Header("MOUSE LOOK")]
        public float mouseSensitivity = 140f;
        public float maxLookUp = 75f;
        public float maxLookDown = 75f;

        [Header("FREE LOOK")]
        public KeyCode freeLookKey = KeyCode.LeftAlt;
        public bool toggleMode = false;
        public float freeLookSensitivity = 1f;
        public float freeLookReturnSpeed = 8f;
        public float maxFreeLookAngle = 80f;

        [Header("BODY WEIGHT")]
        public float accelerationWeight = 0.12f;
        public float stopWeightAmount = 0.035f;
        public float stopWeightReturnSpeed = 7f;
        public float movementLeanAmount = 4f;
        public float movementLeanSmoothness = 6f;

        [Header("JUMP & GRAVITY")]
        public float jumpForce = 1.2f;
        public float gravity = -18f;
        public float groundCheckDistance = 0.4f;

        [Header("BODYCAM POSITION")]
        public float bodycamHeightOffset = -0.08f;
        public float cameraPositionSmoothness = 8f;
        public float cameraRotationSmoothness = 10f;

        [Header("LOOK SWAY")]
        public float lookSwayAmount = 0.035f;
        public float bodyRollAmount = 8f;
        public float bodyRollSpeed = 6f;

        [Header("CAMERA TURN INERTIA")]
        public float turnLagYaw = 2.2f;
        public float turnLagPitch = 1.2f;
        public float turnLagRoll = 3f;
        public float turnLagSmoothness = 5f;

        [Header("STEP ROTATION")]
        public float walkStepPitch = 1.2f;
        public float walkStepRoll = 2.4f;
        public float runStepPitch = 2.2f;
        public float runStepRoll = 4.2f;
        public float stepRotationSmoothness = 9f;

        [Header("IDLE BREATHING")]
        public float breathingAmount = 0.018f;
        public float breathingSpeed = 1.2f;

        [Header("WALK MOTION")]
        public float walkBobSpeed = 7f;
        public float walkBobAmount = 0.045f;
        public float walkSideAmount = 0.025f;

        [Header("RUN MOTION")]
        public float runBobSpeed = 11f;
        public float runBobAmount = 0.09f;
        public float runSideAmount = 0.055f;
        public float runShakeAmount = 0.025f;
        public float runShakeSpeed = 22f;

        [Header("REFERENCE WALK & RUN MOVEMENT")]
        [Tooltip("Uses the heavier, alternating bodycam gait inspired by the reference video.")]
        public bool enableReferenceGait = true;
        [Range(0f, 0.15f)] public float walkForwardMotion = 0.018f;
        [Range(0f, 10f)] public float walkGaitRoll = 6.5f;
        [Range(0f, 8f)] public float walkGaitPitch = 3.2f;
        [Range(0f, 5f)] public float walkGaitYaw = 1.15f;
        [Range(0f, 0.2f)] public float runForwardMotion = 0.035f;
        [Range(0f, 15f)] public float runGaitRoll = 11f;
        [Range(0f, 12f)] public float runGaitPitch = 6f;
        [Range(0f, 8f)] public float runGaitYaw = 2.1f;
        [Range(1f, 10f)] public float footImpactSharpness = 6f;
        [Range(1f, 30f)] public float gaitPositionSmoothness = 8f;
        [Range(1f, 30f)] public float gaitRotationSmoothness = 9f;
        [Range(0f, 1f)] public float strafeGaitMultiplier = 0.65f;

        [Header("HEAVY BODY RESPONSE")]
        public bool enableHeavyBodyResponse = true;
        [Range(0f, 1f)] public float walkFootImpact = 0.42f;
        [Range(0f, 1.5f)] public float runFootImpact = 0.82f;
        [Range(20f, 180f)] public float bodyImpactSpring = 78f;
        [Range(2f, 30f)] public float bodyImpactDamping = 10f;
        [Range(0f, 80f)] public float impactPitchKick = 28f;
        [Range(0f, 60f)] public float impactRollKick = 18f;
        [Range(0f, 0.2f)] public float accelerationBodyLag = 0.065f;
        [Range(0f, 8f)] public float accelerationPitch = 3.8f;
        [Tooltip("Controls the body inertia kick only when movement begins. Set to 0 to start walking without a camera jolt.")]
        [Range(0f, 1f)] public float startMovementInertiaMultiplier = 0f;
        [Tooltip("Reduces walking movement while aiming so the sight remains usable.")]
        [Range(0f, 1f)] public float aimGaitMultiplier = 0.24f;
        [Tooltip("Extra reduction applied to acceleration and stopping inertia while aiming.")]
        [Range(0f, 1f)] public float aimStopInertiaMultiplier = 0.08f;

        CharacterController controller;

        float xRotation;
        float currentRoll;
        Vector3 currentTurnInertia;
        Vector3 currentStepRotation;

        float freeLookYaw;
        float freeLookTargetYaw;
        bool isFreeLooking;

        Vector3 velocity;
        Vector3 currentMove;
        Vector3 moveVelocity;

        Vector3 previousMove;
        Vector3 stopWeightOffset;
        float currentMovementLean;

        float bobTimer;
        float idleTimer;
        float shakeTimer;
        Vector3 currentReferenceOffset;
        float heavyImpactOffset;
        float heavyImpactVelocity;
        Vector3 heavyImpactRotation;
        Vector3 heavyImpactRotationVelocity;
        int previousFootIndex = int.MinValue;
        float previousReferenceMoveAmount;

        float externalShakeTimer;
        float externalShakeDuration;
        float externalShakeIntensity;
        float externalShakeFrequency;
        float externalShakeSeed;
        Quaternion previousExternalShake = Quaternion.identity;

        Vector3 cameraStartPosition;
        Vector3 weaponStartLocalPosition;
        Quaternion weaponStartLocalRotation;

        bool isGrounded;

        public bool IsFreeLooking => isFreeLooking;

        void Start()
        {
            controller = GetComponent<CharacterController>();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            QualitySettings.vSyncCount = 0;    
            Application.targetFrameRate = 60;    // max fps

            if (cameraTransform != null)
            {
                cameraStartPosition = cameraTransform.localPosition;
                cameraStartPosition.y += bodycamHeightOffset;
            }

            if (weaponHolder != null)
            {
                weaponStartLocalPosition = weaponHolder.localPosition;
                weaponStartLocalRotation = weaponHolder.localRotation;
            }
        }

        void Update()
        {
            if (cameraTransform == null || groundCheck == null)
                return;

            HandleFreeLookInput();

            if (enableMouseLook)
                HandleMouseLook();

            if (enableMovement)
                HandleMovement();

            if (enableBodycamMotion)
                HandleBodycamMotion();
        }

        void HandleFreeLookInput()
        {
            if (!enableFreeLook)
            {
                isFreeLooking = false;
                return;
            }

            if (toggleMode)
            {
                if (Input.GetKeyDown(freeLookKey))
                    isFreeLooking = !isFreeLooking;
            }
            else
            {
                isFreeLooking = Input.GetKey(freeLookKey);
            }
        }

        void HandleMouseLook()
        {
            float rawMouseX = Input.GetAxis("Mouse X");
            float rawMouseY = Input.GetAxis("Mouse Y");

            float mouseX = rawMouseX * mouseSensitivity;
            float mouseY = rawMouseY * mouseSensitivity;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -maxLookDown, maxLookUp);

            if (isFreeLooking)
            {
                freeLookTargetYaw += mouseX * freeLookSensitivity;
                freeLookTargetYaw = Mathf.Clamp(freeLookTargetYaw, -maxFreeLookAngle, maxFreeLookAngle);
            }
            else
            {
                freeLookTargetYaw = 0f;
                transform.Rotate(Vector3.up * mouseX);
            }

            freeLookYaw = Mathf.Lerp(
                freeLookYaw,
                freeLookTargetYaw,
                Time.deltaTime * freeLookReturnSpeed
            );

            float horizontalInput = Input.GetAxis("Horizontal");

            float mouseRoll = -mouseX * bodyRollAmount;
            float strafeRoll = -horizontalInput * bodyRollAmount * 0.7f;

            currentRoll = Mathf.Lerp(
                currentRoll,
                mouseRoll + strafeRoll,
                Time.deltaTime * bodyRollSpeed
            );

            Vector3 targetTurnInertia = Vector3.zero;

            if (enableTurnInertia && !isFreeLooking)
            {
                targetTurnInertia = new Vector3(
                    rawMouseY * turnLagPitch,
                    -rawMouseX * turnLagYaw,
                    -rawMouseX * turnLagRoll
                );
            }

            currentTurnInertia = Vector3.Lerp(
                currentTurnInertia,
                targetTurnInertia,
                Time.deltaTime * turnLagSmoothness
            );

            Vector3 externalShakeRotation = GetExternalCameraShakeRotation();

            Quaternion unshakenCameraRotation =
                cameraTransform.localRotation * Quaternion.Inverse(previousExternalShake);

            Quaternion baseCameraRotation = Quaternion.Slerp(
                unshakenCameraRotation,
                Quaternion.Euler(
                    xRotation + currentTurnInertia.x + currentStepRotation.x,
                    freeLookYaw + currentTurnInertia.y,
                    currentRoll + currentMovementLean + currentTurnInertia.z + currentStepRotation.z
                ),
                Time.deltaTime * cameraRotationSmoothness
            );

            previousExternalShake = Quaternion.Euler(externalShakeRotation);
            cameraTransform.localRotation = baseCameraRotation * previousExternalShake;

            if (enableWeaponCompensation)
                HandleWeaponCompensation();
        }

        public void AddCameraShake(float duration, float intensity, float frequency)
        {
            if (duration <= 0f || intensity <= 0f)
                return;

            externalShakeDuration = duration;
            externalShakeTimer = duration;
            externalShakeIntensity = intensity;
            externalShakeFrequency = Mathf.Max(0.01f, frequency);
            externalShakeSeed = Random.Range(0f, 1000f);
        }

        public void StopCameraShake()
        {
            externalShakeTimer = 0f;
        }

        Vector3 GetExternalCameraShakeRotation()
        {
            if (externalShakeTimer <= 0f)
                return Vector3.zero;

            externalShakeTimer = Mathf.Max(0f, externalShakeTimer - Time.deltaTime);

            float fade = externalShakeDuration > 0f
                ? externalShakeTimer / externalShakeDuration
                : 0f;

            float noiseTime = Time.time * externalShakeFrequency;

            return new Vector3(
                (Mathf.PerlinNoise(externalShakeSeed, noiseTime) - 0.5f) * 2f,
                (Mathf.PerlinNoise(externalShakeSeed + 17f, noiseTime) - 0.5f) * 2f,
                (Mathf.PerlinNoise(externalShakeSeed + 37f, noiseTime) - 0.5f) * 2f
            ) * externalShakeIntensity * fade;
        }

        void HandleWeaponCompensation()
        {
            if (weaponHolder == null)
                return;

            Quaternion targetRot = isFreeLooking
                ? weaponStartLocalRotation * Quaternion.Euler(0f, -freeLookYaw, 0f)
                : weaponStartLocalRotation;

            weaponHolder.localPosition = weaponStartLocalPosition;

            weaponHolder.localRotation = Quaternion.Slerp(
                weaponHolder.localRotation,
                targetRot,
                Time.deltaTime * freeLookReturnSpeed
            );
        }

        void HandleMovement()
        {
            isGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundCheckDistance,
                groundMask
            );

            if (isGrounded && velocity.y < 0f)
                velocity.y = -2f;

            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            bool running = Input.GetKey(KeyCode.LeftShift) && z > 0.1f;
            float speed = running ? runSpeed : walkSpeed;

            Vector3 move = (transform.right * x + transform.forward * z).normalized * speed;

            currentMove = Vector3.SmoothDamp(
                currentMove,
                move,
                ref moveVelocity,
                movementSmoothness
            );

            controller.Move(currentMove * Time.deltaTime);

            if (Input.GetButtonDown("Jump") && isGrounded)
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            ApplyWeight();
            previousMove = currentMove;
        }

        void ApplyWeight()
        {
            if (!enableBodyWeight)
                return;

            if (previousMove.magnitude > 1f && currentMove.magnitude < 0.2f)
                stopWeightOffset = -transform.forward * stopWeightAmount;

            stopWeightOffset = Vector3.Lerp(
                stopWeightOffset,
                Vector3.zero,
                Time.deltaTime * stopWeightReturnSpeed
            );

            float moveX = Input.GetAxis("Horizontal");

            currentMovementLean = Mathf.Lerp(
                currentMovementLean,
                -moveX * movementLeanAmount,
                Time.deltaTime * movementLeanSmoothness
            );
        }

        void HandleBodycamMotion()
        {
            float moveAmount = new Vector3(currentMove.x, 0f, currentMove.z).magnitude;
            bool running = Input.GetKey(KeyCode.LeftShift) && moveAmount > walkSpeed;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            Vector3 sway = new Vector3(
                -mouseX * lookSwayAmount,
                -mouseY * lookSwayAmount,
                0f
            );

            if (enableReferenceGait)
            {
                HandleReferenceGait(moveAmount, running, sway);
                return;
            }

            Vector3 offset = Vector3.zero;
            Vector3 targetStepRotation = Vector3.zero;

            if (running && enableHeadBob)
            {
                bobTimer += Time.deltaTime * runBobSpeed;
                shakeTimer += Time.deltaTime * runShakeSpeed;

                offset = new Vector3(
                    Mathf.Sin(bobTimer) * runSideAmount + Mathf.Sin(shakeTimer) * runShakeAmount,
                    Mathf.Abs(Mathf.Cos(bobTimer)) * runBobAmount,
                    0f
                );

                if (enableStepRotation)
                {
                    targetStepRotation = new Vector3(
                        -Mathf.Abs(Mathf.Cos(bobTimer)) * runStepPitch,
                        0f,
                        Mathf.Sin(bobTimer) * runStepRoll
                    );
                }
            }
            else if (moveAmount > 0.1f && enableHeadBob)
            {
                bobTimer += Time.deltaTime * walkBobSpeed;

                offset = new Vector3(
                    Mathf.Sin(bobTimer) * walkSideAmount,
                    Mathf.Abs(Mathf.Cos(bobTimer)) * walkBobAmount,
                    0f
                );

                if (enableStepRotation)
                {
                    targetStepRotation = new Vector3(
                        -Mathf.Abs(Mathf.Cos(bobTimer)) * walkStepPitch,
                        0f,
                        Mathf.Sin(bobTimer) * walkStepRoll
                    );
                }
            }
            else if (enableIdleBreathing)
            {
                idleTimer += Time.deltaTime * breathingSpeed;

                offset = new Vector3(
                    Mathf.Sin(idleTimer) * breathingAmount,
                    Mathf.Cos(idleTimer) * breathingAmount,
                    0f
                );
            }

            currentStepRotation = Vector3.Lerp(
                currentStepRotation,
                targetStepRotation,
                Time.deltaTime * stepRotationSmoothness
            );

            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition,
                cameraStartPosition + offset + sway + stopWeightOffset,
                Time.deltaTime * cameraPositionSmoothness
            );
        }

        void HandleReferenceGait(float moveAmount, bool running, Vector3 lookSway)
        {
            bool moving = moveAmount > 0.1f && enableHeadBob;
            bool aiming = Input.GetMouseButton(1);
            float currentGaitMultiplier = aiming ? aimGaitMultiplier : 1f;
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            Vector3 targetOffset = Vector3.zero;
            Vector3 targetRotation = Vector3.zero;

            if (moving)
            {
                float speed = running ? runBobSpeed : walkBobSpeed;
                float verticalAmount = running ? runBobAmount : walkBobAmount;
                float sideAmount = running ? runSideAmount : walkSideAmount;
                float forwardAmount = running ? runForwardMotion : walkForwardMotion;
                float rollAmount = running ? runGaitRoll : walkGaitRoll;
                float pitchAmount = running ? runGaitPitch : walkGaitPitch;
                float yawAmount = running ? runGaitYaw : walkGaitYaw;

                bobTimer += Time.deltaTime * speed;
                shakeTimer += Time.deltaTime * runShakeSpeed;

                int currentFootIndex = Mathf.FloorToInt(bobTimer / Mathf.PI);

                if (enableHeavyBodyResponse && currentFootIndex != previousFootIndex)
                {
                    if (previousFootIndex != int.MinValue)
                    {
                        float footSide = currentFootIndex % 2 == 0 ? -1f : 1f;
                        float impact = (running ? runFootImpact : walkFootImpact) * currentGaitMultiplier;
                        heavyImpactVelocity -= impact;
                        heavyImpactRotationVelocity.x += impactPitchKick * impact;
                        heavyImpactRotationVelocity.z += footSide * impactRollKick * impact;
                    }

                    previousFootIndex = currentFootIndex;
                }

                float sideWave = Mathf.Sin(bobTimer);
                float oppositeWave = Mathf.Cos(bobTimer);
                float footImpact = Mathf.Pow(Mathf.Abs(oppositeWave), footImpactSharpness);
                float lift = 1f - footImpact;
                float directionMultiplier = Mathf.Abs(verticalInput) >= Mathf.Abs(horizontalInput)
                    ? 1f
                    : strafeGaitMultiplier;

                targetOffset = new Vector3(
                    sideWave * sideAmount * directionMultiplier,
                    lift * verticalAmount,
                    -footImpact * forwardAmount
                );

                targetRotation = new Vector3(
                    -footImpact * pitchAmount + lift * pitchAmount * 0.2f,
                    sideWave * yawAmount * directionMultiplier,
                    -sideWave * rollAmount * directionMultiplier
                );

                if (running)
                {
                    float runNoiseX = Mathf.Sin(shakeTimer * 0.91f) * runShakeAmount;
                    float runNoiseY = Mathf.Sin(shakeTimer * 1.13f + 1.7f) * runShakeAmount;
                    targetOffset += new Vector3(runNoiseX, runNoiseY, 0f);
                    targetRotation += new Vector3(runNoiseY * 24f, runNoiseX * 18f, runNoiseX * 28f);
                }

                targetOffset *= currentGaitMultiplier;
                targetRotation *= currentGaitMultiplier;
            }
            else if (enableIdleBreathing)
            {
                idleTimer += Time.deltaTime * breathingSpeed;
                targetOffset = new Vector3(
                    Mathf.Sin(idleTimer * 0.7f) * breathingAmount * 0.45f,
                    Mathf.Cos(idleTimer) * breathingAmount,
                    Mathf.Sin(idleTimer * 0.5f) * breathingAmount * 0.25f
                );
                targetRotation = new Vector3(
                    Mathf.Sin(idleTimer * 0.65f) * 0.35f,
                    Mathf.Cos(idleTimer * 0.48f) * 0.2f,
                    Mathf.Sin(idleTimer * 0.55f) * 0.3f
                );
            }


            UpdateHeavyBodySpring();

            if (enableHeavyBodyResponse)
            {
                float currentStopMultiplier = aiming ? aimStopInertiaMultiplier : 1f;
                float speedChange = (moveAmount - previousReferenceMoveAmount) /
                                    Mathf.Max(Time.deltaTime, 0.0001f);
                // Starting and stopping now have independent strength. A positive
                // speed change means movement is beginning; negative means stopping.
                if (speedChange > 0f)
                    speedChange *= startMovementInertiaMultiplier;

                float bodyLag = Mathf.Clamp(speedChange * accelerationBodyLag * currentStopMultiplier, -0.12f, 0.12f);
                targetOffset.z -= bodyLag;
                targetRotation.x += Mathf.Clamp(speedChange * accelerationPitch * currentStopMultiplier, -8f, 8f);
                targetOffset.y += heavyImpactOffset * currentGaitMultiplier;
                targetRotation += heavyImpactRotation * currentGaitMultiplier;
            }

            previousReferenceMoveAmount = moveAmount;

            float positionResponse = 1f - Mathf.Exp(-gaitPositionSmoothness * Time.deltaTime);
            float rotationResponse = 1f - Mathf.Exp(-gaitRotationSmoothness * Time.deltaTime);

            currentReferenceOffset = Vector3.Lerp(currentReferenceOffset, targetOffset, positionResponse);
            currentStepRotation = Vector3.Lerp(currentStepRotation, targetRotation, rotationResponse);

            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition,
                cameraStartPosition + currentReferenceOffset + lookSway + stopWeightOffset,
                positionResponse
            );
        }

        void UpdateHeavyBodySpring()
        {
            if (!enableHeavyBodyResponse)
            {
                heavyImpactOffset = 0f;
                heavyImpactVelocity = 0f;
                heavyImpactRotation = Vector3.zero;
                heavyImpactRotationVelocity = Vector3.zero;
                return;
            }

            float deltaTime = Time.deltaTime;

            float verticalAcceleration = -heavyImpactOffset * bodyImpactSpring -
                                         heavyImpactVelocity * bodyImpactDamping;
            heavyImpactVelocity += verticalAcceleration * deltaTime;
            heavyImpactOffset += heavyImpactVelocity * deltaTime;
            heavyImpactOffset = Mathf.Clamp(heavyImpactOffset, -0.13f, 0.06f);

            Vector3 rotationAcceleration = -heavyImpactRotation * bodyImpactSpring -
                                           heavyImpactRotationVelocity * bodyImpactDamping;
            heavyImpactRotationVelocity += rotationAcceleration * deltaTime;
            heavyImpactRotation += heavyImpactRotationVelocity * deltaTime;
            heavyImpactRotation = Vector3.ClampMagnitude(heavyImpactRotation, 12f);
        }

        public void RandomizeSettings()
        {
            walkSpeed = Random.Range(2.5f, 4f);
            runSpeed = Random.Range(5f, 7f);
            movementSmoothness = Random.Range(0.1f, 0.28f);

            mouseSensitivity = Random.Range(110f, 180f);

            lookSwayAmount = Random.Range(0.02f, 0.09f);
            bodyRollAmount = Random.Range(5f, 18f);
            bodyRollSpeed = Random.Range(4f, 9f);

            walkBobSpeed = Random.Range(5f, 9f);
            walkBobAmount = Random.Range(0.03f, 0.08f);
            walkSideAmount = Random.Range(0.02f, 0.06f);

            runBobSpeed = Random.Range(9f, 15f);
            runBobAmount = Random.Range(0.08f, 0.2f);
            runSideAmount = Random.Range(0.05f, 0.11f);
            runShakeAmount = Random.Range(0.02f, 0.07f);
            runShakeSpeed = Random.Range(18f, 30f);

            breathingAmount = Random.Range(0.015f, 0.04f);
            breathingSpeed = Random.Range(0.8f, 1.8f);

            stopWeightAmount = Random.Range(0.02f, 0.08f);
            movementLeanAmount = Random.Range(3f, 9f);
            movementLeanSmoothness = Random.Range(4f, 9f);
        }

        public void ApplyPreset(BodycamPreset preset)
        {
            if (preset == BodycamPreset.Realistic)
            {
                walkSpeed = 3.2f;
                runSpeed = 5.8f;
                movementSmoothness = 0.18f;
                mouseSensitivity = 140f;

                lookSwayAmount = 0.035f;
                bodyRollAmount = 8f;
                bodyRollSpeed = 6f;

                walkBobSpeed = 7f;
                walkBobAmount = 0.045f;
                walkSideAmount = 0.025f;

                runBobSpeed = 11f;
                runBobAmount = 0.09f;
                runSideAmount = 0.055f;
                runShakeAmount = 0.025f;
                runShakeSpeed = 22f;

                breathingAmount = 0.018f;
                breathingSpeed = 1.2f;

                stopWeightAmount = 0.035f;
                movementLeanAmount = 4f;
                movementLeanSmoothness = 6f;
            }
            else if (preset == BodycamPreset.Cinematic)
            {
                walkSpeed = 2.8f;
                runSpeed = 5f;
                movementSmoothness = 0.24f;
                mouseSensitivity = 115f;

                lookSwayAmount = 0.05f;
                bodyRollAmount = 10f;
                bodyRollSpeed = 4.5f;

                walkBobSpeed = 6f;
                walkBobAmount = 0.06f;
                walkSideAmount = 0.035f;

                runBobSpeed = 10f;
                runBobAmount = 0.12f;
                runSideAmount = 0.07f;
                runShakeAmount = 0.04f;
                runShakeSpeed = 20f;

                breathingAmount = 0.025f;
                breathingSpeed = 1f;

                stopWeightAmount = 0.05f;
                movementLeanAmount = 6f;
                movementLeanSmoothness = 4f;
            }
            else if (preset == BodycamPreset.Extreme)
            {
                walkSpeed = 3.6f;
                runSpeed = 6.5f;
                movementSmoothness = 0.12f;
                mouseSensitivity = 160f;

                lookSwayAmount = 0.075f;
                bodyRollAmount = 14f;
                bodyRollSpeed = 8f;

                walkBobSpeed = 8f;
                walkBobAmount = 0.08f;
                walkSideAmount = 0.05f;

                runBobSpeed = 14f;
                runBobAmount = 0.16f;
                runSideAmount = 0.1f;
                runShakeAmount = 0.065f;
                runShakeSpeed = 28f;

                breathingAmount = 0.03f;
                breathingSpeed = 1.5f;

                stopWeightAmount = 0.07f;
                movementLeanAmount = 8f;
                movementLeanSmoothness = 8f;
            }
            else if (preset == BodycamPreset.Horror)
            {
                walkSpeed = 2.6f;
                runSpeed = 4.5f;
                movementSmoothness = 0.28f;
                mouseSensitivity = 120f;

                lookSwayAmount = 0.08f;
                bodyRollAmount = 16f;
                bodyRollSpeed = 5f;

                walkBobSpeed = 5.5f;
                walkBobAmount = 0.07f;
                walkSideAmount = 0.045f;

                runBobSpeed = 12f;
                runBobAmount = 0.18f;
                runSideAmount = 0.09f;
                runShakeAmount = 0.06f;
                runShakeSpeed = 26f;

                breathingAmount = 0.035f;
                breathingSpeed = 1.4f;

                stopWeightAmount = 0.06f;
                movementLeanAmount = 7f;
                movementLeanSmoothness = 5f;
            }
            else if (preset == BodycamPreset.Tactical)
            {
                walkSpeed = 2.8f;
                runSpeed = 5.2f;
                movementSmoothness = 0.16f;
                mouseSensitivity = 100f;

                lookSwayAmount = 0.02f;
                bodyRollAmount = 4f;
                bodyRollSpeed = 8f;

                walkBobSpeed = 7f;
                walkBobAmount = 0.03f;
                walkSideAmount = 0.015f;

                runBobSpeed = 10f;
                runBobAmount = 0.07f;
                runSideAmount = 0.035f;
                runShakeAmount = 0.015f;
                runShakeSpeed = 18f;

                breathingAmount = 0.01f;
                breathingSpeed = 0.8f;

                stopWeightAmount = 0.02f;
                movementLeanAmount = 2.5f;
                movementLeanSmoothness = 9f;
            }
            else if (preset == BodycamPreset.FoundFootage)
            {
                walkSpeed = 3f;
                runSpeed = 6f;
                movementSmoothness = 0.2f;
                mouseSensitivity = 150f;

                lookSwayAmount = 0.09f;
                bodyRollAmount = 18f;
                bodyRollSpeed = 5.5f;

                walkBobSpeed = 6.5f;
                walkBobAmount = 0.08f;
                walkSideAmount = 0.055f;

                runBobSpeed = 13f;
                runBobAmount = 0.2f;
                runSideAmount = 0.11f;
                runShakeAmount = 0.07f;
                runShakeSpeed = 30f;

                breathingAmount = 0.03f;
                breathingSpeed = 1.6f;

                stopWeightAmount = 0.065f;
                movementLeanAmount = 9f;
                movementLeanSmoothness = 5f;
            }
        }
    }
}
