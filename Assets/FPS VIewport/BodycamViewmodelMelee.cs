using UnityEngine;
using UnityEngine.Events;

namespace PurgeFPS
{
    /// <summary>
    /// Damage types a melee attack can deal. An attack can have more than one
    /// (tick several in the Inspector).
    /// </summary>
    [System.Flags]
    public enum MeleeDamageType
    {
        None = 0,
        Puncture = 1,
        Slicing = 2,
        Impact = 4
    }

    /// <summary>What a target is told about a melee hit.</summary>
    public struct MeleeHitInfo
    {
        public float amount;
        public MeleeDamageType types;
        public Vector3 point;
        public Vector3 normal;
    }

    /// <summary>
    /// Optional. Put this on a target (or any parent of its collider) to receive the damage
    /// together with its types, e.g. to give armour a weakness or resistance.
    /// Targets without it still get the plain damage number as before.
    /// </summary>
    public interface IMeleeDamageable
    {
        void TakeMeleeDamage(MeleeHitInfo hit);
    }

    /// <summary>What happened when an incoming attack met the player's guard.</summary>
    public enum BlockResult
    {
        None,      // not blocking (or the attack came from outside the block angle)
        Blocked,   // normal block: part of the damage is absorbed
        Parried    // hit inside the parry window: all damage is negated
    }

    /// <summary>
    /// First-person melee using a rigged arms viewmodel.
    /// One of these per melee weapon: put it on that weapon's Arms object (child of its slot
    /// in the weapon switcher).
    ///
    /// A weapon has 1 to 3 states. State 1 is configured by the fields below (WEAPON,
    /// ATTACK CHAIN, BLOCK, ANIMATOR STATE NAMES). States 2 and 3 are in EXTRA STATES.
    /// Every state has its own idle, walk, stats, attack chain and block. Switching state plays a
    /// transition animation into the new state's idle. It is possible while idle, and also during the
    /// tail of a swing or of another switch once their unlock times (see STATE SWITCHING) have passed.
    ///
    /// BLOCKING: hold the block key. The first "Parry Window" seconds of a block are a parry
    /// (all damage negated), after that it is a normal block (damage reduced). Incoming damage
    /// has to be routed through ProcessIncomingDamage (see that method).
    ///
    /// The Animator Controller needs, with NO transitions and no parameters:
    ///   - an Idle and a Walk state for every weapon state,
    ///   - one state per attack used in the chains,
    ///   - one Block state per weapon state that can block,
    ///   - one state per switch transition you define.
    /// A state can be used by more than one step (e.g. "Attack 0_1", "Attack 0_2", "Attack 0_1").
    ///
    /// Default state names (already filled in on a new component, change them freely):
    ///   state 1:  Idle,  Walk, Attack 0_1, 0_2, 0_3, Block 0
    ///   state 2:  Idle1, Walk, Attack 1_1, 1_2, 1_3, Block 1
    ///   state 3:  Idle2, Walk, Attack 2_1, 2_2, 2_3, Block 2
    /// </summary>
    [AddComponentMenu("Realistic Bodycam/Viewmodel Melee")]
    public class BodycamViewmodelMelee : MonoBehaviour
    {
        const int MaxStates = 3;

        [System.Serializable]
        public class AttackStep
        {
            [Tooltip("Animator state played for this swing.")]
            public string stateName = "Attack 0_1";

            [Tooltip("Damage types this swing deals. Tick one or more.")]
            public MeleeDamageType damageTypes = MeleeDamageType.Slicing;

            [Tooltip("1 = normal speed, 2 = twice as fast. Hit Delay and Duration scale with it automatically.")]
            public float animationSpeed = 1f;

            [Tooltip("Seconds from click to the hit landing, at normal speed (1).")]
            public float hitDelay = 0.4f;

            [Tooltip("Seconds this swing lasts before the next swing can follow, at normal speed (1).")]
            public float duration = 1f;

            [Tooltip("General damage multiplier for this swing. Final damage = the state's Damage x this (use e.g. 1.5 for a finisher).")]
            public float damageMultiplier = 1f;

            [Tooltip("Stamina spent by this swing. The swing is blocked if there isn't enough.")]
            public float staminaCost = 15f;

            [Tooltip("Optional. Overrides the weapon's swing sound for this step.")]
            public AudioClip swingSound;
        }

        /// <summary>Block settings of one weapon state.</summary>
        [System.Serializable]
        public class BlockSettings
        {
            [Tooltip("Off = this weapon state can't block.")]
            public bool canBlock = true;

            [Tooltip("Animator state held while blocking.")]
            public string blockState = "Block 0";

            [Tooltip("Seconds at the START of a block during which an incoming hit is PARRIED (all damage negated). After this it is a normal block.")]
            public float parryWindow = 0.3f;

            [Tooltip("Share of the damage a normal block absorbs. 0 = none, 1 = all of it.")]
            [Range(0f, 1f)] public float damageReduction = 0.75f;

