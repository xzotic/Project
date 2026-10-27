using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Player Health & Diegetic Death")]
    public class BodycamPlayerHealth : MonoBehaviour
    {
        [Header("HEALTH")]
        [Min(1f)] public float maxHealth = 100f;
        public bool invincible;

        [Header("REFERENCES")]
        [Tooltip("The camera transform that will fall when the player dies.")]
        public Transform cameraTransform;
        [Tooltip("Controller, weapon motion and other behaviours that must stop on death.")]
        public Behaviour[] systemsToDisable;
        [Tooltip("Optional. Hidden as soon as the player dies.")]
        public GameObject weaponRoot;

        [Header("DIEGETIC FALL")]
        public bool randomizeFallSide = true;
        [Range(0.05f, 0.5f)] public float lossOfBalanceDuration = 0.14f;
        [Range(0.2f, 2f)] public float fallDuration = 0.68f;
        [Range(0.05f, 0.5f)] public float impactDuration = 0.12f;
        [Range(0.1f, 2f)] public float settleDuration = 0.48f;
        public Vector3 deathLocalPositionOffset = new Vector3(0.08f, -1.35f, 0.18f);
        public Vector3 deathLocalRotation = new Vector3(18f, 4f, 88f);
        [Range(0f, 0.2f)] public float fallShake = 0.035f;
        [Range(0f, 0.2f)] public float impactBounce = 0.07f;

        [Header("DEATH FALL SWAY")]
        public bool enableStrongFallSway = true;
        [Tooltip("Side-to-side displacement while the body is falling.")]
        [Range(0f, 0.3f)] public float fallSwayPositionAmount = 0.11f;
        [Tooltip("Large chest/bodycam rotation during the fall.")]
        [Range(0f, 45f)] public float fallSwayRotationAmount = 20f;
        [Range(0.5f, 5f)] public float fallSwayCycles = 2.15f;
        [Range(0f, 1f)] public float fallSwayIrregularity = 0.3f;

        [Header("FALLEN CAMERA MOTION")]
        public bool enableFallenCameraMotion = true;
        [Tooltip("Small breathing-like movement while the bodycam is on the ground.")]
        [Range(0f, 0.05f)] public float fallenPositionAmount = 0.012f;
        [Range(0f, 5f)] public float fallenRotationAmount = 1.1f;
        [Range(0.1f, 5f)] public float fallenMotionSpeed = 0.85f;
        [Tooltip("Irregular trembling mixed with the slow movement.")]
        [Range(0f, 3f)] public float fallenTrembleAmount = 0.45f;

        [Header("BLACKOUT AFTER IMPACT")]
        public bool enableImpactBlackout = true;
        [Tooltip("Time between the ground impact and the screen starting to go black.")]
        [Range(0f, 1f)] public float blackoutDelay = 0.18f;
        [Range(0.01f, 1f)] public float blackoutFadeDuration = 0.14f;

        [Header("AUDIO")]
        public AudioSource audioSource;
        public AudioClip deathSound;
        public AudioClip groundImpactSound;

        [Header("CAM DISCONNECTED (OPTIONAL)")]
        [Tooltip("Uses the same digital message system shown when the game starts.")]
        public BodycamStartupMessage cameraStatusMessage;
        [Tooltip("Assign a CanvasGroup containing your static/CAM DISCONNECTED image.")]
        public CanvasGroup disconnectedOverlay;
        [Min(0f)] public float disconnectedDelay = 1.1f;
        [Range(0.05f, 2f)] public float disconnectedFadeDuration = 0.18f;

        [Header("RESPAWN (OPTIONAL)")]
        public bool autoRespawn;
        [Min(0f)] public float autoRespawnDelay = 1.25f;

        [Header("EVENTS")]
        public UnityEvent onDamaged;
        public UnityEvent onDeath;
        public UnityEvent onRespawn;

        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        public float HealthNormalized => maxHealth > 0f ? CurrentHealth / maxHealth : 0f;

        Vector3 cameraStartPosition;
        Quaternion cameraStartRotation;
        Vector3 playerStartPosition;
        Quaternion playerStartRotation;
        Coroutine deathRoutine;
        Coroutine fallenMotionRoutine;
        Coroutine blackoutRoutine;
        Texture2D blackTexture;
        float blackoutAlpha;

        void Awake()
        {
            CurrentHealth = maxHealth;
            playerStartPosition = transform.position;
            playerStartRotation = transform.rotation;

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (cameraStatusMessage == null)
                cameraStatusMessage = FindObjectOfType<BodycamStartupMessage>();

            if (cameraTransform != null)
            {
                cameraStartPosition = cameraTransform.localPosition;
                cameraStartRotation = cameraTransform.localRotation;
            }

            blackTexture = new Texture2D(1, 1);
            blackTexture.SetPixel(0, 0, Color.white);
            blackTexture.Apply();
            blackoutAlpha = 0f;

            SetDisconnectedOverlay(0f);
        }

        public void TakeDamage(float damage)
        {
            if (IsDead || invincible || damage <= 0f)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            onDamaged?.Invoke();

            if (CurrentHealth <= 0f)
                Die();
        }

        public void Heal(float amount)
        {
            if (!IsDead && amount > 0f)
                CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }

        public void Kill()
        {
            if (IsDead)
                return;

            CurrentHealth = 0f;
            Die();
        }

        void Die()
        {
            IsDead = true;
            SetSystemsEnabled(false);

            if (weaponRoot != null)
                weaponRoot.SetActive(false);

            if (audioSource != null && deathSound != null)
                audioSource.PlayOneShot(deathSound);

            onDeath?.Invoke();

            if (deathRoutine != null)
                StopCoroutine(deathRoutine);

            deathRoutine = StartCoroutine(DeathSequence());
        }

        IEnumerator DeathSequence()
        {
            if (cameraTransform == null)
                yield break;

            Vector3 startPosition = cameraTransform.localPosition;
            Quaternion startRotation = cameraTransform.localRotation;
            float side = randomizeFallSide && Random.value < 0.5f ? -1f : 1f;

            Quaternion balanceRotation = startRotation * Quaternion.Euler(-7f, side * 5f, -side * 13f);
            yield return AnimateCamera(startPosition, startPosition + new Vector3(-side * 0.025f, -0.04f, 0f),
                startRotation, balanceRotation, lossOfBalanceDuration, 0.25f);

            Vector3 groundPosition = cameraStartPosition + Vector3.Scale(deathLocalPositionOffset, new Vector3(side, 1f, 1f));
            Quaternion groundRotation = cameraStartRotation * Quaternion.Euler(
                deathLocalRotation.x, deathLocalRotation.y * side, deathLocalRotation.z * side);

            yield return AnimateFallCamera(cameraTransform.localPosition, groundPosition,
                cameraTransform.localRotation, groundRotation, fallDuration, fallShake, side);

            if (audioSource != null && groundImpactSound != null)
                audioSource.PlayOneShot(groundImpactSound);

            if (enableImpactBlackout)
                blackoutRoutine = StartCoroutine(BlackoutAfterImpact());

            Vector3 impactPosition = groundPosition + new Vector3(0f, impactBounce, -impactBounce * 0.35f);
            Quaternion impactRotation = groundRotation * Quaternion.Euler(-4f, 0f, -side * 3f);
            yield return AnimateCamera(groundPosition, impactPosition, groundRotation, impactRotation,
                impactDuration, fallShake * 1.7f);

            yield return AnimateCamera(impactPosition, groundPosition, impactRotation, groundRotation,
                settleDuration, fallShake * 0.25f);

            if (enableFallenCameraMotion)
                fallenMotionRoutine = StartCoroutine(FallenCameraMotion(groundPosition, groundRotation));

            float disconnectedMessageDuration = 0f;

            if (cameraStatusMessage != null || disconnectedOverlay != null)
            {
                yield return new WaitForSeconds(disconnectedDelay);

                if (cameraStatusMessage != null)
                {
                    cameraStatusMessage.ShowDisconnected();
                    disconnectedMessageDuration = cameraStatusMessage.TotalSequenceDuration;
                }

                if (disconnectedOverlay != null)
                    yield return FadeDisconnectedOverlay();
            }

            if (autoRespawn)
            {
                float safeRespawnDelay = Mathf.Max(autoRespawnDelay, disconnectedMessageDuration);
                yield return new WaitForSeconds(safeRespawnDelay);
                Respawn();
            }
        }

        IEnumerator FallenCameraMotion(Vector3 basePosition, Quaternion baseRotation)
        {
            float timer = 0f;
            float seed = Random.Range(0f, 100f);

            while (IsDead)
            {
                timer += Time.deltaTime * fallenMotionSpeed;

                float slowX = Mathf.Sin(timer * 0.73f);
                float slowY = Mathf.Sin(timer * 1.17f + 1.4f);
                float breathing = Mathf.Sin(timer * 1.85f);

                float trembleX = (Mathf.PerlinNoise(seed, timer * 3.7f) - 0.5f) * 2f;
                float trembleY = (Mathf.PerlinNoise(seed + 19f, timer * 4.3f) - 0.5f) * 2f;
                float trembleZ = (Mathf.PerlinNoise(seed + 37f, timer * 3.1f) - 0.5f) * 2f;

                Vector3 positionMotion = new Vector3(
                    slowX * fallenPositionAmount * 0.45f,
                    breathing * fallenPositionAmount,
                    slowY * fallenPositionAmount * 0.3f
                );

                Vector3 rotationMotion = new Vector3(
                    slowY * fallenRotationAmount + trembleX * fallenTrembleAmount,
                    slowX * fallenRotationAmount * 0.55f + trembleY * fallenTrembleAmount,
                    breathing * fallenRotationAmount * 0.35f + trembleZ * fallenTrembleAmount
                );

                cameraTransform.localPosition = basePosition + positionMotion;
                cameraTransform.localRotation = baseRotation * Quaternion.Euler(rotationMotion);
                yield return null;
            }
        }

        IEnumerator BlackoutAfterImpact()
        {
            if (blackoutDelay > 0f)
                yield return new WaitForSeconds(blackoutDelay);

            float elapsed = 0f;

            while (elapsed < blackoutFadeDuration)
            {
                elapsed += Time.deltaTime;
                blackoutAlpha = blackoutFadeDuration > 0f
                    ? Mathf.Clamp01(elapsed / blackoutFadeDuration)
                    : 1f;
                yield return null;
            }

            blackoutAlpha = 1f;
            blackoutRoutine = null;
        }

        void OnGUI()
        {
            if (blackoutAlpha <= 0f || blackTexture == null)
                return;

            GUI.depth = 1000;
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, blackoutAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), blackTexture);
            GUI.color = previousColor;
        }

        IEnumerator AnimateCamera(Vector3 fromPosition, Vector3 toPosition,
            Quaternion fromRotation, Quaternion toRotation, float duration, float shakeAmount)
        {
            float elapsed = 0f;
            float seed = Random.Range(0f, 100f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                float smoothT = t * t * (3f - 2f * t);
                float remainingShake = shakeAmount * (1f - t);

                Vector3 shake = new Vector3(
                    Mathf.PerlinNoise(seed, elapsed * 24f) - 0.5f,
                    Mathf.PerlinNoise(seed + 11f, elapsed * 27f) - 0.5f,
                    Mathf.PerlinNoise(seed + 23f, elapsed * 21f) - 0.5f
                ) * remainingShake;

                cameraTransform.localPosition = Vector3.LerpUnclamped(fromPosition, toPosition, smoothT) + shake;
                cameraTransform.localRotation = Quaternion.SlerpUnclamped(fromRotation, toRotation, smoothT) *
                                                Quaternion.Euler(shake * 80f);
                yield return null;
            }

            cameraTransform.localPosition = toPosition;
            cameraTransform.localRotation = toRotation;
        }

        IEnumerator AnimateFallCamera(Vector3 fromPosition, Vector3 toPosition,
            Quaternion fromRotation, Quaternion toRotation, float duration, float shakeAmount, float fallSide)
        {
            float elapsed = 0f;
            float seed = Random.Range(0f, 100f);
            float phase = Random.Range(-0.35f, 0.35f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                float smoothT = t * t * (3f - 2f * t);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float wave = Mathf.Sin((t * fallSwayCycles * Mathf.PI * 2f) + phase);
                float secondaryWave = Mathf.Sin((t * fallSwayCycles * 1.47f * Mathf.PI * 2f) + 1.2f);

                float noise = (Mathf.PerlinNoise(seed, elapsed * 7f) - 0.5f) * 2f;
                float irregularWave = wave + noise * fallSwayIrregularity;

                Vector3 swayPosition = Vector3.zero;
                Vector3 swayRotation = Vector3.zero;

                if (enableStrongFallSway)
                {
                    swayPosition = new Vector3(
                        irregularWave * fallSwayPositionAmount * envelope,
                        secondaryWave * fallSwayPositionAmount * 0.22f * envelope,
                        -Mathf.Abs(wave) * fallSwayPositionAmount * 0.3f * envelope
                    );

                    swayRotation = new Vector3(
                        secondaryWave * fallSwayRotationAmount * 0.55f * envelope,
                        irregularWave * fallSwayRotationAmount * 0.7f * envelope,
                        -irregularWave * fallSwayRotationAmount * fallSide * envelope
                    );
                }

                float remainingShake = shakeAmount * (1f - t);
                Vector3 shake = new Vector3(
                    Mathf.PerlinNoise(seed + 13f, elapsed * 24f) - 0.5f,
                    Mathf.PerlinNoise(seed + 29f, elapsed * 27f) - 0.5f,
                    Mathf.PerlinNoise(seed + 41f, elapsed * 21f) - 0.5f
                ) * remainingShake;

                cameraTransform.localPosition = Vector3.LerpUnclamped(fromPosition, toPosition, smoothT) +
                                                swayPosition + shake;
                cameraTransform.localRotation = Quaternion.SlerpUnclamped(fromRotation, toRotation, smoothT) *
                                                Quaternion.Euler(swayRotation + shake * 80f);
                yield return null;
            }

            cameraTransform.localPosition = toPosition;
            cameraTransform.localRotation = toRotation;
        }

        IEnumerator FadeDisconnectedOverlay()
        {
            float elapsed = 0f;
            disconnectedOverlay.gameObject.SetActive(true);

            while (elapsed < disconnectedFadeDuration)
            {
                elapsed += Time.deltaTime;
                disconnectedOverlay.alpha = disconnectedFadeDuration > 0f
                    ? Mathf.Clamp01(elapsed / disconnectedFadeDuration)
                    : 1f;
                yield return null;
            }

            SetDisconnectedOverlay(1f);
        }

        public void Respawn()
        {
            if (blackoutRoutine != null)
            {
                StopCoroutine(blackoutRoutine);
                blackoutRoutine = null;
            }

            blackoutAlpha = 0f;

            if (fallenMotionRoutine != null)
            {
                StopCoroutine(fallenMotionRoutine);
                fallenMotionRoutine = null;
            }

            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
                deathRoutine = null;
            }

            CharacterController characterController = GetComponent<CharacterController>();
            if (characterController != null)
                characterController.enabled = false;

            transform.SetPositionAndRotation(playerStartPosition, playerStartRotation);

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = cameraStartPosition;
                cameraTransform.localRotation = cameraStartRotation;
            }

            if (characterController != null)
                characterController.enabled = true;

            CurrentHealth = maxHealth;
            IsDead = false;
            SetDisconnectedOverlay(0f);
            SetSystemsEnabled(true);

            if (weaponRoot != null)
                weaponRoot.SetActive(true);

            if (cameraStatusMessage != null)
                cameraStatusMessage.ShowConnected();

            onRespawn?.Invoke();
        }

        void OnDestroy()
        {
            if (blackTexture != null)
                Destroy(blackTexture);
        }

        void SetSystemsEnabled(bool value)
        {
            if (systemsToDisable == null)
                return;

            foreach (Behaviour system in systemsToDisable)
            {
                if (system != null && system != this)
                    system.enabled = value;
            }
        }

        void SetDisconnectedOverlay(float alpha)
        {
            if (disconnectedOverlay == null)
                return;

            disconnectedOverlay.alpha = alpha;
            disconnectedOverlay.interactable = false;
            disconnectedOverlay.blocksRaycasts = alpha > 0.99f;
            disconnectedOverlay.gameObject.SetActive(alpha > 0f);
        }
    }
}
