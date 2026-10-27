using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Weapon Flashlight")]
    [DisallowMultipleComponent]
    public class BodycamWeaponFlashlight : MonoBehaviour
    {
        [Header("REFERENCES")]
        [Tooltip("Point Light or Spot Light attached to the weapon.")]
        public Light flashlight;

        public AudioSource audioSource;
        public AudioClip toggleSound;

        [Header("INPUT")]
        public KeyCode toggleKey = KeyCode.F;

        [Header("START STATE")]
        public bool startEnabled = true;

        bool isOn;

        public bool IsOn => isOn;

        void Awake()
        {
            if (flashlight == null)
                flashlight = GetComponentInChildren<Light>(true);

            isOn = startEnabled;
            ApplyLightState();
        }

        void OnEnable()
        {
            ApplyLightState();
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                ToggleFlashlight();
        }

        public void ToggleFlashlight()
        {
            isOn = !isOn;
            ApplyLightState();
            PlayToggleSound();
        }

        public void SetFlashlight(bool enabled)
        {
            isOn = enabled;
            ApplyLightState();
        }

        public void TurnOn()
        {
            SetFlashlight(true);
        }

        public void TurnOff()
        {
            SetFlashlight(false);
        }

        void ApplyLightState()
        {
            if (flashlight != null)
                flashlight.enabled = isOn;
        }

        void PlayToggleSound()
        {
            if (audioSource != null && toggleSound != null)
                audioSource.PlayOneShot(toggleSound);
        }
    }
}
