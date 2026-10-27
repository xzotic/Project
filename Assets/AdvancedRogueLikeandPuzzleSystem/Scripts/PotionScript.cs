using System.Collections;
using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class PotionScript : MonoBehaviour
    {
        public int Extra_Health = 25;
        public int Extra_Mana = 50;
        public int Extra_Speed = 2;
        public string Name;
        public PotionType potionType;
        bool grabbale = false;
        float lastTime = 0;

        public int ConfiguredAmount
        {
            get
            {
                switch (potionType)
                {
                    case PotionType.Health: return Extra_Health;
                    case PotionType.Mana: return Extra_Mana;
                    case PotionType.Speed: return Extra_Speed;
                }
                return 0;
            }
        }

        private void Start()
        {
            StartCoroutine(EnableGrabAfterDelay());
        }

        IEnumerator EnableGrabAfterDelay()
        {
            yield return new WaitForSeconds(1.5f);
            grabbale = true;
        }

        public void GrabIt()
        {
            if (InventorySystem.Instance == null || InventorySystem.Instance.Database == null)
            {
                Debug.LogError("[ARPS] No InventorySystem in the scene, so potions cannot be picked up.  Add the InventorySystem component to your GameManager object and assign the Item Database.", this);
                return;
            }

            ItemDefinition definition = InventorySystem.Instance.Database.GetByPotionType(potionType);
            if (definition == null)
            {
                Debug.LogError("[ARPS] No " + potionType + " potion in the Item Database.", this);
                return;
            }

            if (!InventorySystem.Instance.Add(definition.Id, 1, ConfiguredAmount))
            {
                if (GameCanvas_Controller.instance != null) GameCanvas_Controller.instance.Show_Warning("CANNOT CARRY MORE!");
                return;
            }

            if (GameCanvas_Controller.instance != null)
            {
                GameCanvas_Controller.instance.Show_Image_Potion(potionType);
                GameCanvas_Controller.instance.Show_Grabbed_Text(Name);
            }
            if (AudioManager.instance != null) AudioManager.instance.Play_Grab();
            GameSession.MarkConsumed(gameObject);
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && grabbale && Time.time > lastTime + 0.5f)
            {
                lastTime = Time.time;
                GrabIt();
            }
        }
    }

    // Values are explicit (and not contiguous) because scenes, prefabs and ItemDefinition assets
    // store the integer. Never renumber existing members; append new ones with a new value.
    public enum PotionType
    {
        Health = 0,
        Speed = 1,
        Mana = 4
    }
}