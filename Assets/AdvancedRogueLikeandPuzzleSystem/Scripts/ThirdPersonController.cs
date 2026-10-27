using UnityEngine;
using System.Collections;

namespace AdvancedRogueLikeandPuzzleSystem
{
    // Provenance: adapted from the ThirdPersonController in Unity's "Starter Assets - Third
    // Person Character Controller" (Unity Technologies, Unity Companion License). Changes include
    // input routed through GameManager's InputActions and the mobile joystick, the swimming mode,
    // mana-costed sprint dash, footstep audio, and TPS/FPS camera-relative movement.
    // See THIRD_PARTY_NOTICES.md at the project root.

    /// <summary>
    /// Moves the hero with a CharacterController: camera-relative walking and sprinting,
    /// jumping and gravity, a sphere-cast ground check, swimming inside "WaterForSwimming"
    /// triggers, and a mana-costed sprint dash. Movement is skipped while the hero is attacking,
    /// defending, dead, or while the inventory is open.
    /// </summary>
    public class ThirdPersonController : MonoBehaviour
    {
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.335f;
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;
        [Space(10)]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        private Vector2 vectorMove = Vector2.zero;
        [Space(10)]
        public float JumpTimeout = 1;
        public float FallTimeout = 0.15f;

        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        public float _speed;
        public float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _fallTimeoutDelta;

        public int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        public Animator _animator;
        private CharacterController _controller;
        public bool isSwimming = false;
        public ParticleSystem SwimmingParticle;

        private const float _threshold = 0.01f;
        private bool _hasAnimator;
        public static ThirdPersonController Instance;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();

            AssignAnimationIDs();

            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            if (HeroController.instance == null || GameManager.Instance == null) return;
            if (HeroController.instance.Health <= 0) return;
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) return;

            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            if (!isSwimming)
            {
                Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
                Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDGrounded, Grounded);
                }
            }
        }
        float lastTime = 0;

        Vector3 inputDirection;
        float inputMagnitude = 0;
        float targetSpeed = 0;
        private void Move()
        {
            if (GameManager.Instance.controllerType == ControllerType.KeyboardMouse)
            {
                vectorMove = GameManager.Instance.moveAction.ReadValue<Vector2>();
                targetSpeed = GameManager.Instance.sprintAction.IsPressed() ? Mathf.Lerp(targetSpeed, SprintSpeed, 0.2f) : Mathf.Lerp(targetSpeed, MoveSpeed, 0.2f);
            }
            else if (GameManager.Instance.controllerType == ControllerType.Mobile)
            {
                vectorMove = new Vector2(SimpleJoystick.Instance.HorizontalValue, SimpleJoystick.Instance.VerticalValue);
                if (Mathf.Abs(SimpleJoystick.Instance.VerticalValue) > 0.75f || Mathf.Abs(SimpleJoystick.Instance.HorizontalValue) > 0.75f)
                {
                    targetSpeed = Mathf.Lerp(targetSpeed, SprintSpeed, 0.1f);
                }
                else
                {
                    targetSpeed = Mathf.Lerp(targetSpeed, MoveSpeed, 0.1f);
                }
            }

            if (vectorMove == Vector2.zero) targetSpeed = 0.0f;
            float currentHorizontalSpeed = new Vector3(vectorMove.x, 0.0f, vectorMove.y).magnitude;
            float speedOffset = Time.deltaTime;
            inputMagnitude = 1;
            _speed = targetSpeed;
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

            if (vectorMove != Vector2.zero)
            {
                inputDirection = new Vector3(vectorMove.x, 0.0f, vectorMove.y).normalized;
                if(Time.time > lastTime + 0.35f && !HeroController.instance.inDefendMode && !isSwimming)
                {
                    lastTime = Time.time;
                    AudioSource footstepSource = HeroController.instance.audio_Footstep;
                    AudioClip footstep = AudioManager.instance != null ? AudioManager.instance.Get_RandomFootstep() : null;
                    if (footstepSource != null && footstep != null)
                    {
                        footstepSource.clip = footstep;
                        footstepSource.Play();
                    }
                }
            }
            else if (HeroController.instance.audio_Footstep != null)
            {
                HeroController.instance.audio_Footstep.Stop();
            }

            Transform activeCamera = GameManager.Instance.GetCurrentCamera();
            if (vectorMove != Vector2.zero && activeCamera != null)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + activeCamera.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }

            if (_speed == 0 && (_verticalVelocity < 0 && Grounded))
                return;
            if (HeroController.instance.isHitting || HeroController.instance.inDefendMode) return;
            if (isSwimming)
            {
                _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
                if (transform.position.y < waterInTouch.transform.position.y - 0.75f)
                {
                    transform.position = new Vector3(transform.position.x, waterInTouch.transform.position.y - 0.75f, transform.position.z);
                }
            }
            else
            {
                _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }

            if(GameManager.Instance.sprintAction.WasPressedThisFrame() && HeroController.instance.Mana >= ManaSpending_Sprint)
            {
                GameCanvas_Controller.instance.Update_Mana_Bar(ManaSpending_Sprint);
                StartCoroutine(TrailRendererShow());
            }
        }
        public TrailRenderer SprinttrailRenderer;
        public ParticleSystem SprintParticle;
        public int ManaSpending_Sprint = 20;

        public void Sprint_Now()
        {
            if (HeroController.instance.Mana >= ManaSpending_Sprint && vectorMove != Vector2.zero)
            {
                GameCanvas_Controller.instance.Update_Mana_Bar(ManaSpending_Sprint);
                StartCoroutine(TrailRendererShow());
            }
        }

        IEnumerator TrailRendererShow()
        {
            SprinttrailRenderer.enabled = true;
            SprintParticle.Play();
            AudioManager.instance.Play_Sprint();
            yield return new WaitForSeconds(0.1f);
            HeroController.instance.characterController.Move(transform.forward * 5);
            yield return new WaitForSeconds(0.2f);
            SprintParticle.Stop();
            SprinttrailRenderer.enabled = false;

        }
        private Transform waterInTouch;
        private float lastTimeJump = 0;

        private void JumpAndGravity()
        {
            if (!this.enabled) return;

            if (isSwimming)
            {
                return;
            }

            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (GameManager.Instance.jumpAction.WasReleasedThisFrame() && Time.time > lastTimeJump + JumpTimeout)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    lastTimeJump = Time.time;
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

            }
            else
            {
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Water"))
            {
                AudioManager.instance.Play_Swimming();
                SwimmingParticle.Play();
            }
            if (other.CompareTag("WaterForSwimming"))
            {
                _animator.SetBool("Swim", true);
                waterInTouch = other.transform;
                AudioManager.instance.Play_Swimming();
                isSwimming = true;
                SwimmingParticle.Play();
                Grounded = true;
            }
        }

        public void JumpNow()
        {
            if (!this.enabled) return;

            if (Time.time > lastTimeJump + JumpTimeout)
            {
                lastTimeJump = Time.time;
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                Grounded = false;
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Water"))
            {
                AudioManager.instance.Stop_Swimming();
                SwimmingParticle.Stop();
            }
            if (other.CompareTag("WaterForSwimming"))
            {
                AudioManager.instance.Stop_Swimming();
                _animator.SetBool("Swim", false);
                SwimmingParticle.Stop();
                isSwimming = false;
            }
        }
    }
}