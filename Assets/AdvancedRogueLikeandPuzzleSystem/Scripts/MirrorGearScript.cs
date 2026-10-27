using UnityEngine;
using UnityEngine.Serialization;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class MirrorGearScript : MonoBehaviour
    {
        public GameObject mirror;
        public bool isInteractable = false;
        [FormerlySerializedAs("cark")]
        public GameObject gear;
        bool interacted = false;
        public AudioSource audioSource;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
        }

        /// <summary>
        /// Toggles "mirror mode": the hero is frozen, the TPS camera pulls back 4 units so the
        /// light beam is visible, and horizontal input rotates the mirror (see Update). A second
        /// call restores the camera and hero. The mirror reflects the "Light" beam that opens doors.
        /// </summary>
        public void Activate()
        {
            if (!interacted)
            {
                interacted = true;
                AdvancedTPSCamera.Instance.distance = AdvancedTPSCamera.Instance.distance + 4;
                AdvancedTPSCamera.Instance.maxDistance = AdvancedTPSCamera.Instance.maxDistance + 4;
                AdvancedTPSCamera.Instance.minDistance = AdvancedTPSCamera.Instance.minDistance + 4;
                HeroController.instance.characterController.enabled = false;
                HeroController.instance.thirdPersonController._animator.SetFloat(HeroController.instance.thirdPersonController._animIDSpeed, 0);
                HeroController.instance.thirdPersonController.enabled = false;
                GameCanvas_Controller.instance.Show_MirrorUI(true);
            }
            else
            {
                interacted = false;
                GameCanvas_Controller.instance.Show_MirrorUI(false);
                AdvancedTPSCamera.Instance.distance = AdvancedTPSCamera.Instance.distance - 4;
                AdvancedTPSCamera.Instance.maxDistance = AdvancedTPSCamera.Instance.maxDistance - 4;
                AdvancedTPSCamera.Instance.minDistance = AdvancedTPSCamera.Instance.minDistance - 4;
                HeroController.instance.characterController.enabled = true;
                HeroController.instance.thirdPersonController.enabled = true;
                audioSource.Stop();
            }
        }

        void Update()
        {
            if (isInteractable)
            {
                if(GameManager.Instance.controllerType == ControllerType.KeyboardMouse)
                {
                    if (GameManager.Instance.interactAction.WasReleasedThisFrame())
                    {
                        Activate();
                    }
                }
                
                if(interacted)
                {
                    float h = 0;
                    if(GameManager.Instance.controllerType == ControllerType.KeyboardMouse)
                    {
                        h = GameManager.Instance.moveAction.ReadValue<Vector2>().x * Time.deltaTime * 3;
                    }
                    else
                    {
                        h = SimpleJoystick.Instance.HorizontalValue * Time.deltaTime * 3;
                    }
                    mirror.transform.Rotate(new Vector3(0, h, 0));
                    HeroController.instance.thirdPersonController._animator.SetFloat(HeroController.instance.thirdPersonController._animIDSpeed, 0);
                    gear.transform.Rotate(new Vector3(0, 0, h * 5));
                    if(Mathf.Abs(h) > 0)
                    {
                        if (!audioSource.isPlaying)
                        {
                            audioSource.Play();
                        }
                    }
                    else
                    {
                        audioSource.Stop();
                    }
                }
            }
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
                interacted = false;
            }
        }
    }
}