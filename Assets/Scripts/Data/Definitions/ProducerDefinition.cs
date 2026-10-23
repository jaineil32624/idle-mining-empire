using System;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Shared base for anything that produces ore automatically (workers and drills).</summary>
    public abstract class ProducerDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public double baseCost = 50;
        public double costGrowth = 1.15;
        [Tooltip("Ore-units per second (multiplied by the deepest zone's ore value).")]
        public double ratePerSecond = 0.2;
        [Tooltip("Zone index that must be unlocked to purchase.")]
        public int unlockZone;
        public Color color = Color.white;

        public double CostAt(int owned) => Math.Ceiling(baseCost * Math.Pow(costGrowth, owned));
    }
}