            [Tooltip("Seconds to blend from the current animation into the Block animation. Lower = snappier. The parry window starts the moment the block starts, so a long blend means the arms reach the guard pose late.")]
            [Min(0f)] public float blockCrossFadeTime = 0.2f;
        }

        /// <summary>Everything that is different in weapon state 2 or 3 (state 1 uses the fields below).</summary>
        [System.Serializable]
        public class WeaponStateConfig
        {
            [Tooltip("Optional label for the Inspector.")]
            public string label = "";

            [Tooltip("Looping idle state of this weapon state.")]
            public string idleState = "Idle1";

            [Tooltip("Looping walk state of this weapon state.")]
            public string walkState = "Walk";

            public float damage = 40f;
            public float attackDistance = 2.5f;

            [Tooltip("Radius of the hit sweep.")]
            public float hitRadius = 0.25f;

            [Tooltip("The combo of this state (length = number of hits).")]
            public AttackStep[] attackChain = new AttackStep[]
            {
                new AttackStep { stateName = "Attack 1_1" },
                new AttackStep { stateName = "Attack 1_2" },
                new AttackStep { stateName = "Attack 1_3" }
            };

            [Tooltip("If you wait longer than this after a swing ends, the combo restarts at step 1.")]
            public float chainResetTime = 1f;

            [Tooltip("Extra pause after the LAST swing of the chain before a new chain can start.")]
            public float chainEndRecovery = 0f;

            public BlockSettings block = new BlockSettings { blockState = "Block 1" };
        }

        /// <summary>The animation played when switching from one state to another (each direction is its own entry).</summary>
        [System.Serializable]
        public class StateTransition
        {
            [Range(1, 3)] public int fromState = 1;
            [Range(1, 3)] public int toState = 2;

            [Tooltip("Animator state played during the switch. It should end on the pose of the new state's idle.")]
            public string animatorState = "Switch 0_1";

            [Tooltip("1 = normal speed. Duration scales with it automatically.")]
            public float animationSpeed = 1f;

            [Tooltip("Seconds the switch takes at normal speed (1). 0 = instant.")]
            public float duration = 1f;

            [Tooltip("Seconds into the switch after which you can attack, block or switch again, cutting off the rest of the animation. 0 = wait for the full Duration. Scales with Animation Speed like Duration.")]
            public float unlockTime = 0.5f;

            public AudioClip sound;
        }

        // The default for a new component: state 2 and state 3 filled in with their own names.
        static WeaponStateConfig DefaultExtraState(int index)
        {
            return new WeaponStateConfig
            {
                label = "State " + (index + 1),
                idleState = "Idle" + index,
                walkState = "Walk",
                attackChain = new AttackStep[]
                {
                    new AttackStep { stateName = "Attack " + index + "_1" },
                    new AttackStep { stateName = "Attack " + index + "_2" },
                    new AttackStep { stateName = "Attack " + index + "_3" }
                },
                block = new BlockSettings { blockState = "Block " + index }
            };
        }

        [Header("REFERENCES")]
        [Tooltip("The arms animator.")]
        public Animator animator;

        [Tooltip("Optional. This weapon's own animator, separate from the arms. Its controller needs the same state names (Idle, Walk, every attack, block and switch state).")]
        public Animator weaponMeshAnimator;
        public AudioSource audioSource;
        public Camera playerCamera;

        [Tooltip("Optional. Blocks attacks while the weapon is being raised or lowered.")]
        public BodycamWeaponSwitcher switcher;

        [Tooltip("The object with the CharacterController (the player root). Used to pick Idle or Walk.")]
        public Transform playerRoot;

        [Tooltip("Optional. Found automatically on the player root.")]
        public BodycamStamina stamina;

        [Header("ANIMATOR STATE NAMES (state 1)")]
        public string idleState = "Idle";
        public string walkState = "Walk";
        public float crossFadeTime = 0.2f;

        [Tooltip("Horizontal speed (m/s) above which the Walk animation plays.")]
        public float walkSpeedThreshold = 0.5f;

        [Header("WEAPON (state 1)")]
        public float damage = 40f;
        public float attackDistance = 2.5f;

        [Tooltip("Radius of the hit sweep.")]
        public float hitRadius = 0.25f;

        public bool holdToRepeat = true;
        public LayerMask hitMask = ~0;

        [Header("ATTACK CHAIN (state 1) (length = number of hits in the combo)")]
        public AttackStep[] attackChain = new AttackStep[]
        {
            new AttackStep { stateName = "Attack 0_1" },
            new AttackStep { stateName = "Attack 0_2" },
            new AttackStep { stateName = "Attack 0_3" }
        };

        [Tooltip("If you wait longer than this after a swing ends, the combo restarts at step 1.")]
        public float chainResetTime = 1f;

        [Tooltip("Extra pause after the LAST swing of the chain before a new chain can start.")]
        public float chainEndRecovery = 0f;

