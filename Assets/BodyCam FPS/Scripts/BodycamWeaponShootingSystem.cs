using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Weapon Shooting System")]
    public class BodycamWeaponShootingSystem : MonoBehaviour
    {
        [Header("REFERENCES")]
        public Transform muzzlePoint;
        public GameObject bulletPrefab;
        public ParticleSystem muzzleFlash;
        public AudioSource audioSource;
        public AudioClip shootSound;

        [Header("SHOOTING")]
        public float fireRate = 0.15f;
        public float bulletSpeed = 80f;
        public float bulletLifeTime = 5f;

        float nextFireTime;

        void Update()
        {
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }

        void Shoot()
        {
            if (bulletPrefab == null || muzzlePoint == null)
                return;

            GameObject bullet = Instantiate(
                bulletPrefab,
                muzzlePoint.position,
                muzzlePoint.rotation
            );

            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = muzzlePoint.forward * bulletSpeed;
            }

            Destroy(bullet, bulletLifeTime);

            if (muzzleFlash != null)
                muzzleFlash.Play();

            if (audioSource != null && shootSound != null)
                audioSource.PlayOneShot(shootSound);
        }
    }
}
