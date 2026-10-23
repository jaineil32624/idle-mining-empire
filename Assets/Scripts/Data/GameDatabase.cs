using System.Collections.Generic;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>
    /// Central registry of all game content. If an asset named "GameDatabase" exists in a
    /// Resources folder it is used (export one via "Idle Mining Empire > Export Database Assets"
    /// and tweak values in the Inspector); otherwise the built-in defaults below are generated.
    /// </summary>
    [CreateAssetMenu(menuName = "Idle Mining Empire/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public List<OreDefinition> ores = new List<OreDefinition>();
        public List<ZoneDefinition> zones = new List<ZoneDefinition>();
        public List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();
        public List<WorkerDefinition> workers = new List<WorkerDefinition>();
        public List<DrillDefinition> drills = new List<DrillDefinition>();
        public List<PetDefinition> pets = new List<PetDefinition>();
        public List<ResearchNodeDefinition> research = new List<ResearchNodeDefinition>();
        public List<QuestDefinition> storyQuests = new List<QuestDefinition>();
        public List<AchievementDefinition> achievements = new List<AchievementDefinition>();

        Dictionary<string, OreDefinition> oreLookup;
        Dictionary<string, UpgradeDefinition> upgradeLookup;
        Dictionary<string, PetDefinition> petLookup;
        Dictionary<string, ResearchNodeDefinition> researchLookup;

        public void BuildLookups()
        {
            oreLookup = new Dictionary<string, OreDefinition>();
            foreach (var o in ores) oreLookup[o.id] = o;
            upgradeLookup = new Dictionary<string, UpgradeDefinition>();
            foreach (var u in upgrades) upgradeLookup[u.id] = u;
            petLookup = new Dictionary<string, PetDefinition>();
            foreach (var p in pets) petLookup[p.id] = p;
            researchLookup = new Dictionary<string, ResearchNodeDefinition>();
            foreach (var r in research) researchLookup[r.id] = r;
        }

        public OreDefinition GetOre(string id)
        {
            if (oreLookup == null) BuildLookups();
            return id != null && oreLookup.TryGetValue(id, out var o) ? o : null;
        }

        public UpgradeDefinition GetUpgrade(string id)
        {
            if (upgradeLookup == null) BuildLookups();
            return id != null && upgradeLookup.TryGetValue(id, out var u) ? u : null;
        }

        public PetDefinition GetPet(string id)
        {
            if (petLookup == null) BuildLookups();
            return id != null && petLookup.TryGetValue(id, out var p) ? p : null;
        }

        public ResearchNodeDefinition GetResearch(string id)
        {
            if (researchLookup == null) BuildLookups();
            return id != null && researchLookup.TryGetValue(id, out var r) ? r : null;
        }

        public static GameDatabase Load()
        {
            var db = Resources.Load<GameDatabase>("GameDatabase");
            if (db == null || db.zones.Count == 0) db = DefaultContent.Create();
            db.BuildLookups();
            return db;
        }
    }
}
