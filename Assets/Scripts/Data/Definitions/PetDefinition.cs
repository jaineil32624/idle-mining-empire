using UnityEngine;

namespace IdleMiningEmpire
{
    [CreateAssetMenu(menuName = "Idle Mining Empire/Pet", fileName = "Pet")]
    public class PetDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Rarity rarity;
        public StatType bonusStat;
        [Tooltip("Percent bonus at level 1 (0.1 = +10%).")]
        public double bonusPercent = 0.05;
        public Color bodyColor = Color.white;
        public Color accentColor = Color.black;
        [Tooltip("0 blob, 1 eared, 2 winged, 3 horned, 4 tentacled")]
        public int shape;
    }
}
