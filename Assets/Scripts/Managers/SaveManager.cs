using System;
using System.IO;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>
    /// JSON save system with auto-save every 30 seconds and offline progression.
    /// Desktop/Android: JSON file in persistentDataPath (with a .bak backup).
    /// WebGL: the same JSON stored through PlayerPrefs (IndexedDB), which flushes reliably in browsers.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public const float AutoSaveInterval = 30f;
        const string PrefsKey = "IdleMiningEmpire_Save";
        const string FileName = "idle_mining_empire_save.json";

        GameManager gm;
        float timer;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        static bool UsePrefs => Application.platform == RuntimePlatform.WebGLPlayer;

        public SaveData Load()
        {
            gm = GameManager.Instance;
            string json = null;
            try
            {
                if (UsePrefs) json = PlayerPrefs.GetString(PrefsKey, null);
                else if (File.Exists(FilePath)) json = File.ReadAllText(FilePath);
                else if (File.Exists(FilePath + ".bak")) json = File.ReadAllText(FilePath + ".bak");
            }
            catch (Exception e) { Debug.LogWarning("[Save] Could not read save: " + e.Message); }

            SaveData data = null;
            if (!string.IsNullOrEmpty(json))
            {
                try { data = JsonUtility.FromJson<SaveData>(json); }
                catch (Exception e) { Debug.LogWarning("[Save] Corrupt save, starting fresh: " + e.Message); }
            }
            if (data == null) data = NewSave();
            Sanitize(data);
            return data;
        }

        static SaveData NewSave()
        {
            return new SaveData { lastSaveUtcTicks = DateTime.UtcNow.Ticks };
        }

        static void Sanitize(SaveData d)
        {
            if (d.inventory == null) d.inventory = new System.Collections.Generic.List<IdCount>();
            if (d.upgrades == null) d.upgrades = new System.Collections.Generic.List<IdLevel>();
            if (d.workers == null) d.workers = new System.Collections.Generic.List<IdLevel>();
            if (d.drills == null) d.drills = new System.Collections.Generic.List<IdLevel>();
            if (d.research == null) d.research = new System.Collections.Generic.List<string>();
            if (d.pets == null) d.pets = new System.Collections.Generic.List<PetInstance>();
            if (d.quests == null) d.quests = new System.Collections.Generic.List<QuestState>();
            if (d.achievements == null) d.achievements = new System.Collections.Generic.List<string>();
            if (d.stats == null) d.stats = new LifetimeStats();
            if (d.playerLevel < 1) d.playerLevel = 1;
            if (d.abyssDepth < 1) d.abyssDepth = 1;
            if (d.abyssUnlockedDepth < 1) d.abyssUnlockedDepth = 1;
            if (double.IsNaN(d.coins) || d.coins < 0) d.coins = 0;
            if (double.IsNaN(d.gems) || d.gems < 0) d.gems = 0;
            d.currentZone = Mathf.Clamp(d.currentZone, 0, Math.Max(0, d.unlockedZone));
        }

        void Update()
        {
            if (gm == null || !gm.Ready) return;
            timer += Time.unscaledDeltaTime;
            if (timer >= AutoSaveInterval)
            {
                timer = 0;
                SaveNow();
            }
        }

        public void SaveNow()
        {
            if (gm == null || gm.Save == null || !gm.Ready) return;
            try
            {
                gm.Save.lastSaveUtcTicks = DateTime.UtcNow.Ticks;
                if (gm.Player != null) gm.Save.playerX = gm.Player.transform.position.x;
                string json = JsonUtility.ToJson(gm.Save);
                if (UsePrefs)
                {
                    PlayerPrefs.SetString(PrefsKey, json);
                    PlayerPrefs.Save();
                }
                else
                {
                    string tmp = FilePath + ".tmp";
                    File.WriteAllText(tmp, json);
                    if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".bak", true);
                    if (File.Exists(FilePath)) File.Delete(FilePath);
                    File.Move(tmp, FilePath);
                }
                if (gm.UI != null) gm.UI.FlashSaved();
            }
            catch (Exception e) { Debug.LogWarning("[Save] Failed: " + e.Message); }
        }

        /// <summary>Grants rewards for time away and shows the "While you were away" screen.</summary>
        public void ApplyOfflineProgress(bool onStartup)
        {
            var s = gm.Save;
            long now = DateTime.UtcNow.Ticks;
            double seconds = (now - s.lastSaveUtcTicks) / (double)TimeSpan.TicksPerSecond;
            s.lastSaveUtcTicks = now;
            if (seconds < 60 || seconds > 3600 * 24 * 365) return;

            double cap = gm.Stat(StatType.OfflineHours) * 3600;
            double counted = Math.Min(seconds, cap);
            gm.Automation.CalcOffline(counted, out double coins, out double ores);
            if (coins <= 0 && ores <= 0)
            {
                if (onStartup && seconds > 300) GameEvents.RaiseToast("Welcome back! Hire workers and buy drills to earn while away.");
                return;
            }
            gm.Currency.AddCoins(coins);
            s.stats.oresMined += ores;
            gm.UI.ShowOfflineReward(seconds, counted, coins, ores);
            SaveNow();
        }

        public void DeleteSave()
        {
            try
            {
                if (UsePrefs) { PlayerPrefs.DeleteKey(PrefsKey); PlayerPrefs.Save(); }
                else
                {
                    if (File.Exists(FilePath)) File.Delete(FilePath);
                    if (File.Exists(FilePath + ".bak")) File.Delete(FilePath + ".bak");
                }
            }
            catch (Exception e) { Debug.LogWarning("[Save] Delete failed: " + e.Message); }
        }
    }
}
