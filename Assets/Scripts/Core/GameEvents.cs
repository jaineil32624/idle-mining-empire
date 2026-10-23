using System;

namespace IdleMiningEmpire
{
    /// <summary>Decoupled event bus. Managers raise events; quests, achievements, UI and audio listen.</summary>
    public static class GameEvents
    {
        public static event Action<OreDefinition, double> OreCollected;
        public static event Action<double> CoinsEarned;
        public static event Action<Rarity, bool> RockBroken;          // rarity, isBoss
        public static event Action<UpgradeDefinition, int> UpgradeBought;
        public static event Action<int> ZoneUnlocked;
        public static event Action<int> ZoneEntered;
        public static event Action<PetInstance> PetHatched;
        public static event Action<ResearchNodeDefinition> ResearchCompleted;
        public static event Action<ProducerDefinition> ProducerBought;
        public static event Action PrestigeDone;
        public static event Action<int> LevelUp;
        public static event Action StatsChanged;
        public static event Action<string> Toast;

        public static void RaiseOreCollected(OreDefinition o, double n) => OreCollected?.Invoke(o, n);
        public static void RaiseCoinsEarned(double c) => CoinsEarned?.Invoke(c);
        public static void RaiseRockBroken(Rarity r, bool boss) => RockBroken?.Invoke(r, boss);
        public static void RaiseUpgradeBought(UpgradeDefinition u, int lvl) => UpgradeBought?.Invoke(u, lvl);
        public static void RaiseZoneUnlocked(int z) => ZoneUnlocked?.Invoke(z);
        public static void RaiseZoneEntered(int z) => ZoneEntered?.Invoke(z);
        public static void RaisePetHatched(PetInstance p) => PetHatched?.Invoke(p);
        public static void RaiseResearchCompleted(ResearchNodeDefinition r) => ResearchCompleted?.Invoke(r);
        public static void RaiseProducerBought(ProducerDefinition p) => ProducerBought?.Invoke(p);
        public static void RaisePrestige() => PrestigeDone?.Invoke();
        public static void RaiseLevelUp(int l) => LevelUp?.Invoke(l);
        public static void RaiseStatsChanged() => StatsChanged?.Invoke();
        public static void RaiseToast(string msg) => Toast?.Invoke(msg);

        /// <summary>Static events survive domain-reload-disabled play mode; clear them on boot.</summary>
        public static void ClearAll()
        {
            OreCollected = null; CoinsEarned = null; RockBroken = null; UpgradeBought = null;
            ZoneUnlocked = null; ZoneEntered = null; PetHatched = null; ResearchCompleted = null;
            ProducerBought = null; PrestigeDone = null; LevelUp = null; StatsChanged = null; Toast = null;
        }
    }
}
