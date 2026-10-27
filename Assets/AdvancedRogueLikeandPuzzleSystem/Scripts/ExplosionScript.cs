using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class ExplosionScript : MonoBehaviour
    {
        public AudioSource audioSource;

        void Start()
        {
            GameManager.Instance.ShakeTheCamera(0.55f);
            audioSource.Play();
        }
    }
}