        [Header("BLOCK (state 1)")]
        public BlockSettings block = new BlockSettings { blockState = "Block 0" };

        [Header("EXTRA STATES (up to 2: the first is state 2, the second is state 3. Delete entries for weapons with fewer states)")]
        public WeaponStateConfig[] extraStates = new WeaponStateConfig[]
        {
            DefaultExtraState(1),
            DefaultExtraState(2)
        };

        [Header("STATE SWITCHING")]
        [Tooltip("One entry per direction you want animated (1>2, 2>1, 1>3, 3>1, 2>3, 3>2). A direction without an entry switches instantly.")]
        public StateTransition[] stateTransitions = new StateTransition[0];

        [Tooltip("Key N selects state N (first key = state 1, and so on).")]
        public KeyCode[] stateKeys = new KeyCode[] { KeyCode.Z, KeyCode.X, KeyCode.C };

        [Tooltip("Optional. Cycles 1 > 2 > 3 > 1. Set to None to disable.")]
        public KeyCode cycleKey = KeyCode.V;

        [Tooltip("Off = switching is only allowed while the Idle animation is playing (or in the tail of a swing / switch, see the other two settings). On = Walk counts too.")]
        public bool allowSwitchWhileWalking = false;

        [Tooltip("Seconds after a swing's hit lands before you can switch state, cutting off the rest of that swing. -1 = wait for the whole swing.")]
        public float switchDelayAfterAttack = 0.25f;

        [Header("BLOCK INPUT & EFFECTS (shared by all states)")]
        [Tooltip("Hold to block. The parry window starts the moment the block starts.")]
        public KeyCode blockKey = KeyCode.Mouse1;

        [Tooltip("After releasing the block, wait this long before blocking again.")]
        public float blockCooldown = 0.4f;

        [Tooltip("Minimum seconds between two parry windows (measured from when the last one opened). Blocking during the cooldown still works, but as a normal block with no parry window. Stops spam clicking from keeping the parry on.")]
        public float parryCooldown = 1f;

        [Tooltip("Total width of the cone in front of the camera that can be blocked or parried. Attacks from outside it hit normally.")]
        [Range(10f, 360f)] public float blockAngle = 140f;

        public AudioClip blockSound;
        public AudioClip parrySound;

        public UnityEvent onBlocked;
        public UnityEvent onParried;

        [Header("DEBUG")]
        [Tooltip("Logs when the parry window opens and closes, and when a hit is parried or blocked. Also shows a note on screen while blocking. Editor and development builds only.")]
        public bool debugParryWindow = true;

        [Tooltip("Press this key during Play mode to turn the parry debug on or off. Set to None to disable the shortcut.")]
        public KeyCode debugToggleKey = KeyCode.F9;

        [Tooltip("Draws the hit sweep of every weapon state (and where the last swing hit or missed) as gizmos. Visible in the Scene view, and in the Game view when its Gizmos button is on. Editor only.")]
        public bool debugShowHitbox = true;

        [Tooltip("How many seconds the result of the last swing stays drawn.")]
        public float hitboxResultTime = 1f;

        [Tooltip("Press this key during Play mode to turn the hitbox display on or off. Set to None to disable the shortcut.")]
        public KeyCode hitboxToggleKey = KeyCode.F10;

        [Header("EFFECTS")]
        public AudioClip swingSound;
        public AudioClip hitSound;
        public Vector2 swingPitchRange = new Vector2(0.9f, 1.1f);

        [Tooltip("Optional. Spawned at the hit point.")]
        public GameObject hitEffectPrefab;
        public float hitEffectLifetime = 5f;

        /// <summary>Raised with the new state number (1-3) once the weapon has landed in a new state.</summary>
        public event System.Action<int> StateChanged;

        /// <summary>Raised when an incoming attack is blocked or parried.</summary>
        public event System.Action<BlockResult> BlockResolved;

        /// <summary>The current weapon state, 1-3.</summary>
        public int ActiveStateNumber => activeIndex + 1;

        /// <summary>How many states this weapon has (1-3).</summary>
        public int StateCount => states != null ? states.Length : 1;

        public bool IsBlocking => blocking;

        /// <summary>True while a hit would be parried.</summary>
        public bool ParryWindowActive =>
            blocking && blockHasParryWindow && states != null &&
            Time.time - blockStartTime <= states[activeIndex].parryWindow;

        class RuntimeState
        {
            public int idleHash, walkHash;
            public float damage, attackDistance, hitRadius, chainResetTime, chainEndRecovery;
            public AttackStep[] chain;
            public int[] stepHashes;
            public bool canBlock;
            public int blockHash;
            public float parryWindow, blockReduction, blockFadeTime;
        }

        RuntimeState[] states;
        StateTransition[,] transitions;
        int[,] transitionHashes;

