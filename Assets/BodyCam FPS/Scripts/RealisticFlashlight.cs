using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Flashlight System")]
    public class RealisticBodycamFlashlight : MonoBehaviour
    {
        [Header("REFERENCES")]
        public Transform cameraTransform;
        public Light flashlight;
        public AudioSource audioSource;
        public AudioClip toggleSound;

        [Header("INPUT")]
        public KeyCode toggleKey = KeyCode.F;

        [Header("FOLLOW")]
        [Range(1f, 30f)] public float positionFollowSpeed = 12f;
        [Range(1f, 30f)] public float rotationFollowSpeed = 10f;

        [Header("INERTIA")]
        [Range(0f, 10f)] public float inertiaAmount = 4f;

        [Header("SWAY")]
        [Range(0f, 10f)] public float swayAmount = 2f;

        [Header("MOVEMENT BOB")]
        public float walkBobAmount = 1.5f;
        public float walkBobSpeed = 6f;

        public float runBobAmount = 3f;
        public float runBobSpeed = 10f;

        [Header("IDLE MOTION")]
        public float idleAmount = 0.5f;
        public float idleSpeed = 1f;

        Vector3 baseLocalPosition;
        Quaternion baseLocalRotation;

        float bobTimer;
        float idleTimer;

        Vector3 currentPos;
        Quaternion currentRot;

        bool isOn = true;

        void Start()
        {
            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;

            currentPos = transform.position;
            currentRot = transform.rotation;

            if (flashlight != null)
                flashlight.enabled = isOn;
        }

        void Update()
        {
            if (cameraTransform == null) return;

            HandleToggle();

            FollowCamera();
            ApplyMotion();
        }

        void HandleToggle()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                isOn = !isOn;

                if (flashlight != null)
                    flashlight.enabled = isOn;

                if (audioSource != null && toggleSound != null)
                    audioSource.PlayOneShot(toggleSound);
            }
        }

        void FollowCamera()
        {
            currentPos = Vector3.Lerp(
                currentPos,
                cameraTransform.position,
                Time.deltaTime * positionFollowSpeed
            );

            transform.position = currentPos;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            Quaternion inertia = Quaternion.Euler(
                -mouseY * inertiaAmount,
                mouseX * inertiaAmount,
                0
            );

            Quaternion targetRotation = cameraTransform.rotation * inertia;

            currentRot = Quaternion.Slerp(
                currentRot,
                targetRotation,
                Time.deltaTime * rotationFollowSpeed
            );

            transform.rotation = currentRot;
        }

        void ApplyMotion()
        {
            float move = Mathf.Abs(Input.GetAxis("Vertical")) + Mathf.Abs(Input.GetAxis("Horizontal"));
            bool running = Input.GetKey(KeyCode.LeftShift);

            Vector3 motionOffset = Vector3.zero;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            Vector3 swayOffset = new Vector3(
                -mouseX * swayAmount,
                -mouseY * swayAmount,
                0
            );

            if (move > 0.1f)
            {
                if (running)
                    bobTimer += Time.deltaTime * runBobSpeed;
                else
                    bobTimer += Time.deltaTime * walkBobSpeed;

                float amount = running ? runBobAmount : walkBobAmount;

                float x = Mathf.Sin(bobTimer) * amount;
                float y = Mathf.Abs(Mathf.Cos(bobTimer)) * amount;

                motionOffset = new Vector3(x, y, 0);
            }
            else
            {
                idleTimer += Time.deltaTime * idleSpeed;

                float x = Mathf.Sin(idleTimer) * idleAmount;
                float y = Mathf.Cos(idleTimer * 0.5f) * idleAmount;

                motionOffset = new Vector3(x, y, 0);
            }

            Vector3 targetLocalPos = baseLocalPosition + motionOffset + swayOffset;

            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetLocalPos,
                Time.deltaTime * 8f
            );

            Quaternion targetLocalRot = baseLocalRotation * Quaternion.Euler(
                motionOffset.y * 0.2f,
                motionOffset.x * 0.2f,
                -swayOffset.x * 2f
            );

            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetLocalRot,
                Time.deltaTime * 10f
            );
        }
    }
}
