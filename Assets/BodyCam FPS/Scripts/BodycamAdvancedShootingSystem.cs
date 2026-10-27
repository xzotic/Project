using System.Collections;
using UnityEngine;

namespace RealisticBodycam
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
                Physics.Raycast(shotRay, out _, range, hitMask);
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

        void PlayIdleAnimation()
        {
            if (!playIdleOnEnable || weaponAnimator == null || string.IsNullOrEmpty(idleStateName))
                return;

            int idleStateHash = Animator.StringToHash(idleStateName);
            int baseLayerIdleHash = Animator.StringToHash("Base Layer." + idleStateName);

            if (weaponAnimator.HasState(0, idleStateHash))
                weaponAnimator.Play(idleStateHash, 0, 0f);
            else if (weaponAnimator.HasState(0, baseLayerIdleHash))
                weaponAnimator.Play(baseLayerIdleHash, 0, 0f);
        }

        void PlayFireAnimation()
        {
            if (weaponAnimator == null || string.IsNullOrEmpty(fireTriggerName))
                return;

            if (!string.IsNullOrEmpty(reloadTriggerName))
                weaponAnimator.ResetTrigger(reloadTriggerName);

            weaponAnimator.SetTrigger(fireTriggerName);
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
            if (weaponAnimator == null || string.IsNullOrEmpty(reloadTriggerName))
                return;

            if (!string.IsNullOrEmpty(fireTriggerName))
                weaponAnimator.ResetTrigger(fireTriggerName);

            weaponAnimator.ResetTrigger(reloadTriggerName);
            weaponAnimator.SetTrigger(reloadTriggerName);
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

            if (weaponAnimator != null && !string.IsNullOrEmpty(reloadTriggerName))
                weaponAnimator.ResetTrigger(reloadTriggerName);
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

            if (weaponAnimator != null)
            {
                if (!string.IsNullOrEmpty(fireTriggerName))
                    weaponAnimator.ResetTrigger(fireTriggerName);

                if (!string.IsNullOrEmpty(reloadTriggerName))
                    weaponAnimator.ResetTrigger(reloadTriggerName);
            }
        }

        void OnEnable()
        {
            if (weaponAnimator != null)
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
