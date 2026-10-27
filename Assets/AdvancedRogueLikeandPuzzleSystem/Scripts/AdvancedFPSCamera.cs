using UnityEngine;
using UnityEngine.InputSystem;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class AdvancedFPSCamera : MonoBehaviour
    {
        public float horizontalRotationSpeed = 250.0f;
        public float verticalRotationSpeed = 150.0f;
        public float rotationDampening = 0.75f;
        public float minVerticalAngle = -60.0f;
        public float maxVerticalAngle = 60.0f;
        public Transform characterBody;
        private float h, v;
        private Quaternion newRotation, smoothRotation;
        private Transform cameraTransform;
        public float fCamShakeImpulse = 0.0f;
        public static AdvancedFPSCamera Instance;
        public Transform FPSPos;

        void Start()
        {
            Initialize();
        }

        private void Awake()
        {
            Instance = this;
        }

        void Initialize()
        {
            h = 0.0f;
            v = this.transform.localEulerAngles.x;

            cameraTransform = this.transform;
            NullErrorCheck();
        }

        void NullErrorCheck()
        {
            if (!characterBody)
            {
                Debug.LogWarning("Character Body is not assigned!");
            }
        }

        void LateUpdate()
        {
            if (GameManager.Instance == null || FPSPos == null) return;

            if (GameManager.Instance.controllerType == ControllerType.Mobile)
            {
                if (Touchpad.Instance == null) return;
                h += Touchpad.Instance.HorizontalValue * horizontalRotationSpeed * 100 * Time.deltaTime;
                v -= Touchpad.Instance.VerticalValue * verticalRotationSpeed * 100 * Time.deltaTime;
            }
            else
            {
                Vector2 vector2 = GameManager.Instance.mouseDeltaAction.ReadValue<Vector2>();
                h += vector2.x * horizontalRotationSpeed * Time.deltaTime;
                v -= vector2.y * verticalRotationSpeed * Time.deltaTime;
            }

            h = ClampAngle(h, -360.0f, 360.0f);
            v = ClampAngle(v, minVerticalAngle, maxVerticalAngle);

            newRotation = Quaternion.Euler(v, h, 0.0f);
            smoothRotation = Quaternion.Slerp(smoothRotation, newRotation, TimeSignature((1 / rotationDampening) * 100.0f));

            smoothRotation.eulerAngles = new Vector3(smoothRotation.eulerAngles.x, smoothRotation.eulerAngles.y, 0.0f);

            cameraTransform.localRotation = Quaternion.Euler(v, 0.0f, 0.0f);
            
            if (characterBody != null)
            {
                characterBody.rotation = Quaternion.Euler(0.0f, smoothRotation.eulerAngles.y, 0.0f);
            }

            transform.position = FPSPos.position;
            transform.rotation = smoothRotation;

            if (fCamShakeImpulse > 0.0f)
                shakeCamera();
        }

        private float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360)
                angle += 360;

            if (angle > 360)
                angle -= 360;

            return Mathf.Clamp(angle, min, max);
        }

        private float TimeSignature(float speed)
        {
            return 1.0f / (1.0f + 80.0f * Mathf.Exp(-speed * 0.02f));
        }

        public void shakeCamera()
        {
            cameraTransform.position += new Vector3(Random.Range(-fCamShakeImpulse, fCamShakeImpulse) / 4, Random.Range(-fCamShakeImpulse, fCamShakeImpulse) / 4, Random.Range(-fCamShakeImpulse, fCamShakeImpulse) / 4);
            fCamShakeImpulse -= Time.deltaTime * fCamShakeImpulse * 4.0f;
            if (fCamShakeImpulse < 0.01f)
            {
                fCamShakeImpulse = 0.0f;
            }
        }
    }
}
