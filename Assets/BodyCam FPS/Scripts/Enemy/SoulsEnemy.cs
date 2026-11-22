using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PurgeFPS
{
    /// <summary>One move an enemy can perform. Add as many as you like to the enemy's Attacks list.</summary>
    [Serializable]
    public class SoulsAttack
    {
        public string name = "Slash";

        [Tooltip("Animator trigger played when this attack starts. Leave empty for no animation.")]
        public string animatorTrigger = "Attack";

        [Min(0f)] public float damage = 30f;

        [Tooltip("Higher = picked more often.")]
        [Min(0f)] public float weight = 1f;

        [Header("When it can be used")]
        public float minRange = 0f;
        public float maxRange = 2.6f;

        [Header("Timing (seconds) - match these to your animation")]
        [Tooltip("The telegraph: time from the start of the move until it can hurt. Give the player time to read it.")]
        public float windup = 0.7f;

        [Tooltip("The enemy keeps turning toward the player for this long, then locks its direction. Short = easy to sidestep.")]
        public float trackingTime = 0.35f;

        [Tooltip("How long the hit can land.")]
        public float activeTime = 0.2f;

        [Tooltip("Time after the swing when the enemy is helpless. This is the player's punish window.")]
        public float recovery = 0.9f;

        [Header("Hit")]
        public float hitRange = 2.8f;
        [Range(10f, 180f)] public float hitAngle = 80f;

        [Header("Movement")]
        [Tooltip("Distance the enemy steps forward during the active part of the swing.")]
        public float lungeDistance = 0f;

        [Tooltip("Cannot be staggered from the windup until the swing ends.")]
        public bool hyperArmor = false;

        [Header("Animation-driven hitbox (optional)")]
        [Tooltip("Off = the timing numbers above decide when the swing hurts. On = Animation Events in the clip decide: 'AttackHitStart' opens the hit, 'AttackHitEnd' closes it.")]
        public bool useAnimationEvents = false;

        [Tooltip("Weapon or hand hitboxes (EnemyHitbox) that are active during this attack. Leave empty to use the flat wedge above.")]
        public EnemyHitbox[] hitboxes;

        [Tooltip("The animation clip switches the hitbox colliders on and off itself (animate the collider's Enabled, or the object's Is Active). Any contact while a hitbox is on then hurts, and the windup / active / recovery phases follow the hitbox. Needs at least one hitbox assigned.")]
        public bool animatorControlsHitboxes = false;

        [Tooltip("Safety limit when using Animation Events: the attack is cut off after this many seconds if an event never arrives.")]
        public float maxDuration = 4f;
    }

    public enum SoulsEnemyState
    {
        Idle,
        Chase,
        Cooldown,
        Attack,
        Stagger,
        Return,
        Dead
    }

    public enum AttackPhase
    {
        None,
        Windup,
        Active,
        Recovery
    }

    /// <summary>
    /// A deliberate, readable melee enemy in the style of Dark Souls:
    ///  - notices you with a vision cone and short hearing range, not through walls
    ///  - telegraphed attacks: windup, a brief tracking period, an active swing, then a long recovery
    ///  - circles you between attacks instead of spamming
    ///  - poise: enough hits stagger it, heavy hits (Impact) break it faster
    ///  - weaknesses and resistances to Slicing / Puncture / Impact
    ///  - leash: gives up the chase and walks back to where it started
    ///
    /// Works with your existing weapons: bullets and body melee call TakeDamage(float),
    /// the viewmodel melee calls TakeMeleeDamage, and hits on the player go through
    /// BodycamPlayerHealth.TakeDamageFrom, so your block and parry apply.
    ///
    /// Hitboxes can be timed by the numbers on each attack, or tied to the animation:
    /// tick "Use Animation Events", add 'AttackHitStart' / 'AttackHitEnd' events to the clip,
    /// and optionally assign weapon hitboxes (EnemyHitbox) so the real swing path does the hitting.
    /// Or tick "Animator Controls Hitboxes" and let the clip itself switch the hitbox colliders on and off.
    ///
    /// Needs a NavMeshAgent and a Collider on the same object.
    /// Animator (all optional): float "Speed", triggers "Stagger" and "Die",
    /// plus one trigger per attack (the name you type in the attack).
    /// </summary>
    [AddComponentMenu("Enemies/Souls Enemy")]
    [RequireComponent(typeof(NavMeshAgent))]
    public class SoulsEnemy : MonoBehaviour, IMeleeDamageable
    {
        [Header("HEALTH & POISE")]
        [Min(1f)] public float maxHealth = 120f;

        [Tooltip("How much damage it takes to stagger the enemy. 0 = staggers on every hit.")]
        [Min(0f)] public float maxPoise = 40f;

        public float poiseRegenDelay = 4f;
        public float poiseRegenRate = 15f;

        [Header("DAMAGE TYPES (1 = normal, 0.5 = resists, 1.5 = weak)")]
        public float slicingMultiplier = 1f;
        public float punctureMultiplier = 1f;
        public float impactMultiplier = 1f;
        public float firearmMultiplier = 1f;

        [Header("SENSES")]
        public float viewRange = 14f;
        [Range(20f, 360f)] public float viewAngle = 110f;
        [Tooltip("Notices the player this close even from behind.")]
        public float hearingRange = 3.5f;
        public LayerMask lineOfSightBlockers = ~0;

        [Header("MOVEMENT")]
        public float walkSpeed = 1.6f;
        public float chaseSpeed = 3.2f;
        public float turnSpeed = 6f;
        public float strafeSpeed = 1.3f;

        [Header("COMBAT")]
        [Tooltip("Distance at which the enemy stops charging in and waits for its next attack.")]
        public float engageDistance = 3.5f;

        public Vector2 cooldownBetweenAttacks = new Vector2(0.6f, 1.6f);

        [Tooltip("Strafes around the player while waiting for the next attack.")]
        public bool circleWhileWaiting = true;

        public float staggerTime = 1.1f;

        public List<SoulsAttack> attacks = new List<SoulsAttack>
        {
            new SoulsAttack
            {
                name = "Quick Slash",
                animatorTrigger = "Attack1",
                damage = 25f,
                weight = 2f,
                maxRange = 2.4f,
                windup = 0.6f,
                trackingTime = 0.3f,
                activeTime = 0.2f,
                recovery = 0.8f
            },
            new SoulsAttack
            {
                name = "Heavy Overhead",
                animatorTrigger = "Attack2",
                damage = 55f,
                weight = 1f,
                maxRange = 3.2f,
                windup = 1.1f,
                trackingTime = 0.5f,
                activeTime = 0.25f,
                recovery = 1.4f,
                lungeDistance = 1f,
                hyperArmor = true
            }
        };

        [Header("LEASH")]
        [Tooltip("Gives up the chase beyond this distance from the player.")]
        public float loseRange = 28f;

        [Tooltip("Gives up the chase beyond this distance from where it started.")]
        public float leashDistance = 35f;

        public bool healOnReturn = true;

        [Header("DEATH")]
        public float destroyAfterDeath = 8f;
        public bool disableCollidersOnDeath = true;

        [Header("REFERENCES (auto-found if empty)")]
        public BodycamPlayerHealth player;
        public Animator animator;

        [Header("HIT DETECTION")]
        [Tooltip("Vertical reach of every attack: the player must be within this height difference.")]
        public float hitVerticalReach = 2.2f;

        [Header("DEBUG")]
        [Tooltip("Draws the attack hitbox while playing: yellow = windup, red = active (can hurt), cyan = recovery. Visible in the Scene view, and in the Game view when Gizmos is on.")]
        public bool debugHitboxes = true;

        [Tooltip("Writes a Console line for every attack: hit or miss, with the real distance, angle and height.")]
        public bool logAttacks = false;

        [Tooltip("Shows a text label above the enemy with its state, attack phase, health and poise (Scene view and Game view with Gizmos on).")]
        public bool debugLabel = true;

        [Tooltip("Colors the enemy by attack phase so you can read its attacks without animations. Turn off once you have animations.")]
        public bool tintByPhase = true;

        public Renderer tintRenderer;

        public SoulsEnemyState State => state;
        public AttackPhase Phase => attackPhase;
        public float CurrentHealth { get; private set; }
        public float CurrentPoise { get; private set; }
        public bool IsDead => state == SoulsEnemyState.Dead;

        SoulsEnemyState state = SoulsEnemyState.Idle;
        NavMeshAgent agent;
        Coroutine routine;
        SoulsAttack currentAttack;
        bool armorActive;

        AttackPhase attackPhase = AttackPhase.None;
        float phaseEndTime;
        float debugDistance;
        float debugAngle;
        float debugHeight;
        MaterialPropertyBlock tintBlock;

        bool animHitStart;
        bool animHitEnd;
        bool swingHit;

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        Vector3 lastPosition;
        float moveSpeed;

        float nextAttackTime;
        float poiseRegenAt;
        float circleDirection = 1f;

        readonly Dictionary<int, AnimatorControllerParameterType> animParams =
            new Dictionary<int, AnimatorControllerParameterType>();

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int StaggerHash = Animator.StringToHash("Stagger");
        static readonly int DieHash = Animator.StringToHash("Die");

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;   // this script controls turning, so turns feel heavy

            CurrentHealth = maxHealth;
            CurrentPoise = maxPoise;

            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            lastPosition = transform.position;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (tintRenderer == null)
                tintRenderer = GetComponentInChildren<Renderer>();

            if (animator != null)
            {
                foreach (AnimatorControllerParameter p in animator.parameters)
                    animParams[p.nameHash] = p.type;
            }
        }

        void Start()
        {
            if (player == null)
                player = FindFirstObjectByType<BodycamPlayerHealth>();
        }

        // ---------------- main loop ----------------

        void Update()
        {
            if (state == SoulsEnemyState.Dead)
                return;

            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            UpdatePoise(dt);

            if (player != null && !player.IsDead)
            {
                Vector3 toPlayer = player.transform.position - transform.position;
                toPlayer.y = 0f;
                float distance = toPlayer.magnitude;

                switch (state)
                {
                    case SoulsEnemyState.Idle:
                        if (CanSense(toPlayer, distance))
                            BeginChase();
                        break;

                    case SoulsEnemyState.Chase:
                        UpdateChase(toPlayer, distance);
                        break;

                    case SoulsEnemyState.Cooldown:
                        UpdateCooldown(toPlayer, distance, dt);
                        break;
                }
            }
            else if (state == SoulsEnemyState.Chase || state == SoulsEnemyState.Cooldown)
            {
                BeginReturn();   // player is dead: go home
            }

            if (state == SoulsEnemyState.Return)
                UpdateReturn();

            UpdateAnimatorSpeed(dt);
        }

        // ---------------- senses ----------------

        bool CanSense(Vector3 toPlayer, float distance)
        {
            if (distance <= hearingRange)
                return true;

            if (distance > viewRange)
                return false;

            if (Vector3.Angle(transform.forward, toPlayer) > viewAngle * 0.5f)
                return false;

            Vector3 from = transform.position + Vector3.up * 1.5f;
            Vector3 to = player.transform.position + Vector3.up * 1.2f;

            if (Physics.Linecast(from, to, out RaycastHit hit, lineOfSightBlockers, QueryTriggerInteraction.Ignore))
                return hit.transform.GetComponentInParent<BodycamPlayerHealth>() != null;

            return true;
        }

        // ---------------- chase / cooldown / return ----------------

        void BeginChase()
        {
            state = SoulsEnemyState.Chase;
            agent.isStopped = false;
            agent.speed = chaseSpeed;
        }

        void UpdateChase(Vector3 toPlayer, float distance)
        {
            if (distance > loseRange || Vector3.Distance(transform.position, spawnPosition) > leashDistance)
            {
                BeginReturn();
                return;
            }

            if (distance <= engageDistance)
            {
                if (Time.time >= nextAttackTime)
                {
                    SoulsAttack attack = PickAttack(distance);

                    if (attack != null)
                    {
                        StartAttack(attack);
                        return;
                    }
                }
                else
                {
                    BeginCooldown();
                    return;
                }
            }

            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.transform.position);
            RotateToward(agent.desiredVelocity, turnSpeed);
        }

        void BeginCooldown()
        {
            state = SoulsEnemyState.Cooldown;
            agent.ResetPath();
            agent.isStopped = true;
            circleDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        }

        void UpdateCooldown(Vector3 toPlayer, float distance, float dt)
        {
            RotateToward(toPlayer, turnSpeed);

            if (circleWhileWaiting)
            {
                Vector3 side = Vector3.Cross(Vector3.up, toPlayer.normalized) * circleDirection;
                Vector3 move = side * strafeSpeed;

                // Keep a comfortable distance while circling.
                if (distance < engageDistance * 0.7f)
                    move -= toPlayer.normalized * strafeSpeed;
                else if (distance > engageDistance * 1.1f)
                    move += toPlayer.normalized * strafeSpeed * 0.5f;

                agent.Move(move * dt);
            }

            if (Time.time >= nextAttackTime || distance > engageDistance * 1.6f)
            {
                state = SoulsEnemyState.Chase;
                agent.isStopped = false;
            }
        }

        void BeginReturn()
        {
            state = SoulsEnemyState.Return;
            agent.isStopped = false;
            agent.speed = walkSpeed * 1.5f;
            agent.SetDestination(spawnPosition);
        }

        void UpdateReturn()
        {
            RotateToward(agent.desiredVelocity, turnSpeed);

            if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.4f)
                return;

            state = SoulsEnemyState.Idle;
            agent.ResetPath();
            transform.rotation = spawnRotation;

            if (healOnReturn)
            {
                CurrentHealth = maxHealth;
                CurrentPoise = maxPoise;
            }
        }

        // ---------------- attacks ----------------

        SoulsAttack PickAttack(float distance)
        {
            float total = 0f;

            foreach (SoulsAttack a in attacks)
            {
                if (distance >= a.minRange && distance <= a.maxRange)
                    total += a.weight;
            }

            if (total <= 0f)
                return null;

            float roll = UnityEngine.Random.value * total;

            foreach (SoulsAttack a in attacks)
            {
                if (distance < a.minRange || distance > a.maxRange)
                    continue;

                roll -= a.weight;

                if (roll <= 0f)
                    return a;
            }

            return null;
        }

        void StartAttack(SoulsAttack attack)
        {
            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(AttackRoutine(attack));
        }

        IEnumerator AttackRoutine(SoulsAttack a)
        {
            state = SoulsEnemyState.Attack;
            currentAttack = a;

            agent.ResetPath();
            agent.isStopped = true;

            animHitStart = false;
            animHitEnd = false;
            swingHit = false;

            bool byAnimator = AnimatorDriven(a);
            bool byEvents = a.useAnimationEvents || byAnimator;
            bool usesHitboxes = HasHitboxes(a);
            float timeout = Mathf.Max(a.maxDuration, 0.5f);

            PrepareHitboxes(a);
            SetTrigger(a.animatorTrigger);
            armorActive = a.hyperArmor;
            SetPhase(AttackPhase.Windup, a.windup);

            // Windup: turns toward the player for the tracking time, then commits to a direction.
            // With Animation Events it lasts until the clip fires AttackHitStart.
            float t = 0f;

            while (byEvents ? (!SwingStarted(a) && t < timeout) : (t < a.windup))
            {
                if (t < a.trackingTime && player != null)
                {
                    Vector3 to = player.transform.position - transform.position;
                    to.y = 0f;
                    RotateToward(to, turnSpeed);
                }

                t += Time.deltaTime;
                yield return null;
            }

            if (byEvents && !SwingStarted(a))
            {
                if (byAnimator)
                    Debug.LogWarning($"[{name}] {a.name}: no hitbox ever switched on. Check the clip animates the hitbox collider's Enabled (or the object's Is Active), and that the Animator is on an object above the hitbox.", this);
                else
                    Debug.LogWarning($"[{name}] {a.name}: never received an 'AttackHitStart' animation event. Check the event is on the clip and the EnemyAnimationEvents component is on the Animator object.", this);
            }

            // Active: the hit can land once.
            SetPhase(AttackPhase.Active, a.activeTime);
            Vector3 lungeDirection = transform.forward;

            if (!byAnimator)
                OpenHitboxes(a);   // animator-driven hitboxes open themselves
            float activeElapsed = 0f;

            while (byEvents ? (!SwingEnded(a) && activeElapsed < timeout) : (activeElapsed < a.activeTime))
            {
                float dt = Time.deltaTime;

                if (a.lungeDistance > 0f && a.activeTime > 0f)
                    agent.Move(lungeDirection * (a.lungeDistance / a.activeTime) * dt);

                // Wedge attacks check the player here. Hitbox attacks report through OnHitboxContact.
                if (!usesHitboxes && !swingHit && PlayerInHitbox(a))
                    DealHit(a);

                activeElapsed += dt;
                yield return null;
            }

            CloseHitboxes(a);
            armorActive = false;

            if (logAttacks && !swingHit)
            {
                if (usesHitboxes)
                    Debug.Log($"[{name}] {a.name} MISSED  (weapon hitbox never touched the player)", this);
                else
                    Debug.Log($"[{name}] {a.name} MISSED  distance {debugDistance:0.00}/{a.hitRange:0.00}  angle {debugAngle:0}/{a.hitAngle * 0.5f:0}  height {debugHeight:0.00}/{hitVerticalReach:0.00}", this);
            }

            // Recovery: helpless, this is the punish window.
            SetPhase(AttackPhase.Recovery, a.recovery);
            yield return new WaitForSeconds(a.recovery);

            nextAttackTime = Time.time + UnityEngine.Random.Range(cooldownBetweenAttacks.x, cooldownBetweenAttacks.y);
            ClearPhase();
            currentAttack = null;
            routine = null;

            state = SoulsEnemyState.Chase;
            agent.isStopped = false;
        }

        // ---------------- animation events & weapon hitboxes ----------------

        /// <summary>Animation Event: the swing starts to hurt from this frame.</summary>
        public void AttackHitStart()
        {
            if (state == SoulsEnemyState.Attack)
                animHitStart = true;
        }

        /// <summary>Animation Event: the swing stops hurting from this frame.</summary>
        public void AttackHitEnd()
        {
            if (state == SoulsEnemyState.Attack)
                animHitEnd = true;
        }

        /// <summary>True when this attack's hitboxes are switched on and off by the animation clip itself.</summary>
        static bool AnimatorDriven(SoulsAttack a)
        {
            return a != null && a.animatorControlsHitboxes && HasHitboxes(a);
        }

        bool SwingStarted(SoulsAttack a)
        {
            return AnimatorDriven(a) ? AnyHitboxOpen(a) : animHitStart;
        }

        bool SwingEnded(SoulsAttack a)
        {
            return AnimatorDriven(a) ? !AnyHitboxOpen(a) : animHitEnd;
        }

        bool AnyHitboxOpen(SoulsAttack a)
        {
            if (!HasHitboxes(a))
                return false;

            foreach (EnemyHitbox h in a.hitboxes)
            {
                if (h != null && h.IsOpen)
                    return true;
            }

            return false;
        }

        /// <summary>Starts every attack with its hitboxes closed, and tells them who controls them.</summary>
        void PrepareHitboxes(SoulsAttack a)
        {
            if (!HasHitboxes(a))
                return;

            foreach (EnemyHitbox h in a.hitboxes)
            {
                if (h == null)
                    continue;

                h.SetOpen(false);
                h.SetAnimatorControlled(a.animatorControlsHitboxes);
            }
        }

        static bool HasHitboxes(SoulsAttack a)
        {
            return a != null && a.hitboxes != null && a.hitboxes.Length > 0;
        }

        void OpenHitboxes(SoulsAttack a)
        {
            if (!HasHitboxes(a))
                return;

            foreach (EnemyHitbox h in a.hitboxes)
            {
                if (h != null)
                    h.SetOpen(true);
            }
        }

        void CloseHitboxes(SoulsAttack a)
        {
            if (!HasHitboxes(a))
                return;

            foreach (EnemyHitbox h in a.hitboxes)
            {
                if (h == null)
                    continue;

                h.SetOpen(false);
                h.SetAnimatorControlled(false);
            }
        }

        void CloseAllHitboxes()
        {
            foreach (SoulsAttack a in attacks)
                CloseHitboxes(a);
        }

        /// <summary>Called by an EnemyHitbox that overlapped something while it was open.</summary>
        public void OnHitboxContact(Collider other)
        {
            if (state != SoulsEnemyState.Attack || swingHit)
                return;

            if (currentAttack == null || player == null || player.IsDead)
                return;

            // Timed and event attacks only hurt during the active phase. Animator-driven hitboxes
            // decide for themselves (their collider is on), but never during recovery.
            if (AnimatorDriven(currentAttack) ? attackPhase == AttackPhase.Recovery : attackPhase != AttackPhase.Active)
                return;

            if (other.GetComponentInParent<BodycamPlayerHealth>() != player)
                return;

            DealHit(currentAttack);
        }

        void DealHit(SoulsAttack a)
        {
            swingHit = true;
            player.TakeDamageFrom(a.damage, transform.position);   // block / parry applies here

            if (logAttacks)
                Debug.Log($"[{name}] {a.name} HIT  damage {a.damage:0}", this);
        }

        bool PlayerInHitbox(SoulsAttack a)
        {
            if (player == null || player.IsDead)
                return false;

            Vector3 to = player.transform.position - transform.position;

            debugHeight = to.y;
            to.y = 0f;
            debugDistance = to.magnitude;
            debugAngle = Vector3.Angle(transform.forward, to);

            if (Mathf.Abs(debugHeight) > hitVerticalReach)
                return false;

            if (debugDistance > a.hitRange)
                return false;

            return debugAngle <= a.hitAngle * 0.5f;
        }

        // ---------------- taking damage ----------------

        /// <summary>Called by bullets and body melee (SendMessageUpwards "TakeDamage").</summary>
        public void TakeDamage(float amount)
        {
            ReceiveHit(amount * firearmMultiplier, MeleeDamageType.None, false);
        }

        /// <summary>Called by the viewmodel melee.</summary>
        public void TakeMeleeDamage(MeleeHitInfo hit)
        {
            ReceiveHit(hit.amount * TypeMultiplier(hit.types), hit.types, true);
        }

        float TypeMultiplier(MeleeDamageType types)
        {
            float sum = 0f;
            int count = 0;

            if ((types & MeleeDamageType.Slicing) != 0) { sum += slicingMultiplier; count++; }
            if ((types & MeleeDamageType.Puncture) != 0) { sum += punctureMultiplier; count++; }
            if ((types & MeleeDamageType.Impact) != 0) { sum += impactMultiplier; count++; }

            return count > 0 ? sum / count : 1f;
        }

        void ReceiveHit(float amount, MeleeDamageType types, bool isMelee)
        {
            if (state == SoulsEnemyState.Dead || amount <= 0f)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            // Hits wake the enemy up even if it hadn't noticed the player.
            if (state == SoulsEnemyState.Idle || state == SoulsEnemyState.Return)
                BeginChase();

            if (CurrentHealth <= 0f)
            {
                Die();
                return;
            }

            float poiseDamage = amount;

            if ((types & MeleeDamageType.Impact) != 0)
                poiseDamage *= 1.5f;

            CurrentPoise -= poiseDamage;
            poiseRegenAt = Time.time + poiseRegenDelay;

            bool armored = state == SoulsEnemyState.Attack && armorActive;

            if (CurrentPoise <= 0f && !armored)
                Stagger(staggerTime);
            else if (CurrentPoise < 0f)
                CurrentPoise = 0f;
        }

        void UpdatePoise(float dt)
        {
            if (CurrentPoise < maxPoise && Time.time >= poiseRegenAt)
                CurrentPoise = Mathf.Min(maxPoise, CurrentPoise + poiseRegenRate * dt);
        }

        void Stagger(float duration)
        {
            if (routine != null)
                StopCoroutine(routine);

            armorActive = false;
            CloseAllHitboxes();
            ClearPhase();
            currentAttack = null;
            CurrentPoise = maxPoise;

            routine = StartCoroutine(StaggerRoutine(duration));
        }

        IEnumerator StaggerRoutine(float duration)
        {
            state = SoulsEnemyState.Stagger;
            agent.ResetPath();
            agent.isStopped = true;

            SetTrigger("Stagger");

            yield return new WaitForSeconds(duration);

            nextAttackTime = Time.time + 0.4f;
            routine = null;

            state = SoulsEnemyState.Chase;
            agent.isStopped = false;
        }

        void Die()
        {
            if (routine != null)
                StopCoroutine(routine);

            routine = null;
            armorActive = false;
            CloseAllHitboxes();
            ClearPhase();
            currentAttack = null;
            state = SoulsEnemyState.Dead;

            agent.isStopped = true;
            agent.enabled = false;

            SetTrigger("Die");

            if (disableCollidersOnDeath)
            {
                foreach (Collider c in GetComponentsInChildren<Collider>())
                    c.enabled = false;
            }

            if (destroyAfterDeath > 0f)
                Destroy(gameObject, destroyAfterDeath);
        }

        // ---------------- helpers ----------------

        void RotateToward(Vector3 direction, float speed)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, speed * Time.deltaTime);
        }

        void UpdateAnimatorSpeed(float dt)
        {
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            moveSpeed = delta.magnitude / dt;
            lastPosition = transform.position;

            if (animator != null
                && animParams.TryGetValue(SpeedHash, out AnimatorControllerParameterType type)
                && type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(SpeedHash, moveSpeed, 0.1f, dt);
            }
        }

        void SetTrigger(string triggerName)
        {
            if (animator == null || string.IsNullOrEmpty(triggerName))
                return;

            int hash = Animator.StringToHash(triggerName);

            if (animParams.TryGetValue(hash, out AnimatorControllerParameterType type)
                && type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(hash);
            }
        }

        // ---------------- debugging: phase, tint, hitbox drawing ----------------

        void SetPhase(AttackPhase phase, float duration)
        {
            attackPhase = phase;
            phaseEndTime = Time.time + duration;
            ApplyTint(phase);
        }

        void ClearPhase()
        {
            attackPhase = AttackPhase.None;
            ApplyTint(AttackPhase.None);
        }

        static Color PhaseColor(AttackPhase phase)
        {
            switch (phase)
            {
                case AttackPhase.Windup: return new Color(1f, 0.85f, 0.1f);    // yellow: telegraph
                case AttackPhase.Active: return new Color(1f, 0.15f, 0.1f);    // red: can hurt you
                case AttackPhase.Recovery: return new Color(0.2f, 0.85f, 1f);  // cyan: punish window
                default: return Color.white;
            }
        }

        void ApplyTint(AttackPhase phase)
        {
            if (!tintByPhase || tintRenderer == null)
                return;

            if (phase == AttackPhase.None)
            {
                tintRenderer.SetPropertyBlock(null);   // back to the real material color
                return;
            }

            if (tintBlock == null)
                tintBlock = new MaterialPropertyBlock();

            Color c = PhaseColor(phase);
            tintBlock.SetColor("_BaseColor", c);   // URP Lit
            tintBlock.SetColor("_Color", c);       // Standard / built-in
            tintRenderer.SetPropertyBlock(tintBlock);
        }

        /// <summary>Draws the flat wedge an attack can hit inside, plus its vertical reach.</summary>
        void DrawHitZone(SoulsAttack a, Color color)
        {
            const int segments = 24;

            Vector3 origin = transform.position;
            float half = a.hitAngle * 0.5f;
            Vector3 up = Vector3.up * hitVerticalReach;

            Gizmos.color = color;

            Vector3 prev = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-half, half, i / (float)segments);
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
                Vector3 point = origin + dir * a.hitRange;

                if (i == 0 || i == segments)
                {
                    Gizmos.DrawLine(origin, point);              // wedge edge on the ground
                    Gizmos.DrawLine(origin + up, point + up);    // wedge edge at full height
                    Gizmos.DrawLine(point, point + up);          // vertical edge
                }

                if (i > 0)
                {
                    Gizmos.DrawLine(prev, point);                // arc on the ground
                    Gizmos.DrawLine(prev + up, point + up);      // arc at full height
                }

                prev = point;
            }
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || state == SoulsEnemyState.Dead)
                return;

            if (debugHitboxes)
            {
                if (currentAttack != null && !HasHitboxes(currentAttack))
                    DrawHitZone(currentAttack, PhaseColor(attackPhase));

                // Line to the player: green while the player is inside the current attack's hitbox.
                if (player != null && !player.IsDead)
                {
                    bool inside = currentAttack != null
                                  && (HasHitboxes(currentAttack) ? swingHit : PlayerInHitbox(currentAttack));

                    Gizmos.color = inside ? Color.green : new Color(1f, 1f, 1f, 0.25f);
                    Vector3 from = transform.position + Vector3.up * 1f;
                    Vector3 to = player.transform.position + Vector3.up * 1f;
                    Gizmos.DrawLine(from, to);
                    Gizmos.DrawWireSphere(to, 0.15f);
                }
            }

#if UNITY_EDITOR
            if (debugLabel)
            {
                float remaining = Mathf.Max(0f, phaseEndTime - Time.time);

                string text = state.ToString();

                if (attackPhase != AttackPhase.None && currentAttack != null)
                    text += "\n" + currentAttack.name + ": " + attackPhase + " " + remaining.ToString("0.00") + "s";

                text += "\nHP " + CurrentHealth.ToString("0") + "/" + maxHealth.ToString("0")
                        + "   Poise " + CurrentPoise.ToString("0") + "/" + maxPoise.ToString("0");

                UnityEditor.Handles.Label(transform.position + Vector3.up * 2.6f, text);
            }
#endif
        }

        void OnDrawGizmosSelected()
        {
            // Senses and ranges.
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, viewRange);

            Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + left * viewRange);
            Gizmos.DrawLine(transform.position, transform.position + right * viewRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, hearingRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, engageDistance);

            // Every attack's reach, so you can tune ranges and angles without pressing Play.
            if (!Application.isPlaying || currentAttack == null)
            {
                foreach (SoulsAttack a in attacks)
                {
                    if (!HasHitboxes(a))
                        DrawHitZone(a, new Color(1f, 0.5f, 0f, 0.6f));
                }
            }
        }
    }
}