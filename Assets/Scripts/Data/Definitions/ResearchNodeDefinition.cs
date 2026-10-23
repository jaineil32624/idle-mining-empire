using UnityEngine;

namespace IdleMiningEmpire
{
    [CreateAssetMenu(menuName = "Idle Mining Empire/Research Node", fileName = "Research")]
    public class ResearchNodeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public ResearchCategory category;
        public int tier;
        public double costCoins;
        public double costGems;
        public string prerequisiteId;
        public StatModifier[] modifiers;
    }
}
