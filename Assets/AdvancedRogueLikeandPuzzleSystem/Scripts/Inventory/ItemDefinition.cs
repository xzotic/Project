using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public enum ItemKind
    {
        Weapon,
        Potion
    }

    /// <summary>
    /// Data asset describing one item type: identity, icon, stacking rules and weapon or potion
    /// data. A weapon's <see cref="Id"/> must equal the <c>ID</c> on its prefab's
    /// <see cref="WeaponScript"/>, because world pickups report that id to the inventory.
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "Advanced Roguelike/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("IDENTITY")]
        [Tooltip("Unique across the whole database. Weapons reuse the ID on their WeaponScript.")]
        public int Id;
        public string DisplayName = "New Item";
        [Tooltip("Shown in the inventory grid. Optional - slots fall back to the name.")]
        public Sprite Icon;
        public ItemKind Kind = ItemKind.Weapon;

        [Header("STACKING")]
        public bool Stackable = false;
        [Min(1)]
        public int MaxStack = 1;
        [Tooltip("When on, the hero can never hold more than one stack of this item. " +
                 "This is what caps health and mana potions at 4 and keeps the HUD bottles in sync.")]
        public bool SingleStackOnly = false;

        [Header("WEAPON (Kind = Weapon)")]
        [Tooltip("Spawned and parented to the hand socket when equipped.")]
        public GameObject WeaponPrefab;
        public WeaponType WeaponType = WeaponType.Sword;

        [Header("POTION (Kind = Potion)")]
        public PotionType PotionType = PotionType.Health;
        [Tooltip("Health restored, mana restored, extra speed, or effect duration in seconds - depending on Potion Type.")]
        public int PotionParameter = 25;

        public bool IsWeapon => Kind == ItemKind.Weapon;
        public bool IsPotion => Kind == ItemKind.Potion;

        public bool UsesQuickSlot => IsPotion && (PotionType == PotionType.Health || PotionType == PotionType.Mana);
    }
}
