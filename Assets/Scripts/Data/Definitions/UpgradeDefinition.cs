using System;
using UnityEngine;

namespace IdleMiningEmpire
{
    [CreateAssetMenu(menuName = "Idle Mining Empire/Upgrade", fileName = "Upgrade")]
    public class UpgradeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public UpgradeCategory category;
        public CurrencyType currency = CurrencyType.Coins;
        public StatModifier modifier;
        public double baseCost = 10;
        public double costGrowth = 1.15;
        [Tooltip("0 = unlimited")]
        public int maxLevel;

        public double CostAt(int level) => Math.Ceiling(baseCost * Math.Pow(costGrowth, level));
        public bool IsMaxed(int level) => maxLevel > 0 && level >= maxLevel;
    }
}
