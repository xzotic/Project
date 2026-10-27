using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Footstep Audio Advanced")]
    [RequireComponent(typeof(AudioSource))]
    public class BodycamFootstepAudioAdvanced : MonoBehaviour
    {
        [Header("REFERENCES")]
        public CharacterController controller;

        [Header("INPUT")]
        public KeyCode runKey = KeyCode.LeftShift;

        [Header("WALK FOOTSTEPS")]
        public AudioClip[] walkClips;
        public float walkStepInterval = 0.55f;
        public float walkVolume = 0.5f;

        [Header("RUN FOOTSTEPS")]
        public AudioClip[] runClips;
        public float runStepInterval = 0.32f;
        public float runVolume = 0.8f;

        [Header("AUDIO VARIATION")]
        public float pitchMin = 0.95f;
        public float pitchMax = 1.05f;

        AudioSource audioSource;
        float stepTimer;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            if (controller == null)
                controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            if (controller == null)
                return;

            bool isMoving = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
            bool isRunning = Input.GetKey(runKey) && Input.GetAxisRaw("Vertical") > 0.1f;

            if (!controller.isGrounded || !isMoving)
            {
                stepTimer = 0f;
                return;
            }

            stepTimer += Time.deltaTime;

            float interval = isRunning ? runStepInterval : walkStepInterval;

            if (stepTimer >= interval)
            {
                PlayFootstep(isRunning);
                stepTimer = 0f;
            }
        }

        void PlayFootstep(bool running)
        {
            AudioClip[] clips = running ? runClips : walkClips;

            if (clips == null || clips.Length == 0)
                return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];

            audioSource.pitch = Random.Range(pitchMin, pitchMax);
            audioSource.PlayOneShot(clip, running ? runVolume : walkVolume);
        }
    }
}
