using UnityEngine;

namespace RealisticBodycam
{
    /// <summary>
    /// First-person melee using a rigged arms viewmodel (the Arms.fbx from the
    /// BigAndCrispy Unity-First-Person-Melee template).
    ///
    /// Put this on the Arms object (child of the "Melee" slot in the weapon switcher).
    /// The slot is only active while melee is selected, so this script only runs then.
    ///
    /// The Animator Controller needs four states, named exactly as in the fields below,
    /// with NO transitions and no parameters: Idle, Walk, Attack 1, Attack 2.
    /// The script switches between them with CrossFade.
    /// </summary>
    [AddComponentMenu("Realistic Bodycam/Viewmodel Melee")]
    public class BodycamViewmodelMelee : MonoBehaviour
    {
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
        public string attack1State = "Attack 1";
        public string attack2State = "Attack 2";
        public float crossFadeTime = 0.2f;

        [Tooltip("Horizontal speed (m/s) above which the Walk animation plays.")]
        public float walkSpeedThreshold = 0.5f;

        [Header("ATTACK")]
        public float damage = 40f;
        public float attackDistance = 2.5f;

        [Tooltip("Radius of the hit sweep. 0.25 is forgiving; use a small value for a precise hit.")]
        public float hitRadius = 0.25f;

        [Tooltip("Seconds from click to the hit landing. Tune to match the swing.")]
        public float attackDelay = 0.4f;

        [Tooltip("Seconds before the next swing can start.")]
        public float swingDuration = 1f;

        public bool holdToRepeat = true;
        public LayerMask hitMask = ~0;

        [Header("EFFECTS")]
        public AudioClip swingSound;
        public AudioClip hitSound;
        public Vector2 swingPitchRange = new Vector2(0.9f, 1.1f);

        [Tooltip("Optional. Spawned at the hit point.")]
        public GameObject hitEffectPrefab;
        public float hitEffectLifetime = 5f;

        int idleHash, walkHash, attack1Hash, attack2Hash;
        int currentHash;

        float nextAttackTime;
        float hitTime;
        bool hitPending;
        int attackCount;

        Vector3 lastPlayerPosition;

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (audioSource == null)
                audioSource = GetComponentInParent<AudioSource>();

            if (playerCamera == null)
                playerCamera = Camera.main;

            idleHash = Animator.StringToHash(idleState);
            walkHash = Animator.StringToHash(walkState);
            attack1Hash = Animator.StringToHash(attack1State);
            attack2Hash = Animator.StringToHash(attack2State);
        }

        // Called every time the Melee slot is selected (the slot is activated).
        void OnEnable()
        {
            hitPending = false;
            nextAttackTime = 0f;
            currentHash = 0;

            if (playerRoot != null)
                lastPlayerPosition = playerRoot.position;

            if (animator != null)
            {
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

            // Idle / Walk, unless a swing is still playing.
            if (Time.time >= nextAttackTime)
                ChangeState(speed > walkSpeedThreshold ? walkHash : idleHash);
        }

        void Attack()
        {
            if (Time.time < nextAttackTime)
                return;

            nextAttackTime = Time.time + swingDuration;
            hitTime = Time.time + attackDelay;
            hitPending = true;

            if (audioSource != null && swingSound != null)
            {
                audioSource.pitch = Random.Range(swingPitchRange.x, swingPitchRange.y);
                audioSource.PlayOneShot(swingSound);
            }

            ChangeState(attackCount == 0 ? attack1Hash : attack2Hash);
            attackCount = 1 - attackCount;
        }

        void ChangeState(int stateHash)
        {
            if (animator == null || currentHash == stateHash)
                return;

            currentHash = stateHash;
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
