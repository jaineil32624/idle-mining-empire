using UnityEngine;

namespace IdleMiningEmpire
{
    [CreateAssetMenu(menuName = "Idle Mining Empire/Zone", fileName = "Zone")]
    public class ZoneDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public int index;
        public string[] oreIds = new string[3];

        [Header("Difficulty")]
        public double baseRockHp = 5;
        [Tooltip("Mining Power required to unlock and mine efficiently.")]
        public double hardness = 1;
        public double unlockCost;
        public double xpBase = 2;
        public int requiresPrestiges;
        [Tooltip("The infinite endgame zone: HP / value scale with depth.")]
        public bool isAbyss;

        [Header("Look")]
        public Color skyTop = new Color(0.45f, 0.75f, 1f);
        public Color skyBottom = new Color(0.8f, 0.92f, 1f);
        public Color wallColor = new Color(0.35f, 0.3f, 0.25f);
        public Color groundColor = new Color(0.4f, 0.3f, 0.2f);
        public Color groundTop = new Color(0.35f, 0.75f, 0.3f);
        public Color rockColor = new Color(0.55f, 0.55f, 0.58f);
        public Color ambientColor = new Color(1f, 1f, 1f, 0.3f);

        [Header("Music")]
        public float bpm = 110;
        public int rootMidi = 57;
        public int scale; // index into SoundFactory scales
        public int musicSeed = 1;
    }
}
