using System.Collections;
using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class SpikeTrapScript : MonoBehaviour
    {
        public int Damage;
        bool userTouched = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                userTouched = true;
                StartCoroutine(RaiseSpikes());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                userTouched = false;
            }
        }

        IEnumerator RaiseSpikes()
        {
            yield return new WaitForSeconds(0.5f);
            GetComponent<Animation>().Play();
            AudioManager.instance.Play_Door_KnifeOut();
            yield return new WaitForSeconds(0.5f);
            if (userTouched)
            {
                HeroController.instance.GetDamagePlayer(Damage);
            }
        }
    }
}