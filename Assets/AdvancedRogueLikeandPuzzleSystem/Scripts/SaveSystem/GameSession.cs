using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedRogueLikeandPuzzleSystem
{
    /// <summary>
    /// Runtime owner of the save game. Tracks which world objects were consumed (picked up,
    /// chests opened) and which doors were opened, builds a <see cref="SaveData"/> snapshot with
    /// <see cref="Capture"/>, and writes a snapshot back into the scene with <see cref="Apply"/>.
    /// Lives on the GameManager prefab; world objects are identified by <see cref="SaveableId"/>.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance;

        /// <summary>
        /// Set by <see cref="ContinueGame"/> before the scene loads. Static because it must
        /// survive the scene change, while the GameSession instance itself does not.
        /// </summary>
        public static bool LoadSaveOnStart;

        [Tooltip("Scene the main menu loads for New Game and Continue.")]
        public string GameplaySceneName = "DemoScene";

        [Tooltip("Write a save automatically when the player opens a door for the first time.")]
        public bool AutoSaveOnDoorOpened = true;

        private readonly HashSet<string> consumedObjects = new HashSet<string>();
        private readonly HashSet<string> openedDoors = new HashSet<string>();

        private void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            // Wait one frame so every other singleton (HeroController, InventorySystem,
            // GameCanvas_Controller) has run Awake/Start and equipped its defaults before
            // the save overwrites that state.
            yield return null;

            ValidateSaveableIds();

            if (LoadSaveOnStart)
            {
                LoadSaveOnStart = false;
                Apply(SaveSystem.Load());
            }
        }

        private void ValidateSaveableIds()
        {
            var seen = new Dictionary<string, SaveableId>();
            SaveableId[] saveables = FindObjectsByType<SaveableId>(FindObjectsSortMode.None);

            for (int i = 0; i < saveables.Length; i++)
            {
                if (!saveables[i].HasId)
                {
                    Debug.LogError("[ARPS] '" + saveables[i].name + "' has a SaveableId with no id, so its state will not be saved.", saveables[i]);
                    continue;
                }

                if (seen.TryGetValue(saveables[i].Id, out SaveableId other))
                {
                    Debug.LogError("[ARPS] '" + saveables[i].name + "' and '" + other.name + "' share the same SaveableId. Duplicated objects keep the id of the original: clear the Id field on one of them so a new one is generated.", saveables[i]);
                    continue;
                }
                seen.Add(saveables[i].Id, saveables[i]);
            }
        }

        /// <summary>
        /// Records that a scene object was picked up or used. On load it is destroyed, except
        /// chests, which are restored in their opened state. Objects without a SaveableId are ignored.
        /// </summary>
        public static void MarkConsumed(GameObject worldObject)
        {
            if (Instance == null || worldObject == null) return;
            SaveableId saveable = worldObject.GetComponent<SaveableId>();
            if (saveable != null && saveable.HasId)
            {
                Instance.consumedObjects.Add(saveable.Id);
            }
        }

        /// <summary>
        /// Records a door as opened. The first time each door opens, an auto-save is written
        /// when <see cref="AutoSaveOnDoorOpened"/> is on.
        /// </summary>
        public static void MarkDoorOpened(GameObject door)
        {
            if (Instance == null || door == null) return;
            SaveableId saveable = door.GetComponent<SaveableId>();
            if (saveable == null || !saveable.HasId) return;

            if (Instance.openedDoors.Add(saveable.Id) && Instance.AutoSaveOnDoorOpened)
            {
                Instance.SaveNow();
            }
        }

        /// <summary>Builds a snapshot of the hero, inventory and world state. Does not touch disk.</summary>
        public SaveData Capture()
        {
            var data = new SaveData();
            data.SceneName = SceneManager.GetActiveScene().name;

            HeroController hero = HeroController.instance;
            if (hero != null)
            {
                data.Position = hero.transform.position;
                data.RotY = hero.transform.eulerAngles.y;
                data.Health = hero.Health;
                data.Mana = hero.Mana;
                data.Coin = hero.Coin;
                data.Level = hero.Level;
                data.KeysGrabbed = new List<int>(hero.Keys_Grabbed);
            }

            if (InventorySystem.Instance != null)
            {
                data.Items = InventorySystem.Instance.CaptureEntries();
                data.EquippedAttackId = InventorySystem.Instance.EquippedAttackId;
                data.EquippedDefendId = InventorySystem.Instance.EquippedDefendId;
            }

            data.ConsumedObjects = new List<string>(consumedObjects);
            data.OpenedDoors = new List<string>(openedDoors);
            return data;
        }

        /// <summary>Captures the current state and writes it through <see cref="SaveSystem.Save"/>.</summary>
        public bool SaveNow()
        {
            bool ok = SaveSystem.Save(Capture());
            if (ok && GameCanvas_Controller.instance != null)
            {
                GameCanvas_Controller.instance.Show_Warning("GAME SAVED");
            }
            return ok;
        }

        /// <summary>
        /// Writes a snapshot back into the loaded scene: the world first (destroy consumed objects,
        /// restore doors and chests), then the hero, then the inventory. The inventory goes last
        /// because re-equipping weapons needs the hero's body sockets.
        /// </summary>
        public void Apply(SaveData data)
        {
            if (data == null) return;

            consumedObjects.Clear();
            openedDoors.Clear();
            for (int i = 0; i < data.ConsumedObjects.Count; i++) consumedObjects.Add(data.ConsumedObjects[i]);
            for (int i = 0; i < data.OpenedDoors.Count; i++) openedDoors.Add(data.OpenedDoors[i]);

            ApplyWorld();
            ApplyHero(data);
            ApplyInventory(data);
        }

        private void ApplyWorld()
        {
            SaveableId[] saveables = FindObjectsByType<SaveableId>(FindObjectsSortMode.None);
            for (int i = 0; i < saveables.Length; i++)
            {
                SaveableId saveable = saveables[i];
                if (!saveable.HasId) continue;

                if (consumedObjects.Contains(saveable.Id))
                {
                    if (saveable.TryGetComponent(out ChestScript chest))
                    {
                        chest.RestoreAsOpened();
                    }
                    else
                    {
                        Destroy(saveable.gameObject);
                    }
                    continue;
                }

                if (openedDoors.Contains(saveable.Id) && saveable.TryGetComponent(out DoorScript door))
                {
                    door.RestoreAsOpened();
                }
            }
        }

        private void ApplyHero(SaveData data)
        {
            HeroController hero = HeroController.instance;
            if (hero == null) return;

            CharacterController controller = hero.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;

            hero.transform.position = data.Position;
            hero.transform.rotation = Quaternion.Euler(0f, data.RotY, 0f);

            if (controller != null) controller.enabled = wasEnabled;

            hero.Health = Mathf.Clamp(data.Health, 1, (int)hero.TotalHealth);
            hero.Mana = Mathf.Clamp(data.Mana, 0, (int)hero.TotalMana);
            hero.Coin = data.Coin;
            hero.Level = data.Level;
            // HeroController still seeds Coin/Level from PlayerPrefs (V1 persistence), so keep
            // both stores in step; otherwise the next scene load would revert to stale values.
            PlayerPrefs.SetInt(PrefsKeyCoin, hero.Coin);
            PlayerPrefs.SetInt(PrefsKeyLevel, hero.Level);

            hero.Keys_Grabbed.Clear();
            for (int i = 0; i < data.KeysGrabbed.Count; i++) hero.Keys_Grabbed.Add(data.KeysGrabbed[i]);

            if (hero.Animator != null) hero.Animator.SetInteger("Health", hero.Health);

            if (GameCanvas_Controller.instance != null)
            {
                GameCanvas_Controller.instance.Update_Health_Bar();
                GameCanvas_Controller.instance.Update_Mana_Bar(0);
                GameCanvas_Controller.instance.Update_Text_Coin();
            }
        }

        private void ApplyInventory(SaveData data)
        {
            if (InventorySystem.Instance == null) return;
            InventorySystem.Instance.RestoreEntries(data.Items, data.EquippedAttackId, data.EquippedDefendId);

            if (GameCanvas_Controller.instance != null)
            {
                GameCanvas_Controller.instance.Refresh_Potion_Slots();
            }
        }

        // PlayerPrefs keys written by HeroController (V1 persistence that predates SaveSystem).
        private const string PrefsKeyCoin = "Coin";
        private const string PrefsKeyLevel = "Level";
        private static readonly string[] LegacyPrefsKeys = { PrefsKeyCoin, PrefsKeyLevel, "WeaponAttack", "WeaponDefend" };

        public static void StartNewGame(string sceneName)
        {
            SaveSystem.Delete();
            // A new game must not inherit coins/level from a previous run through PlayerPrefs.
            for (int i = 0; i < LegacyPrefsKeys.Length; i++) PlayerPrefs.DeleteKey(LegacyPrefsKeys[i]);
            PlayerPrefs.Save();
            LoadSaveOnStart = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Loads the scene stored in the save (or the fallback) and flags the new scene's
        /// GameSession to apply the save one frame after Start.
        /// </summary>
        public static void ContinueGame(string fallbackSceneName)
        {
            SaveData data = SaveSystem.Load();
            LoadSaveOnStart = data != null;
            Time.timeScale = 1f;
            SceneManager.LoadScene(data != null && !string.IsNullOrEmpty(data.SceneName) ? data.SceneName : fallbackSceneName);
        }
    }
}
