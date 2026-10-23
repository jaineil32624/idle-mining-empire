# ⛏️ Idle Mining Empire

A 2D idle mining tycoon made in **Unity 6**. Mine rocks, sell ore, buy upgrades, hire workers, build drills, hatch pets, research a 54-node tech tree, dig all the way to the Earth's Core, Rebirth for Ancient Crystals, then go down into the Abyss, which never ends.

Runs on **Windows**, **Android** and **WebGL**. The WebGL build is playable in the browser on GitHub Pages and itch.io.

**Play in your browser:** https://jaineil32624.github.io/idle-mining-empire/ (works once a WebGL build has been pushed, see below)

---

## Features

| System | What's in it |
|---|---|
| **World** | 7 zones going down: Surface, Cave Layer, Deep Mines, Mantle, Core, Ancient Ruins (unlocked by Rebirth), and the Abyss, which has infinite depths. Each zone has its own sky, parallax cave layers, rock look, ambient particles and generated chiptune music. |
| **Mining** | Health bars, hit flash and squash, particles, ore pop-outs that get pulled into your backpack, floating damage numbers, critical hits, screen shake and synthesized sound effects. |
| **Rock rarities** | Common, Rare, Epic, Legendary, Mythic. Rarer rocks have more HP, glow, and drop a lot more. |
| **Ores** | 21 ores, from Stone to Ancient Technology, Void Essence and Eternium. |
| **Upgrades** | Pickaxe: Power, Crit Chance, Crit Damage, Speed, Luck. Backpack: Capacity, Auto-Sell Bonus, Ore Multiplier. Movement: Walk Speed, Dash Cooldown, Jump Height. Premium upgrades cost gems. |
| **Currencies** | Coins, Gems (premium), Ancient Crystals (prestige). |
| **Automation** | Workers: Beginner Miner, Professional Miner, Drill Operator, Robot Miner, AI Mining Unit. Drills: Rusty, Industrial, Mega, Quantum, Alien Extractor. |
| **Offline progress** | A "While away for 3h…" screen showing coins earned and ores mined. You can pay gems to double it. |
| **Research tree** | 54 nodes across Mining, Economy, Automation, Exploration, Rare Ores and Legendary Equipment. Unlocks things like auto-mine, auto-sell and splash damage. |
| **Pets** | 25 pets in 5 rarities with 3 egg types. Bonuses to Mining Speed, Luck, Ore Value, Backpack Size and Coins. Pets level up automatically and follow you around. |
| **Quests** | A 21-step story questline plus side quests that rotate and scale with your progress. |
| **Leveling** | Max level 999. Stat boosts, 16 titles, 8 cosmetic outfits, and features that unlock as you level. |
| **Prestige** | Rebirth after reaching the Core. Resets coins, ores, equipment and workers. Keeps crystals, research, pets, gem upgrades and cosmetics. |
| **Endgame** | Ancient Ruins with hidden Guardian bosses, an Abyss Warden every 10 depths, 24 achievements (8 of them secret), and progression that keeps scaling. |
| **UI** | Scales for phone, tablet and desktop, in landscape and portrait, and respects the safe area. On-screen touch controls appear automatically on mobile. |
| **Saving** | JSON save that auto-saves every 30 seconds and keeps a backup file. In the browser it saves to the browser's storage. |

Every sprite, sound and music track is **generated in code**. There are no imported art or audio files, so the repo is small and the WebGL download loads quickly.

## Controls

| | PC | Mobile |
|---|---|---|
| Move | A / D or arrow keys | On-screen arrows, or tap the ground |
| Mine | Click or hold on a rock, or hold **E** | Tap a rock |
| Jump / Dash | Space / Shift | On-screen buttons |
| Sell | **Q** or the SELL button | SELL button |
| Menus | **1–9**, Esc to close | Bottom bar |

Selling at the **Trading Post** on the Surface pays 100%. Selling anywhere else pays a reduced rate, which you can upgrade.

---

## Installation (Unity Hub)

