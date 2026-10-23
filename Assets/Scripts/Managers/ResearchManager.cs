using System.Collections.Generic;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>The 54-node research tree across six categories.</summary>
    public class ResearchManager : MonoBehaviour, IStatProvider
    {
        public const int UnlockLevel = 5;
        GameManager gm;
        readonly HashSet<string> done = new HashSet<string>();

        public void Init(GameManager g)
        {
            gm = g;
            Reload();
        }

        public void Reload()
        {
            done.Clear();
            foreach (var id in gm.Save.research) done.Add(id);
            GameEvents.RaiseStatsChanged();
        }

        public int CompletedCount => done.Count;
        public bool IsDone(ResearchNodeDefinition n) => done.Contains(n.id);

        public bool PrereqMet(ResearchNodeDefinition n) =>
            string.IsNullOrEmpty(n.prerequisiteId) || done.Contains(n.prerequisiteId);

        public bool FeatureUnlocked => gm.Save.playerLevel >= UnlockLevel;

        public bool CanResearch(ResearchNodeDefinition n) =>
            FeatureUnlocked && !IsDone(n) && PrereqMet(n) &&
            gm.Currency.CanAfford(CurrencyType.Coins, n.costCoins) &&
            gm.Currency.CanAfford(CurrencyType.Gems, n.costGems);

        public bool TryResearch(ResearchNodeDefinition n)
        {
            if (!CanResearch(n)) { gm.Audio.Play(Sfx.Error); return false; }
            gm.Currency.TrySpend(CurrencyType.Coins, n.costCoins);
            gm.Currency.TrySpend(CurrencyType.Gems, n.costGems);
            done.Add(n.id);
            gm.Save.research.Add(n.id);
            gm.Audio.Play(Sfx.LevelUp);
            GameEvents.RaiseStatsChanged();
            GameEvents.RaiseResearchCompleted(n);
            GameEvents.RaiseToast("Research complete: " + n.displayName);
            return true;
        }

        public void ContributeStats(StatSheet sheet)
        {
            foreach (var n in gm.Db.research)
            {
                if (!done.Contains(n.id) || n.modifiers == null) continue;
                foreach (var m in n.modifiers) m.Apply(sheet, 1);
            }
        }

        public void ResetAll()
        {
            gm.Save.research.Clear();
            Reload();
        }

        public static string CategoryName(ResearchCategory c)
        {
            switch (c)
            {
                case ResearchCategory.RareOres: return "Rare Ores";
                case ResearchCategory.LegendaryEquipment: return "Legendary Gear";
                default: return c.ToString();
            }
        }

        public static Color CategoryColor(ResearchCategory c)
        {
            switch (c)
            {
                case ResearchCategory.Mining: return new Color(0.95f, 0.45f, 0.3f);
                case ResearchCategory.Economy: return new Color(1f, 0.8f, 0.2f);
                case ResearchCategory.Automation: return new Color(0.4f, 0.75f, 1f);
                case ResearchCategory.Exploration: return new Color(0.4f, 0.9f, 0.5f);
                case ResearchCategory.RareOres: return new Color(0.75f, 0.5f, 1f);
                default: return new Color(1f, 0.5f, 0.8f);
            }
        }
    }
}
