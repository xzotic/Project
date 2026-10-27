using System.Collections;
using UnityEngine;

namespace RealisticBodycam
{
    /// <summary>
    /// Body-based melee. Put this on the full-body model (the object with the Animator).
    /// Animator needs: Trigger "AttackL", Trigger "AttackR", and an upper-body
    /// override layer (index set below) whose weight this script controls.
    /// The melee weapon is a slot in BodycamWeaponSwitcher (an empty GameObject).
    /// </summary>
    [AddComponentMenu("Realistic Bodycam/Body Melee")]
    public class BodycamBodyMelee : MonoBehaviour
    {
        [Header("REFERENCES")]
        public Animator animator;
        public BodycamWeaponSwitcher switcher;
        public Camera playerCamera;

        [Tooltip("Sword / weapon mesh parented to the right hand bone. Shown only while melee is equipped.")]
        public GameObject meleeItem;

        [Header("SETUP")]
        [Tooltip("Position of the melee slot in the switcher's Weapons list (0 = first). 3 = fourth slot / key 4.")]
        public int meleeWeaponIndex = 3;

        [Tooltip("Animator layer that holds the upper-body melee animations.")]
        public int meleeLayerIndex = 1;

        public float layerBlendSpeed = 8f;

        [Header("ATTACK")]
        public float damage = 40f;
        public float range = 2f;
        public float radius = 0.4f;

        [Tooltip("Seconds between clicking and the hit landing. Tune to match the swing.")]
        public float hitDelay = 0.25f;

        public float attackCooldown = 0.6f;

        [Tooltip("If you wait longer than this between swings, the combo restarts with the left swing.")]
        public float comboResetTime = 1.2f;

        public LayerMask hitMask = ~0;

        static readonly int AttackLHash = Animator.StringToHash("AttackL");
        static readonly int AttackRHash = Animator.StringToHash("AttackR");

        bool equipped;
        bool nextIsRight;
        float nextAttackTime;
        float lastAttackTime;
        float layerWeight;

        public bool IsMeleeEquipped => equipped;

        /// <summary>True while the arms / weapon should be visible.</summary>
        public bool ShowArms => layerWeight > 0.01f;

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (playerCamera == null)
                playerCamera = Camera.main;

            if (meleeItem != null)
                meleeItem.SetActive(false);
        }

        void Update()
        {
            if (animator == null)
                return;

            equipped = switcher != null
                       && !switcher.IsSwitching
                       && switcher.CurrentWeaponIndex == meleeWeaponIndex;

            layerWeight = Mathf.MoveTowards(
                layerWeight,
                equipped ? 1f : 0f,
                layerBlendSpeed * Time.deltaTime
            );

            animator.SetLayerWeight(meleeLayerIndex, layerWeight);

            if (meleeItem != null)
                meleeItem.SetActive(ShowArms);

            if (equipped && Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
                Attack();
        }

        void Attack()
        {
            if (Time.time - lastAttackTime > comboResetTime)
                nextIsRight = false;

            animator.SetTrigger(nextIsRight ? AttackRHash : AttackLHash);
            nextIsRight = !nextIsRight;

            lastAttackTime = Time.time;
            nextAttackTime = Time.time + attackCooldown;

            StartCoroutine(HitAfterDelay());
        }

        IEnumerator HitAfterDelay()
        {
            yield return new WaitForSeconds(hitDelay);
            DoHit();
        }

        /// <summary>
        /// Called after hitDelay. You can also call this from an Animation Event
        /// on the attack clips for exact timing (then set hitDelay very small
        /// or remove the coroutine).
        /// </summary>
        public void DoHit()
        {
            if (playerCamera == null)
                return;

            Transform cam = playerCamera.transform;

            if (!Physics.SphereCast(
                    cam.position,
                    radius,
                    cam.forward,
                    out RaycastHit hit,
                    range,
                    hitMask,
                    QueryTriggerInteraction.Ignore))
                return;

            BodycamRespawningTarget respawning =
                hit.transform.GetComponentInParent<BodycamRespawningTarget>();

            if (respawning != null)
            {
                respawning.TakeDamage(damage);
                return;
            }

            BodycamShootableTarget target =
                hit.transform.GetComponentInParent<BodycamShootableTarget>();

            if (target != null)
            {
                target.TakeDamage(damage);
            }
            else
            {
                hit.transform.SendMessageUpwards(
                    "TakeDamage",
                    damage,
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }
    }
}
