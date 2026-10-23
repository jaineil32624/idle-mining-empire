using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Stats, achievements (including secret ones), cosmetics and settings.</summary>
    public class MorePanel : UIPanel
    {
        public override string Title => "More";
        public override string[] Tabs => new[] { "Stats", "Achievements", "Settings" };

        public override void Build(RectTransform content, int tab)
        {
            switch (tab)
            {
                case 0: BuildStats(content); break;
                case 1: BuildAchievements(content); break;
                default: BuildSettings(content); break;
            }
        }

        void BuildStats(RectTransform content)
        {
            Rows.Header(content, "Your Stats");
            var stats = Rows.Paragraph(content, "", 25);
            Rows.Header(content, "Lifetime");
            var life = Rows.Paragraph(content, "", 25);
            ui.AddRefresher(() =>
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < (int)StatType.Count; i++)
                {
                    var s = (StatType)i;
                    if (s == StatType.StartingCoins && gm.Stat(s) <= 0) continue;
                    sb.Append(StatInfo.Name(s)).Append(": ").Append(UIFactory.Colorize(StatInfo.FormatFinal(s, gm.Stat(s)), UIFactory.Gold));
                    sb.Append(i % 2 == 0 ? "        " : "\n");
                }
                stats.text = sb.ToString();
                var st = gm.Save.stats;
                life.text =
                    "Rocks broken: " + NumberFormatter.Format(st.rocksBroken) + "      Ores mined: " + NumberFormatter.Format(st.oresMined) + "\n" +
                    "Coins earned: " + NumberFormatter.Format(st.coinsEarned) + "      Critical hits: " + NumberFormatter.Format(st.crits) + "\n" +
                    "Mythic rocks: " + NumberFormatter.Format(st.mythicRocks) + "      Guardians defeated: " + NumberFormatter.Format(st.bossesDefeated) + "\n" +
                    "Pets hatched: " + NumberFormatter.Format(st.petsHatched) + "      Rebirths: " + NumberFormatter.Format(st.prestiges) + "\n" +
                    "Deepest Abyss depth: " + NumberFormatter.Format(st.maxAbyssDepth) + "      Time played: " + NumberFormatter.Time(st.playSeconds) + "\n" +
                    "Achievements: " + gm.Achievements.UnlockedCount + " / " + gm.Db.achievements.Count;
            });
        }

        void BuildAchievements(RectTransform content)
        {
            foreach (var a in gm.Db.achievements)
            {
                bool got = gm.Achievements.IsUnlocked(a);
                bool hidden = a.secret && !got;
                var row = Rows.Make(content, hidden ? SpriteFactory.LockIcon : SpriteFactory.StarIcon,
                    hidden ? "??? Secret Achievement" : a.displayName,
                    hidden ? "Keep exploring to discover this one..." : a.description + "   (+" + NumberFormatter.Format(a.rewardGems) + " gems)",
                    100, got ? new Color(0.15f, 0.3f, 0.2f) : (Color?)null, got ? Color.white : new Color(0.4f, 0.4f, 0.45f));
                if (!hidden && !got)
                {
                    var prog = UIFactory.Txt(row.buttons, "", 24, UIFactory.TextDim, TextAnchor.MiddleRight, true);
                    UIFactory.LE(prog.gameObject, 220, 60);
                    var def = a;
                    ui.AddRefresher(() => prog.text = NumberFormatter.Format(System.Math.Min(def.threshold, gm.Achievements.Value(def.kind))) + " / " + NumberFormatter.Format(def.threshold));
                }
                else if (got)
                {
                    var t = UIFactory.Txt(row.buttons, "UNLOCKED", 24, UIFactory.Green, TextAnchor.MiddleRight, true);
                    UIFactory.LE(t.gameObject, 200, 60);
                }
            }
        }

        void BuildSettings(RectTransform content)
        {
            Toggle(content, "Music", () => gm.Save.musicOn, v => { gm.Save.musicOn = v; gm.Audio.ApplyMusicSetting(); });
            Toggle(content, "Sound Effects", () => gm.Save.sfxOn, v => gm.Save.sfxOn = v);
            Toggle(content, "Reduced Effects (faster on old phones)", () => gm.Save.lowFx, v => gm.Save.lowFx = v);

            var touch = Rows.Make(content, SpriteFactory.Arrow, "Touch Controls", "On-screen move / jump / dash buttons.", 110);
            var tb = Rows.Button(touch, "", UIFactory.Blue, () => { gm.Save.touchControls = (gm.Save.touchControls + 1) % 3; }, 220);
            ui.AddRefresher(() => tb.Set(gm.Save.touchControls == 0 ? "Auto" : gm.Save.touchControls == 1 ? "Always On" : "Off", true));

            var cos = Rows.Make(content, SpriteFactory.Pickaxe(gm.Levels.CurrentCosmetic.head), "Cosmetic", "", 120);
            var cb = Rows.Button(cos, "Next", UIFactory.Purple, CycleCosmetic, 200);
            ui.AddRefresher(() =>
            {
                var c = gm.Levels.CurrentCosmetic;
                cos.title.text = "Cosmetic: " + UIFactory.Colorize(c.name, c.head);
                cos.sub.text = "Unlocked " + (gm.Levels.BestCosmeticIndex + 1) + " / " + LevelManager.Cosmetics.Length + " outfits. New outfits unlock as you level up.";
                if (cos.icon != null) cos.icon.sprite = SpriteFactory.Pickaxe(c.head);
            });

            var save = Rows.Make(content, SpriteFactory.BagIcon, "Save Game", "Auto-saves every " + SaveManager.AutoSaveInterval + " seconds. Saves are stored on this device" +
                (Application.platform == RuntimePlatform.WebGLPlayer ? " in your browser." : ":\n" + SaveManager.FilePath), 120);
            Rows.Button(save, "Save Now", UIFactory.Green, () => { gm.Saves.SaveNow(); ui.Toast("Game saved."); }, 220);

            var reset = Rows.Make(content, SpriteFactory.LockIcon, "Reset Progress", "Deletes your save and starts over. Cannot be undone!", 110, new Color(0.3f, 0.12f, 0.12f));
            Rows.Button(reset, "Reset", UIFactory.Red, () =>
                ui.ShowConfirm("Reset everything?", "ALL progress, pets, crystals and gems will be permanently deleted.", "Delete", () =>
                    ui.ShowConfirm("Are you absolutely sure?", "This is your last chance.", "Yes, reset", gm.ResetProgress, UIFactory.Red), UIFactory.Red), 200);

            Rows.Paragraph(content, UIFactory.Colorize("Idle Mining Empire v" + Application.version + "  |  Built with Unity " + Application.unityVersion +
                                                       "\nControls: A/D move, Space jump, Shift dash, click/hold rocks or hold E to mine, Q sell, 1-9 menus, Esc close.", UIFactory.TextDim), 22);
        }

        void CycleCosmetic()
        {
            int best = gm.Levels.BestCosmeticIndex;
            int cur = gm.Save.selectedCosmetic < 0 ? best : gm.Save.selectedCosmetic;
            gm.Save.selectedCosmetic = (cur + 1) % (best + 1);
            gm.Player.RefreshLook();
        }

        void Toggle(RectTransform content, string label, System.Func<bool> get, System.Action<bool> set)
        {
            var row = Rows.Make(content, SpriteFactory.StarIcon, label, "", 100);
            var b = Rows.Button(row, "", UIFactory.Green, () => set(!get()), 200);
            ui.AddRefresher(() => b.Set(get() ? "ON" : "OFF", true, get() ? UIFactory.Green : UIFactory.PanelLight));
        }
    }
}
