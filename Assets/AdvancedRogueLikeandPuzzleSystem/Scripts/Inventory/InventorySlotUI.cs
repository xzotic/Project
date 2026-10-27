using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image Background;
        public Image IconImage;
        public Text AmountLabel;
        public Text FallbackLabel;
        public GameObject EquippedMarker;
        public Button Button;

        private InventoryUI owner;
        private int itemId;
        private bool filled;

        public int ItemId => itemId;
        public bool IsFilled => filled;

        public void Bind(InventoryUI inventoryUI)
        {
            owner = inventoryUI;
            if (Button != null)
            {
                Button.onClick.AddListener(OnClicked);
            }
        }

        public void SetEmpty()
        {
            filled = false;
            itemId = 0;
            if (IconImage != null) IconImage.enabled = false;
            if (AmountLabel != null) AmountLabel.text = string.Empty;
            if (FallbackLabel != null) FallbackLabel.text = string.Empty;
            if (EquippedMarker != null) EquippedMarker.SetActive(false);
        }

        public void SetItem(ItemDefinition definition, int amount, bool equipped)
        {
            filled = true;
            itemId = definition.Id;

            if (IconImage != null)
            {
                IconImage.enabled = definition.Icon != null;
                IconImage.sprite = definition.Icon;
            }

            if (FallbackLabel != null)
            {
                FallbackLabel.text = definition.Icon == null ? definition.DisplayName : string.Empty;
            }

            if (AmountLabel != null)
            {
                AmountLabel.text = amount > 1 ? "x" + amount : string.Empty;
            }

            if (EquippedMarker != null) EquippedMarker.SetActive(equipped);
        }

        private void OnClicked()
        {
            if (!filled || owner == null) return;
            owner.OnSlotClicked(itemId);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (filled && owner != null) owner.ShowTooltip(itemId);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (owner != null) owner.HideTooltip();
        }
    }
}
