using System.IO;
using UnityEditor;
using UnityEngine;

namespace IdleMiningEmpire.EditorTools
{
    /// <summary>
    /// Saves the built-in content as a ScriptableObject asset (Assets/Resources/GameDatabase.asset)
    /// so ores, zones, upgrades, pets, research and quests can be tuned in the Inspector.
    /// When the asset exists the game loads it instead of the code defaults.
    /// </summary>
    public static class DatabaseExporter
    {
        const string AssetPath = "Assets/Resources/GameDatabase.asset";

        [MenuItem("Idle Mining Empire/Export Database Assets (edit content in Inspector)", priority = 50)]
        public static void Export()
        {
            if (File.Exists(AssetPath) &&
                !EditorUtility.DisplayDialog("Overwrite?", "GameDatabase.asset already exists. Replace it with the default content?", "Replace", "Cancel"))
                return;

            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.DeleteAsset(AssetPath);
            var db = DefaultContent.Create();
            AssetDatabase.CreateAsset(db, AssetPath);
            void Add<T>(System.Collections.Generic.List<T> list) where T : ScriptableObject
            {
                foreach (var item in list) AssetDatabase.AddObjectToAsset(item, db);
            }
            Add(db.ores);
            Add(db.zones);
            Add(db.upgrades);
            Add(db.workers);
            Add(db.drills);
            Add(db.pets);
            Add(db.research);
            Add(db.storyQuests);
            Add(db.achievements);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = db;
            Debug.Log("[Idle Mining Empire] Exported " + AssetPath + ". Edit values in the Inspector; the game now loads this asset.");
        }

        [MenuItem("Idle Mining Empire/Delete Save Data", priority = 60)]
        public static void DeleteSave()
        {
            if (!EditorUtility.DisplayDialog("Delete save?", "Delete the local save file and PlayerPrefs save?", "Delete", "Cancel")) return;
            string path = Path.Combine(Application.persistentDataPath, "idle_mining_empire_save.json");
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
            PlayerPrefs.DeleteKey("IdleMiningEmpire_Save");
            PlayerPrefs.Save();
            Debug.Log("[Idle Mining Empire] Save deleted.");
        }
    }
}