1. Install **Unity Hub** and **Unity 6 (6000.0 LTS)**. When installing, add these modules: **WebGL Build Support**, **Windows Build Support** and **Android Build Support** (the Android one includes the OpenJDK and Android SDK/NDK options).
2. Get the project:
   ```bash
   git clone https://github.com/jaineil32624/idle-mining-empire.git
   ```
3. In Unity Hub, choose **Add → Add project from disk** and pick the `idle-mining-empire` folder.
   If Hub asks about the editor version, choose any **Unity 6000.0.x** you have installed.
4. Open the project. The first import takes a few minutes. After that, a setup script runs automatically: it creates `Assets/Scenes/Main.unity`, adds it to Build Settings, and applies the player settings.
5. Open `Assets/Scenes/Main.unity` and press **Play**.

> The game builds its whole scene at runtime, so pressing Play in any scene, even an empty one, starts it.

## Build instructions

Use the **Idle Mining Empire → Build** menu in the Unity menu bar:

| Menu item | Output |
|---|---|
| **WebGL** | `docs/`, ready for GitHub Pages and itch.io |
| **Windows** | `Builds/Windows/IdleMiningEmpire.exe` |
| **Android APK** | `Builds/Android/IdleMiningEmpire.apk` |
| **Android App Bundle** | `Builds/Android/IdleMiningEmpire.aab` (for Google Play) |

### Publish to GitHub Pages
1. Run **Idle Mining Empire → Build → WebGL**.
2. Commit and push the `docs/` folder.
3. On GitHub, go to **Settings → Pages → Source: GitHub Actions**. The `Deploy WebGL to GitHub Pages` workflow publishes it. If you'd rather not use Actions, pick **Deploy from a branch → main → /docs** instead.

The build uses gzip with the decompression fallback turned on, so it loads on GitHub Pages without any server configuration.

### Publish to itch.io
Zip the **contents** of `docs/` (not the folder itself), upload it as an **HTML** project, and tick "This file will be played in the browser". A viewport of 1280×720 is a good size, and you should turn on the fullscreen button.

### Automatic cloud builds (optional)
`.github/workflows/unity-build.yml` can build WebGL, Windows and Android on GitHub's servers and deploy the web version for you. It needs three repository secrets: `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` (see the [GameCI activation guide](https://game.ci/docs/github/activation)). Until those secrets exist, the workflow skips itself.

Separately, `compile-check.yml` compiles every script against the real Unity 6 assemblies on every push. It doesn't need a license.

---

## Code architecture

```
Assets/
  Scripts/
    Core/        GameManager (boot + stat aggregation), Stats, GameEvents (event bus), NumberFormatter
    Data/        ScriptableObject definitions, GameDatabase, DefaultContent, SaveData
    Managers/    Currency, Inventory, Upgrade, Research, Pet, Level, Prestige, Quest,
                 Achievement, Automation, Zone, Mining, Save
    Gameplay/    PlayerController, Rock, OrePickup, PetFollower, WorkerAvatar, DrillMachine, CameraFollow
    UI/          UIManager, Hud, UIFactory, Rows, Panels/ (one per menu)
    FX/          SpriteFactory (procedural art), PixelCanvas, FxManager (particles, text, shake)
    Audio/       SoundFactory (synthesized SFX + music), AudioManager
    Input/       Input2D (keyboard, mouse, touch; legacy Input or the new Input System)
  Editor/        ProjectSetup, BuildTools, DatabaseExporter
```

- **Stats pipeline.** Every bonus is a `StatModifier`. Upgrades, research, pets, levels and prestige each implement `IStatProvider`, and `GameManager.Stat(StatType)` caches the combined result.
- **Event bus.** `GameEvents` keeps systems separate from each other. For example, quests and achievements listen for events instead of reaching into the mining code.
- **ScriptableObjects.** All content (ores, zones, upgrades, workers, drills, pets, research, quests, achievements) is ScriptableObjects. Run **Idle Mining Empire → Export Database Assets** to create `Assets/Resources/GameDatabase.asset`, and you can then balance the whole game in the Inspector without touching code.
- **Adding content.** Add an entry in `DefaultContent.cs`, or in the exported asset. For example, a new zone just needs three ores plus a `ZoneDefinition`.

## License
MIT © 2026 Jaineil Rana