        int activeIndex;
        bool switchingState;
        int pendingStateIndex;
        float transitionEndTime;       // the switch animation is over
        float transitionUnlockTime;    // from here on you can act again (the new state becomes active)
        float switchUnlockTime;        // earliest moment a state switch may start

        bool blocking;
        float blockStartTime;
        float blockReleaseTime = -999f;
        bool parryWindowOpen;          // the parry window of the current block is still running
        bool blockHasParryWindow;      // the current block got a parry window at all
        float lastParryWindowStart = -999f;

        int currentHash;
        int comboIndex;
        float nextAttackTime;     // when a new swing may start
        float swingEndTime;       // when the current swing (or state switch) animation is over
        float hitTime;
        float pendingDamage;
        MeleeDamageType pendingTypes;
        bool hitPending;

        Vector3 lastPlayerPosition;

        // Result of the last hit sweep (only used to draw the hitbox gizmos).
        Vector3 lastSweepOrigin, lastSweepDir, lastHitPoint;
        float lastSweepDistance, lastSweepRadius, lastHitDistance;
        float lastSweepTime = -999f;
        bool lastSweepHit;

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (audioSource == null)
                audioSource = GetComponentInParent<AudioSource>();

            if (playerCamera == null)
                playerCamera = Camera.main;

            if (stamina == null)
                stamina = GetComponentInParent<BodycamStamina>();
        }

        void OnValidate()
        {
            if (extraStates != null && extraStates.Length > MaxStates - 1)
                System.Array.Resize(ref extraStates, MaxStates - 1);

            if (stateTransitions != null)
            {
                foreach (StateTransition t in stateTransitions)
                {
                    if (t == null) continue;
                    t.fromState = Mathf.Clamp(t.fromState, 1, MaxStates);
                    t.toState = Mathf.Clamp(t.toState, 1, MaxStates);
                }
            }
        }

        RuntimeState BuildState(string idle, string walk, float dmg, float dist, float radius,
                                float resetTime, float recovery, AttackStep[] chain, BlockSettings blk)
        {
            bool hasBlock = blk != null && blk.canBlock && !string.IsNullOrEmpty(blk.blockState);

            RuntimeState s = new RuntimeState
            {
                idleHash = Animator.StringToHash(idle),
                walkHash = Animator.StringToHash(walk),
                damage = dmg,
                attackDistance = dist,
                hitRadius = radius,
                chainResetTime = resetTime,
                chainEndRecovery = recovery,
                chain = chain ?? new AttackStep[0],
                canBlock = hasBlock,
                blockHash = hasBlock ? Animator.StringToHash(blk.blockState) : 0,
                parryWindow = blk != null ? Mathf.Max(0f, blk.parryWindow) : 0f,
                blockReduction = blk != null ? Mathf.Clamp01(blk.damageReduction) : 0f,
                blockFadeTime = blk != null ? Mathf.Max(0f, blk.blockCrossFadeTime) : 0f
            };

            s.stepHashes = new int[s.chain.Length];

            for (int i = 0; i < s.chain.Length; i++)
                s.stepHashes[i] = Animator.StringToHash(s.chain[i].stateName);

            return s;
        }

        // Called every time this weapon's slot is selected (the slot is activated).
        void OnEnable()
        {
            int extra = extraStates != null ? Mathf.Min(extraStates.Length, MaxStates - 1) : 0;
            states = new RuntimeState[1 + extra];

            states[0] = BuildState(idleState, walkState, damage, attackDistance, hitRadius,
                                   chainResetTime, chainEndRecovery, attackChain, block);

            for (int i = 0; i < extra; i++)
            {
                WeaponStateConfig e = extraStates[i];
                states[i + 1] = BuildState(e.idleState, e.walkState, e.damage, e.attackDistance,
                                           e.hitRadius, e.chainResetTime, e.chainEndRecovery,
                                           e.attackChain, e.block);
            }

            transitions = new StateTransition[MaxStates, MaxStates];
            transitionHashes = new int[MaxStates, MaxStates];

            if (stateTransitions != null)
            {
                foreach (StateTransition t in stateTransitions)
                {
                    if (t == null) continue;

                    int from = t.fromState - 1;
                    int to = t.toState - 1;

                    if (from < 0 || to < 0 || from >= states.Length || to >= states.Length || from == to)
                        continue;

                    transitions[from, to] = t;
                    transitionHashes[from, to] = Animator.StringToHash(t.animatorState);
                }
            }

            // The weapon remembers its state when you switch away and back.
            // A state switch or block that was interrupted is cancelled.
            if (activeIndex >= states.Length)
                activeIndex = 0;

            switchingState = false;
            blocking = false;
            parryWindowOpen = false;
            blockHasParryWindow = false;
            blockReleaseTime = -999f;
            hitPending = false;
            comboIndex = 0;
            nextAttackTime = 0f;
            swingEndTime = 0f;
            switchUnlockTime = 0f;

            if (playerRoot != null)
                lastPlayerPosition = playerRoot.position;

            int idleHash = states[activeIndex].idleHash;

            ForEachAnimator(a =>
            {
                a.speed = 1f;

                if (a.HasState(0, idleHash))
                    a.Play(idleHash, 0, 0f);
            });
            currentHash = idleHash;

            ValidateAllStateNames();
        }

