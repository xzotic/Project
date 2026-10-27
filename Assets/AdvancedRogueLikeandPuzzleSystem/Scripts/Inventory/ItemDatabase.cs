using System.Collections.Generic;
using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    /// <summary>
    /// Registry that maps item ids to <see cref="ItemDefinition"/> assets. The lookup dictionary
    /// is built lazily on first access (a ScriptableObject cannot serialize a Dictionary). Call
    /// <see cref="Refresh"/> after editing <see cref="Items"/> at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Advanced Roguelike/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemDefinition> Items = new List<ItemDefinition>();

        private Dictionary<int, ItemDefinition> lookup;

        private void BuildLookup()
        {
            lookup = new Dictionary<int, ItemDefinition>();
            for (int i = 0; i < Items.Count; i++)
            {
                ItemDefinition item = Items[i];
                if (item == null) continue;

                if (lookup.ContainsKey(item.Id))
                {
                    Debug.LogError("[ARPS] ItemDatabase contains two items with Id " + item.Id +
                                   " ('" + lookup[item.Id].DisplayName + "' and '" + item.DisplayName +
                                   "'). Ids must be unique or saved games will load the wrong item.", this);
                    continue;
                }
                lookup.Add(item.Id, item);
            }
        }

        public ItemDefinition Get(int id)
        {
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out ItemDefinition item) ? item : null;
        }

        public ItemDefinition GetByPotionType(PotionType potionType)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] != null && Items[i].IsPotion && Items[i].PotionType == potionType)
                {
                    return Items[i];
                }
            }
            return null;
        }

        public void Refresh()
        {
            lookup = null;
        }
    }
}
