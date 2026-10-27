using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Overlay Motion")]
    [RequireComponent(typeof(RectTransform))]
    public class BodycamOverlayMotion : MonoBehaviour
    {
        [Header("MOTION")]
        public float moveAmount = 22f;
        public float rotationAmount = 3f;
        public float smoothness = 5f;

        [Header("INPUT INFLUENCE")]
        public float mouseInfluence = 1.35f;
        public float movementInfluence = 0.9f;
        public float inertiaSmoothness = 4f;

        [Header("STEP MOTION")]
        public float walkBobAmount = 4f;
        public float runBobAmount = 8f;
        public float walkBobSpeed = 7f;
        public float runBobSpeed = 11f;
        public float stepRotationAmount = 1.4f;

        [Header("DYNAMIC SHAKE")]
        public float idleShake = 0.5f;
        public float walkShake = 2f;
        public float runShake = 4.5f;
        public float shakeSpeed = 7f;

        RectTransform rect;
        Vector2 startPosition;
        Quaternion startRotation;

        float shakeTimer;
        float bobTimer;

        Vector2 smoothMouseInput;
        Vector2 smoothMovementInput;

        void Start()
        {
            rect = GetComponent<RectTransform>();
            startPosition = rect.anchoredPosition;
            startRotation = rect.localRotation;
        }

        void Update()
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            bool isMoving = Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f;
            bool isRunning = Input.GetKey(KeyCode.LeftShift) && moveZ > 0.1f;

            smoothMouseInput = Vector2.Lerp(
                smoothMouseInput,
                new Vector2(mouseX, mouseY),
                Time.deltaTime * inertiaSmoothness
            );

            smoothMovementInput = Vector2.Lerp(
                smoothMovementInput,
                new Vector2(moveX, moveZ),
                Time.deltaTime * inertiaSmoothness
            );

            float currentShake = idleShake;

            if (isMoving)
                currentShake = isRunning ? runShake : walkShake;

            shakeTimer += Time.deltaTime * shakeSpeed;

            float shakeX = Mathf.PerlinNoise(shakeTimer, 0f) - 0.5f;
            float shakeY = Mathf.PerlinNoise(0f, shakeTimer) - 0.5f;

            Vector2 shakeOffset = new Vector2(shakeX, shakeY) * currentShake;

            Vector2 bobOffset = Vector2.zero;
            float stepRoll = 0f;

            if (isMoving)
            {
                float currentBobSpeed = isRunning ? runBobSpeed : walkBobSpeed;
                float currentBobAmount = isRunning ? runBobAmount : walkBobAmount;

                bobTimer += Time.deltaTime * currentBobSpeed;

                bobOffset = new Vector2(
                    Mathf.Sin(bobTimer) * currentBobAmount,
                    Mathf.Abs(Mathf.Cos(bobTimer)) * currentBobAmount
                );

                stepRoll = Mathf.Sin(bobTimer) * stepRotationAmount;
            }

            Vector2 targetPos = startPosition + new Vector2(
                (-smoothMouseInput.x * mouseInfluence - smoothMovementInput.x * movementInfluence) * moveAmount,
                (-smoothMouseInput.y * mouseInfluence - smoothMovementInput.y * movementInfluence) * moveAmount
            ) + shakeOffset + bobOffset;

            Quaternion targetRot = startRotation * Quaternion.Euler(
                0f,
                0f,
                (-smoothMouseInput.x - smoothMovementInput.x + shakeX * 5f) * rotationAmount + stepRoll
            );

            rect.anchoredPosition = Vector2.Lerp(
                rect.anchoredPosition,
                targetPos,
                Time.deltaTime * smoothness
            );

            rect.localRotation = Quaternion.Slerp(
                rect.localRotation,
                targetRot,
                Time.deltaTime * smoothness
            );
        }
    }
}
