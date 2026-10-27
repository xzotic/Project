using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/One Time Collision Sound")]
    public class BodycamOneShotCollisionSound : MonoBehaviour
    {
        [Header("AUDIO")]
        public AudioSource audioSource;
        public AudioClip sound;

        [Header("SETTINGS")]
        public bool destroyAfterPlay = false;

        bool hasPlayed = false;

        void OnTriggerEnter(Collider other)
        {
            if (hasPlayed)
                return;

            PlaySound();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (hasPlayed)
                return;

            PlaySound();
        }

        void PlaySound()
        {
            if (audioSource != null && sound != null)
            {
                audioSource.PlayOneShot(sound);
            }

            hasPlayed = true;

            if (destroyAfterPlay)
            {
                Destroy(gameObject, 0.1f);
            }
        }
    }
}
