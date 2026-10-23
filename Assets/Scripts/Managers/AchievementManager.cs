using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Normal and secret achievements, checked once per second.</summary>
    public class AchievementManager : MonoBehaviour
    {
        GameManager gm;
        float timer;

        public void Init(GameManager g) { gm = g; }

        public bool IsUnlocked(AchievementDefinition a) => gm.Save.achievements.Contains(a.id);

        public double Value(AchievementKind k)
        {
            var s = gm.Save;
            var st = s.stats;
            switch (k)
            {
                case AchievementKind.RocksBroken: return st.rocksBroken;
                case AchievementKind.CoinsEarned: return st.coinsEarned;
                case AchievementKind.PlayerLevel: return s.playerLevel;
                case AchievementKind.Prestiges: return st.prestiges;
                case AchievementKind.PetsHatched: return st.petsHatched;
                case AchievementKind.MythicPet: return st.mythicPets;
                case AchievementKind.MythicRock: return st.mythicRocks;
                case AchievementKind.BossesDefeated: return st.bossesDefeated;
                case AchievementKind.AbyssDepth: return st.maxAbyssDepth;
                case AchievementKind.Crits: return st.crits;
                case AchievementKind.Jumps: return st.jumps;
                case AchievementKind.ZoneReached: return st.deepestZoneEver;
                case AchievementKind.ResearchCount: return s.research.Count;
                case AchievementKind.WorkersHired: return s.workersHiredThisRun;
                default: return 0;
            }
        }

        void Update()
        {
            if (gm == null || !gm.Ready) return;
            timer += Time.unscaledDeltaTime;
            if (timer < 1f) return;
            timer = 0;
            foreach (var a in gm.Db.achievements)
            {
                if (IsUnlocked(a)) continue;
                if (Value(a.kind) >= a.threshold)
                {
                    gm.Save.achievements.Add(a.id);
                    gm.Currency.AddGems(a.rewardGems);
                    gm.Audio.Play(Sfx.Achievement);
                    GameEvents.RaiseToast((a.secret ? "SECRET achievement: " : "Achievement: ") + a.displayName +
                                          "  (+" + NumberFormatter.Format(a.rewardGems) + " gems)");
                }
            }
        }

        public int UnlockedCount => gm.Save.achievements.Count;
    }
}
