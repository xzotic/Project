using System;
using System.IO;
using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    /// <summary>
    /// File I/O for the single save slot: <see cref="SaveData"/> serialized with JsonUtility to
    /// <c>Application.persistentDataPath/arps_save.json</c>. Failures are logged and reported
    /// through return values instead of thrown, so a bad save can never crash the menu.
    /// </summary>
    public static class SaveSystem
    {
        public const string FileName = "arps_save.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave()
        {
            return File.Exists(FilePath);
        }

        public static bool Save(SaveData data)
        {
            if (data == null) return false;

            try
            {
                data.SavedAtUtc = DateTime.UtcNow.ToString("o");
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(FilePath, json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[ARPS] Could not write the save file at " + FilePath + ": " + e.Message);
                return false;
            }
        }

        public static SaveData Load()
        {
            if (!HasSave()) return null;

            try
            {
                string json = File.ReadAllText(FilePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    Debug.LogError("[ARPS] The save file at " + FilePath + " could not be parsed and will be ignored.");
                }
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("[ARPS] Could not read the save file at " + FilePath + ": " + e.Message);
                return null;
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[ARPS] Could not delete the save file at " + FilePath + ": " + e.Message);
            }
        }
    }
}
