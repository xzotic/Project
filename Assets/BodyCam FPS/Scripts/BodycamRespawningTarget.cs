using System.Collections;
using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Respawning Target")]
    public class BodycamRespawningTarget : MonoBehaviour
    {
        [Header("ROTATION")]
        [Tooltip("Uses the target's current scene rotation as its standing rotation.")]
        public bool useCurrentRotationAsStandingRotation = true;

        [Tooltip("Rotation added to the standing rotation when the target falls.")]
        public Vector3 downRotationOffset = new Vector3(0f, 0f, 90f);

        [Tooltip("Absolute standing rotation used when Use Current Rotation is disabled.")]
        public Vector3 standingRotation = Vector3.zero;

        [Header("TIMING")]
        [Min(0.01f)]
        public float fallDuration = 0.18f;

        [Min(0f)]
        [Tooltip("How long the target stays down before rising again.")]
        public float timeBeforeStanding = 2f;

        [Min(0.01f)]
        public float riseDuration = 0.3f;

        public AnimationCurve movementCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("OPTIONAL AUDIO")]
        public AudioSource audioSource;
        public AudioClip hitSound;
        public AudioClip riseSound;

        Quaternion cachedStandingRotation;
        Quaternion cachedDownRotation;
        Coroutine respawnRoutine;
        bool isCycling;

        public bool IsDown { get; private set; }

        void Awake()
        {
            cachedStandingRotation = useCurrentRotationAsStandingRotation
                ? transform.localRotation
                : Quaternion.Euler(standingRotation);

            cachedDownRotation =
                cachedStandingRotation * Quaternion.Euler(downRotationOffset);

            transform.localRotation = cachedStandingRotation;
        }

        public void TakeDamage(float damage)
        {
            HitTarget();
        }

        public void HitTarget()
        {
            if (isCycling)
                return;

            if (respawnRoutine != null)
                StopCoroutine(respawnRoutine);

            respawnRoutine = StartCoroutine(FallAndReturnRoutine());
        }

        IEnumerator FallAndReturnRoutine()
        {
            isCycling = true;

            PlaySound(hitSound);
            yield return RotateTo(cachedDownRotation, fallDuration);

            IsDown = true;

            if (timeBeforeStanding > 0f)
                yield return new WaitForSeconds(timeBeforeStanding);

            PlaySound(riseSound);
            yield return RotateTo(cachedStandingRotation, riseDuration);

            IsDown = false;
            isCycling = false;
            respawnRoutine = null;
        }

        IEnumerator RotateTo(Quaternion targetRotation, float duration)
        {
            Quaternion startingRotation = transform.localRotation;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
                float curvedTime = movementCurve.Evaluate(normalizedTime);

                transform.localRotation = Quaternion.Slerp(
                    startingRotation,
                    targetRotation,
                    curvedTime
                );

                yield return null;
            }

            transform.localRotation = targetRotation;
        }

        void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        public void ResetTargetImmediately()
        {
            if (respawnRoutine != null)
                StopCoroutine(respawnRoutine);

            transform.localRotation = cachedStandingRotation;
            IsDown = false;
            isCycling = false;
            respawnRoutine = null;
        }
    }
}