        // ---------------- setup check (editor and development builds only) ----------------

        bool namesChecked;

        // Logs a warning for every state name in the Inspector that doesn't exist in the Animator
        // Controller (typos, missing spaces...). Runs once, the first time the weapon is enabled.
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEBUG")]
        void ValidateAllStateNames()
        {
            if (namesChecked)
                return;

            namesChecked = true;

            ValidateStateNames("State 1", idleState, walkState, attackChain, block);

            int extra = extraStates != null ? Mathf.Min(extraStates.Length, MaxStates - 1) : 0;

            for (int i = 0; i < extra; i++)
            {
                WeaponStateConfig e = extraStates[i];
                ValidateStateNames("State " + (i + 2), e.idleState, e.walkState, e.attackChain, e.block);
            }

            if (stateTransitions != null)
            {
                foreach (StateTransition t in stateTransitions)
                {
                    if (t != null)
                        WarnIfMissing(t.animatorState, "Switch " + t.fromState + " > " + t.toState);
                }
            }
        }

        void ValidateStateNames(string label, string idle, string walk, AttackStep[] chain, BlockSettings blk)
        {
            WarnIfMissing(idle, label + " idle");
            WarnIfMissing(walk, label + " walk");

            if (blk != null && blk.canBlock)
                WarnIfMissing(blk.blockState, label + " block");

            if (chain == null)
                return;

            for (int i = 0; i < chain.Length; i++)
                WarnIfMissing(chain[i].stateName, label + " attack " + (i + 1));
        }

