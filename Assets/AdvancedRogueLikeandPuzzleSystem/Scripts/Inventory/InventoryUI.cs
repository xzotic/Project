using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance;

        [Header("WIRING")]
        public GameObject Panel;
        public Transform SlotParent;
        public GameObject SlotPrefab;
        public Button Button_Close;
        public Button Button_Open;

        [Header("TOOLTIP")]
        public GameObject TooltipPanel;
        public Text TooltipTitle;
        public Text TooltipBody;

        [Header("EQUIPPED PANEL")]
        public Image Equipped_Weapon_Icon;
        public Text Equipped_Weapon_Label;
        public Image Equipped_Shield_Icon;
        public Text Equipped_Shield_Label;

        private readonly List<InventorySlotUI> slots = new List<InventorySlotUI>();
        private bool isOpen;
        private bool initialised;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Prepare();
        }

        public void Prepare()
        {
            if (initialised) return;

            Instance = this;
            isOpen = Panel != null && Panel.activeSelf;

            if (Button_Close != null) Button_Close.onClick.AddListener(Close);
            if (Button_Open != null) Button_Open.onClick.AddListener(Toggle);

            BuildSlots();

            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.Changed += Refresh;
            }

            initialised = true;
            Refresh();
        }

        private void OnDestroy()
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.Changed -= Refresh;
            }
        }

        private void BuildSlots()
        {
            if (SlotParent == null || SlotPrefab == null) return;

            int capacity = InventorySystem.Instance != null ? InventorySystem.Instance.Capacity : 20;
            for (int i = 0; i < capacity; i++)
            {
                GameObject go = Instantiate(SlotPrefab, SlotParent);
                go.SetActive(true);
                InventorySlotUI slot = go.GetComponent<InventorySlotUI>();
                if (slot == null) continue;
                slot.Bind(this);
                slot.SetEmpty();
                slots.Add(slot);
            }
        }

        public void Toggle()
        {
            if (isOpen) Close(); else Open();
        }

        public void Open()
        {
            if (Panel == null) return;
            if (GameCanvas_Controller.instance != null && GameCanvas_Controller.instance.GameOverPanel != null
                && GameCanvas_Controller.instance.GameOverPanel.activeSelf) return;

            Prepare();

            isOpen = true;
            Panel.SetActive(true);
            Refresh();

            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void Close()
        {
            if (Panel == null) return;

            isOpen = false;
            Panel.SetActive(false);
            HideTooltip();

            Time.timeScale = 1f;
            if (GameManager.Instance != null && GameManager.Instance.controllerType == ControllerType.KeyboardMouse)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void Refresh()
        {
            if (InventorySystem.Instance == null || InventorySystem.Instance.Database == null) return;

            ItemDatabase database = InventorySystem.Instance.Database;
            List<InventoryEntry> entries = InventorySystem.Instance.Entries;

            for (int i = 0; i < slots.Count; i++)
            {
                if (i < entries.Count)
                {
                    ItemDefinition definition = database.Get(entries[i].Id);
                    if (definition == null)
                    {
                        slots[i].SetEmpty();
                        continue;
                    }
                    slots[i].SetItem(definition, entries[i].Amount, InventorySystem.Instance.IsEquipped(definition.Id));
                }
                else
                {
                    slots[i].SetEmpty();
                }
            }

            RefreshEquippedPanel(database);
        }

        private void RefreshEquippedPanel(ItemDatabase database)
        {
            ApplyEquippedSlot(database.Get(InventorySystem.Instance.EquippedAttackId), Equipped_Weapon_Icon, Equipped_Weapon_Label, "No weapon");
            ApplyEquippedSlot(database.Get(InventorySystem.Instance.EquippedDefendId), Equipped_Shield_Icon, Equipped_Shield_Label, "No shield");
        }

        private static void ApplyEquippedSlot(ItemDefinition definition, Image icon, Text label, string emptyText)
        {
            if (icon != null)
            {
                icon.enabled = definition != null && definition.Icon != null;
                icon.sprite = definition != null ? definition.Icon : null;
            }
            if (label != null)
            {
                label.text = definition != null ? definition.DisplayName : emptyText;
            }
        }

        public void OnSlotClicked(int itemId)
        {
            if (InventorySystem.Instance == null) return;
            InventorySystem.Instance.UseItem(itemId);
            ShowTooltip(itemId);
        }

        public void ShowTooltip(int itemId)
        {
            if (TooltipPanel == null || InventorySystem.Instance == null || InventorySystem.Instance.Database == null) return;

            ItemDefinition definition = InventorySystem.Instance.Database.Get(itemId);
            if (definition == null) return;

            TooltipPanel.SetActive(true);
            if (TooltipTitle != null) TooltipTitle.text = definition.DisplayName.ToUpper();
            if (TooltipBody != null) TooltipBody.text = BuildDescription(definition);
        }

        private static string BuildDescription(ItemDefinition definition)
        {
            if (definition.IsWeapon)
            {
                bool equipped = InventorySystem.Instance != null && InventorySystem.Instance.IsEquipped(definition.Id);
                return definition.WeaponType + "\n" + (equipped ? "Click to unequip" : "Click to equip");
            }

            switch (definition.PotionType)
            {
                case PotionType.Health: return "Restores " + definition.PotionParameter + " health.\nClick to drink";
                case PotionType.Mana: return "Restores " + definition.PotionParameter + " mana.\nClick to drink";
                case PotionType.Speed: return "+" + definition.PotionParameter + " movement speed.\nClick to drink";
            }
            return string.Empty;
        }

        public void HideTooltip()
        {
            if (TooltipPanel != null) TooltipPanel.SetActive(false);
        }

        public void Click_Button_Toggle()
        {
            Toggle();
        }
    }
}
