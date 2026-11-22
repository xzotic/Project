using UnityEngine;

namespace PurgeFPS
{
    /// <summary>
    /// Shakes the player's camera when they take damage (stronger for bigger hits) and when they parry.
    ///
    /// Put this on the player (next to BodycamPlayerHealth). References are found automatically.
    /// It doesn't change any other script:
    ///  - Damage is measured from the player's health, after blocking has reduced it, so a blocked hit
    ///    shakes less than an unblocked one, and a parry (0 damage) gets its own shake.
    ///  - Parries come from BodycamViewmodelMelee.BlockResolved.
    ///  - The shake itself is BodycamFPSController.AddCameraShake.
    ///
    /// Intensity is in degrees of camera rotation. Around 0.3 is a light nudge (your dodge uses 0.3),
    /// 3 or more is a heavy hit.
    /// </summary>
    [AddComponentMenu("Player/Hit Shake")]
    public class PlayerHitShake : MonoBehaviour
    {
        [Header("REFERENCES (auto-found if empty)")]
        public BodycamPlayerHealth health;
        public BodycamFPSController controller;
        public BodycamViewmodelMelee melee;

        [Header("DAMAGE SHAKE")]
        public bool shakeOnDamage = true;

        [Tooltip("Hits smaller than this don't shake the camera.")]
        public float minDamageToShake = 1f;

        [Tooltip("A hit of this much damage (or more) gives the full shake.")]
        public float fullShakeDamage = 50f;

        [Tooltip("Maps damage (0 to 1 of the full-shake damage) to shake strength (0 to 1). Straight line = proportional.")]
        public AnimationCurve strengthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Camera shake in degrees for a small hit.")]
        public float minIntensity = 0.5f;

        [Tooltip("Camera shake in degrees for a full-strength hit.")]
        public float maxIntensity = 3f;

        public float minDuration = 0.15f;
        public float maxDuration = 0.5f;

        [Tooltip("How fast the shake wobbles. Higher = more rattling.")]
        public float frequency = 22f;

        [Header("PARRY SHAKE")]
        public bool shakeOnParry = true;
        public float parryIntensity = 1.4f;
        public float parryDuration = 0.2f;

        [Tooltip("Parries use a faster, sharper wobble than damage.")]
        public float parryFrequency = 35f;

        [Header("DEBUG")]
        [Tooltip("Writes every shake to the Console with the damage and the values used.")]
        public bool logShakes = true;

        float lastHealth;
        float lastIntensity;
        float lastDuration;
        float lastStartTime = -999f;
        float nextMeleeSearch;
        bool warnedNoController;

        void Start()
        {
            if (health == null)
                health = GetComponent<BodycamPlayerHealth>();

            if (health == null)
                health = FindFirstObjectByType<BodycamPlayerHealth>();

            if (controller == null)
                controller = FindFirstObjectByType<BodycamFPSController>();

            if (health != null)
                lastHealth = health.CurrentHealth;

            TryFindMelee();
        }

        void OnDisable()
        {
            if (melee != null)
                melee.BlockResolved -= OnBlockResolved;
        }

        void Update()
        {
            if (melee == null && Time.time >= nextMeleeSearch)
                TryFindMelee();

            if (health == null)
                return;

            float current = health.CurrentHealth;
            float lost = lastHealth - current;
            lastHealth = current;

            if (lost > 0.001f && !health.IsDead)
                OnDamageTaken(lost);
        }

        void TryFindMelee()
        {
            nextMeleeSearch = Time.time + 0.5f;

            if (melee == null && health != null)
            {
                GameObject root = health.weaponRoot != null ? health.weaponRoot : health.gameObject;
                melee = root.GetComponentInChildren<BodycamViewmodelMelee>(true);
            }

            if (melee == null)
                melee = FindFirstObjectByType<BodycamViewmodelMelee>();

            if (melee != null)
            {
                melee.BlockResolved -= OnBlockResolved;   // never subscribe twice
                melee.BlockResolved += OnBlockResolved;
            }
        }

        void OnDamageTaken(float damage)
        {
            if (!shakeOnDamage || damage < minDamageToShake)
                return;

            float t = strengthCurve.Evaluate(Mathf.Clamp01(damage / Mathf.Max(fullShakeDamage, 0.01f)));
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
            float duration = Mathf.Lerp(minDuration, maxDuration, t);

            Shake(duration, intensity, frequency, "DAMAGE " + damage.ToString("0.0") + " (" + (t * 100f).ToString("0") + "% of full)");
        }

        void OnBlockResolved(BlockResult result)
        {
            if (result == BlockResult.Parried && shakeOnParry)
                Shake(parryDuration, parryIntensity, parryFrequency, "PARRY");
        }

        void Shake(float duration, float intensity, float freq, string reason)
        {
            if (controller == null)
            {
                if (!warnedNoController)
                {
                    warnedNoController = true;
                    Debug.LogWarning("[PlayerHitShake] No BodycamFPSController found, so there is nothing to shake.", this);
                }

                return;
            }

            // Don't let a weaker shake cut off a stronger one that is still fading out.
            float elapsed = Time.time - lastStartTime;
            float remaining = lastDuration > 0f ? lastIntensity * Mathf.Clamp01(1f - elapsed / lastDuration) : 0f;

            if (remaining > intensity)
            {
                if (logShakes)
                    Debug.Log("[PlayerHitShake] " + reason + " - shake skipped (a stronger shake is still playing: " + remaining.ToString("0.00") + " > " + intensity.ToString("0.00") + ")", this);

                return;
            }

            controller.AddCameraShake(duration, intensity, freq);

            lastIntensity = intensity;
            lastDuration = duration;
            lastStartTime = Time.time;

            if (logShakes)
                Debug.Log("[PlayerHitShake] " + reason + " -> shake  intensity " + intensity.ToString("0.00") + " deg, duration " + duration.ToString("0.00") + "s, frequency " + freq.ToString("0"), this);
        }
    }
}