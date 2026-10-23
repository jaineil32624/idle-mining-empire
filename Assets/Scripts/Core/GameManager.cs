using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleMiningEmpire
{
    public interface IStatProvider
    {
        void ContributeStats(StatSheet sheet);
    }

    /// <summary>
    /// Root of the game. Creates every manager, loads the save, builds the world and UI.
    /// The game builds itself entirely from code, so it runs from any (even empty) scene.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameDatabase Db { get; private set; }
        public SaveData Save { get; set; }

        // Managers
        public CurrencyManager Currency { get; private set; }
        public InventoryManager Inventory { get; private set; }
        public UpgradeManager Upgrades { get; private set; }
        public ResearchManager Research { get; private set; }
        public PetManager Pets { get; private set; }
        public LevelManager Levels { get; private set; }
        public PrestigeManager Prestige { get; private set; }
        public QuestManager Quests { get; private set; }
        public AchievementManager Achievements { get; private set; }
        public AutomationManager Automation { get; private set; }
        public ZoneManager Zones { get; private set; }
        public MiningManager Mining { get; private set; }
        public SaveManager Saves { get; private set; }
        public AudioManager Audio { get; private set; }
        public FxManager Fx { get; private set; }
        public UIManager UI { get; private set; }

        public PlayerController Player { get; private set; }
        public CameraFollow Cam { get; private set; }

        readonly StatSheet sheet = new StatSheet();
        readonly double[] statCache = new double[(int)StatType.Count];
        bool statsDirty = true;
        IStatProvider[] providers;

        public bool Ready { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Boot();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) Boot();
        }

        /// <summary>Creates the game if it isn't running yet. Safe to call any time.</summary>
        public static void Boot()
        {
            if (Instance != null) return;
            new GameObject("Idle Mining Empire").AddComponent<GameManager>();
        }

        /// <summary>Deletes the save and rebuilds the whole game from scratch.</summary>
        public void ResetProgress()
        {
            Ready = false;
            Saves.DeleteSave();
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (go == gameObject || go.GetComponent<Camera>() != null) continue;
                Destroy(go);
            }
            new GameObject("Rebooter").AddComponent<Rebooter>();
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            GameEvents.ClearAll();
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input2D.Init();

            Db = GameDatabase.Load();

            Saves = gameObject.AddComponent<SaveManager>();
            Save = Saves.Load();

            Currency = gameObject.AddComponent<CurrencyManager>();
            Inventory = gameObject.AddComponent<InventoryManager>();
            Upgrades = gameObject.AddComponent<UpgradeManager>();
            Research = gameObject.AddComponent<ResearchManager>();
            Pets = gameObject.AddComponent<PetManager>();
            Levels = gameObject.AddComponent<LevelManager>();
            Prestige = gameObject.AddComponent<PrestigeManager>();
            Quests = gameObject.AddComponent<QuestManager>();
            Achievements = gameObject.AddComponent<AchievementManager>();
            Automation = gameObject.AddComponent<AutomationManager>();
            Audio = gameObject.AddComponent<AudioManager>();
            Fx = gameObject.AddComponent<FxManager>();
            Zones = gameObject.AddComponent<ZoneManager>();
            Mining = gameObject.AddComponent<MiningManager>();
            UI = gameObject.AddComponent<UIManager>();

            providers = new IStatProvider[] { Upgrades, Research, Pets, Levels, Prestige };
            GameEvents.StatsChanged += MarkStatsDirty;

            Inventory.Init(this);
            Currency.Init(this);
            Upgrades.Init(this);
            Research.Init(this);
            Pets.Init(this);
            Levels.Init(this);
            Prestige.Init(this);
            Quests.Init(this);
            Achievements.Init(this);
            Automation.Init(this);
            Audio.Init(this);
            Fx.Init(this);

            // World
            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Cam = camGo.GetComponent<CameraFollow>();
            if (Cam == null) Cam = camGo.AddComponent<CameraFollow>();
            Cam.Init(this);

            Player = PlayerController.Create(this);
            Zones.Init(this);
            Mining.Init(this);
            UI.Init(this);

            Ready = true;
            Zones.EnterZone(Save.currentZone, true);
            Saves.ApplyOfflineProgress(true);
            Quests.RefreshPassiveProgress();
        }

        public void MarkStatsDirty() => statsDirty = true;

        public double Stat(StatType t)
        {
            if (statsDirty) RecomputeStats();
            return statCache[(int)t];
        }

        void RecomputeStats()
        {
            statsDirty = false;
            sheet.Clear();
            for (int i = 0; i < providers.Length; i++) providers[i].ContributeStats(sheet);
            for (int i = 0; i < statCache.Length; i++) statCache[i] = sheet.Final((StatType)i);
        }

        void Update()
        {
            if (!Ready) return;
            Save.stats.playSeconds += Time.unscaledDeltaTime;
        }

        void OnApplicationPause(bool paused)
        {
            if (!Ready) return;
            if (paused) Saves.SaveNow();
            else Saves.ApplyOfflineProgress(false);
        }

        void OnApplicationQuit()
        {
            if (Ready) Saves.SaveNow();
        }
    }
}
