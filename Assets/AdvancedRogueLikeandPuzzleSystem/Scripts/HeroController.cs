using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using UnityEngine.UI;

namespace AdvancedRogueLikeandPuzzleSystem
{
    /// <summary>
    /// The player character's gameplay hub: stats (health, mana, coin), melee combo and shield
    /// defence, weapon socketing (via <see cref="BodyPart"/>), potion effects, and interaction.
    /// Interaction is tag-driven: OnTriggerStay classifies nearby colliders tagged Door, Weapon or
    /// Item (then narrowed by component: PotionScript, KeyScript, ChestScript, ElevatorScript,
    /// MirrorGearScript) into <see cref="InteractedItem"/>, and the interact input acts on it.
    /// Movement itself lives in <see cref="ThirdPersonController"/>.
    /// </summary>
    public class HeroController : MonoBehaviour
    {
        public SphereCollider sphereCollider;

        [Header("PROFILE DETAILS")]
        public string Name;
        public Sprite ProfileImage;
        public int Coin;
        public int Level;
        public int Mana = 100;
        public float TotalMana = 100;
        [Tooltip("Mana regenerated every 1.5 seconds.")]
        public int ManaRegenPerTick = 5;

        [Header("BODY DETAILS")]
        public int Health;
        public float TotalHealth;
        public float Speed;
        public float SprintSpeed;
        public int HitPower;
        public GameObject Sprite_Interaction;

        public int MaxHealthPot = 4;
        public int CurrentHealthPot = 0;
        public int MaxManaPot = 4;
        public int CurrentManaPot = 0;

        public GameObject InteractedItem;
        public ItemType InteractedItemType;
        public Animator Animator;

        public Transform playerCameraRoot;
        public CharacterController characterController;
        public ThirdPersonController thirdPersonController;
        private Collider cold;
        public Transform characterHead;

        public static HeroController instance;

        public GameObject WeaponAttack;
        public GameObject WeaponDefend;

        private bool WeaponsonHands = false;
        private bool ShieldonHands = false;
        public BodyPart bodyPart;
        public GameObject bodyVisual;
        [HideInInspector]
        public bool inDefendMode = false;
        private int Hitindex = 0;
        public float HittingPeriod = 1.5f;
        [HideInInspector]
        public List<int> Keys_Grabbed = new List<int>();
        [HideInInspector]
        public List<PotionType> Potions_Grabbed = new List<PotionType>();
        [HideInInspector]
        public List<int> Potions_Parameters = new List<int>();
        [HideInInspector]
        [FormerlySerializedAs("audio_AyakSesi")]
        public AudioSource audio_Footstep;

        private void Awake()
        {
            instance = this;
            TotalHealth = Health;
            InitPlayer();
        }

        public void InitPlayer()
        {
            Animator = GetComponent<Animator>();
            Level = PlayerPrefs.GetInt("Level", 1);
            Coin = PlayerPrefs.GetInt("Coin", 0);
            cold = GetComponent<Collider>();
        }

        void Start()
        {
            TotalMana = Mana;
            RefreshProperties();
            characterController = GetComponent<CharacterController>();
            thirdPersonController = GetComponent<ThirdPersonController>();
            thirdPersonController.MoveSpeed = Speed;
            thirdPersonController.SprintSpeed = SprintSpeed;
            GameCanvas_Controller.instance.Update_ProfileDetails(Name, ProfileImage);
        }

        public void Grab_Key(int ID)
        {
            Keys_Grabbed.Add(ID);
        }

        public bool AddPotion(PotionType potionType, int parameter)
        {
            if (InventorySystem.Instance == null || InventorySystem.Instance.Database == null) return false;

            ItemDefinition definition = InventorySystem.Instance.Database.GetByPotionType(potionType);
            if (definition == null)
            {
                Debug.LogError("[ARPS] No " + potionType + " potion in the Item Database.", this);
                return false;
            }
            return InventorySystem.Instance.Add(definition.Id, 1, parameter);
        }

        private Coroutine speedBoostRoutine;

        public int HealthPotionCount => InventorySystem.Instance != null ? InventorySystem.Instance.CountOfPotion(PotionType.Health) : 0;
        public int ManaPotionCount => InventorySystem.Instance != null ? InventorySystem.Instance.CountOfPotion(PotionType.Mana) : 0;

