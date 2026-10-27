using System.Collections;
using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class AudioPlayScript : MonoBehaviour
    {
        public AudioSource audioSource;
        [Tooltip("Seconds between plays. 0 disables the loop.")]
        public float PlayingPeriod = 0;
        [Tooltip("Seconds to wait before the first play.")]
        public float WaitingForStart = 0;

        private void OnEnable()
        {
            if (audioSource == null || PlayingPeriod <= 0f) return;
            StartCoroutine(PlayLoop());
        }

        private IEnumerator PlayLoop()
        {
            if (WaitingForStart > 0f)
            {
                yield return new WaitForSeconds(WaitingForStart);
            }

            var wait = new WaitForSeconds(PlayingPeriod);
            while (enabled && audioSource != null)
            {
                Play();
                yield return wait;
            }
        }

        public void Play()
        {
            if (audioSource == null) return;
            audioSource.Play();
        }
    }
}
