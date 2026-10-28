using UnityEngine;

namespace RealisticBodycam
{
    /// <summary>
    /// First-person melee using a rigged arms viewmodel.
    /// One of these per melee weapon: put it on that weapon's Arms object (child of its slot
    /// in the weapon switcher). Each weapon sets its own attack chain, speed and damage.
    ///
    /// The Animator Controller needs the Idle and Walk states plus one state per attack
    /// used in the chain, with NO transitions and no parameters. A state can be used by
    /// more than one step (e.g. "Attack 1", "Attack 2", "Attack 1").
    /// </summary>
    [AddComponentMenu("Realistic Bodycam/Viewmodel Melee")]
    public class BodycamViewmodelMelee : MonoBehaviour
    {
        [System.Serializable]
        public class AttackStep
        {
            [Tooltip("Animator state played for this swing.")]
            public string stateName = "Attack 1";

            [Tooltip("1 = normal speed, 2 = twice as fast. Hit Delay and Duration scale with it automatically.")]
            public float animationSpeed = 1f;

            [Tooltip("Seconds from click to the hit landing, at normal speed (1).")]
            public float hitDelay = 0.4f;

            [Tooltip("Seconds this swing lasts before the next swing can follow, at normal speed (1).")]
            public float duration = 1f;

            [Tooltip("Multiplies the weapon's damage for this step (use e.g. 1.5 for a finisher).")]
            public float damageMultiplier = 1f;

            [Tooltip("Optional. Overrides the weapon's swing sound for this step.")]
            public AudioClip swingSound;
        }

        [Header("REFERENCES")]
        public Animator animator;
        public AudioSource audioSource;
        public Camera playerCamera;

        [Tooltip("Optional. Blocks attacks while the weapon is being raised or lowered.")]
        public BodycamWeaponSwitcher switcher;

        [Tooltip("The object with the CharacterController (the player root). Used to pick Idle or Walk.")]
        public Transform playerRoot;

        [Header("ANIMATOR STATE NAMES")]
        public string idleState = "Idle";
        public string walkState = "Walk";
        public float crossFadeTime = 0.2f;

        [Tooltip("Horizontal speed (m/s) above which the Walk animation plays.")]
        public float walkSpeedThreshold = 0.5f;

        [Header("WEAPON")]
        public float damage = 40f;
        public float attackDistance = 2.5f;

        [Tooltip("Radius of the hit sweep.")]
        public float hitRadius = 0.25f;

        public bool holdToRepeat = true;
        public LayerMask hitMask = ~0;

        [Header("ATTACK CHAIN (length = number of hits in the combo)")]
        public AttackStep[] attackChain = new AttackStep[]
        {
            new AttackStep { stateName = "Attack 1" },
            new AttackStep { stateName = "Attack 2" }
        };

        [Tooltip("If you wait longer than this after a swing ends, the combo restarts at step 1.")]
        public float chainResetTime = 1f;

        [Tooltip("Extra pause after the LAST swing of the chain before a new chain can start.")]
        public float chainEndRecovery = 0f;

        [Header("EFFECTS")]
        public AudioClip swingSound;
        public AudioClip hitSound;
        public Vector2 swingPitchRange = new Vector2(0.9f, 1.1f);

        [Tooltip("Optional. Spawned at the hit point.")]
        public GameObject hitEffectPrefab;
        public float hitEffectLifetime = 5f;

        int idleHash, walkHash;
        int[] stepHashes;
        int currentHash;

        int comboIndex;
        float nextAttackTime;     // when a new swing may start
        float swingEndTime;       // when the current swing animation is over
        float hitTime;
        float pendingDamage;
        bool hitPending;

