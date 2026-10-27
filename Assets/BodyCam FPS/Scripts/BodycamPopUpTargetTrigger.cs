using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Pop Up Target Trigger")]
    [RequireComponent(typeof(Collider))]
    public class BodycamPopUpTargetTrigger : MonoBehaviour
    {
        [Header("TARGETS")]
        [Tooltip("Targets that will rise when the player enters this trigger.")]
        public Transform[] targets = new Transform[0];

        [Tooltip("Uses each target's rotation in the scene as its individual standing rotation. This allows every target to face a different direction.")]
        public bool useCurrentRotationAsUpRotation = true;

        [Tooltip("When using the current rotation, this is added as the lying-down rotation offset.")]
        public Vector3 downRotation = new Vector3(0f, 0f, 90f);

        [Tooltip("Absolute standing rotation used only when Use Current Rotation As Up Rotation is disabled.")]
        public Vector3 upRotation = Vector3.zero;

        public bool startTargetsDown = true;

        [Header("MOVEMENT")]
        [Min(0.01f)]
        public float movementDuration = 0.25f;

        [Min(0f)]
        [Tooltip("Delay between each target. Use 0 to raise all targets together.")]
        public float delayBetweenTargets = 0f;

        public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Min(0.01f)]
        [Tooltip("How quickly a target falls after being hit by a bullet.")]
        public float hitFallDuration = 0.18f;

        [Header("TRIGGER")]
        public string playerTag = "Player";
        public bool oneShot = true;

        [Header("OPTIONAL RESET")]
        public bool resetAfterDelay = false;

        [Min(0f)]
        public float resetDelay = 3f;

        [Header("OPTIONAL AUDIO")]
        public AudioSource audioSource;
        public AudioClip targetUpSound;
        public AudioClip targetHitSound;

        bool hasActivated;
        Coroutine sequenceRoutine;
        readonly Dictionary<Transform, Coroutine> targetRotationRoutines =
            new Dictionary<Transform, Coroutine>();
        readonly Dictionary<Transform, Quaternion> targetUpRotations =
            new Dictionary<Transform, Quaternion>();
        readonly Dictionary<Transform, Quaternion> targetDownRotations =
            new Dictionary<Transform, Quaternion>();

        void Start()
        {
            CacheTargetRotations();
            PrepareShootableTargets();

            if (!startTargetsDown)
                return;

            SetAllTargetsDown();
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag))
                return;

            ActivateTargets();
        }

        public void ActivateTargets()
        {
            if (hasActivated)
                return;

            hasActivated = true;

            if (sequenceRoutine != null)
                StopCoroutine(sequenceRoutine);

            sequenceRoutine = StartCoroutine(TargetSequence());
        }

        IEnumerator TargetSequence()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                Transform target = targets[i];

                if (target != null)
                {
                    StartTargetRotation(target, GetUpRotation(target), movementDuration);

                    if (audioSource != null && targetUpSound != null)
                        audioSource.PlayOneShot(targetUpSound);
                }

                if (delayBetweenTargets > 0f && i < targets.Length - 1)
                    yield return new WaitForSeconds(delayBetweenTargets);
            }

            if (resetAfterDelay && !oneShot)
            {
                yield return new WaitForSeconds(movementDuration + resetDelay);

                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] != null)
                    {
                        StartTargetRotation(
                            targets[i],
                            GetDownRotation(targets[i]),
                            movementDuration
                        );
                    }

                    if (delayBetweenTargets > 0f && i < targets.Length - 1)
                        yield return new WaitForSeconds(delayBetweenTargets);
                }

                yield return new WaitForSeconds(movementDuration);
                hasActivated = false;
            }

            sequenceRoutine = null;
        }

        public void KnockDownTarget(Transform target)
        {
            if (target == null)
                return;

            StartTargetRotation(target, GetDownRotation(target), hitFallDuration);

            if (audioSource != null && targetHitSound != null)
                audioSource.PlayOneShot(targetHitSound);
        }

        void StartTargetRotation(Transform target, Quaternion targetRotation, float duration)
        {
            if (targetRotationRoutines.TryGetValue(target, out Coroutine activeRoutine) &&
                activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }

            targetRotationRoutines[target] = StartCoroutine(
                RotateTarget(target, targetRotation, duration)
            );
        }

        IEnumerator RotateTarget(Transform target, Quaternion targetRotation, float duration)
        {
            Quaternion startingRotation = target.localRotation;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
                float curvedTime = movementCurve.Evaluate(normalizedTime);

                target.localRotation = Quaternion.Slerp(
                    startingRotation,
                    targetRotation,
                    curvedTime
                );

                yield return null;
            }

            target.localRotation = targetRotation;
            targetRotationRoutines.Remove(target);
        }

        void CacheTargetRotations()
        {
            targetUpRotations.Clear();
            targetDownRotations.Clear();

            foreach (Transform target in targets)
            {
                if (target == null)
                    continue;

                Quaternion standingRotation = useCurrentRotationAsUpRotation
                    ? target.localRotation
                    : Quaternion.Euler(upRotation);

                Quaternion lyingRotation = useCurrentRotationAsUpRotation
                    ? standingRotation * Quaternion.Euler(downRotation)
                    : Quaternion.Euler(downRotation);

                targetUpRotations[target] = standingRotation;
                targetDownRotations[target] = lyingRotation;
            }
        }

        Quaternion GetUpRotation(Transform target)
        {
            return targetUpRotations.TryGetValue(target, out Quaternion rotation)
                ? rotation
                : Quaternion.Euler(upRotation);
        }

        Quaternion GetDownRotation(Transform target)
        {
            return targetDownRotations.TryGetValue(target, out Quaternion rotation)
                ? rotation
                : Quaternion.Euler(downRotation);
        }

        void PrepareShootableTargets()
        {
            foreach (Transform target in targets)
            {
                if (target == null)
                    continue;

                BodycamShootableTarget shootableTarget =
                    target.GetComponent<BodycamShootableTarget>();

                if (shootableTarget == null)
                    shootableTarget = target.gameObject.AddComponent<BodycamShootableTarget>();

                shootableTarget.Initialize(this, target);
            }
        }

        void SetAllTargetsDown()
        {
            foreach (Transform target in targets)
            {
                if (target != null)
                    target.localRotation = GetDownRotation(target);
            }
        }

        void Reset()
        {
            Collider triggerCollider = GetComponent<Collider>();

            if (triggerCollider != null)
                triggerCollider.isTrigger = true;
        }
    }
}
