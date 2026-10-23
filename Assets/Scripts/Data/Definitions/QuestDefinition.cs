using UnityEngine;

namespace IdleMiningEmpire
{
    [CreateAssetMenu(menuName = "Idle Mining Empire/Quest", fileName = "Quest")]
    public class QuestDefinition : ScriptableObject
    {
        public string id;
        public string title;
        public QuestKind kind;
        [Tooltip("Ore id, zone index, rarity index, or upgrade category depending on kind.")]
        public string target;
        public double amount = 1;
        public double rewardCoins;
        public double rewardGems;
        public double rewardXp;
    }
}
