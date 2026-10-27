using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Camera Movement Sound")]
    [RequireComponent(typeof(AudioSource))]
    public class BodycamCameraMovementSound : MonoBehaviour
    {
        [Header("REFERENCES")]
        public Transform cameraTransform;

        [Header("DETECTION")]
        public float movementThreshold = 90f;
        public float strongMovementThreshold = 220f;
        public float cooldown = 0.35f;

        [Header("AUDIO")]
        public AudioClip[] movementClips;
        [Range(0f, 1f)] public float minVolume = 0.25f;
        [Range(0f, 1f)] public float maxVolume = 0.75f;
        public float pitchMin = 0.95f;
        public float pitchMax = 1.08f;

        AudioSource audioSource;
        Quaternion lastRotation;
        float cooldownTimer;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (cameraTransform != null)
                lastRotation = cameraTransform.rotation;

            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        void Update()
        {
            if (cameraTransform == null)
                return;

            cooldownTimer -= Time.deltaTime;

            float angleDelta = Quaternion.Angle(lastRotation, cameraTransform.rotation);
            float rotationSpeed = angleDelta / Mathf.Max(Time.deltaTime, 0.0001f);

            if (rotationSpeed >= movementThreshold && cooldownTimer <= 0f)
            {
                PlayMovementSound(rotationSpeed);
                cooldownTimer = cooldown;
            }

            lastRotation = cameraTransform.rotation;
        }

        void PlayMovementSound(float rotationSpeed)
        {
            if (movementClips == null || movementClips.Length == 0)
                return;

            float strength = Mathf.InverseLerp(
                movementThreshold,
                strongMovementThreshold,
                rotationSpeed
            );

            float volume = Mathf.Lerp(minVolume, maxVolume, strength);

            AudioClip clip = movementClips[Random.Range(0, movementClips.Length)];

            audioSource.pitch = Random.Range(pitchMin, pitchMax);
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
