using System.Collections.Generic;
using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class ChestScript : MonoBehaviour
    {
        public bool isInteractable = false;
        public List<GameObject> hiddenObjects; 

        void Update()
        {
            if (isInteractable && GameManager.Instance.controllerType == ControllerType.KeyboardMouse)
            {
                // Shares Open() with the mobile button so both paths record the chest in the save.
                if (GameManager.Instance.interactAction.WasReleasedThisFrame())
                {
                    Open();
                }
            }
        }

        public void Open()
        {
            if (isInteractable)
            {
                // On PC both this component and HeroController react to the same interact
                // press, so clear the flag to make the second call in that frame a no-op.
                isInteractable = false;
                GetComponent<Animation>().Play("Sandik_OpeningAnimation");
                HeroController.instance.HideInteractSprite();
                foreach (var item in hiddenObjects)
                {
                    if (item != null) item.SetActive(true);
                }
                AudioManager.instance.Play_Chest_Open();
                GetComponent<Collider>().enabled = false;
                this.enabled = false;
                GameSession.MarkConsumed(gameObject);
            }
        }

        public void RestoreAsOpened()
        {
            foreach (var item in hiddenObjects)
            {
                if (item != null) item.SetActive(true);
            }

            Animation animation = GetComponent<Animation>();
            const string clipName = "Sandik_OpeningAnimation";
            if (animation != null && animation.GetClip(clipName) != null)
            {
                animation.Play(clipName);
                animation[clipName].time = animation[clipName].length;
                animation.Sample();
                animation.Stop();
            }

            Collider chestCollider = GetComponent<Collider>();
            if (chestCollider != null) chestCollider.enabled = false;

            isInteractable = false;
            this.enabled = false;
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isInteractable = true;
                HeroController.instance.ShowInteractSprite();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isInteractable = false;
                HeroController.instance.HideInteractSprite();
            }
        }
    }
}