        public void RefreshProperties()
        {
            TotalHealth = Health;
            GameCanvas_Controller.instance.Update_Health_Bar();
        }

        public void ShowInteractSprite()
        {
            if (GameManager.Instance.controllerType == ControllerType.KeyboardMouse)
            {
                Sprite_Interaction.SetActive(true);
            }
            else
            {
                GameCanvas_Controller.instance.Show_Button_Interact();
            }
        }

        public void HideInteractSprite()
        {
            if (GameManager.Instance.controllerType == ControllerType.KeyboardMouse)
            {
                Sprite_Interaction.SetActive(false);
            }
            else
            {
                GameCanvas_Controller.instance.Hide_Button_Interact();
            }
        }

        public void Hit()
        {
            if (!thirdPersonController.enabled) return;

            if ((WeaponsonHands && WeaponAttack != null) && inDefendMode == false && !isHitting)
            {
                Hitindex++;
                Animator.SetTrigger("Hit" + Hitindex.ToString());
                switch (Hitindex)
                {
                    case 1:
                        Animator.ResetTrigger("Hit2");
                        Animator.ResetTrigger("Hit3");
                        break;
                    case 2:
                        Animator.ResetTrigger("Hit1");
                        Animator.ResetTrigger("Hit3");
                        break;
                    case 3:
                        Animator.ResetTrigger("Hit1");
                        Animator.ResetTrigger("Hit2");
                        break;
                }
                isHitting = true;
                AudioManager.instance.Play_WeaponWhoosh();
                if (cutTheSpeedRoutine != null)
                {
                    StopCoroutine(cutTheSpeedRoutine);
                }
                cutTheSpeedRoutine = StartCoroutine(CutTheSpeed());
                if (Hitindex >= 3)
                {
                    Hitindex = 0;
                }
            }
        }

        int power = 0;

        public void Hit1Effect()
        {
            HittingParticle_1.Play();
            WeaponEffectScript.Instance.Attack();
        }

        public void Hit2Effect()
        {
            HittingParticle_2.Play();
            WeaponEffectScript.Instance.Attack();
        }

        public void Hit3Effect()
        {
            HittingParticle_3.Play();
            WeaponEffectScript.Instance.Attack();
        }

        public ParticleSystem HittingParticle_1;
        public ParticleSystem HittingParticle_2;
        public ParticleSystem HittingParticle_3;

        public void Defend()
        {
            if (!thirdPersonController.enabled) return;
            if (ShieldonHands && WeaponDefend != null && Animator.GetBool("Defend") == false)
            {
                Animator.SetBool("Defend", true);
                thirdPersonController.MoveSpeed = 0;
                thirdPersonController.SprintSpeed = 0;
                inDefendMode = true;
            }
            else if (ShieldonHands && WeaponDefend != null && Animator.GetBool("Defend"))
            {
                Animator.SetBool("Defend", false);
                thirdPersonController.MoveSpeed = Speed;
                thirdPersonController.SprintSpeed = SprintSpeed;
                inDefendMode = false;
            }
        }

        public void CheckInteractionInputsForMobile()
        {
            if (InteractedItem != null)
            {
                if (InteractedItemType == ItemType.Key)
                {
                    HideInteractSprite();
                    InteractedItem.GetComponent<KeyScript>().GrabIt();
                }
                else if (InteractedItemType == ItemType.Potion)
                {
                    HideInteractSprite();
                    InteractedItem.GetComponent<PotionScript>().GrabIt();
                }
                else if (InteractedItemType == ItemType.Door)
                {
                    InteractedItem.GetComponent<DoorScript>().TryToOpen();
                }
                else if (InteractedItemType == ItemType.Chest)
                {
                    HideInteractSprite();
                    InteractedItem.GetComponent<ChestScript>().Open();
                }
                else if (InteractedItemType == ItemType.Elevator)
                {
                    HideInteractSprite();
                    InteractedItem.GetComponent<ElevatorScript>().Activate();
                }
                else if (InteractedItemType == ItemType.Mirror)
                {
                    InteractedItem.GetComponent<MirrorGearScript>().Activate();
                }
                if (InteractedItemType == ItemType.Weapon)
                {
                    HideInteractSprite();
                    TaketheWeapon();
                }
            }
        }