        void WarnIfMissing(string stateName, string what)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                Debug.LogWarning("Melee (" + name + "): " + what + " has an empty Animator state name.", this);
                return;
            }

            int hash = Animator.StringToHash(stateName);
            CheckAnimator(animator, hash, stateName, what);
            CheckAnimator(weaponMeshAnimator, hash, stateName, what);
        }

        void CheckAnimator(Animator a, int hash, string stateName, string what)
        {
            if (a == null || a.runtimeAnimatorController == null)
                return;

            if (!a.HasState(0, hash))
                Debug.LogWarning("Melee (" + name + "): " + what + " uses the Animator state \"" + stateName +
                                 "\", which doesn't exist in " + a.runtimeAnimatorController.name +
                                 ". Check the spelling and spaces.", this);
        }

        // Runs an action on the arms animator and on the weapon's own animator (if assigned).
        void ForEachAnimator(System.Action<Animator> action)
        {
            if (animator != null) action(animator);
            if (weaponMeshAnimator != null) action(weaponMeshAnimator);
        }

        // Crossfades every animator to a state on layer 0 and sets their speed.
        // An animator that doesn't have the state is skipped (instead of throwing
        // "Invalid Layer Index" / "State could not be found" errors). The setup check in
        // OnEnable names the states that are missing.
        void CrossFadeAll(int stateHash, float animatorSpeed, float fadeTime)
        {
            ForEachAnimator(a =>
            {
                a.speed = animatorSpeed;

                if (a.HasState(0, stateHash))
                    a.CrossFadeInFixedTime(stateHash, fadeTime, 0, 0f);
            });
        }

        void Update()
        {
#if UNITY_EDITOR || DEBUG
            if (debugToggleKey != KeyCode.None && Input.GetKeyDown(debugToggleKey))
            {
                debugParryWindow = !debugParryWindow;
                Debug.Log("[Melee parry] debug " + (debugParryWindow ? "ON" : "OFF"), this);
            }

            if (hitboxToggleKey != KeyCode.None && Input.GetKeyDown(hitboxToggleKey))
            {
                debugShowHitbox = !debugShowHitbox;
                Debug.Log("[Melee] hitbox display " + (debugShowHitbox ? "ON" : "OFF"), this);
            }
#endif

            float dt = Time.deltaTime;

            if (dt <= 0f || states == null)
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

            // A state switch has finished: the weapon is now in the new state.
            // This has to happen BEFORE the input below, otherwise an attack on the very first
            // free frame would still use the OLD state's chain.
            if (switchingState && Time.time >= transitionUnlockTime)
            {
                switchingState = false;
                SetActiveState(pendingStateIndex);
            }

            bool switching = switcher != null && switcher.IsSwitching;

            if (switching)
            {
                if (blocking)
                    EndBlock();
            }
            else
            {
                HandleStateInput();
                HandleBlockInput();

                bool pressed = holdToRepeat ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

                if (pressed)
                    Attack();
            }

            if (hitPending && Time.time >= hitTime)
            {
                hitPending = false;
                DoHit();
            }

            // Parry window ran out while the block is still held.
            if (blocking && parryWindowOpen && Time.time - blockStartTime > states[activeIndex].parryWindow)
            {
                parryWindowOpen = false;
                DebugLog("parry window CLOSED after " + states[activeIndex].parryWindow.ToString("F2") +
                         "s - now a normal block");
            }

            // Back to Idle / Walk once the swing (or state switch) animation is finished.
            // While blocking, the Block animation is held instead.
            if (!blocking && Time.time >= swingEndTime)
            {
                RuntimeState st = states[activeIndex];
                ChangeState(speed > walkSpeedThreshold ? st.walkHash : st.idleHash);
            }
        }

        // ---------------- blocking and parrying ----------------

        void HandleBlockInput()
        {
            RuntimeState st = states[activeIndex];
            bool held = st.canBlock && Input.GetKey(blockKey);

            if (blocking)
            {
                if (!held)
                    EndBlock();

                return;
            }

            if (held && CanStartBlock())
                StartBlock(st, Input.GetKeyDown(blockKey));
        }

        bool CanStartBlock()
        {
            if (switchingState || hitPending || Time.time < nextAttackTime)
                return false;

            return Time.time >= blockReleaseTime + blockCooldown;
        }

        void StartBlock(RuntimeState st, bool freshPress)
        {
            blocking = true;
            blockStartTime = Time.time;
            swingEndTime = Mathf.Min(swingEndTime, Time.time);   // blocking cuts off a switch tail

            // A parry window only opens on a deliberate new press (not when the button was
            // already held), and not more often than Parry Cooldown.
            float cooldownLeft = lastParryWindowStart + parryCooldown - Time.time;

            blockHasParryWindow = st.parryWindow > 0f && freshPress && cooldownLeft <= 0f;
            parryWindowOpen = blockHasParryWindow;

            if (blockHasParryWindow)
            {
                lastParryWindowStart = Time.time;
                DebugLog("block started - parry window OPEN for " + st.parryWindow.ToString("F2") + "s");
            }
            else if (st.parryWindow <= 0f)
                DebugLog("block started - this state has no parry window");
            else if (!freshPress)
                DebugLog("block started - NO parry window (the button was already held)");
            else
                DebugLog("block started - NO parry window (parry cooldown, " + cooldownLeft.ToString("F2") + "s left)");

            currentHash = st.blockHash;
            CrossFadeAll(st.blockHash, 1f, st.blockFadeTime);
        }

        void EndBlock()
        {
            blocking = false;
            blockReleaseTime = Time.time;
            // Update returns the animation to Idle / Walk right after this.

            if (parryWindowOpen)
                DebugLog("block released after " + (Time.time - blockStartTime).ToString("F2") +
                         "s - parry window CLOSED early");
            else
                DebugLog("block released after " + (Time.time - blockStartTime).ToString("F2") + "s");

            parryWindowOpen = false;
            blockHasParryWindow = false;
        }

        // ---------------- parry debug (editor and development builds only) ----------------

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEBUG")]
        void DebugLog(string message)
        {
            if (debugParryWindow)
                Debug.Log("[Melee parry] " + message + "   (t = " + Time.time.ToString("F2") + ")", this);
        }

        GUIStyle debugStyle;

        void OnGUI()
        {
#if UNITY_EDITOR || DEBUG
            if (!debugParryWindow || !blocking || states == null)
                return;

            float window = states[activeIndex].parryWindow;
            float elapsed = Time.time - blockStartTime;
            bool open = blockHasParryWindow && elapsed <= window;

            if (debugStyle == null)
            {
                debugStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 22,
                    fontStyle = FontStyle.Bold
                };
            }

            string text = open
                ? "PARRY WINDOW OPEN   " + Mathf.Max(0f, window - elapsed).ToString("F2") + "s"
                : (blockHasParryWindow ? "BLOCKING (parry window over)" : "BLOCKING (no parry this block)");

            Color previous = GUI.color;
            GUI.color = open ? Color.green : (blockHasParryWindow ? Color.yellow : new Color(1f, 0.5f, 0f));
            GUI.Label(new Rect(0f, Screen.height * 0.72f, Screen.width, 36f), text, debugStyle);
            GUI.color = previous;
