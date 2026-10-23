using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>A story questline plus three rotating, auto-scaling side quests.</summary>
    public class QuestManager : MonoBehaviour
    {
        public const int SideQuestCount = 3;
        GameManager gm;
        SaveData S => gm.Save;

        public void Init(GameManager g)
        {
            gm = g;
            GameEvents.OreCollected += (o, n) => Progress(QuestKind.CollectOre, o.id, n);
            GameEvents.CoinsEarned += c => Progress(QuestKind.EarnCoins, null, c);
            GameEvents.UpgradeBought += (u, l) => Progress(QuestKind.BuyUpgrade, u.category.ToString(), 1);
            GameEvents.RockBroken += OnRock;
            GameEvents.PetHatched += p => Progress(QuestKind.HatchPet, null, 1);
            GameEvents.ResearchCompleted += r => Progress(QuestKind.Research, null, 1);
            GameEvents.ProducerBought += p =>
            {
                if (p is WorkerDefinition) Progress(QuestKind.HireWorker, null, 1);
                else Progress(QuestKind.BuyDrill, null, 1);
            };
            GameEvents.PrestigeDone += () => Progress(QuestKind.Prestige, null, 1);
            GameEvents.ZoneUnlocked += z => RefreshPassiveProgress();
            GameEvents.LevelUp += l => RefreshPassiveProgress();
            EnsureQuests();
        }

        public List<QuestState> Active => S.quests;

        void EnsureQuests()
        {
            S.quests.RemoveAll(q => q == null || q.claimed);
            bool hasStory = S.quests.Exists(q => q.isStory);
            if (!hasStory && S.storyIndex < gm.Db.storyQuests.Count)
            {
                var d = gm.Db.storyQuests[S.storyIndex];
                S.quests.Insert(0, new QuestState
                {
                    id = d.id, isStory = true, title = d.title, kind = d.kind, target = d.target, amount = d.amount,
                    rewardCoins = d.rewardCoins, rewardGems = d.rewardGems, rewardXp = d.rewardXp
                });
            }
            int side = 0;
            foreach (var q in S.quests) if (!q.isStory) side++;
            while (side < SideQuestCount) { S.quests.Add(GenerateSideQuest()); side++; }
            RefreshPassiveProgress();
        }

        QuestState GenerateSideQuest()
        {
            int z = Mathf.Clamp(S.unlockedZone, 0, gm.Db.zones.Count - 1);
            double factor = gm.Zones.ValueFactor(z);
            double scale = 1 + z * 0.35;
            var q = new QuestState { id = "side_" + (++S.generatedQuestCounter), isStory = false };
            int pick = UnityEngine.Random.Range(0, 5);
            switch (pick)
            {
                case 0:
                    q.kind = QuestKind.BreakRocks;
                    q.amount = Math.Round(UnityEngine.Random.Range(25, 80) * scale);
                    q.title = "Break " + q.amount + " rocks";
                    break;
                case 1:
                {
                    var zone = gm.Db.zones[UnityEngine.Random.Range(Math.Max(0, z - 1), z + 1)];
                    string oreId = zone.oreIds[UnityEngine.Random.Range(0, zone.oreIds.Length)];
                    var ore = gm.Db.GetOre(oreId);
                    q.kind = QuestKind.CollectOre;
                    q.target = oreId;
                    q.amount = Math.Round(UnityEngine.Random.Range(15, 50) * scale);
                    q.title = "Collect " + q.amount + " " + (ore != null ? ore.displayName : oreId);
                    break;
                }
                case 2:
                    q.kind = QuestKind.EarnCoins;
                    q.amount = Math.Round(factor * UnityEngine.Random.Range(300, 900) * scale);
                    q.title = "Earn " + NumberFormatter.Format(q.amount) + " coins";
                    break;
                case 3:
                {
                    int minR = z >= 3 ? 2 : 1;
                    q.kind = QuestKind.BreakRarity;
                    q.target = minR.ToString();
                    q.amount = minR == 1 ? UnityEngine.Random.Range(3, 9) : UnityEngine.Random.Range(1, 4);
                    q.title = "Break " + q.amount + " " + (Rarity)minR + "+ rocks";
                    break;
                }
                default:
                    q.kind = QuestKind.BuyUpgrade;
                    q.target = "";
                    q.amount = UnityEngine.Random.Range(3, 8);
                    q.title = "Buy " + q.amount + " upgrades";
                    break;
            }
            q.rewardCoins = Math.Round(factor * 120 * scale * UnityEngine.Random.Range(0.8f, 1.4f));
            q.rewardGems = UnityEngine.Random.Range(2, 7);
            q.rewardXp = Math.Round(LevelManager.XpToNext(S.playerLevel) * 0.2);
            return q;
        }

        void OnRock(Rarity r, bool boss)
        {
            Progress(QuestKind.BreakRocks, null, 1);
            foreach (var q in S.quests)
                if (q.kind == QuestKind.BreakRarity && int.TryParse(q.target, out int min) && (int)r >= min)
                    Add(q, 1);
            if (boss) Progress(QuestKind.DefeatBoss, null, 1);
        }

        void Progress(QuestKind kind, string target, double amount)
        {
            foreach (var q in S.quests)
            {
                if (q.kind != kind || q.claimed) continue;
                if (kind == QuestKind.CollectOre && q.target != target) continue;
                if (kind == QuestKind.BuyUpgrade && !string.IsNullOrEmpty(q.target) && q.target != target) continue;
                Add(q, amount);
            }
        }

        void Add(QuestState q, double amount)
        {
            bool wasDone = IsComplete(q);
            q.progress = Math.Min(q.amount, q.progress + amount);
            if (!wasDone && IsComplete(q))
            {
                gm.Audio.Play(Sfx.Quest);
                GameEvents.RaiseToast("Quest complete: " + q.title + " (claim it!)");
            }
        }

        public void RefreshPassiveProgress()
        {
            foreach (var q in S.quests)
            {
                double p = q.progress;
                if (q.kind == QuestKind.ReachZone && int.TryParse(q.target, out int zi))
                    p = S.unlockedZone >= zi ? 1 : 0;
                else if (q.kind == QuestKind.ReachLevel)
                    p = Math.Min(q.amount, S.playerLevel);
                else if (q.kind == QuestKind.Prestige && S.stats.prestiges >= q.amount)
                    p = q.amount;
                if (p > q.progress) Add(q, p - q.progress);
            }
        }

        public bool IsComplete(QuestState q) => q.progress >= q.amount - 1e-9;

        public void Claim(QuestState q)
        {
            if (q.claimed || !IsComplete(q)) { gm.Audio.Play(Sfx.Error); return; }
            q.claimed = true;
            if (q.rewardCoins > 0) gm.Currency.AddCoins(q.rewardCoins, false);
            if (q.rewardGems > 0) gm.Currency.AddGems(q.rewardGems);
            if (q.rewardXp > 0) gm.Levels.AddXp(q.rewardXp);
            if (q.isStory) S.storyIndex++;
            gm.Audio.Play(Sfx.Buy);
            gm.Fx.Confetti();
            EnsureQuests();
        }

        public int ClaimableCount
        {
            get
            {
                int n = 0;
                foreach (var q in S.quests) if (!q.claimed && IsComplete(q)) n++;
                return n;
            }
        }

        public QuestState Story => S.quests.Find(q => q.isStory && !q.claimed);

        public static string RewardText(QuestState q)
        {
            var parts = new List<string>();
            if (q.rewardCoins > 0) parts.Add(NumberFormatter.Format(q.rewardCoins) + " coins");
            if (q.rewardGems > 0) parts.Add(NumberFormatter.Format(q.rewardGems) + " gems");
            if (q.rewardXp > 0) parts.Add(NumberFormatter.Format(q.rewardXp) + " XP");
            return string.Join(" + ", parts);
        }
    }
}