        public void CheckInteractionInputs()
        {
            var interacted = (GameManager.Instance.interactAction.WasReleasedThisFrame());
            if (interacted && InteractedItem != null)
            {
                if (InteractedItemType == ItemType.Key)
                {
                    HideInteractSprite();
                    InteractedItem.GetComponent<KeyScript>().GrabIt();
                }
                else if (InteractedItemType == ItemType.Potion)
                {
                    HideInteractSprite();
                    InteractedItem.GetComponent<PotionScript>().GrabIt();
                }
                else if (InteractedItemType == ItemType.Door)
                {
                    if (InteractedItem != null && InteractedItem.GetComponent<DoorScript>() != null)
                    {
                        InteractedItem.GetComponent<DoorScript>().TryToOpen();
                    }
                }
                else if (InteractedItemType == ItemType.Chest)
                {
                    HideInteractSprite();
                    InteractedItem.GetComponent<ChestScript>().Open();
                }
                if (InteractedItemType == ItemType.Weapon)
                {
                    TaketheWeapon();
                }
            }
        }

        public void CheckWeaponInputs()
        {
            if (GameManager.Instance.mouseRightAction.IsPressed() && ShieldonHands && WeaponDefend != null)
            {
                Animator.SetBool("Defend", true);
                GameManager.Instance.ZoomFovIfFPSCameraActive();
                thirdPersonController.MoveSpeed = 0;
                thirdPersonController.SprintSpeed = 0;
                inDefendMode = true;
            }
            else if (GameManager.Instance.mouseRightAction.WasReleasedThisFrame() && ShieldonHands && WeaponDefend != null)
            {
                Animator.SetBool("Defend", false);
                GameManager.Instance.ZoomOutFovIfFPSCameraActive();
                thirdPersonController.MoveSpeed = Speed;
                thirdPersonController.SprintSpeed = SprintSpeed;
                inDefendMode = false;
            }
            if (GameManager.Instance.mouseAction.WasReleasedThisFrame() && (WeaponsonHands && WeaponAttack != null) && inDefendMode == false && !isHitting)
            {
                power = HeroController.instance.HitPower;
                Hit();
            }

        }

        public void CheckPotionInputsForMobile(int i)
        {
            if (InventorySystem.Instance == null) return;

            if (i == 1) InventorySystem.Instance.UsePotion(PotionType.Health);
            else if (i == 2) InventorySystem.Instance.UsePotion(PotionType.Mana);
        }

        public void CheckPotionInputs()
        {
            if (InventorySystem.Instance == null || GameManager.Instance == null) return;

            if (GameManager.Instance.usingHealPotionAction != null && GameManager.Instance.usingHealPotionAction.WasReleasedThisFrame())
            {
                InventorySystem.Instance.UsePotion(PotionType.Health);
            }
            if (GameManager.Instance.usingManaPotionAction != null && GameManager.Instance.usingManaPotionAction.WasReleasedThisFrame())
            {
                InventorySystem.Instance.UsePotion(PotionType.Mana);
            }
        }

        private void Update()
        {
            if (Health <= 0)
            {
                return;
            }

            ManaUpdate();

            if (gettingHit) return;

            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) return;

            if (GameManager.Instance != null && GameManager.Instance.controllerType == ControllerType.KeyboardMouse)
            {
                CheckInteractionInputs();
                CheckWeaponInputs();
                CheckPotionInputs();
            }
        }
        private float LastManaUpdateTime = 0;

        public void ManaUpdate()
        {
            if (Time.time >= LastManaUpdateTime + 1.5f)
            {
                LastManaUpdateTime = Time.time;
                if (Mana < TotalMana)
                {
                    Mana += ManaRegenPerTick;
                    if (Mana > TotalMana)
                    {
                        Mana = (int)TotalMana;
                    }
                    if (GameCanvas_Controller.instance != null)
                    {
                        GameCanvas_Controller.instance.Update_Mana_Bar(0);
                    }
                }
            }
        }

