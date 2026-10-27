using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public enum MinimapIconType
    {
        Player,
        Enemy,
        Item,
        Door,
        Chest,
        Objective
    }

    public class MinimapIcon : MonoBehaviour
    {
        public MinimapIconType IconType = MinimapIconType.Item;
        [Tooltip("World-space size of the blip.")]
        public float Size = 1.5f;
        [Tooltip("Height above the object the blip floats at.")]
        public float Height = 3f;

        private Transform blip;

        private void Start()
        {
            if (Minimap.Instance == null) return;
            blip = Minimap.Instance.CreateBlip(this);
        }

        private void LateUpdate()
        {
            if (blip == null) return;

            blip.position = new Vector3(transform.position.x, transform.position.y + Height, transform.position.z);

            blip.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
        }

        private void OnDestroy()
        {
            if (blip != null) Destroy(blip.gameObject);
        }
    }
}