#endif
        }

        /// <summary>
        /// Call this BEFORE applying damage to the player and apply the returned number instead.
        /// Inside the parry window the returned damage is 0. After it, the damage is reduced by
        /// the state's Damage Reduction. If the player isn't blocking, the damage is returned
        /// unchanged. This version has no attacker position, so the hit counts as coming from the front.
        /// </summary>
        public float ProcessIncomingDamage(float incomingDamage, out BlockResult result)
        {
            return ProcessIncomingDamage(incomingDamage, Vector3.zero, false, out result);
        }

        /// <summary>
        /// Same, but attacks from outside the Block Angle (e.g. from behind) are not blocked.
        /// </summary>
        public float ProcessIncomingDamage(float incomingDamage, Vector3 sourcePosition, out BlockResult result)
        {
            return ProcessIncomingDamage(incomingDamage, sourcePosition, true, out result);
        }

        float ProcessIncomingDamage(float incomingDamage, Vector3 sourcePosition, bool hasSource, out BlockResult result)
        {
            result = BlockResult.None;

            if (!blocking || states == null || incomingDamage <= 0f)
                return incomingDamage;

            if (hasSource && playerCamera != null)
            {
                Vector3 toSource = sourcePosition - playerCamera.transform.position;

                if (toSource.sqrMagnitude > 0.0001f &&
                    Vector3.Angle(playerCamera.transform.forward, toSource) > blockAngle * 0.5f)
                {
                    DebugLog("hit ignored - it came from outside the block angle");
                    return incomingDamage;
                }
            }

            RuntimeState st = states[activeIndex];
            bool parried = blockHasParryWindow && Time.time - blockStartTime <= st.parryWindow;
            result = parried ? BlockResult.Parried : BlockResult.Blocked;

            DebugLog("hit " + (parried ? "PARRIED" : "BLOCKED") + " " +
                     (Time.time - blockStartTime).ToString("F2") + "s into the block (parry window = " +
                     st.parryWindow.ToString("F2") + "s)");

            AudioClip clip = parried ? parrySound : blockSound;

            if (audioSource != null && clip != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(clip);
            }

            if (parried)
                onParried?.Invoke();
            else
                onBlocked?.Invoke();

            BlockResolved?.Invoke(result);

            return parried ? 0f : incomingDamage * (1f - st.blockReduction);
        }

        // ---------------- hitbox visualizer (editor only) ----------------

#if UNITY_EDITOR
        // Green = state 1, cyan = state 2, magenta = state 3. The active state is bright, the others faint.
        // After a swing, the sphere where the sweep stopped is drawn red (hit) or orange (miss),
        // with a yellow dot at the exact hit point.
        void OnDrawGizmos()
        {
            if (!debugShowHitbox)
                return;

            Camera cam = playerCamera != null ? playerCamera : Camera.main;

            if (cam == null)
                return;

            Transform c = cam.transform;
            int active = Application.isPlaying && states != null ? activeIndex : 0;

            DrawSweepGizmo(c, attackDistance, hitRadius, Color.green, active == 0);

            int extra = extraStates != null ? Mathf.Min(extraStates.Length, MaxStates - 1) : 0;

            for (int i = 0; i < extra; i++)
            {
                WeaponStateConfig e = extraStates[i];

                if (e != null)
                    DrawSweepGizmo(c, e.attackDistance, e.hitRadius, i == 0 ? Color.cyan : Color.magenta, active == i + 1);
            }

            float age = Time.time - lastSweepTime;

            if (age >= 0f && age <= hitboxResultTime)
            {
                Vector3 stop = lastSweepOrigin + lastSweepDir * (lastSweepHit ? lastHitDistance : lastSweepDistance);

                Gizmos.color = lastSweepHit ? Color.red : new Color(1f, 0.5f, 0f);
                Gizmos.DrawWireSphere(stop, lastSweepRadius);
                Gizmos.DrawLine(lastSweepOrigin, stop);

                if (lastSweepHit)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(lastHitPoint, 0.04f);
                }
            }
        }

        static void DrawSweepGizmo(Transform cam, float distance, float radius, Color color, bool strong)
        {
            color.a = strong ? 0.9f : 0.2f;
            Gizmos.color = color;

            Vector3 start = cam.position;
            Vector3 end = start + cam.forward * distance;
            Vector3 right = cam.right * radius;
            Vector3 up = cam.up * radius;

            Gizmos.DrawWireSphere(start, radius);
            Gizmos.DrawWireSphere(end, radius);
            Gizmos.DrawLine(start + right, end + right);
            Gizmos.DrawLine(start - right, end - right);
            Gizmos.DrawLine(start + up, end + up);
            Gizmos.DrawLine(start - up, end - up);
        }