        [HideInInspector]
        public bool isHitting = false;
        private Coroutine cutTheSpeedRoutine;
        private IEnumerator CutTheSpeed()
        {
            thirdPersonController.MoveSpeed = 0;
            thirdPersonController.SprintSpeed = 0;
            yield return new WaitForSeconds((Hitindex == 3 ? HittingPeriod * 2 : HittingPeriod));
            isHitting = false;
            thirdPersonController.MoveSpeed = Speed;
            thirdPersonController.SprintSpeed = SprintSpeed;
            cutTheSpeedRoutine = null;
        }

        IEnumerator PutOnWeapons()
        {
            yield return new WaitForSeconds(0.35f);
            WeaponAttack.transform.parent = bodyPart.WeaponAttackPutOnParent;
            WeaponAttack.transform.localPosition = bodyPart.WeaponAttackPutOnTransform.localPosition;
            WeaponAttack.transform.localRotation = bodyPart.WeaponAttackPutOnTransform.localRotation;
        }

        IEnumerator PutOffWeapons()
        {
            yield return new WaitForSeconds(0.6f);
            WeaponAttack.transform.parent = bodyPart.WeaponAttackPutOffParent;
            WeaponAttack.transform.localPosition = bodyPart.WeaponAttackPutOffTransform.localPosition;
            WeaponAttack.transform.localRotation = bodyPart.WeaponAttackPutOffTransform.localRotation;
        }

        IEnumerator PutOnShield()
        {
            yield return new WaitForSeconds(0.35f);
            WeaponDefend.transform.parent = bodyPart.WeaponDefendPutOnParent;
            WeaponDefend.transform.localPosition = bodyPart.WeaponDefendPutOnTransform.localPosition;
            WeaponDefend.transform.localRotation = bodyPart.WeaponDefendPutOnTransform.localRotation;
        }

        IEnumerator PutOffShield()
        {
            yield return new WaitForSeconds(0.6f);
            inDefendMode = false;
            WeaponDefend.transform.parent = bodyPart.WeaponDefendPutOffParent;
            WeaponDefend.transform.localPosition = bodyPart.WeaponDefendPutOffTransform.localPosition;
            WeaponDefend.transform.localRotation = bodyPart.WeaponDefendPutOffTransform.localRotation;
        }

        public void GetActionOnWeapon()
        {
            if (thirdPersonController.isSwimming) return;

            if (WeaponAttack != null)
            {
                if (!WeaponsonHands)
                {
                    StartCoroutine(PutOnWeapons());
                    GameCanvas_Controller.instance.Show_Button_Hit();
                    Animator.SetTrigger("DrawIn");
                }
                if (WeaponAttack != null)
                {
                    if (!WeaponsonHands)
                    {
                        AudioManager.instance.Play_PutOnWeapon();
                        WeaponsonHands = true;
                    }
                }
            }
            if (WeaponDefend != null)
            {
                if(!ShieldonHands)
                {
                    StartCoroutine(PutOnShield());
                    GameCanvas_Controller.instance.Show_Button_Shield();
                }
                if (WeaponDefend != null)
                {
                    if (!ShieldonHands)
                    {
                        AudioManager.instance.Play_PutOnWeapon();
                        ShieldonHands = true;
                    }
                }
            }
            
        }

        public void Lock(bool decision)
        {
            characterController.enabled = decision;
            thirdPersonController.enabled = decision;
        }

        public void TaketheWeapon()
        {
            if (InteractedItem == null) return;

            WeaponScript newWeapon = InteractedItem.GetComponent<WeaponScript>();
            if (newWeapon == null)
            {
                Debug.LogWarning("[ARPS] '" + InteractedItem.name + "' is tagged Weapon but has no WeaponScript component.", InteractedItem);
                InteractedItem = null;
                return;
            }
            if (newWeapon.isTaken) { InteractedItem = null; return; }

            if (InventorySystem.Instance == null)
            {
                Debug.LogError("[ARPS] No InventorySystem in the scene, so weapons cannot be picked up.  Add the InventorySystem component to your GameManager object and assign the Item Database.", this);
                return;
            }

            if (!InventorySystem.Instance.Add(newWeapon.ID))
            {
                if (GameCanvas_Controller.instance != null) GameCanvas_Controller.instance.Show_Warning("INVENTORY FULL!");
                return;
            }

            if (GameCanvas_Controller.instance != null) GameCanvas_Controller.instance.Show_Grabbed_Text(newWeapon.Name);
            if (AudioManager.instance != null) AudioManager.instance.Play_Grab();

            bool isShield = newWeapon.weaponType == WeaponType.Shield;
            bool handIsFree = isShield ? WeaponDefend == null : WeaponAttack == null;

            HideInteractSprite();
            GameObject worldCopy = InteractedItem;
            InteractedItem = null;
            InteractedItemType = ItemType.None;
            GameSession.MarkConsumed(worldCopy);
            Destroy(worldCopy);

            if (handIsFree)
            {
                InventorySystem.Instance.Equip(newWeapon.ID);
            }
        }

