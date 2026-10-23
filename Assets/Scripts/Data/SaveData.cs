using System;
using System.Collections.Generic;

namespace IdleMiningEmpire
{
    [Serializable] public class IdCount { public string id; public double count; public IdCount() { } public IdCount(string i, double c) { id = i; count = c; } }
    [Serializable] public class IdLevel { public string id; public int level; public IdLevel() { } public IdLevel(string i, int l) { id = i; level = l; } }

    [Serializable]
    public class PetInstance
    {
        public string uid;
        public string defId;
        public int level = 1;
        public double xp;
        public bool equipped;
    }

    [Serializable]
    public class QuestState
    {
        public string id;          // story id or generated id
        public bool isStory;
        public string title;
        public QuestKind kind;
        public string target;
        public double amount;
        public double progress;
        public double rewardCoins, rewardGems, rewardXp;
        public bool claimed;
    }

    [Serializable]
    public class LifetimeStats
    {
        public double rocksBroken;
        public double coinsEarned;
        public double oresMined;
        public double crits;
        public double jumps;
        public double petsHatched;
        public double mythicRocks;
        public double mythicPets;
        public double bossesDefeated;
        public double playSeconds;
        public double prestiges;
        public double maxAbyssDepth;
        public int deepestZoneEver;
    }

    /// <summary>The entire persistent game state. Serialized to JSON by SaveManager.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public long lastSaveUtcTicks;

        // Currency
        public double coins;
        public double gems;
        public double crystals;
        public double totalCrystalsEarned;
        public double coinsEarnedThisRun;

        // Progress
        public int playerLevel = 1;
        public double playerXp;
        public int currentZone;
        public int unlockedZone;           // highest unlocked zone index
        public int abyssDepth = 1;         // current Abyss depth
        public int abyssUnlockedDepth = 1;
        public float playerX;

        public List<IdCount> inventory = new List<IdCount>();
        public List<IdLevel> upgrades = new List<IdLevel>();
        public List<IdLevel> workers = new List<IdLevel>();
        public List<IdLevel> drills = new List<IdLevel>();
        public List<string> research = new List<string>();
        public List<PetInstance> pets = new List<PetInstance>();
        public List<QuestState> quests = new List<QuestState>();
        public int storyIndex;
        public int generatedQuestCounter;
        public List<string> achievements = new List<string>();

        public LifetimeStats stats = new LifetimeStats();
        public int workersHiredThisRun;

        // Settings
        public bool musicOn = true;
        public bool sfxOn = true;
        public bool lowFx;
        public bool tutorialSeen;
        public int selectedCosmetic = -1; // -1 = best unlocked
        public int touchControls;         // 0 = auto, 1 = always on, 2 = off

        // ---- Helpers --------------------------------------------------------
        public static int GetLevel(List<IdLevel> list, string id)
        {
            for (int i = 0; i < list.Count; i++) if (list[i].id == id) return list[i].level;
            return 0;
        }

        public static void SetLevel(List<IdLevel> list, string id, int level)
        {
            for (int i = 0; i < list.Count; i++) if (list[i].id == id) { list[i].level = level; return; }
            list.Add(new IdLevel(id, level));
        }
    }
}
