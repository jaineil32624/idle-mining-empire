using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>
    /// Builds the default content set (ores, zones, upgrades, pets, 54 research nodes, quests...).
    /// Everything is created as ScriptableObject instances so it can be exported to assets and
    /// edited in the Inspector without touching code.
    /// </summary>
    public static class DefaultContent
    {
        static T SO<T>(string name) where T : ScriptableObject
        {
            var t = ScriptableObject.CreateInstance<T>();
            t.name = name;
            return t;
        }

        static Color C(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static GameDatabase Create()
        {
            var db = SO<GameDatabase>("GameDatabase");
            AddOres(db);
            AddZones(db);
            AddUpgrades(db);
            AddProducers(db);
            AddPets(db);
            AddResearch(db);
            AddQuests(db);
            AddAchievements(db);
            return db;
        }

        // ------------------------------------------------------------------ ORES
        static void Ore(GameDatabase db, string id, string name, string color, string hi, double value, int zone, bool abyss = false)
        {
            var o = SO<OreDefinition>(id);
            o.id = id; o.displayName = name; o.color = C(color); o.highlight = C(hi);
            o.baseValue = value; o.zoneIndex = zone; o.scalesWithAbyss = abyss;
            db.ores.Add(o);
        }

        static void AddOres(GameDatabase db)
        {
            Ore(db, "stone", "Stone", "9a9a9e", "c8c8cc", 1, 0);
            Ore(db, "coal", "Coal", "2b2b30", "5a5a66", 3, 0);
            Ore(db, "copper", "Copper", "d9773b", "ffb27a", 8, 0);

            Ore(db, "iron", "Iron", "b0a090", "e6d8c8", 40, 1);
            Ore(db, "silver", "Silver", "cfd6e2", "ffffff", 90, 1);
            Ore(db, "gold", "Gold", "ffc61a", "fff2a8", 200, 1);

            Ore(db, "platinum", "Platinum", "b8c6d8", "f2f6ff", 1200, 2);
            Ore(db, "emerald", "Emerald", "21c46b", "a8ffcf", 2500, 2);
            Ore(db, "ruby", "Ruby", "e0115f", "ff8ab4", 5000, 2);

            Ore(db, "diamond", "Diamond", "8fe9ff", "ffffff", 30000, 3);
            Ore(db, "titanium", "Titanium", "7d8794", "c9d2dd", 60000, 3);
            Ore(db, "uranium", "Uranium", "7cfc00", "e0ffb0", 120000, 3);

            Ore(db, "exotic", "Exotic Crystal", "ff5cf0", "ffd0fb", 8e5, 4);
            Ore(db, "alien", "Alien Material", "39ff14", "c8ffb8", 1.6e6, 4);
            Ore(db, "ancienttech", "Ancient Technology", "ffaa00", "fff0b0", 3.5e6, 4);

            Ore(db, "rune", "Rune Stone", "7b68ee", "d8d0ff", 3e7, 5);
            Ore(db, "starmetal", "Star Metal", "fff4b0", "ffffff", 7e7, 5);
            Ore(db, "void", "Void Essence", "6a0dad", "e0a0ff", 1.5e8, 5);

            Ore(db, "abyssal", "Abyssal Shard", "1e90ff", "b0e0ff", 4e8, 6, true);
            Ore(db, "darkmatter", "Dark Matter", "302860", "9080ff", 9e8, 6, true);
            Ore(db, "eternium", "Eternium", "ff4500", "ffd0a0", 2e9, 6, true);
        }

        // ----------------------------------------------------------------- ZONES
        static ZoneDefinition Zone(GameDatabase db, int index, string id, string name, string desc, string[] ores,
            double hp, double hardness, double unlock, double xp,
            string skyTop, string skyBottom, string wall, string ground, string groundTop, string rock, string ambient,
            float bpm, int root, int scale)
        {
            var z = SO<ZoneDefinition>(id);
            z.id = id; z.displayName = name; z.description = desc; z.index = index; z.oreIds = ores;
            z.baseRockHp = hp; z.hardness = hardness; z.unlockCost = unlock; z.xpBase = xp;
            z.skyTop = C(skyTop); z.skyBottom = C(skyBottom); z.wallColor = C(wall);
            z.groundColor = C(ground); z.groundTop = C(groundTop); z.rockColor = C(rock);
            var a = C(ambient); a.a = 0.55f; z.ambientColor = a;
            z.bpm = bpm; z.rootMidi = root; z.scale = scale; z.musicSeed = 17 + index * 31;
            db.zones.Add(z);
            return z;
        }

        static void AddZones(GameDatabase db)
        {
            Zone(db, 0, "surface", "Surface", "Sunny hills and soft stone. Every empire starts here.",
                new[] { "stone", "coal", "copper" }, 5, 1, 0, 2,
                "6ec3ff", "d8f1ff", "5f9a52", "6b4a2b", "5cc85c", "9a9a9e", "ffffff", 112, 60, 0);
            Zone(db, 1, "cave", "Cave Layer", "Dripping caverns hiding veins of precious metal.",
                new[] { "iron", "silver", "gold" }, 60, 20, 2500, 8,
                "1e1830", "463a58", "2e2640", "4a3b33", "7a6656", "7a6e66", "ffe08a", 96, 57, 1);
            Zone(db, 2, "deep", "Deep Mines", "Abandoned shafts glittering with gemstones.",
                new[] { "platinum", "emerald", "ruby" }, 900, 250, 1.5e5, 30,
                "08201f", "1d4a40", "0f302a", "26362f", "3fae7e", "4f6b66", "7dffcf", 104, 55, 2);
            Zone(db, 3, "mantle", "Mantle Layer", "Molten rivers and crushing heat. Diamonds form here.",
                new[] { "diamond", "titanium", "uranium" }, 14000, 3500, 1.2e7, 120,
                "2a0804", "8a2a0a", "40160a", "3a1a10", "ff6a1a", "6a3a2a", "ffaa55", 120, 52, 3);
            Zone(db, 4, "core", "Core Layer", "The heart of the planet. Reality bends around strange materials.",
                new[] { "exotic", "alien", "ancienttech" }, 2.2e5, 5e4, 1.5e9, 500,
                "1f0830", "ff7a2a", "34103a", "2a1030", "ffcc33", "5a2a5a", "ffd0ff", 128, 50, 4);
            var ruins = Zone(db, 5, "ruins", "Ancient Ruins", "A lost underground civilization. Their machines still hum. Guardians still watch.",
                new[] { "rune", "starmetal", "void" }, 4e6, 9e5, 2.5e11, 2000,
                "061420", "1a4a5a", "142636", "22323f", "40e0d0", "3a5a6a", "80fff0", 90, 53, 5);
            ruins.requiresPrestiges = 1;
            var abyss = Zone(db, 6, "abyss", "The Abyss", "A bottomless descent. Every depth is harder, richer and stranger. There is no end.",
                new[] { "abyssal", "darkmatter", "eternium" }, 6e7, 1.2e7, 5e13, 6000,
                "000004", "14002e", "0a0a1a", "0a0a14", "7a2aff", "1c1c30", "9a6aff", 84, 48, 6);
            abyss.isAbyss = true;
            abyss.requiresPrestiges = 1;
        }

        // -------------------------------------------------------------- UPGRADES
        static void Up(GameDatabase db, string id, string name, string desc, UpgradeCategory cat, CurrencyType cur,
            StatModifier mod, double cost, double growth, int max)
        {
            var u = SO<UpgradeDefinition>(id);
            u.id = id; u.displayName = name; u.description = desc; u.category = cat; u.currency = cur;
            u.modifier = mod; u.baseCost = cost; u.costGrowth = growth; u.maxLevel = max;
            db.upgrades.Add(u);
        }

        static void AddUpgrades(GameDatabase db)
        {
            var P = UpgradeCategory.Pickaxe; var B = UpgradeCategory.Backpack; var M = UpgradeCategory.Movement;
            var G = UpgradeCategory.Premium; var X = UpgradeCategory.Prestige;
            var coins = CurrencyType.Coins; var gems = CurrencyType.Gems; var cry = CurrencyType.Crystals;

            // Pickaxe
            Up(db, "pick_power", "Mining Power", "Hit harder. Required to break into deeper zones.", P, coins,
                new StatModifier(StatType.MiningPower, ModifierMode.Curve, 1, 1.1), 10, 1.17, 0);
            Up(db, "pick_crit", "Critical Chance", "Chance for a devastating critical hit.", P, coins,
                StatModifier.Flat(StatType.CritChance, 0.01), 50, 1.35, 45);
            Up(db, "pick_critdmg", "Critical Damage", "Critical hits deal even more damage.", P, coins,
                StatModifier.Flat(StatType.CritDamage, 0.15), 75, 1.3, 100);
            Up(db, "pick_speed", "Mining Speed", "Swing your pickaxe faster.", P, coins,
                StatModifier.Pct(StatType.MiningSpeed, 0.06), 40, 1.32, 50);
            Up(db, "pick_luck", "Luck", "Find rarer rocks and rarer ores more often.", P, coins,
                StatModifier.Pct(StatType.Luck, 0.08), 100, 1.33, 60);
            // Backpack
            Up(db, "bag_capacity", "Capacity", "Carry more ore before selling.", B, coins,
                new StatModifier(StatType.BackpackCapacity, ModifierMode.Quadratic, 10), 25, 1.25, 0);
            Up(db, "bag_autosell", "Auto-Sell Bonus", "Better prices when selling away from the Trading Station.", B, coins,
                StatModifier.Flat(StatType.AutoSellBonus, 0.025), 200, 1.45, 20);
            Up(db, "bag_oremult", "Ore Multiplier", "Rocks drop more ore.", B, coins,
                StatModifier.Pct(StatType.OreMultiplier, 0.1), 500, 1.4, 50);
            // Movement
            Up(db, "move_speed", "Walk Speed", "Move faster around the mine.", M, coins,
                StatModifier.Pct(StatType.WalkSpeed, 0.05), 30, 1.3, 30);
            Up(db, "move_dash", "Dash Cooldown", "Dash more often.", M, coins,
                StatModifier.Flat(StatType.DashCooldown, -0.06), 60, 1.32, 25);
            Up(db, "move_jump", "Jump Height", "Jump higher.", M, coins,
                StatModifier.Pct(StatType.JumpHeight, 0.05), 40, 1.3, 20);
            // Premium (gems) - kept through Rebirth
            Up(db, "gem_golden", "Golden Touch", "Every ore is worth more. Permanent.", G, gems,
                StatModifier.Pct(StatType.OreValue, 0.1), 10, 1.25, 25);
            Up(db, "gem_turbo", "Turbo Swing", "Permanent mining speed.", G, gems,
                StatModifier.Pct(StatType.MiningSpeed, 0.05), 15, 1.3, 20);
            Up(db, "gem_lucky", "Lucky Charm", "Permanent luck.", G, gems,
                StatModifier.Pct(StatType.Luck, 0.1), 15, 1.3, 20);
            Up(db, "gem_night", "Night Shift", "+1 hour of offline progress.", G, gems,
                StatModifier.Flat(StatType.OfflineHours, 1), 20, 1.3, 20);
            Up(db, "gem_overtime", "Overtime Pay", "Workers produce more while you are away.", G, gems,
                StatModifier.Flat(StatType.OfflineEfficiency, 0.05), 25, 1.35, 10);
            Up(db, "gem_petpen", "Pet Pen", "+1 active pet slot.", G, gems,
                StatModifier.Flat(StatType.PetSlots, 1), 100, 2.0, 3);
            Up(db, "gem_magnet", "Gem Magnet", "Find gems in rocks more often.", G, gems,
                StatModifier.Flat(StatType.GemChance, 0.001), 30, 1.35, 10);
            Up(db, "gem_union", "Worker Union", "Workers and drills produce more.", G, gems,
                StatModifier.Pct(StatType.WorkerEfficiency, 0.1), 20, 1.3, 25);
            // Prestige (Ancient Crystals) - permanent account-wide
            Up(db, "cry_might", "Ancient Might", "+50% Mining Power forever.", X, cry,
                StatModifier.Pct(StatType.MiningPower, 0.5), 1, 1.5, 0);
            Up(db, "cry_wealth", "Crystal Wealth", "+25% coins forever.", X, cry,
                StatModifier.Pct(StatType.CoinMultiplier, 0.25), 1, 1.5, 0);
            Up(db, "cry_workers", "Eternal Workforce", "+50% worker output forever.", X, cry,
                StatModifier.Pct(StatType.WorkerEfficiency, 0.5), 2, 1.6, 0);
            Up(db, "cry_drills", "Drill Mastery", "+50% drill output forever.", X, cry,
                StatModifier.Pct(StatType.DrillEfficiency, 0.5), 2, 1.6, 0);
            Up(db, "cry_fortune", "Fortune of Ages", "+20% luck forever.", X, cry,
                StatModifier.Pct(StatType.Luck, 0.2), 3, 1.7, 30);
            Up(db, "cry_headstart", "Head Start", "Begin every run with coins in your pocket.", X, cry,
                new StatModifier(StatType.StartingCoins, ModifierMode.Exponential, 400, 6), 2, 2.0, 15);
            Up(db, "cry_memory", "Deep Memory", "+25% XP forever.", X, cry,
                StatModifier.Pct(StatType.XpMultiplier, 0.25), 2, 1.6, 0);
            Up(db, "cry_resonance", "Crystal Resonance", "+10% Ancient Crystals from every Rebirth.", X, cry,
                StatModifier.Pct(StatType.CrystalGain, 0.1), 5, 1.8, 0);
        }

        // ------------------------------------------------------------- PRODUCERS
        static void AddProducers(GameDatabase db)
        {
            void W(string id, string name, string desc, double cost, double rate, int zone, string col)
            {
                var w = SO<WorkerDefinition>(id);
                w.id = id; w.displayName = name; w.description = desc; w.baseCost = cost; w.costGrowth = 1.15;
                w.ratePerSecond = rate; w.unlockZone = zone; w.color = C(col);
                db.workers.Add(w);
            }
            void D(string id, string name, string desc, double cost, double rate, int zone, string col)
            {
                var d = SO<DrillDefinition>(id);
                d.id = id; d.displayName = name; d.description = desc; d.baseCost = cost; d.costGrowth = 1.2;
                d.ratePerSecond = rate; d.unlockZone = zone; d.color = C(col);
                db.drills.Add(d);
            }
            W("worker_beginner", "Beginner Miner", "Eager, slow, cheap. Mines and delivers ore for you.", 50, 0.25, 0, "f4c27a");
            W("worker_pro", "Professional Miner", "Knows which end of the pickaxe to hold.", 1200, 1.5, 1, "6ab0ff");
            W("worker_drillop", "Drill Operator", "Brings a jackhammer to work.", 40000, 9, 2, "ffd23a");
            W("worker_robot", "Robot Miner", "Never sleeps. Never complains.", 1.5e6, 60, 3, "b8c4d0");
            W("worker_ai", "AI Mining Unit", "A swarm intelligence that optimises every swing.", 8e7, 400, 4, "a06aff");

            D("drill_rusty", "Rusty Drill", "Squeaky, but it keeps digging even while you're offline.", 600, 1.2, 0, "a0623a");
            D("drill_industrial", "Industrial Drill", "A proper diesel-powered borer.", 2e4, 8, 1, "e8b030");
            D("drill_mega", "Mega Drill", "The size of a house.", 8e5, 50, 2, "e04040");
            D("drill_quantum", "Quantum Drill", "Digs in several realities at once.", 4e7, 320, 3, "40e0ff");
            D("drill_alien", "Alien Extractor", "Reverse-engineered from the Core. Pulls ore through solid rock.", 3e9, 2200, 4, "60ff60");
        }

        // ------------------------------------------------------------------ PETS
        static void AddPets(GameDatabase db)
        {
            StatType[] stats = { StatType.MiningSpeed, StatType.Luck, StatType.OreValue, StatType.BackpackCapacity, StatType.CoinMultiplier };
            double[] pct = { 0.05, 0.12, 0.25, 0.5, 1.0 };
            string[,] names =
            {
                { "Pebble Pup", "Dusty Mole", "Copper Crab", "Sack Snail", "Coin Beetle" },
                { "Cave Bat", "Lucky Lizard", "Silver Fox", "Pack Mule", "Gold Finch" },
                { "Crystal Golem", "Clover Sprite", "Ruby Drake", "Hoard Turtle", "Midas Cat" },
                { "Magma Wyrm", "Fortune Phoenix", "Diamond Dragon", "Titan Tortoise", "Treasure Kraken" },
                { "Core Leviathan", "Starlight Kirin", "Void Serpent", "Ancient Behemoth", "Cosmic Whale" }
            };
            string[,] colors =
            {
                { "b0a898", "8a6a4a", "e0784a", "a8c070", "e8c040" },
                { "6a5a8a", "6ad06a", "d0d8e8", "a08060", "ffd040" },
                { "8affff", "60ff90", "ff4070", "4a9a6a", "ffcf3a" },
                { "ff5a1a", "ffa020", "aaf4ff", "6a8a5a", "e04a8a" },
                { "3a6aff", "fff0c0", "7a1aff", "c06a2a", "60d0ff" }
            };
            int[] shapes = { 1, 0, 4, 0, 3, 2, 0, 1, 1, 2, 3, 2, 3, 0, 1, 3, 2, 3, 0, 4, 4, 3, 4, 3, 4 };
            for (int r = 0; r < 5; r++)
                for (int s = 0; s < 5; s++)
                {
                    string id = "pet_" + r + "_" + s;
                    var p = SO<PetDefinition>(id);
                    p.id = id; p.displayName = names[r, s]; p.rarity = (Rarity)r; p.bonusStat = stats[s];
                    p.bonusPercent = pct[r]; p.bodyColor = C(colors[r, s]);
                    p.accentColor = Color.Lerp(p.bodyColor, Color.black, 0.45f);
                    p.shape = shapes[r * 5 + s];
                    db.pets.Add(p);
                }
        }

        // -------------------------------------------------------------- RESEARCH
        static void AddResearch(GameDatabase db)
        {
            var catMult = new[] { 1.0, 1.2, 1.5, 1.0, 2.0, 5.0 };
            void R(ResearchCategory cat, int tier, string name, string desc, params StatModifier[] mods)
            {
                string id = "res_" + cat.ToString().ToLowerInvariant() + "_" + tier;
                var n = SO<ResearchNodeDefinition>(id);
                n.id = id; n.displayName = name; n.description = desc; n.category = cat; n.tier = tier;
                n.costCoins = System.Math.Round(400 * System.Math.Pow(13, tier) * catMult[(int)cat]);
                n.costGems = cat == ResearchCategory.LegendaryEquipment ? 10 * (tier + 1) : (tier >= 6 ? 5 * (tier - 4) : 0);
                n.prerequisiteId = tier > 0 ? "res_" + cat.ToString().ToLowerInvariant() + "_" + (tier - 1) : "";
                n.modifiers = mods;
                db.research.Add(n);
            }
            StatModifier P(StatType s, double v) => StatModifier.Pct(s, v);
            StatModifier F(StatType s, double v) => StatModifier.Flat(s, v);

            var c = ResearchCategory.Mining;
            R(c, 0, "Sharpened Edges", "Grind your pickaxe to a fine edge.", P(StatType.MiningPower, 0.25));
            R(c, 1, "Steady Grip", "Swing with practised rhythm.", P(StatType.MiningSpeed, 0.1));
            R(c, 2, "Weak Points", "Spot the cracks in every rock.", F(StatType.CritChance, 0.03));
            R(c, 3, "Seismic Strikes", "Crits shake the ground.", F(StatType.CritDamage, 0.5));
            R(c, 4, "Auto Swing", "Your miner mines nearby rocks automatically when idle.", F(StatType.AutoMine, 1));
            R(c, 5, "Tempered Steel", "Forge-hardened pickaxe heads.", P(StatType.MiningPower, 0.5));
            R(c, 6, "Shockwave", "Each hit also damages nearby rocks.", F(StatType.Splash, 0.25));
            R(c, 7, "Precision Mastery", "Every swing finds its mark.", F(StatType.CritChance, 0.05));
            R(c, 8, "World Breaker", "Rocks fear you.", P(StatType.MiningPower, 1.0));

            c = ResearchCategory.Economy;
            R(c, 0, "Haggling", "Talk the trader up.", P(StatType.OreValue, 0.1));
            R(c, 1, "Bulk Contracts", "Steady buyers pay more.", P(StatType.CoinMultiplier, 0.15));
            R(c, 2, "Remote Trading", "Better prices when selling from anywhere.", F(StatType.AutoSellBonus, 0.1));
            R(c, 3, "Market Insight", "Know what's in demand.", P(StatType.OreValue, 0.2));
            R(c, 4, "Auto-Seller", "Your backpack sells itself when full.", F(StatType.AutoSell, 1));
            R(c, 5, "Investment Banking", "Money makes money.", P(StatType.CoinMultiplier, 0.3));
            R(c, 6, "Gem Appraisal", "Spot gems hidden inside rocks.", F(StatType.GemChance, 0.003));
            R(c, 7, "Monopoly", "You ARE the market.", P(StatType.OreValue, 0.5));
            R(c, 8, "Golden Empire", "Everything you touch turns to coins.", P(StatType.CoinMultiplier, 1.0));

            c = ResearchCategory.Automation;
            R(c, 0, "Lunch Breaks", "Happy workers mine faster.", P(StatType.WorkerEfficiency, 0.2));
            R(c, 1, "Lubricated Gears", "Drills spin smoother.", P(StatType.DrillEfficiency, 0.2));
            R(c, 2, "Shift Rotation", "Workers keep going while you're away.", F(StatType.OfflineEfficiency, 0.1));
            R(c, 3, "Conveyor Belts", "Ore delivers itself.", P(StatType.WorkerEfficiency, 0.4));
            R(c, 4, "Overclocking", "Push drills past their limits.", P(StatType.DrillEfficiency, 0.5));
            R(c, 5, "Night Crew", "+4 hours of offline progress.", F(StatType.OfflineHours, 4));
            R(c, 6, "Automated Logistics", "Robots manage the workers.", P(StatType.WorkerEfficiency, 0.75));
            R(c, 7, "Fusion Cores", "Drills run on miniature suns.", P(StatType.DrillEfficiency, 1.0));
            R(c, 8, "Singularity Engine", "Workers and drills double their output.",
                P(StatType.WorkerEfficiency, 1.0), P(StatType.DrillEfficiency, 1.0));

            c = ResearchCategory.Exploration;
            R(c, 0, "Trail Shoes", "Comfortable boots.", P(StatType.WalkSpeed, 0.1));
            R(c, 1, "Spring Boots", "Bouncy soles.", P(StatType.JumpHeight, 0.15));
            R(c, 2, "Dash Thrusters", "Shorter dash cooldown.", F(StatType.DashCooldown, -0.2));
            R(c, 3, "Prospector's Eye", "+1 rock spawns in every zone.", F(StatType.RockCount, 1));
            R(c, 4, "Long Reach", "Mine rocks from further away.", F(StatType.MineRange, 1));
            R(c, 5, "Cartography", "Learn from every expedition.", P(StatType.XpMultiplier, 0.25));
            R(c, 6, "Rocket Boots", "Go fast. Go high.", P(StatType.WalkSpeed, 0.2), P(StatType.JumpHeight, 0.2));
            R(c, 7, "Seismic Map", "+2 rocks spawn in every zone.", F(StatType.RockCount, 2));
            R(c, 8, "Master Explorer", "The deep holds no secrets from you.", P(StatType.XpMultiplier, 0.5));

            c = ResearchCategory.RareOres;
            R(c, 0, "Keen Eye", "Rarer rocks appear more often.", P(StatType.Luck, 0.1));
            R(c, 1, "Ore Sense", "Rocks drop more ore.", P(StatType.OreMultiplier, 0.1));
            R(c, 2, "Rare Finder", "Even rarer rocks.", P(StatType.Luck, 0.2));
            R(c, 3, "Vein Tracking", "Follow the vein for more ore.", P(StatType.OreMultiplier, 0.2));
            R(c, 4, "Epic Instinct", "Feel the epic rocks calling.", P(StatType.Luck, 0.3));
            R(c, 5, "Gem Veins", "Gems hide in every crack.", F(StatType.GemChance, 0.005));
            R(c, 6, "Legendary Lure", "Legendary rocks seek you out.", P(StatType.Luck, 0.5));
            R(c, 7, "Motherlode", "Massive ore yields.", P(StatType.OreMultiplier, 0.5));
            R(c, 8, "Mythic Resonance", "Mythic rocks hum your name.", P(StatType.Luck, 1.0));

            c = ResearchCategory.LegendaryEquipment;
            R(c, 0, "Titanium Handle", "A pickaxe that never breaks.", P(StatType.MiningPower, 0.3));
            R(c, 1, "Diamond Tip", "Crits cut like glass.", F(StatType.CritDamage, 1.0));
            R(c, 2, "Bottomless Bag", "+50% backpack size.", P(StatType.BackpackCapacity, 0.5));
            R(c, 3, "Hover Boots", "Glide across the mine.", P(StatType.WalkSpeed, 0.25));
            R(c, 4, "Plasma Edge", "A blade of pure energy.", P(StatType.MiningPower, 0.75));
            R(c, 5, "Quantum Pockets", "+100% backpack size.", P(StatType.BackpackCapacity, 1.0));
            R(c, 6, "Chrono Gauntlet", "Bend time between swings.", P(StatType.MiningSpeed, 0.3));
            R(c, 7, "Relic Hammer", "Forged by the Ancients.", F(StatType.CritChance, 0.05), F(StatType.CritDamage, 1.0));
            R(c, 8, "Excalibur Pickaxe", "The legendary pickaxe of the first miner.", P(StatType.MiningPower, 2.0), P(StatType.PetXp, 1.0));
        }

        // ---------------------------------------------------------------- QUESTS
        static void AddQuests(GameDatabase db)
        {
            int i = 0;
            void Q(string title, QuestKind kind, string target, double amount, double coins, double gems, double xp)
            {
                var q = SO<QuestDefinition>("story_" + i);
                q.id = "story_" + i; q.title = title; q.kind = kind; q.target = target; q.amount = amount;
                q.rewardCoins = coins; q.rewardGems = gems; q.rewardXp = xp;
                db.storyQuests.Add(q);
                i++;
            }
            Q("Mine 100 Stone", QuestKind.CollectOre, "stone", 100, 150, 0, 40);
            Q("Earn 1,000 Coins", QuestKind.EarnCoins, "", 1000, 0, 5, 60);
            Q("Upgrade your Pickaxe", QuestKind.BuyUpgrade, "Pickaxe", 1, 200, 0, 50);
            Q("Hire a Beginner Miner", QuestKind.HireWorker, "", 1, 300, 0, 60);
            Q("Upgrade your Backpack", QuestKind.BuyUpgrade, "Backpack", 1, 400, 3, 80);
            Q("Reach the Cave Layer", QuestKind.ReachZone, "1", 1, 0, 10, 200);
            Q("Collect 10 Gold Ore", QuestKind.CollectOre, "gold", 10, 5000, 0, 300);
            Q("Hatch a Pet", QuestKind.HatchPet, "", 1, 0, 10, 300);
            Q("Complete a Research", QuestKind.Research, "", 1, 10000, 0, 400);
            Q("Buy a Drill", QuestKind.BuyDrill, "", 1, 0, 10, 500);
            Q("Break a Rare Rock", QuestKind.BreakRarity, "1", 1, 0, 5, 500);
            Q("Reach the Deep Mines", QuestKind.ReachZone, "2", 1, 0, 20, 1500);
            Q("Collect 25 Rubies", QuestKind.CollectOre, "ruby", 25, 2e5, 0, 3000);
            Q("Reach Player Level 25", QuestKind.ReachLevel, "", 25, 0, 25, 0);
            Q("Reach the Mantle Layer", QuestKind.ReachZone, "3", 1, 0, 30, 1e4);
            Q("Break an Epic Rock", QuestKind.BreakRarity, "2", 1, 0, 15, 5000);
            Q("Reach the Earth's Core", QuestKind.ReachZone, "4", 1, 0, 50, 5e4);
            Q("Rebirth for the first time", QuestKind.Prestige, "", 1, 0, 100, 0);
            Q("Discover the Ancient Ruins", QuestKind.ReachZone, "5", 1, 0, 100, 2e5);
            Q("Defeat a Hidden Guardian", QuestKind.DefeatBoss, "", 1, 0, 150, 5e5);
            Q("Enter the Abyss", QuestKind.ReachZone, "6", 1, 0, 200, 1e6);
        }

        // ---------------------------------------------------------- ACHIEVEMENTS
        static void AddAchievements(GameDatabase db)
        {
            void A(string id, string name, string desc, AchievementKind kind, double threshold, double gems, bool secret = false)
            {
                var a = SO<AchievementDefinition>(id);
                a.id = id; a.displayName = name; a.description = desc; a.kind = kind; a.threshold = threshold;
                a.rewardGems = gems; a.secret = secret;
                db.achievements.Add(a);
            }
            A("ach_first", "First Swing", "Break your first rock.", AchievementKind.RocksBroken, 1, 2);
            A("ach_rocks_1k", "Rock Crusher", "Break 1,000 rocks.", AchievementKind.RocksBroken, 1000, 10);
            A("ach_rocks_100k", "Mountain Mover", "Break 100,000 rocks.", AchievementKind.RocksBroken, 1e5, 50);
            A("ach_coins_1m", "Millionaire", "Earn 1M coins in total.", AchievementKind.CoinsEarned, 1e6, 10);
            A("ach_coins_1b", "Billionaire", "Earn 1B coins in total.", AchievementKind.CoinsEarned, 1e9, 25);
            A("ach_coins_1t", "Trillionaire", "Earn 1T coins in total.", AchievementKind.CoinsEarned, 1e12, 50);
            A("ach_lvl_50", "Seasoned Miner", "Reach level 50.", AchievementKind.PlayerLevel, 50, 20);
            A("ach_lvl_250", "Veteran", "Reach level 250.", AchievementKind.PlayerLevel, 250, 50);
            A("ach_lvl_999", "Living Legend", "Reach the maximum level, 999.", AchievementKind.PlayerLevel, 999, 500);
            A("ach_core", "Journey to the Center", "Reach the Earth's Core.", AchievementKind.ZoneReached, 4, 30);
            A("ach_prestige_1", "Born Again", "Rebirth once.", AchievementKind.Prestiges, 1, 25);
            A("ach_prestige_10", "Cycle of Ages", "Rebirth 10 times.", AchievementKind.Prestiges, 10, 100);
            A("ach_pets_10", "Pet Collector", "Hatch 10 pets.", AchievementKind.PetsHatched, 10, 10);
            A("ach_pets_100", "Zookeeper", "Hatch 100 pets.", AchievementKind.PetsHatched, 100, 40);
            A("ach_workers_50", "Foreman", "Hire 50 workers in a single run.", AchievementKind.WorkersHired, 50, 15);
            A("ach_research_all", "Omniscient", "Complete every research node.", AchievementKind.ResearchCount, 54, 100);
            // Secret achievements
            A("ach_s_mythic", "Mythic Encounter", "Break a Mythic rock.", AchievementKind.MythicRock, 1, 25, true);
            A("ach_s_mythicpet", "The Chosen One", "Hatch a Mythic pet.", AchievementKind.MythicPet, 1, 50, true);
            A("ach_s_boss", "Guardian Slayer", "Defeat a hidden Guardian.", AchievementKind.BossesDefeated, 1, 50, true);
            A("ach_s_boss25", "Titan Hunter", "Defeat 25 Guardians.", AchievementKind.BossesDefeated, 25, 150, true);
            A("ach_s_abyss10", "Staring into the Void", "Reach Abyss depth 10.", AchievementKind.AbyssDepth, 10, 100, true);
            A("ach_s_abyss50", "Beyond Comprehension", "Reach Abyss depth 50.", AchievementKind.AbyssDepth, 50, 300, true);
            A("ach_s_crits", "Critical Thinker", "Land 10,000 critical hits.", AchievementKind.Crits, 1e4, 30, true);
            A("ach_s_jumps", "Bunny Hop", "Jump 1,000 times.", AchievementKind.Jumps, 1000, 15, true);
        }
    }
}
