using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    /// <summary>
    /// Plain serializable snapshot written by <see cref="SaveSystem"/>. JsonUtility only handles
    /// public fields of supported types (no dictionaries, no properties), which is why the
    /// position is stored as three floats and the world state as string lists.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Format version. Currently written but not checked on load.</summary>
        public int Version = 1;
        public string SavedAtUtc = "";
        public string SceneName = "";

        [Header("HERO")]
        public float PosX, PosY, PosZ;
        public float RotY;
        public int Health = 100;
        public int Mana = 100;
        public int Coin;
        public int Level = 1;

        [Header("INVENTORY")]
        public List<InventoryEntry> Items = new List<InventoryEntry>();
        public int EquippedAttackId;
        public int EquippedDefendId;

        [Header("WORLD")]
        public List<int> KeysGrabbed = new List<int>();
        public List<string> ConsumedObjects = new List<string>();
        public List<string> OpenedDoors = new List<string>();

        public Vector3 Position
        {
            get { return new Vector3(PosX, PosY, PosZ); }
            set { PosX = value.x; PosY = value.y; PosZ = value.z; }
        }

        public string DisplayDate
        {
            get
            {
                if (DateTime.TryParse(SavedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed))
                {
                    return parsed.ToLocalTime().ToString("dd MMM yyyy  HH:mm");
                }
                return SavedAtUtc;
            }
        }
    }
}