        public void EquipWeaponInstance(WeaponScript weapon)
        {
            if (weapon == null || bodyPart == null) return;

            Transform t = weapon.transform;
            if (weapon.weaponType == WeaponType.Shield)
            {
                DestroyEquippedDefend();
                t.parent = bodyPart.WeaponDefendPutOnParent;
                t.localPosition = bodyPart.WeaponDefendPutOnTransform.localPosition;
                t.localRotation = bodyPart.WeaponDefendPutOnTransform.localRotation;
                t.localScale = bodyPart.WeaponDefendPutOnTransform.localScale;
                WeaponDefend = weapon.gameObject;
                PlayerPrefs.SetInt("WeaponDefend", weapon.ID);
            }
            else
            {
                DestroyEquippedAttack();
                t.parent = bodyPart.WeaponAttackPutOnParent;
                t.localPosition = bodyPart.WeaponAttackPutOnTransform.localPosition;
                t.localRotation = bodyPart.WeaponAttackPutOnTransform.localRotation;
                WeaponAttack = weapon.gameObject;
                PlayerPrefs.SetInt("WeaponAttack", weapon.ID);
            }

            GetActionOnWeapon();
        }

        public void DestroyEquippedAttack()
        {
            if (WeaponAttack != null) Destroy(WeaponAttack);
            WeaponAttack = null;
            WeaponsonHands = false;
            isHitting = false;
            if (GameCanvas_Controller.instance != null) GameCanvas_Controller.instance.Hide_Button_Hit();
        }

        public void DestroyEquippedDefend()
        {
            if (WeaponDefend != null) Destroy(WeaponDefend);
            WeaponDefend = null;
            ShieldonHands = false;
            inDefendMode = false;
            if (Animator != null) Animator.SetBool("Defend", false);
            if (GameCanvas_Controller.instance != null) GameCanvas_Controller.instance.Hide_Button_Shield();
        }

