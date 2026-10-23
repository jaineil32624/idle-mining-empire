using UnityEngine;

namespace IdleMiningEmpire
{
    [CreateAssetMenu(menuName = "Idle Mining Empire/Achievement", fileName = "Achievement")]
    public class AchievementDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string description;
        public AchievementKind kind;
        public double threshold = 1;
        public bool secret;
        public double rewardGems = 5;
    }
}
