using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Wind Sound Simple")]
    [RequireComponent(typeof(AudioSource))]
    public class BodycamWindSoundSimple : MonoBehaviour
    {
        [Header("INPUT")]
        public KeyCode runKey = KeyCode.LeftShift;

        [Header("AUDIO")]
        public float targetVolume = 1f;
        public float fadeInSpeed = 5f;
        public float fadeOutSpeed = 4f;

        AudioSource audioSource;
        bool shouldPlay;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 0f;

            if (!audioSource.isPlaying)
                audioSource.Play();
        }

        void Update()
        {
            bool running = Input.GetKey(runKey) && Input.GetAxisRaw("Vertical") > 0.1f;

            shouldPlay = running;

            float target = shouldPlay ? targetVolume : 0f;
            float speed = shouldPlay ? fadeInSpeed : fadeOutSpeed;

            audioSource.volume = Mathf.MoveTowards(
                audioSource.volume,
                target,
                Time.deltaTime * speed
            );
        }
    }
}