#endif

        // ---------------- weapon states ----------------

        void HandleStateInput()
        {
            if (states.Length <= 1)
                return;

            int target = -1;

            if (stateKeys != null)
            {
                for (int i = 0; i < stateKeys.Length && i < states.Length; i++)
                {
                    if (stateKeys[i] != KeyCode.None && Input.GetKeyDown(stateKeys[i]))
                    {
                        target = i;
                        break;
                    }
                }
            }

            if (target < 0 && cycleKey != KeyCode.None && Input.GetKeyDown(cycleKey))
                target = (activeIndex + 1) % states.Length;

            if (target >= 0)
                TrySwitchState(target + 1);
        }

        bool CanSwitchNow()
        {
            if (switchingState || blocking || hitPending || Time.time < switchUnlockTime)
                return false;

            RuntimeState st = states[activeIndex];

            // Idle, or the tail of a swing / state switch that may be cut short.
            if (currentHash == st.idleHash || Time.time < swingEndTime)
                return true;

            return allowSwitchWhileWalking && currentHash == st.walkHash;
        }

        /// <summary>
        /// Switches to weapon state 1-3 (the same numbers as in the Inspector).
        /// Only works while idle. Returns true if the switch started.
        /// </summary>
        public bool TrySwitchState(int stateNumber)
        {
            if (states == null)
                return false;

            int target = stateNumber - 1;

            if (target < 0 || target >= states.Length || target == activeIndex)
                return false;

            if (!CanSwitchNow())
                return false;

            StateTransition t = transitions[activeIndex, target];

            // Switching cuts off whatever is left of the current swing.
            swingEndTime = Mathf.Min(swingEndTime, Time.time);
            nextAttackTime = Mathf.Min(nextAttackTime, Time.time);

            // No transition animation defined for this direction: switch instantly.
            if (t == null || t.duration <= 0f)
            {
                SetActiveState(target);
                ChangeState(states[target].idleHash);
                return true;
            }

            float speed = Mathf.Max(0.05f, t.animationSpeed);

            switchingState = true;
            pendingStateIndex = target;
            comboIndex = 0;

            float length = t.duration / speed;
            float unlock = t.unlockTime > 0f ? Mathf.Min(t.unlockTime / speed, length) : length;

            transitionEndTime = Time.time + length;
            transitionUnlockTime = Time.time + unlock;

            swingEndTime = transitionEndTime;         // the animation keeps playing until here
            nextAttackTime = transitionUnlockTime;    // but you can attack or block from the unlock time
            switchUnlockTime = transitionUnlockTime;  // and switch again

            if (audioSource != null && t.sound != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(t.sound);
            }

            currentHash = transitionHashes[activeIndex, target];
            CrossFadeAll(currentHash, speed, crossFadeTime);

            return true;
        }

        void SetActiveState(int index)
        {
            activeIndex = index;
            comboIndex = 0;
            StateChanged?.Invoke(activeIndex + 1);
        }

        // ---------------- attacking ----------------

        void Attack()
        {
            if (states == null || blocking)
                return;

            RuntimeState st = states[activeIndex];

            if (Time.time < nextAttackTime || st.stepHashes.Length == 0)
                return;

            // Combo restarts if the player waited too long since the last swing ended.
            if (Time.time > swingEndTime + st.chainResetTime || comboIndex >= st.chain.Length)
                comboIndex = 0;

            AttackStep step = st.chain[comboIndex];
            float speed = Mathf.Max(0.05f, step.animationSpeed);

            if (stamina != null && !stamina.TryConsume(step.staminaCost))
                return;

            pendingDamage = st.damage * step.damageMultiplier;
            pendingTypes = step.damageTypes;
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
            currentHash = st.stepHashes[comboIndex];
            CrossFadeAll(currentHash, speed, crossFadeTime);

            comboIndex++;

            if (comboIndex >= st.chain.Length)
            {
                comboIndex = 0;
                nextAttackTime += st.chainEndRecovery;
            }

            // A state switch is allowed a short moment after the hit lands (or after the whole
            // swing if Switch Delay After Attack is negative).
            switchUnlockTime = switchDelayAfterAttack < 0f
                ? nextAttackTime
                : Mathf.Min(hitTime + switchDelayAfterAttack, nextAttackTime);
        }

        // Used for Idle and Walk only.
        void ChangeState(int stateHash)
        {
            if (currentHash == stateHash)
                return;

            currentHash = stateHash;
            CrossFadeAll(stateHash, 1f, crossFadeTime);
        }

        void DoHit()
        {
            if (playerCamera == null)
                return;

            RuntimeState st = states[activeIndex];
            Transform cam = playerCamera.transform;

            bool didHit = Physics.SphereCast(
                cam.position,
                st.hitRadius,
                cam.forward,
                out RaycastHit hit,
                st.attackDistance,
                hitMask,
                QueryTriggerInteraction.Ignore);

            // Remember the sweep so the hitbox gizmos can show what just happened.
            lastSweepOrigin = cam.position;
            lastSweepDir = cam.forward;
            lastSweepDistance = st.attackDistance;
            lastSweepRadius = st.hitRadius;
            lastSweepTime = Time.time;
            lastSweepHit = didHit;

            if (didHit)
            {
                lastHitPoint = hit.point;
                lastHitDistance = hit.distance;
            }

            if (!didHit)
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

            // Targets that understand damage types get them.
            IMeleeDamageable typed = hit.transform.GetComponentInParent<IMeleeDamageable>();

            if (typed != null)
            {
                typed.TakeMeleeDamage(new MeleeHitInfo
                {
                    amount = pendingDamage,
                    types = pendingTypes,
                    point = hit.point,
                    normal = hit.normal
                });
                return;
            }

            // Everything else gets the plain damage number, as before.
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