using UnityEngine;

namespace PurgeFPS
{
    [AddComponentMenu("Realistic Bodycam/Stepped Animator Gun")]
    public class SteppedAnimatorGun : MonoBehaviour
    {
        [Header("Animators to Control")]
        [Tooltip("Add your Arms and Gun animators here so they step on the exact same frame.")]
        public Animator[] animators;

        [Header("Settings")]
        [Tooltip("Check to animate 'on twos' (choppy/stylized). Uncheck for smooth 60+ fps.")]
        public bool animateOnTwos = true;

        [Tooltip("Target framerate when animating on twos (12 = on twos).")]
        public int targetFrameRate = 12;

        private float timer;
        private float frameInterval;
        private bool wasAnimatingOnTwos;

        void Awake()
        {
            UpdateModeSettings();
        }

        void OnValidate()
        {
            // Allows you to toggle the checkbox in the Inspector during Play Mode
            UpdateModeSettings();
        }

        void UpdateModeSettings()
        {
            frameInterval = 1f / targetFrameRate;

            if (animators == null) return;

            // Enable or disable Unity's native updates depending on mode
            foreach (var anim in animators)
            {
                if (anim != null)
                {
                    anim.enabled = !animateOnTwos;
                }
            }

            wasAnimatingOnTwos = animateOnTwos;
        }

        void Update()
        {
            if (animators == null) return;

            // Catch toggling the bool at runtime via code
            if (animateOnTwos != wasAnimatingOnTwos)
            {
                UpdateModeSettings();
            }

            // If smooth, Unity handles updates automatically; do nothing here
            if (!animateOnTwos) return;

            // --- ON TWOS LOGIC ---
            timer += Time.deltaTime;

            if (timer >= frameInterval)
            {
                // Advance all animators together
                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i] != null)
                    {
                        animators[i].Update(timer);
                    }
                }

                timer %= frameInterval;
            }
        }
    }
}