        public bool ApplyPotionEffect(ItemDefinition definition, int amount)
        {
            if (definition == null || !definition.IsPotion) return false;
            if (amount <= 0) amount = definition.PotionParameter;

            switch (definition.PotionType)
            {
                case PotionType.Health:
                    if (Health >= TotalHealth) return false;
                    AddHealth(amount);
                    return true;

                case PotionType.Mana:
                    if (Mana >= TotalMana) return false;
                    AddMana(amount);
                    return true;

                case PotionType.Speed:
                    if (speedBoostRoutine != null) return false;
                    speedBoostRoutine = StartCoroutine(AddSpeed(amount));
                    return true;
            }
            return false;
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Door"))
            {
                InteractedItemType = ItemType.Door;
                InteractedItem = other.gameObject;
                ShowInteractSprite();
            }
            else if (other.CompareTag("Weapon") && other.TryGetComponent(out WeaponScript weaponInRange) && !weaponInRange.isTaken)
            {
                InteractedItemType = ItemType.Weapon;
                InteractedItem = other.gameObject;
                ShowInteractSprite();
            }
            else if (other.CompareTag("Item"))
            {
                if (other.GetComponent<PotionScript>() != null)
                {
                    InteractedItemType = ItemType.Potion;
                }
                else if (other.GetComponent<KeyScript>() != null)
                {
                    InteractedItemType = ItemType.Key;
                }
                if (other.GetComponent<ChestScript>() != null)
                {
                    InteractedItemType = ItemType.Chest;
                }
                if (other.GetComponent<ElevatorScript>() != null)
                {
                    InteractedItemType = ItemType.Elevator;
                }
                if (other.GetComponent<MirrorGearScript>() != null)
                {
                    InteractedItemType = ItemType.Mirror;
                }
                InteractedItem = other.gameObject;
                ShowInteractSprite();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Item"))
            {
                if (InteractedItemType == ItemType.Key || InteractedItemType == ItemType.Potion || InteractedItemType == ItemType.Chest || InteractedItemType == ItemType.Elevator || InteractedItemType == ItemType.Mirror)
                {
                    InteractedItem = null;
                    HideInteractSprite();
                    InteractedItemType = ItemType.None;
                }
            }
            else if (other.CompareTag("Door"))
            {
                if (InteractedItemType == ItemType.Door)
                {
                    InteractedItem = null;
                    HideInteractSprite();
                    InteractedItemType = ItemType.None;
                }
            }
            else if (other.CompareTag("Weapon"))
            {
                if (InteractedItemType == ItemType.Weapon)
                {
                    InteractedItem = null;
                    HideInteractSprite();
                    InteractedItemType = ItemType.None;
                }
            }
        }

        public void ResetCollider()
        {
            cold.enabled = false;
            cold.enabled = true;
        }

        private bool gettingHit = false;
        IEnumerator ResetGetHit()
        {
            gettingHit = true;
            thirdPersonController.MoveSpeed = 0;
            thirdPersonController.SprintSpeed = 0;
            yield return new WaitForSeconds(0.25f);
            thirdPersonController.MoveSpeed = Speed;
            thirdPersonController.SprintSpeed = SprintSpeed;
            gettingHit = false;
        }

        public void AddHealth(int adding)
        {
            Health = Health + adding;
            if (Health > TotalHealth)
            {
                Health = (int)TotalHealth;
            }
            GameCanvas_Controller.instance.Update_Health_Bar();
        }

        public void AddMana(int adding)
        {
            Mana = Mana + adding;
            if (Mana > TotalMana)
            {
                Mana = (int)TotalMana;
            }
            GameCanvas_Controller.instance.Update_Mana_Bar(0);
        }

        [Tooltip("How long the Speed potion buff lasts, in seconds.")]
        public float SpeedPotionDuration = 15f;

        IEnumerator AddSpeed(int extraSpeed)
        {
            Speed = Speed + extraSpeed;
            if (thirdPersonController != null)
            {
                thirdPersonController.MoveSpeed = thirdPersonController.MoveSpeed + extraSpeed;
                thirdPersonController.SprintSpeed = thirdPersonController.SprintSpeed + extraSpeed;
            }
            yield return new WaitForSeconds(SpeedPotionDuration);
            Speed = Speed - extraSpeed;
            if (thirdPersonController != null)
            {
                thirdPersonController.MoveSpeed = thirdPersonController.MoveSpeed - extraSpeed;
                thirdPersonController.SprintSpeed = thirdPersonController.SprintSpeed - extraSpeed;
            }
            speedBoostRoutine = null;
        }

        public void GetDamagePlayer(int damage)
        {
            if (gettingHit) return;
            if (Health <= 0)
            {
                return;
            }
            if (inDefendMode)
            {
                Animator.SetTrigger("GetImpact");
                AudioManager.instance.Play_ShieldImpact();
                return;
            }
            Health = Health - damage;
            if (Health <= 0)
            {
                Health = 0;
            }
            Animator.SetInteger("Health", Health);
            if (!isHitting)
            {
                StartCoroutine(ResetGetHit());
            }
            if (Health == 0)
            {
                AudioManager.instance.Play_Die();
                GetComponent<CharacterController>().enabled = false;
                GetComponent<ThirdPersonController>().enabled = false;
                Animator.SetTrigger("Die");
                StartCoroutine(GameCanvas_Controller.instance.Show_GameOver_Panel());
            }
            GameCanvas_Controller.instance.Update_Health_Bar();
            GameCanvas_Controller.instance.Show_Damage_Effect();
            GameManager.Instance.ShakeTheCamera(0.3f);
        }

        public void UpdateCoin(int amount)
        {
            Coin += amount;
            PlayerPrefs.SetInt("Coin", Coin);
            AudioManager.instance.Play_Coin();
            GameCanvas_Controller.instance.Update_Text_Coin();
        }
    }

    public enum ItemType
    {
        Weapon,
        Key,
        Door,
        Potion,
        None,
        Chest,
        Elevator,
        Button,
        Mirror
    }
}