using UnityEngine;

namespace IdleMiningEmpire
{
    [CreateAssetMenu(menuName = "Idle Mining Empire/Ore", fileName = "Ore")]
    public class OreDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Color color = Color.gray;
        public Color highlight = Color.white;
        [Tooltip("Coins earned per ore before multipliers.")]
        public double baseValue = 1;
        public int zoneIndex;
        [Tooltip("Abyss ores scale with your deepest Abyss depth.")]
        public bool scalesWithAbyss;
    }
}
