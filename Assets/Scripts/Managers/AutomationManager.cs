using System.Collections.Generic;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Workers (online, reduced offline) and drills (full speed offline) that mine and deliver ore automatically.</summary>
    public class AutomationManager : MonoBehaviour
    {
        GameManager gm;
        double pendingCoins, pendingOres;
        float flushTimer;
        readonly List<GameObject> visuals = new List<GameObject>();

        public void Init(GameManager g)
        {
            gm = g;
            GameEvents.ProducerBought += p => RebuildVisuals();
            GameEvents.ZoneEntered += z => RebuildVisuals();
            GameEvents.PrestigeDone += RebuildVisuals;
        }

        public int Owned(ProducerDefinition p) =>
            SaveData.GetLevel(p is DrillDefinition ? gm.Save.drills : gm.Save.workers, p.id);

        public double Cost(ProducerDefinition p) => p.CostAt(Owned(p));

        public bool IsUnlocked(ProducerDefinition p)
        {
            int needLevel = p is DrillDefinition ? LevelManager.DrillsUnlockLevel : LevelManager.WorkersUnlockLevel;
            return gm.Save.playerLevel >= needLevel && gm.Save.unlockedZone >= p.unlockZone;
        }

        public string LockReason(ProducerDefinition p)
        {
            int needLevel = p is DrillDefinition ? LevelManager.DrillsUnlockLevel : LevelManager.WorkersUnlockLevel;
            if (gm.Save.playerLevel < needLevel) return "Unlocks at Lv " + needLevel;
            return "Reach " + gm.Db.zones[p.unlockZone].displayName;
        }

        public bool Buy(ProducerDefinition p)
        {
            if (!IsUnlocked(p) || !gm.Currency.TrySpend(CurrencyType.Coins, Cost(p)))
            {
                gm.Audio.Play(Sfx.Error);
                return false;
            }
            var list = p is DrillDefinition ? gm.Save.drills : gm.Save.workers;
            SaveData.SetLevel(list, p.id, Owned(p) + 1);
            if (p is WorkerDefinition) gm.Save.workersHiredThisRun++;
            gm.Audio.Play(Sfx.Buy);
            GameEvents.RaiseProducerBought(p);
            return true;
        }

        double RawUnits(bool drills)
        {
            double units = 0;
            if (drills) foreach (var d in gm.Db.drills) units += Owned(d) * d.ratePerSecond;
            else foreach (var w in gm.Db.workers) units += Owned(w) * w.ratePerSecond;
            return units * gm.Stat(drills ? StatType.DrillEfficiency : StatType.WorkerEfficiency);
        }

        double CoinFactor => gm.Zones.ValueFactor(gm.Save.unlockedZone) * gm.Stat(StatType.OreValue) * gm.Stat(StatType.CoinMultiplier);

        public double WorkerCoinsPerSecond => RawUnits(false) * CoinFactor;
        public double DrillCoinsPerSecond => RawUnits(true) * CoinFactor;
        public double CoinsPerSecond => WorkerCoinsPerSecond + DrillCoinsPerSecond;
        public double OresPerSecond => RawUnits(false) + RawUnits(true);

        /// <summary>Offline result for a duration (seconds already capped by caller).</summary>
        public void CalcOffline(double seconds, out double coins, out double ores)
        {
            double eff = gm.Stat(StatType.OfflineEfficiency);
            coins = (WorkerCoinsPerSecond * eff + DrillCoinsPerSecond) * seconds;
            ores = (RawUnits(false) * eff + RawUnits(true)) * seconds;
        }

        void Update()
        {
            if (gm == null || !gm.Ready) return;
            float dt = Time.deltaTime;
            pendingCoins += CoinsPerSecond * dt;
            pendingOres += OresPerSecond * dt;
            flushTimer += dt;
            if (flushTimer >= 0.5f)
            {
                flushTimer = 0;
                if (pendingCoins > 0) gm.Currency.AddCoins(pendingCoins);
                gm.Save.stats.oresMined += pendingOres;
                pendingCoins = 0;
                pendingOres = 0;
            }
        }

        public void RebuildVisuals()
        {
            foreach (var v in visuals) if (v != null) Destroy(v);
            visuals.Clear();
            if (gm.Zones == null || !gm.Zones.Built) return;
            float x = 14f;
            foreach (var w in gm.Db.workers)
            {
                int n = Owned(w);
                if (n <= 0) continue;
                visuals.Add(WorkerAvatar.Create(gm, w, n, x).gameObject);
                x -= 2.4f;
            }
            float dx = -6f;
            foreach (var d in gm.Db.drills)
            {
                int n = Owned(d);
                if (n <= 0) continue;
                visuals.Add(DrillMachine.Create(gm, d, n, dx).gameObject);
                dx += 3.2f;
            }
        }
    }
}
