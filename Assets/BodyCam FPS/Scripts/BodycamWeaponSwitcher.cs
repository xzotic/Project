using System.Collections;
using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Weapon Switcher")]
    [DisallowMultipleComponent]
    public class BodycamWeaponSwitcher : MonoBehaviour
    {
        [Header("WEAPONS")]
        [Tooltip("Add each complete weapon GameObject in the desired selection order.")]
        public GameObject[] weapons;

        [Min(0)]
        public int startingWeaponIndex = 0;

        [Header("INPUT")]
        public bool useMouseWheel = true;
        public bool useNumberKeys = true;

        [Header("SWITCH ANIMATION")]
        [Tooltip("Local offset used to move the weapon out of the camera view.")]
        public Vector3 hiddenOffset = new Vector3(0f, -1f, 0f);

        [Min(0.01f)] public float lowerDuration = 0.18f;
        [Min(0.01f)] public float raiseDuration = 0.22f;

        int currentWeaponIndex;
        bool isSwitching;
        Vector3[] weaponStartPositions;

        static readonly KeyCode[] NumberKeys =
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9
        };

        public int CurrentWeaponIndex => currentWeaponIndex;
        public bool IsSwitching => isSwitching;

        void Start()
        {
            if (weapons == null || weapons.Length == 0)
                return;

            weaponStartPositions = new Vector3[weapons.Length];

            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == null)
                    continue;

                weaponStartPositions[i] = weapons[i].transform.localPosition;
                weapons[i].SetActive(false);
            }

            currentWeaponIndex = Mathf.Clamp(startingWeaponIndex, 0, weapons.Length - 1);

            if (weapons[currentWeaponIndex] != null)
            {
                weapons[currentWeaponIndex].transform.localPosition = weaponStartPositions[currentWeaponIndex];
                weapons[currentWeaponIndex].SetActive(true);
            }
        }

        void Update()
        {
            if (weapons == null || weapons.Length == 0 || isSwitching)
                return;

            if (useMouseWheel)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");

                if (scroll > 0f)
                    SelectNextWeapon();
                else if (scroll < 0f)
                    SelectPreviousWeapon();
            }

            if (useNumberKeys)
                CheckNumberKeys();
        }

        void CheckNumberKeys()
        {
            int availableKeys = Mathf.Min(weapons.Length, NumberKeys.Length);

            for (int i = 0; i < availableKeys; i++)
            {
                if (Input.GetKeyDown(NumberKeys[i]))
                {
                    SelectWeapon(i);
                    return;
                }
            }
        }

        public void SelectNextWeapon()
        {
            if (weapons == null || weapons.Length == 0)
                return;

            SelectWeapon((currentWeaponIndex + 1) % weapons.Length);
        }

        public void SelectPreviousWeapon()
        {
            if (weapons == null || weapons.Length == 0)
                return;

            int previousIndex = currentWeaponIndex - 1;

            if (previousIndex < 0)
                previousIndex = weapons.Length - 1;

            SelectWeapon(previousIndex);
        }

        public void SelectWeapon(int weaponIndex)
        {
            if (weapons == null || weapons.Length == 0)
                return;

            if (weaponIndex < 0 || weaponIndex >= weapons.Length)
                return;

            if (isSwitching || weaponIndex == currentWeaponIndex || weapons[weaponIndex] == null)
                return;

            StartCoroutine(SwitchWeaponRoutine(weaponIndex));
        }

        IEnumerator SwitchWeaponRoutine(int newWeaponIndex)
        {
            isSwitching = true;

            GameObject currentWeapon = weapons[currentWeaponIndex];

            if (currentWeapon != null)
            {
                Vector3 visiblePosition = weaponStartPositions[currentWeaponIndex];
                Vector3 hiddenPosition = visiblePosition + hiddenOffset;

                yield return MoveWeapon(
                    currentWeapon.transform,
                    currentWeapon.transform.localPosition,
                    hiddenPosition,
                    lowerDuration
                );

                currentWeapon.SetActive(false);
                currentWeapon.transform.localPosition = visiblePosition;
            }

            currentWeaponIndex = newWeaponIndex;

            GameObject newWeapon = weapons[currentWeaponIndex];
            Vector3 newVisiblePosition = weaponStartPositions[currentWeaponIndex];
            Vector3 newHiddenPosition = newVisiblePosition + hiddenOffset;

            newWeapon.transform.localPosition = newHiddenPosition;
            newWeapon.SetActive(true);

            yield return MoveWeapon(
                newWeapon.transform,
                newHiddenPosition,
                newVisiblePosition,
                raiseDuration
            );

            newWeapon.transform.localPosition = newVisiblePosition;
            isSwitching = false;
        }

        IEnumerator MoveWeapon(Transform weapon, Vector3 start, Vector3 end, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float smoothTime = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);

                weapon.localPosition = Vector3.LerpUnclamped(start, end, smoothTime);
                yield return null;
            }

            weapon.localPosition = end;
        }
    }
}
