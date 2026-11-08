using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PurgeFPS
{
    [AddComponentMenu("Realistic Bodycam/Advanced Shooting System")]
    public class BodycamAdvancedShootingSystem : MonoBehaviour
    {
        public enum ReloadType
        {
            FullMagazine,
            ShellByShell
        }

        [Header("REFERENCES")]
        public Camera playerCamera;
        public Transform weaponTransform;
        public Animator weaponAnimator;

        [Tooltip("Optional second animator (for example the gun, when the arms and gun are separate rigs). " +
                 "It gets the same idle, fire and reload commands as the Weapon Animator. " +
                 "Its controller needs the same state names and trigger names.")]
        public Animator gunAnimator;

        public BodycamFPSController bodycamController;

        [Header("WEAPON ANIMATIONS")]
        [Tooltip("Name of the looping idle state in the Animator Controller.")]
        public string idleStateName = "Idle";

        [Tooltip("Animator trigger used when the weapon fires.")]
        public string fireTriggerName = "Fire";

        [Tooltip("Animator trigger used when the weapon reloads.")]
        public string reloadTriggerName = "Reload";

        public bool playIdleOnEnable = true;

        [Header("AUDIO")]
        public AudioSource audioSource;
        public AudioClip shootSound;

        [Tooltip("Sound played once when a Full Magazine reload starts.")]
        public AudioClip fullReloadSound;

        [Tooltip("Sound played for every shell inserted during a Shell By Shell reload.")]
        public AudioClip shellInsertSound;

        [Header("MUZZLE FLASH")]
        public ParticleSystem muzzleFlash;

        [Header("MUZZLE LIGHT")]
        public Light muzzleLight;

        [Min(0.01f)]
        [Tooltip("How long the point light stays enabled after each shot.")]
        public float muzzleLightDuration = 0.05f;

        [Header("SHOOTING")]
        public LayerMask hitMask;
        public float range = 100f;
        public float fireRate = 0.15f;

        [Header("PHYSICAL BULLET")]
        [Tooltip("Creates a visible physical bullet at the muzzle when firing.")]
        public bool usePhysicalBullet = true;

        [Tooltip("Prefab used as the visible bullet. A small sphere with a material is enough.")]
        public GameObject bulletPrefab;

        [Tooltip("Point at the end of the weapon barrel where the bullet is created.")]
        public Transform bulletSpawnPoint;

        [Tooltip("When enabled, shots follow the blue Z axis of the Bullet Spawn Point instead of the center of the camera.")]
        public bool useWeaponDirection = true;

        [Tooltip("Optional local rotation correction if the weapon model uses a different forward axis.")]
        public Vector3 shotDirectionOffset;

        [Min(0.01f)]
        public float bulletSpeed = 120f;

        [Min(0.01f)]
        public float bulletLifetime = 5f;

        [Min(0f)]
        public float bulletDamage = 25f;

        public bool bulletUseGravity = false;

        [Header("HITSCAN (used when Use Physical Bullet is off)")]
        [Tooltip("Spawned where a shot hits. A Particle System or VFX Graph prefab with Play On Awake enabled. " +
                 "Its forward (blue) axis points away from the surface. Damage uses Bullet Damage.")]
        public GameObject impactEffect;

        [Min(1)]
        [Tooltip("How many impact effects are reused in turn (avoids creating a new object per hit).")]
        public int impactPoolSize = 8;

        [Min(1)]
        [Tooltip("Rays fired per shot. Use e.g. 8 for the shotgun. Each ray deals Bullet Damage.")]
        public int pelletsPerShot = 1;

        [Min(0f)]
        [Tooltip("Random spread cone in degrees (half angle). 0 = perfectly accurate.")]
        public float spreadAngle = 0f;

        [Min(0f)]
        [Tooltip("Impulse applied to rigid bodies that are hit. 0 = none.")]
        public float hitForce = 0f;

        [Header("TRACERS (hitscan)")]
        public bool showTracers = true;

        [Tooltip("Optional. Leave empty for a simple built-in unlit material. Use e.g. an additive particle material for a glow.")]
        public Material tracerMaterial;

        public Color tracerColor = new Color(1f, 0.85f, 0.45f, 1f);

        [Min(0.001f)]
        public float tracerWidth = 0.02f;

        [Min(0.1f)]
        [Tooltip("Length of the streak in metres.")]
        public float tracerLength = 3f;

        [Min(1f)]
        [Tooltip("Speed of the streak in metres per second.")]
        public float tracerSpeed = 300f;

        [Range(0f, 1f)]
        [Tooltip("Chance that a shot (or pellet) shows a tracer. 1 = every shot, 0.33 = about every third.")]
        public float tracerChance = 1f;

        [Min(1)]
        [Tooltip("Most tracers visible at once. If more are fired, the extra ones are skipped.")]
        public int maxTracers = 32;

        [Header("BULLET HOLES (hitscan)")]
        [Tooltip("A flat Quad (or URP Decal Projector) with a bullet hole material. It must have NO collider. " +
                 "Placed on the surface that was hit, facing out of it.")]
        public GameObject bulletHolePrefab;

        [Min(1)]
        [Tooltip("How many holes exist at once. The oldest hole is reused when the limit is reached. " +
                 "A shotgun fires several rays per shot, so give it a larger number.")]
        public int bulletHolePoolSize = 24;

        [Tooltip("Distance the hole is lifted off the surface, to stop it flickering into the wall.")]
        public float bulletHoleSurfaceOffset = 0.005f;

        [Tooltip("Random size multiplier (min, max) applied to the prefab's scale.")]
        public Vector2 bulletHoleSizeRange = new Vector2(0.9f, 1.15f);

        [Tooltip("Holes on targets and rigid bodies follow the object when it moves.")]
        public bool bulletHolesFollowMovingObjects = true;

        [Header("AMMUNITION & RELOAD")]
        public bool useAmmoSystem = true;

        [Tooltip("Full Magazine reloads the entire weapon at once. Shell By Shell inserts one shell per animation.")]
        public ReloadType reloadType = ReloadType.FullMagazine;

        [Min(1)]
        public int magazineSize = 15;

        public KeyCode reloadKey = KeyCode.R;

        [Min(0.01f)]
        [Tooltip("This value should match the length of the reload animation.")]
        public float reloadDuration = 1.8f;

        [Min(0.01f)]
        [Tooltip("For Shell By Shell reloads, this must match the length of the single-shell insert animation.")]
        public float shellInsertDuration = 0.75f;

        [Range(0f, 0.5f)]
        [Tooltip("Starts the next shell animation slightly before the current animation ends, preventing a visible return to Idle between shells.")]
        public float shellAnimationOverlap = 0.1f;

        [Tooltip("Allows a Shell By Shell reload to be interrupted by firing after at least one shell has been inserted.")]
        public bool allowShellReloadInterrupt = true;

        public bool autoReloadWhenEmpty = true;

        [Header("RELOAD CAMERA SHAKE")]
        public bool enableReloadCameraShake = true;

        [Min(0f)]
        [Tooltip("Camera shake strength during a Full Magazine reload, in degrees.")]
        public float fullReloadShakeIntensity = 0.45f;

        [Min(0f)]
        [Tooltip("Camera shake strength for each shell inserted.")]
        public float shellReloadShakeIntensity = 0.6f;

        [Min(0.01f)]
        public float reloadShakeFrequency = 20f;

        [Min(0.01f)]
        [Tooltip("Duration of the camera impulse for each inserted shell.")]
        public float shellReloadShakeDuration = 0.2f;

        [Header("WEAPON RECOIL")]
        public Vector3 recoilPosition = new Vector3(0f, 0f, -0.08f);
        public Vector3 recoilRotation = new Vector3(-6f, 2f, 2f);
        public float recoilReturnSpeed = 12f;
        public float recoilSnappiness = 18f;

        [Header("RANDOM RECOIL")]
        public bool enableRandomRecoil = true;

        [Range(0f, 1f)]
        [Tooltip("Random variation applied to the overall recoil strength.")]
        public float recoilStrengthVariation = 0.2f;

        [Tooltip("Maximum random horizontal movement applied to the weapon.")]
        public float horizontalPositionVariation = 0.015f;

        [Tooltip("Maximum random vertical movement applied to the weapon.")]
        public float verticalPositionVariation = 0.01f;

        [Tooltip("Random variation applied to the weapon's vertical rotation.")]
        public float verticalRotationVariation = 1.2f;

        [Header("CAMERA RECOIL")]
        public bool enableCameraRecoil = true;
        public float cameraRecoilAmount = 1.5f;
        public float cameraHorizontalRecoil = 0.6f;
        public float cameraRollRecoil = 0.35f;

        Vector3 originalWeaponPosition;
        Quaternion originalWeaponRotation;

        Vector3 currentRecoilPosition;
        Vector3 targetRecoilPosition;

        Vector3 currentRecoilRotation;
        Vector3 targetRecoilRotation;

        float nextFireTime;
        Coroutine muzzleLightRoutine;
        Coroutine reloadRoutine;

        int currentAmmo;
        bool isReloading;

        public int CurrentAmmo => currentAmmo;
        public bool IsReloading => isReloading;

        void Start()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            if (weaponTransform == null)
                weaponTransform = transform;

            if (weaponAnimator == null)
                weaponAnimator = GetComponentInChildren<Animator>();

            CacheGunAnimatorParameters();

            if (bodycamController == null)
                bodycamController = GetComponentInParent<BodycamFPSController>();

            originalWeaponPosition = weaponTransform.localPosition;
            originalWeaponRotation = weaponTransform.localRotation;

            currentAmmo = Mathf.Max(1, magazineSize);

            if (muzzleLight != null)
                muzzleLight.enabled = false;

            PlayIdleAnimation();
        }

        void Update()
        {
            if (useAmmoSystem && Input.GetKeyDown(reloadKey))
                StartReload();

            bool wantsToFire = Input.GetMouseButton(0) && Time.time >= nextFireTime;

            if (wantsToFire && isReloading && CanInterruptShellReload())
                CancelReload();

            if (!isReloading && wantsToFire)
            {
                if (!useAmmoSystem || currentAmmo > 0)
                {
                    Shoot();
                    nextFireTime = Time.time + fireRate;
                }
                else if (autoReloadWhenEmpty)
                {
                    StartReload();
                }
            }

            UpdateRecoil();
        }

        void Shoot()
        {
            if (playerCamera == null)
                return;

            if (useAmmoSystem)
                currentAmmo--;

            PlayFireAnimation();

            if (audioSource != null && shootSound != null)
                audioSource.PlayOneShot(shootSound);

            if (muzzleFlash != null)
                muzzleFlash.Play();

            FlashMuzzleLight();

            FireShot();

            ApplyRecoil();
        }

        void FireShot()
        {
            Transform shotPoint = bulletSpawnPoint != null ? bulletSpawnPoint : weaponTransform;
            Vector3 spawnPosition = shotPoint.position;

            Vector3 shotDirection = useWeaponDirection
                ? shotPoint.TransformDirection(Quaternion.Euler(shotDirectionOffset) * Vector3.forward).normalized
                : playerCamera.transform.forward;

            Vector3 rayOrigin = useWeaponDirection
                ? spawnPosition
                : playerCamera.transform.position;

            Ray shotRay = new Ray(rayOrigin, shotDirection);

            if (!usePhysicalBullet || bulletPrefab == null)
            {
                for (int i = 0; i < pelletsPerShot; i++)
                {
                    Vector3 pelletDirection = SpreadDirection(shotDirection, spreadAngle);
                    Ray pelletRay = new Ray(rayOrigin, pelletDirection);

                    bool didHit = TryHitscan(pelletRay, out RaycastHit hit);

                    if (didHit)
                        ApplyHitscan(hit, pelletDirection);

                    SpawnTracer(spawnPosition, didHit ? hit.point : rayOrigin + pelletDirection * range);
                }

                return;
            }

            GameObject bulletObject = Instantiate(
                bulletPrefab,
                spawnPosition,
                Quaternion.LookRotation(shotDirection, shotPoint.up)
            );

            BodycamProjectile projectile = bulletObject.GetComponent<BodycamProjectile>();

            if (projectile == null)
                projectile = bulletObject.AddComponent<BodycamProjectile>();

            projectile.Initialize(
                shotDirection,
                bulletSpeed,
                bulletLifetime,
                bulletDamage,
                bulletUseGravity,
                hitMask,
                transform.root
            );
        }

        // ---------------- hitscan ----------------

        static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

        GameObject[] impactPool;
        int impactPoolIndex;

        static Vector3 SpreadDirection(Vector3 direction, float angle)
        {
            if (angle <= 0f)
                return direction;

            Vector3 right = Vector3.Cross(direction, Vector3.up);

            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.right;

            right.Normalize();
            Vector3 up = Vector3.Cross(right, direction).normalized;

            Vector2 offset = Random.insideUnitCircle * Mathf.Tan(angle * Mathf.Deg2Rad);

            return (direction + right * offset.x + up * offset.y).normalized;
        }

        // Nearest hit that is not part of the player itself (allocation free).
        bool TryHitscan(Ray ray, out RaycastHit result)
        {
            int count = Physics.RaycastNonAlloc(
                ray,
                hitBuffer,
                range,
                hitMask,
                QueryTriggerInteraction.Ignore
            );

            Transform owner = transform.root;
            float nearest = float.MaxValue;
            bool found = false;
            result = default;

            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = hitBuffer[i];

                if (candidate.collider.transform.IsChildOf(owner))
                    continue;

                if (candidate.distance < nearest)
                {
                    nearest = candidate.distance;
                    result = candidate;
                    found = true;
                }
            }

            return found;
        }

        void ApplyHitscan(RaycastHit hit, Vector3 direction)
        {
            Transform hitTransform = hit.collider.transform;

            BodycamRespawningTarget respawningTarget =
                hitTransform.GetComponentInParent<BodycamRespawningTarget>();

            if (respawningTarget != null)
            {
                respawningTarget.TakeDamage(bulletDamage);
            }
            else
            {
                BodycamShootableTarget shootableTarget =
                    hitTransform.GetComponentInParent<BodycamShootableTarget>();

                if (shootableTarget != null)
                {
                    shootableTarget.TakeDamage(bulletDamage);
                }
                else
                {
                    hitTransform.SendMessageUpwards(
                        "TakeDamage",
                        bulletDamage,
                        SendMessageOptions.DontRequireReceiver
                    );
                }
            }

            if (hitForce > 0f && hit.rigidbody != null)
                hit.rigidbody.AddForceAtPosition(direction * hitForce, hit.point, ForceMode.Impulse);

            SpawnImpactEffect(hit.point, hit.normal);
            SpawnBulletHole(hit);
        }

        // Reuses a small pool of impact objects: restarting an object by disabling and enabling
        // it replays its Particle System or VFX Graph without creating garbage.
        void SpawnImpactEffect(Vector3 point, Vector3 normal)
        {
            if (impactEffect == null)
                return;

            if (impactPool == null || impactPool.Length != impactPoolSize)
            {
                impactPool = new GameObject[impactPoolSize];
                impactPoolIndex = 0;
            }

            GameObject effect = impactPool[impactPoolIndex];

            if (effect == null)
            {
                effect = Instantiate(impactEffect);
                impactPool[impactPoolIndex] = effect;
            }

            impactPoolIndex = (impactPoolIndex + 1) % impactPool.Length;

            effect.SetActive(false);
            effect.transform.SetPositionAndRotation(
                point + normal * 0.01f,
                Quaternion.LookRotation(normal)
            );
            effect.SetActive(true);
        }

        // ---------------- tracers ----------------
        // One particle system per gun draws every tracer as a stretched streak. Nothing is
        // created per shot: each tracer is just one emitted particle.

        ParticleSystem tracerSystem;
        ParticleSystemRenderer tracerRenderer;
        float tracerShownLength = -1f;
        float tracerShownSpeed = -1f;
        static Material tracerDefaultMaterial;

        void SpawnTracer(Vector3 from, Vector3 to)
        {
            if (!showTracers)
                return;

            if (tracerChance < 1f && Random.value > tracerChance)
                return;

            Vector3 delta = to - from;
            float distance = delta.magnitude;

            if (distance < 0.5f)
                return;

            EnsureTracerSystem();

            if (tracerSystem == null)
                return;

            float speed = Mathf.Max(1f, tracerSpeed);
            float length = Mathf.Max(0.1f, tracerLength);

            // Streak length = particle speed x velocity scale, so it only needs updating when these change.
            if (!Mathf.Approximately(length, tracerShownLength) || !Mathf.Approximately(speed, tracerShownSpeed))
            {
                tracerShownLength = length;
                tracerShownSpeed = speed;
                tracerRenderer.velocityScale = length / speed;
            }

            Vector3 direction = delta / distance;

            // The streak trails behind the particle, so start it one streak-length ahead of the muzzle.
            float startOffset = Mathf.Min(length, distance * 0.5f);

            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
            emit.position = from + direction * startOffset;
            emit.velocity = direction * speed;
            emit.startLifetime = (distance - startOffset) / speed;
            emit.startSize = tracerWidth;
            emit.startColor = tracerColor;

            tracerSystem.Emit(emit, 1);
        }

        // Builds the tracer particle system once (and again if the scene was reloaded).
        void EnsureTracerSystem()
        {
            if (tracerSystem != null)
                return;

            GameObject tracerObject = new GameObject("GunTracers");
            tracerSystem = tracerObject.AddComponent<ParticleSystem>();
            tracerSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = tracerSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Max(1, maxTracers);
            main.gravityModifier = 0f;
            main.startSpeed = 0f;

            ParticleSystem.EmissionModule emission = tracerSystem.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = tracerSystem.shape;
            shape.enabled = false;

            tracerRenderer = tracerObject.GetComponent<ParticleSystemRenderer>();
            tracerRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            tracerRenderer.lengthScale = 1f;
            tracerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracerRenderer.receiveShadows = false;
            tracerRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            tracerRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            tracerRenderer.sharedMaterial = tracerMaterial != null ? tracerMaterial : GetTracerDefaultMaterial();

            tracerShownLength = -1f;
            tracerShownSpeed = -1f;

            tracerSystem.Play();
        }

        static Material GetTracerDefaultMaterial()
        {
            if (tracerDefaultMaterial == null)
            {
                // Sprites/Default uses the particle colour. It is always available.
                Shader shader = Shader.Find("Sprites/Default");

                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

                if (shader == null)
                    return null;

                tracerDefaultMaterial = new Material(shader) { name = "Gun Tracer (auto)" };
            }

            return tracerDefaultMaterial;
        }

        GameObject[] bulletHolePool;
        int bulletHoleIndex;

        // Reuses a ring of hole objects (no garbage per shot). The oldest hole is moved to the new spot.
        void SpawnBulletHole(RaycastHit hit)
        {
            if (bulletHolePrefab == null)
                return;

            if (bulletHolePool == null || bulletHolePool.Length != bulletHolePoolSize)
            {
                bulletHolePool = new GameObject[bulletHolePoolSize];
                bulletHoleIndex = 0;
            }

            GameObject hole = bulletHolePool[bulletHoleIndex];

            if (hole == null)
            {
                hole = Instantiate(bulletHolePrefab);
                bulletHolePool[bulletHoleIndex] = hole;
            }

            bulletHoleIndex = (bulletHoleIndex + 1) % bulletHolePool.Length;

            Transform holeTransform = hole.transform;
            holeTransform.SetParent(null);   // release it from the object it was stuck to before

            // The prefab's forward (blue) axis points INTO the surface; a Quad then faces out of it.
            // A random spin around the surface normal makes the holes look different.
            Vector3 normal = hit.normal;
            Quaternion rotation = Quaternion.LookRotation(-normal)
                                  * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            holeTransform.SetPositionAndRotation(
                hit.point + normal * bulletHoleSurfaceOffset,
                rotation
            );

            holeTransform.localScale = bulletHolePrefab.transform.localScale
                                       * Random.Range(bulletHoleSizeRange.x, bulletHoleSizeRange.y);

            if (bulletHolesFollowMovingObjects)
            {
                Transform surface = hit.collider.transform;

                bool moving = hit.rigidbody != null
                              || surface.GetComponentInParent<BodycamShootableTarget>() != null
                              || surface.GetComponentInParent<BodycamRespawningTarget>() != null;

                if (moving)
                    holeTransform.SetParent(surface, true);
            }

            hole.SetActive(true);
        }

        // ---------------- animators (the Weapon Animator and the optional Gun Animator) ----------------

        HashSet<string> gunParameterNames;

        // Remembers which parameters the gun's controller has, so missing ones never log warnings.
        void CacheGunAnimatorParameters()
        {
            // The same animator assigned to both fields would receive every command twice.
            if (gunAnimator == weaponAnimator)
                gunAnimator = null;

            if (gunAnimator == null || gunAnimator.runtimeAnimatorController == null)
                return;

            gunParameterNames = new HashSet<string>();

            foreach (AnimatorControllerParameter parameter in gunAnimator.parameters)
                gunParameterNames.Add(parameter.name);
        }

        // The Weapon Animator is used as before. The Gun Animator is only used for parameters its controller has.
        bool HasParameter(Animator animator, string parameterName)
        {
            if (animator != gunAnimator)
                return true;

            return gunParameterNames != null && gunParameterNames.Contains(parameterName);
        }

        void SetTriggerSafe(Animator animator, string triggerName)
        {
            if (animator != null && !string.IsNullOrEmpty(triggerName) && HasParameter(animator, triggerName))
                animator.SetTrigger(triggerName);
        }

        void ResetTriggerSafe(Animator animator, string triggerName)
        {
            if (animator != null && !string.IsNullOrEmpty(triggerName) && HasParameter(animator, triggerName))
                animator.ResetTrigger(triggerName);
        }

        void PlayIdleAnimation()
        {
            if (!playIdleOnEnable || string.IsNullOrEmpty(idleStateName))
                return;

            PlayIdleState(weaponAnimator);
            PlayIdleState(gunAnimator);
        }

        void PlayIdleState(Animator animator)
        {
            if (animator == null)
                return;

            int idleStateHash = Animator.StringToHash(idleStateName);
            int baseLayerIdleHash = Animator.StringToHash("Base Layer." + idleStateName);

            if (animator.HasState(0, idleStateHash))
                animator.Play(idleStateHash, 0, 0f);
            else if (animator.HasState(0, baseLayerIdleHash))
                animator.Play(baseLayerIdleHash, 0, 0f);
        }

        void PlayFireAnimation()
        {
            if (string.IsNullOrEmpty(fireTriggerName))
                return;

            ResetTriggerSafe(weaponAnimator, reloadTriggerName);
            ResetTriggerSafe(gunAnimator, reloadTriggerName);

            SetTriggerSafe(weaponAnimator, fireTriggerName);
            SetTriggerSafe(gunAnimator, fireTriggerName);
        }

        public void StartReload()
        {
            if (!useAmmoSystem || isReloading || currentAmmo >= magazineSize)
                return;

            if (reloadRoutine != null)
                StopCoroutine(reloadRoutine);

            reloadRoutine = StartCoroutine(ReloadRoutine());
        }

        IEnumerator ReloadRoutine()
        {
            isReloading = true;

            if (reloadType == ReloadType.ShellByShell)
                yield return ReloadShellsOneByOne();
            else
                yield return ReloadFullMagazine();

            isReloading = false;
            reloadRoutine = null;
        }

        IEnumerator ReloadFullMagazine()
        {
            PlayReloadAnimation();
            PlayReloadSound(fullReloadSound);
            PlayReloadCameraShake(reloadDuration, fullReloadShakeIntensity);

            yield return new WaitForSeconds(reloadDuration);

            currentAmmo = magazineSize;
        }

        IEnumerator ReloadShellsOneByOne()
        {
            while (currentAmmo < magazineSize)
            {
                PlayReloadAnimation();
                PlayReloadSound(shellInsertSound);
                PlayReloadCameraShake(shellReloadShakeDuration, shellReloadShakeIntensity);

                float timeUntilNextShell = Mathf.Max(
                    0.01f,
                    shellInsertDuration - shellAnimationOverlap
                );

                yield return new WaitForSeconds(timeUntilNextShell);

                currentAmmo = Mathf.Min(currentAmmo + 1, magazineSize);
            }
        }

        void PlayReloadSound(AudioClip reloadSound)
        {
            if (audioSource != null && reloadSound != null)
                audioSource.PlayOneShot(reloadSound);
        }

        void PlayReloadCameraShake(float duration, float intensity)
        {
            if (!enableReloadCameraShake || bodycamController == null)
                return;

            bodycamController.AddCameraShake(duration, intensity, reloadShakeFrequency);
        }

        void PlayReloadAnimation()
        {
            if (string.IsNullOrEmpty(reloadTriggerName))
                return;

            ResetTriggerSafe(weaponAnimator, fireTriggerName);
            ResetTriggerSafe(gunAnimator, fireTriggerName);

            ResetTriggerSafe(weaponAnimator, reloadTriggerName);
            ResetTriggerSafe(gunAnimator, reloadTriggerName);

            SetTriggerSafe(weaponAnimator, reloadTriggerName);
            SetTriggerSafe(gunAnimator, reloadTriggerName);
        }

        bool CanInterruptShellReload()
        {
            return allowShellReloadInterrupt &&
                   reloadType == ReloadType.ShellByShell &&
                   currentAmmo > 0;
        }

        public void CancelReload()
        {
            if (!isReloading)
                return;

            if (reloadRoutine != null)
                StopCoroutine(reloadRoutine);

            reloadRoutine = null;
            isReloading = false;

            if (enableReloadCameraShake && bodycamController != null)
                bodycamController.StopCameraShake();

            ResetTriggerSafe(weaponAnimator, reloadTriggerName);
            ResetTriggerSafe(gunAnimator, reloadTriggerName);
        }

        void FlashMuzzleLight()
        {
            if (muzzleLight == null)
                return;

            if (muzzleLightRoutine != null)
                StopCoroutine(muzzleLightRoutine);

            muzzleLightRoutine = StartCoroutine(MuzzleLightRoutine());
        }

        IEnumerator MuzzleLightRoutine()
        {
            muzzleLight.enabled = true;

            yield return new WaitForSeconds(muzzleLightDuration);

            muzzleLight.enabled = false;
            muzzleLightRoutine = null;
        }

        void OnDisable()
        {
            if (muzzleLight != null)
                muzzleLight.enabled = false;

            if (muzzleLightRoutine != null)
                StopCoroutine(muzzleLightRoutine);

            muzzleLightRoutine = null;
            CancelReload();

            ResetTriggerSafe(weaponAnimator, fireTriggerName);
            ResetTriggerSafe(weaponAnimator, reloadTriggerName);
            ResetTriggerSafe(gunAnimator, fireTriggerName);
            ResetTriggerSafe(gunAnimator, reloadTriggerName);
        }

        void OnEnable()
        {
            if (weaponAnimator != null || gunAnimator != null)
                PlayIdleAnimation();
        }

        void ApplyRecoil()
        {
            float strengthMultiplier = 1f;
            float randomPositionX = 0f;
            float randomPositionY = 0f;
            float randomRotationX = 0f;
            float randomRotationY = 0f;
            float randomRotationZ = 0f;

            if (enableRandomRecoil)
            {
                strengthMultiplier = Random.Range(
                    1f - recoilStrengthVariation,
                    1f + recoilStrengthVariation
                );

                randomPositionX = Random.Range(
                    -horizontalPositionVariation,
                    horizontalPositionVariation
                );

                randomPositionY = Random.Range(
                    -verticalPositionVariation,
                    verticalPositionVariation
                );

                randomRotationX = Random.Range(
                    -verticalRotationVariation,
                    verticalRotationVariation
                );

                randomRotationY = Random.Range(-recoilRotation.y, recoilRotation.y);
                randomRotationZ = Random.Range(-recoilRotation.z, recoilRotation.z);
            }

            targetRecoilPosition += recoilPosition * strengthMultiplier;
            targetRecoilPosition += new Vector3(randomPositionX, randomPositionY, 0f);

            targetRecoilRotation += new Vector3(
                recoilRotation.x * strengthMultiplier + randomRotationX,
                randomRotationY,
                randomRotationZ
            );

            if (enableCameraRecoil && playerCamera != null)
            {
                float horizontalCameraKick = enableRandomRecoil
                    ? Random.Range(-cameraHorizontalRecoil, cameraHorizontalRecoil)
                    : 0f;

                float cameraRollKick = enableRandomRecoil
                    ? Random.Range(-cameraRollRecoil, cameraRollRecoil)
                    : 0f;

                playerCamera.transform.localRotation *= Quaternion.Euler(
                    -cameraRecoilAmount * strengthMultiplier,
                    horizontalCameraKick,
                    cameraRollKick
                );
            }
        }

        void UpdateRecoil()
        {
            targetRecoilPosition = Vector3.Lerp(targetRecoilPosition, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
            targetRecoilRotation = Vector3.Lerp(targetRecoilRotation, Vector3.zero, Time.deltaTime * recoilReturnSpeed);

            currentRecoilPosition = Vector3.Lerp(currentRecoilPosition, targetRecoilPosition, Time.deltaTime * recoilSnappiness);
            currentRecoilRotation = Vector3.Lerp(currentRecoilRotation, targetRecoilRotation, Time.deltaTime * recoilSnappiness);

            weaponTransform.localPosition = originalWeaponPosition + currentRecoilPosition;
            weaponTransform.localRotation = originalWeaponRotation * Quaternion.Euler(currentRecoilRotation);
        }
    }
}