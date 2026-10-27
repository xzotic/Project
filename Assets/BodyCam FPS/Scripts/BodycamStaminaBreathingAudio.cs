using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Stamina Breathing Audio")]
    public class BodycamStaminaBreathingAudio : MonoBehaviour
    {
        [Header("References")]
        public CharacterController controller;
        public AudioSource tiredLoopSource;

        [Header("Input")]
        public KeyCode runKey = KeyCode.LeftShift;

        [Header("Tired Trigger")]
        public float continuousRunTimeToStart = 6f;

        [Header("Recovery Time")]
        public float recoveryDuration = 4f; // 🔥 tempo que continua após parar

        [Header("Audio Fade")]
        [Range(0f, 1f)] public float tiredVolume = 0.8f;
        public float fadeInSpeed = 3f;
        public float fadeOutSpeed = 2f;

        float runTimer;
        float recoveryTimer;
        bool isTired;
        bool isRecovering;

        void Start()
        {
            if (controller == null)
                controller = GetComponent<CharacterController>();

            if (tiredLoopSource != null)
            {
                tiredLoopSource.loop = true;
                tiredLoopSource.playOnAwake = false;
                tiredLoopSource.volume = 0f;

                if (!tiredLoopSource.isPlaying)
                    tiredLoopSource.Play();
            }
        }

        void Update()
        {
            if (controller == null || tiredLoopSource == null)
                return;

            bool movingForward = Input.GetAxisRaw("Vertical") > 0.1f;
            bool running = Input.GetKey(runKey) && movingForward && controller.isGrounded;

            if (running)
            {
                runTimer += Time.deltaTime;
                isRecovering = false;

                if (runTimer >= continuousRunTimeToStart)
                    isTired = true;
            }
            else
            {
                runTimer = 0f;

                if (isTired)
                {
                    isRecovering = true;
                    recoveryTimer = recoveryDuration;
                    isTired = false;
                }
            }

            // 🔥 Recuperação após correr
            if (isRecovering)
            {
                recoveryTimer -= Time.deltaTime;

                if (recoveryTimer <= 0f)
                    isRecovering = false;
            }

            bool playTiredAudio = isTired || isRecovering;

            float targetVolume = playTiredAudio ? tiredVolume : 0f;
            float fadeSpeed = playTiredAudio ? fadeInSpeed : fadeOutSpeed;

            tiredLoopSource.volume = Mathf.MoveTowards(
                tiredLoopSource.volume,
                targetVolume,
                Time.deltaTime * fadeSpeed
            );
        }
    }
}
