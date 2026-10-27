using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    /// <summary>
    /// One stack in the inventory. Only ids are stored (not asset references) so the entry can
    /// be serialized straight into <see cref="SaveData"/>; <see cref="ItemDatabase"/> resolves the id.
    /// <c>Parameter</c> carries a per-pickup potion strength, so potions with different strengths never stack.
    /// </summary>
    [Serializable]
    public class InventoryEntry
    {
        public int Id;
        public int Amount;
        public int Parameter;

        public InventoryEntry() { }

        public InventoryEntry(int id, int amount, int parameter)
        {
            Id = id;
            Amount = amount;
            Parameter = parameter;
        }
    }

    /// <summary>
    /// Owns the hero's items and equipped weapon/shield ids. Every mutation raises
    /// <see cref="Changed"/>; the inventory window and the HUD potion bottles subscribe to it
    /// instead of polling. Equipping spawns the definition's WeaponPrefab and hands it to
    /// <see cref="HeroController.EquipWeaponInstance"/>.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance;

        [Header("SETUP")]
        public ItemDatabase Database;
        [Tooltip("How many distinct stacks the hero can carry.")]
        public int Capacity = 20;

        [Header("RUNTIME (read only)")]
        public List<InventoryEntry> Entries = new List<InventoryEntry>();
        public int EquippedAttackId = 0;
        public int EquippedDefendId = 0;

        public event Action Changed;
        public InventoryUI InventoryUI;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (Database == null)
            {
                Debug.LogError("[ARPS] InventorySystem has no Item Database assigned. Drag Scripts/Inventory/Database/ItemDatabase.asset into the field.", this);
            }
            if (InventoryUI == null)
            {
                Debug.LogError("[ARPS] InventorySystem has no Inventory UI assigned, so the inventory key will do nothing. Drag the inventory window object into the field.", this);
                return;
            }

            InventoryUI.Prepare();
        }

        public void RaiseChanged()
        {
            Changed?.Invoke();
        }

        void Update()
        {
            if (InventoryUI == null || GameManager.Instance == null || GameManager.Instance.inventoryAction == null) return;

            if (GameManager.Instance.inventoryAction.WasReleasedThisFrame())
            {
                InventoryUI.Toggle();
            }
        }

        public int CountOf(int id)
        {
            int total = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Id == id) total += Entries[i].Amount;
            }
            return total;
        }

        public int CountOfPotion(PotionType potionType)
        {
            if (Database == null) return 0;
            ItemDefinition definition = Database.GetByPotionType(potionType);
            return definition == null ? 0 : CountOf(definition.Id);
        }

        public bool IsFull => Entries.Count >= Capacity;

        /// <summary>
        /// Adds <paramref name="amount"/> of item <paramref name="id"/>, filling existing stacks
        /// first. Returns false when the item is unknown, the definition is SingleStackOnly and
        /// a stack already exists, or the inventory runs out of capacity. When capacity runs
        /// out partway through, the stacks already placed stay in the inventory.
        /// A <paramref name="parameter"/> below 0 means "use the definition's PotionParameter".
        /// </summary>
        public bool Add(int id, int amount = 1, int parameter = -1)
        {
            if (Database == null || amount <= 0) return false;

            ItemDefinition definition = Database.Get(id);
            if (definition == null)
            {
                Debug.LogError("[ARPS] Tried to pick up item Id " + id + " but it is not in the Item Database.", this);
                return false;
            }

            if (parameter < 0) parameter = definition.PotionParameter;

            if (definition.Stackable)
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (Entries[i].Id == id && Entries[i].Parameter == parameter && Entries[i].Amount < definition.MaxStack)
                    {
                        int room = definition.MaxStack - Entries[i].Amount;
                        int moved = Mathf.Min(room, amount);
                        Entries[i].Amount += moved;
                        amount -= moved;
                        if (amount <= 0)
                        {
                            RaiseChanged();
                            return true;
                        }
                    }
                }
            }

            if (definition.SingleStackOnly && CountOf(id) > 0)
            {
                return false;
            }

            while (amount > 0)
            {
                if (IsFull)
                {
                    RaiseChanged();
                    return false;
                }
                int stack = definition.Stackable ? Mathf.Min(definition.MaxStack, amount) : 1;
                Entries.Add(new InventoryEntry(id, stack, parameter));
                amount -= stack;
            }

            RaiseChanged();
            return true;
        }

        public bool Remove(int id, int amount = 1)
        {
            if (CountOf(id) < amount) return false;

            for (int i = Entries.Count - 1; i >= 0 && amount > 0; i--)
            {
                if (Entries[i].Id != id) continue;

                int taken = Mathf.Min(Entries[i].Amount, amount);
                Entries[i].Amount -= taken;
                amount -= taken;
                if (Entries[i].Amount <= 0)
                {
                    Entries.RemoveAt(i);
                }
            }

            RaiseChanged();
            return true;
        }

        public void Clear()
        {
            Entries.Clear();
            EquippedAttackId = 0;
            EquippedDefendId = 0;
            RaiseChanged();
        }

        public bool IsEquipped(int id)
        {
            return id != 0 && (EquippedAttackId == id || EquippedDefendId == id);
        }

        public bool Equip(int id)
        {
            if (Database == null || HeroController.instance == null) return false;
            if (CountOf(id) <= 0) return false;

            ItemDefinition definition = Database.Get(id);
            if (definition == null || !definition.IsWeapon) return false;

            if (definition.WeaponPrefab == null)
            {
                Debug.LogError("[ARPS] Item '" + definition.DisplayName + "' has no Weapon Prefab assigned, so it cannot be equipped.", Database);
                return false;
            }

            bool isShield = definition.WeaponType == WeaponType.Shield;
            if (isShield) UnequipDefend(); else UnequipAttack();

            GameObject spawned = Instantiate(definition.WeaponPrefab);
            WeaponScript weapon = spawned.GetComponent<WeaponScript>();
            if (weapon == null)
            {
                Debug.LogError("[ARPS] Weapon prefab '" + definition.WeaponPrefab.name + "' has no WeaponScript component.", definition);
                Destroy(spawned);
                return false;
            }

            weapon.isTaken = true;
            if (weapon.colliderGrab != null) weapon.colliderGrab.enabled = false;

            HeroController.instance.EquipWeaponInstance(weapon);

            if (isShield) EquippedDefendId = id; else EquippedAttackId = id;

            RaiseChanged();
            return true;
        }

        public void UnequipAttack()
        {
            if (HeroController.instance != null) HeroController.instance.DestroyEquippedAttack();
            EquippedAttackId = 0;
            RaiseChanged();
        }

        public void UnequipDefend()
        {
            if (HeroController.instance != null) HeroController.instance.DestroyEquippedDefend();
            EquippedDefendId = 0;
            RaiseChanged();
        }

        public void ToggleEquip(int id)
        {
            if (Database == null) return;
            ItemDefinition definition = Database.Get(id);
            if (definition == null || !definition.IsWeapon) return;

            if (IsEquipped(id))
            {
                if (definition.WeaponType == WeaponType.Shield) UnequipDefend();
                else UnequipAttack();
            }
            else
            {
                Equip(id);
            }
        }

        public bool UsePotion(PotionType potionType)
        {
            if (Database == null || HeroController.instance == null) return false;

            ItemDefinition definition = Database.GetByPotionType(potionType);
            if (definition == null || CountOf(definition.Id) <= 0) return false;

            int amount = definition.PotionParameter;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Id == definition.Id && Entries[i].Amount > 0)
                {
                    if (Entries[i].Parameter > 0) amount = Entries[i].Parameter;
                    break;
                }
            }

            if (!HeroController.instance.ApplyPotionEffect(definition, amount)) return false;

            Remove(definition.Id, 1);
            if (AudioManager.instance != null) AudioManager.instance.Play_Potion_Dring();
            return true;
        }

        public bool UseItem(int id)
        {
            if (Database == null) return false;
            ItemDefinition definition = Database.Get(id);
            if (definition == null) return false;

            if (definition.IsPotion) return UsePotion(definition.PotionType);

            ToggleEquip(id);
            return true;
        }

        public List<InventoryEntry> CaptureEntries()
        {
            var copy = new List<InventoryEntry>(Entries.Count);
            for (int i = 0; i < Entries.Count; i++)
            {
                copy.Add(new InventoryEntry(Entries[i].Id, Entries[i].Amount, Entries[i].Parameter));
            }
            return copy;
        }

        public void RestoreEntries(List<InventoryEntry> entries, int equippedAttackId, int equippedDefendId)
        {
            HeroController.instance?.DestroyEquippedAttack();
            HeroController.instance?.DestroyEquippedDefend();

            Entries.Clear();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] == null || entries[i].Amount <= 0) continue;
                    Entries.Add(new InventoryEntry(entries[i].Id, entries[i].Amount, entries[i].Parameter));
                }
            }

            EquippedAttackId = 0;
            EquippedDefendId = 0;
            if (equippedAttackId != 0) Equip(equippedAttackId);
            if (equippedDefendId != 0) Equip(equippedDefendId);

            RaiseChanged();
        }
    }
}
