using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class ElevatorScript : MonoBehaviour
    {
        [FormerlySerializedAs("asansor")]
        public GameObject elevatorPlatform;
        [FormerlySerializedAs("ustKat")]
        public Transform topFloor;
        [FormerlySerializedAs("altKat")]
        public Transform bottomFloor;
        [FormerlySerializedAs("leftCark")]
        public GameObject leftGear;
        [FormerlySerializedAs("rightCark")]
        public GameObject rightGear;
        public GameObject arm;
        public bool isInteractable = false;
        public bool isOntheWay = false;
        public bool isDown = true;
        public AudioSource audioSource;

        /// <summary>
        /// Pulls the lever: parents the hero to the platform, locks their movement, and after a
        /// short lever animation moves the platform to the other floor (Update drives the travel).
        /// Called by the interact key on PC and by the interact button on mobile.
        /// </summary>
        public void Activate()
        {
            if (!isOntheWay)
            {
                HeroController.instance.transform.parent = elevatorPlatform.transform;
                HeroController.instance.thirdPersonController._animator.SetFloat(HeroController.instance.thirdPersonController._animIDSpeed, 0);
                HeroController.instance.characterController.enabled = false;
                HeroController.instance.thirdPersonController.enabled = false;
                arm.GetComponent<Animation>().Play();
                HeroController.instance.HideInteractSprite();
                AudioManager.instance.Play_Arm_Pulling();
                StartCoroutine(StartLiftJourney());
                if (isDown)
                {
                    isDown = false;
                    StartCoroutine(StartElevatorSound());
                }
                else
                {
                    isDown = true;
                    StartCoroutine(StartElevatorSound());
                }
            }
        }

        void Update()
        {
            if (isInteractable)
            {
                if (GameManager.Instance.controllerType == ControllerType.KeyboardMouse)
                {
                    if (GameManager.Instance.interactAction.WasReleasedThisFrame())
                    {
                        Activate();
                    }
                }
            }
            if(isOntheWay)
            {
                if(isDown)
                {
                    leftGear.transform.Rotate(Vector3.forward * 1f);
                    rightGear.transform.Rotate(Vector3.forward * 1f);
                    elevatorPlatform.transform.position = Vector3.MoveTowards(elevatorPlatform.transform.position, bottomFloor.transform.position, Time.deltaTime * 1.5f);
                    if(elevatorPlatform.transform.position.y <= bottomFloor.transform.position.y+0.5f)
                    {
                        isOntheWay = false;
                        HeroController.instance.transform.parent = null;
                        HeroController.instance.characterController.enabled = true;
                        HeroController.instance.thirdPersonController.enabled = true;
                        GameManager.Instance.ShakeTheCamera(0.5f);
                        audioSource.Stop();
                    }
                }
                else
                {
                    leftGear.transform.Rotate(Vector3.forward * -1f);
                    rightGear.transform.Rotate(Vector3.forward * -1f);
                    elevatorPlatform.transform.position = Vector3.MoveTowards(elevatorPlatform.transform.position, topFloor.transform.position, Time.deltaTime * 1.5f);
                    if(elevatorPlatform.transform.position.y >= topFloor.transform.position.y)
                    {
                        isOntheWay = false;
                        HeroController.instance.transform.parent = null;
                        HeroController.instance.characterController.enabled = true;
                        HeroController.instance.thirdPersonController.enabled = true;
                        GameManager.Instance.ShakeTheCamera(0.5f);
                        audioSource.Stop();
                    }
                }
            }
        }

        IEnumerator StartElevatorSound()
        {
            yield return new WaitForSeconds(1);
            audioSource.Play();
        }

        IEnumerator StartLiftJourney()
        {
            yield return new WaitForSeconds(1.5f);
            isOntheWay = true;
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