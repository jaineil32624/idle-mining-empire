namespace IdleMiningEmpire
{
    public enum Rarity { Common = 0, Rare = 1, Epic = 2, Legendary = 3, Mythic = 4 }

    public enum CurrencyType { Coins = 0, Gems = 1, Crystals = 2 }

    /// <summary>Every tunable number in the game is a stat. Upgrades, research, pets,
    /// levels and prestige all contribute modifiers to these.</summary>
    public enum StatType
    {
        MiningPower,
        CritChance,
        CritDamage,
        MiningSpeed,
        Luck,
        BackpackCapacity,
        AutoSellBonus,
        OreMultiplier,
        WalkSpeed,
        DashCooldown,
        JumpHeight,
        OreValue,
        CoinMultiplier,
        WorkerEfficiency,
        DrillEfficiency,
        OfflineHours,
        OfflineEfficiency,
        XpMultiplier,
        GemChance,
        PetSlots,
        AutoMine,
        AutoSell,
        Splash,
        RockCount,
        MineRange,
        CrystalGain,
        StartingCoins,
        PetXp,
        Count // keep last
    }

    /// <summary>How a modifier is applied.</summary>
    public enum ModifierMode
    {
        Flat,        // final = (base + flat) ...
        Percent,     // ... * (1 + sum of percents)
        Curve,       // flat += value * ((1 + L) * expBase^L - 1)       (used by Mining Power)
        Quadratic,   // flat += value * L * (1 + L / 10)                (used by Backpack)
        Exponential  // flat += value * (expBase^L - 1)
    }

    public enum UpgradeCategory { Pickaxe, Backpack, Movement, Premium, Prestige }

    public enum ResearchCategory { Mining, Economy, Automation, Exploration, RareOres, LegendaryEquipment }

    public enum QuestKind
    {
        CollectOre, EarnCoins, BuyUpgrade, ReachZone, BreakRocks, BreakRarity,
        HatchPet, Research, HireWorker, BuyDrill, Prestige, ReachLevel, DefeatBoss
    }

    public enum AchievementKind
    {
        RocksBroken, CoinsEarned, PlayerLevel, Prestiges, PetsHatched, MythicPet,
        MythicRock, BossesDefeated, AbyssDepth, Crits, Jumps, ZoneReached, ResearchCount, WorkersHired
    }
}