        Vector3 lastPlayerPosition;

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (audioSource == null)
                audioSource = GetComponentInParent<AudioSource>();

            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        // Called every time this weapon's slot is selected (the slot is activated).
        void OnEnable()
        {
            idleHash = Animator.StringToHash(idleState);
            walkHash = Animator.StringToHash(walkState);

            int count = attackChain != null ? attackChain.Length : 0;
            stepHashes = new int[count];

            for (int i = 0; i < count; i++)
                stepHashes[i] = Animator.StringToHash(attackChain[i].stateName);

            hitPending = false;
            comboIndex = 0;
            nextAttackTime = 0f;
            swingEndTime = 0f;

            if (playerRoot != null)
                lastPlayerPosition = playerRoot.position;

            if (animator != null)
            {
                animator.speed = 1f;
                animator.Play(idleHash, 0, 0f);
                currentHash = idleHash;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (dt <= 0f)
                return;

            // Horizontal speed from real movement (CharacterController.velocity is unreliable
            // when Move() is called twice per frame, as BodycamFPSController does).
            float speed = 0f;

            if (playerRoot != null)
            {
                Vector3 delta = playerRoot.position - lastPlayerPosition;
                delta.y = 0f;
                speed = delta.magnitude / dt;
                lastPlayerPosition = playerRoot.position;
            }

            bool switching = switcher != null && switcher.IsSwitching;

            if (!switching)
            {
                bool pressed = holdToRepeat ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

                if (pressed)
                    Attack();
            }

            if (hitPending && Time.time >= hitTime)
            {
                hitPending = false;
                DoHit();
            }

            // Back to Idle / Walk once the swing animation is finished.
            if (Time.time >= swingEndTime)
                ChangeState(speed > walkSpeedThreshold ? walkHash : idleHash);
        }

        void Attack()
        {
            if (Time.time < nextAttackTime || stepHashes == null || stepHashes.Length == 0)
                return;

            // Combo restarts if the player waited too long since the last swing ended.
            if (Time.time > swingEndTime + chainResetTime)
                comboIndex = 0;

            AttackStep step = attackChain[comboIndex];
            float speed = Mathf.Max(0.05f, step.animationSpeed);

            pendingDamage = damage * step.damageMultiplier;
            hitTime = Time.time + step.hitDelay / speed;
            hitPending = true;

            swingEndTime = Time.time + step.duration / speed;
            nextAttackTime = swingEndTime;

            AudioClip clip = step.swingSound != null ? step.swingSound : swingSound;

            if (audioSource != null && clip != null)
            {
                audioSource.pitch = Random.Range(swingPitchRange.x, swingPitchRange.y);
                audioSource.PlayOneShot(clip);
            }

            // Always restart the swing animation, even if the same state is used twice in a row.
            if (animator != null)
            {
                animator.speed = speed;
                currentHash = stepHashes[comboIndex];
                animator.CrossFadeInFixedTime(currentHash, crossFadeTime, 0, 0f);
            }

            comboIndex++;

            if (comboIndex >= attackChain.Length)
            {
                comboIndex = 0;
                nextAttackTime += chainEndRecovery;
            }
        }

        // Used for Idle and Walk only.
        void ChangeState(int stateHash)
        {
            if (animator == null || currentHash == stateHash)
                return;

            currentHash = stateHash;
            animator.speed = 1f;
            animator.CrossFadeInFixedTime(stateHash, crossFadeTime);
        }

        void DoHit()
        {
            if (playerCamera == null)
                return;

            Transform cam = playerCamera.transform;

            if (!Physics.SphereCast(
                    cam.position,
                    hitRadius,
                    cam.forward,
                    out RaycastHit hit,
                    attackDistance,
                    hitMask,
                    QueryTriggerInteraction.Ignore))
                return;

            if (audioSource != null && hitSound != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(hitSound);
            }

            if (hitEffectPrefab != null)
            {
                GameObject fx = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(fx, hitEffectLifetime);
            }

            BodycamRespawningTarget respawning =
                hit.transform.GetComponentInParent<BodycamRespawningTarget>();

            if (respawning != null)
            {
                respawning.TakeDamage(pendingDamage);
                return;
            }

            BodycamShootableTarget target =
                hit.transform.GetComponentInParent<BodycamShootableTarget>();

            if (target != null)
            {
                target.TakeDamage(pendingDamage);
            }
            else
            {
                hit.transform.SendMessageUpwards(
                    "TakeDamage",
                    pendingDamage,
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }
    }
}