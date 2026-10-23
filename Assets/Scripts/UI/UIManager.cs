using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace IdleMiningEmpire
{
    /// <summary>Base class for every menu window (Upgrades, Shop, Pets, ...).</summary>
    public abstract class UIPanel
    {
        protected GameManager gm;
        protected UIManager ui;
        public void Bind(GameManager g, UIManager u) { gm = g; ui = u; }
        public abstract string Title { get; }
        public virtual string[] Tabs => null;
        public abstract void Build(RectTransform content, int tab);
    }

    /// <summary>
    /// Owns the canvas, HUD, modal menus, popups, toasts and screen effects.
    /// Uses a CanvasScaler that adapts to phones, tablets and desktop (landscape and portrait).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        GameManager gm;
        Canvas canvas;
        CanvasScaler scaler;
        public RectTransform Root { get; private set; }
        Hud hud;

        // modal window
        RectTransform modalRoot, tabBar, contentHolder;
        Text modalTitle;
        UIPanel activePanel;
        int activeTab;
        readonly List<Action> refreshers = new List<Action>();
        float refreshTimer;
        ScrollRect currentScroll;

        // popup
        RectTransform popupRoot;
        bool popupOpen;
        readonly Queue<Action> popupQueue = new Queue<Action>();

        // toasts / fx
        RectTransform toastRoot;
        readonly Queue<string> toastQueue = new Queue<string>();
        float toastCooldown;
        Image flash;
        Text savedText;
        float savedTimer;

        public readonly Dictionary<string, UIPanel> Panels = new Dictionary<string, UIPanel>();
        public bool IsModalOpen => activePanel != null || popupOpen;
        public UIPanel ActivePanel => activePanel;

        public void Init(GameManager g)
        {
            gm = g;
            EnsureEventSystem();

            var canvasGo = new GameObject("UI Canvas");
            canvasGo.layer = 5;
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.pixelPerfect = false;
            scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Root = (RectTransform)canvasGo.transform;

            // Safe area container (notches on phones)
            var safe = UIFactory.Rect("SafeArea", Root);
            UIFactory.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            hud = new Hud(gm, this, safe);

            Register("upgrades", new UpgradesPanel());
            Register("shop", new ShopPanel());
            Register("inventory", new InventoryPanel());
            Register("research", new ResearchPanel());
            Register("pets", new PetsPanel());
            Register("quests", new QuestsPanel());
            Register("zones", new ZonesPanel());
            Register("prestige", new PrestigePanel());
            Register("more", new MorePanel());

            BuildModal();
            BuildPopupLayer();

            toastRoot = UIFactory.Rect("Toasts", safe);
            UIFactory.Place(toastRoot, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(1000, 400));
            UIFactory.VLayout(toastRoot.gameObject, 8, null, TextAnchor.UpperCenter, true, true, false, false);

            flash = UIFactory.Img(Root, new Color(1, 1, 1, 0), SpriteFactory.White, "Flash");
            UIFactory.Stretch(flash.rectTransform);
            flash.raycastTarget = false;

            savedText = UIFactory.Txt(safe, "Saved", 20, new Color(1, 1, 1, 0), TextAnchor.LowerLeft);
            UIFactory.Place(savedText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 150), new Vector2(200, 30));

            GameEvents.Toast += Toast;

            if (!gm.Save.tutorialSeen) ShowWelcome();
        }

        void Register(string id, UIPanel p)
        {
            p.Bind(gm, this);
            Panels[id] = p;
        }

        void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_LEGACY_INPUT_MANAGER
            es.AddComponent<StandaloneInputModule>();
#elif ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#endif
        }

        // ------------------------------------------------------------ Modal menus
        void BuildModal()
        {
            modalRoot = UIFactory.Rect("Modal", Root);
            UIFactory.Stretch(modalRoot);
            var dim = UIFactory.Img(modalRoot, new Color(0, 0, 0, 0.55f), SpriteFactory.White, "Dim");
            UIFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Close);

            var win = UIFactory.Img(modalRoot, UIFactory.PanelBg, SpriteFactory.RoundedRect, "Window");
            win.raycastTarget = true;
            var wrt = win.rectTransform;
            wrt.anchorMin = new Vector2(0.5f, 0.5f);
            wrt.anchorMax = new Vector2(0.5f, 0.5f);
            wrt.sizeDelta = new Vector2(1400, 900);
            wrt.anchoredPosition = new Vector2(0, 10);
            win.gameObject.AddComponent<ModalFit>();
            var border = UIFactory.Img(win.transform, new Color(1f, 0.8f, 0.3f, 0.35f), SpriteFactory.RoundedFrame, "Border");
            UIFactory.Stretch(border.rectTransform, -2, -2, -2, -2);

            var header = UIFactory.Img(win.transform, UIFactory.PanelMid, SpriteFactory.RoundedRect, "Header");
            UIFactory.Place(header.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(0, 90));
            header.rectTransform.anchorMin = new Vector2(0, 1);
            header.rectTransform.anchorMax = new Vector2(1, 1);
            header.rectTransform.offsetMin = new Vector2(10, -100);
            header.rectTransform.offsetMax = new Vector2(-10, -10);

            modalTitle = UIFactory.Txt(header.transform, "Title", 46, UIFactory.Gold, TextAnchor.MiddleLeft, true);
            UIFactory.Stretch(modalTitle.rectTransform, 30, 0, 130, 0);

            var close = UIFactory.Btn(header.transform, "X", UIFactory.Red, Close, 40);
            UIFactory.Place(close.rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(84, 70));

            tabBar = UIFactory.Rect("Tabs", win.transform);
            tabBar.anchorMin = new Vector2(0, 1); tabBar.anchorMax = new Vector2(1, 1);
            tabBar.pivot = new Vector2(0.5f, 1);
            tabBar.offsetMin = new Vector2(20, -180); tabBar.offsetMax = new Vector2(-20, -110);
            UIFactory.HLayout(tabBar.gameObject, 10, null, TextAnchor.MiddleLeft, true, true, true, true);

            contentHolder = UIFactory.Rect("ContentHolder", win.transform);
            UIFactory.Stretch(contentHolder, 20, 190, 20, 20);

            modalRoot.gameObject.SetActive(false);
        }

        public void Toggle(string id)
        {
            if (Panels.TryGetValue(id, out var p) && activePanel == p) Close();
            else Open(id);
        }

        public void Open(string id, int tab = 0)
        {
            if (!Panels.TryGetValue(id, out var p)) return;
            if (popupOpen) return;
            activePanel = p;
            activeTab = Mathf.Max(0, tab);
            modalRoot.gameObject.SetActive(true);
            modalRoot.SetAsLastSibling();
            if (popupRoot != null) popupRoot.SetAsLastSibling();
            flash.transform.SetAsLastSibling();
            Rebuild(true);
        }

        public void Close()
        {
            activePanel = null;
            refreshers.Clear();
            modalRoot.gameObject.SetActive(false);
        }

        /// <summary>Rebuilds the current panel (call after structural changes like hatching a pet).</summary>
        public void Rebuild(bool resetScroll = false)
        {
            if (activePanel == null) return;
            float scrollPos = currentScroll != null ? currentScroll.verticalNormalizedPosition : 1f;
            refreshers.Clear();
            modalTitle.text = activePanel.Title;

            for (int i = tabBar.childCount - 1; i >= 0; i--) Destroy(tabBar.GetChild(i).gameObject);
            var tabs = activePanel.Tabs;
            tabBar.gameObject.SetActive(tabs != null && tabs.Length > 0);
            if (tabs != null)
            {
                if (activeTab >= tabs.Length) activeTab = 0;
                for (int i = 0; i < tabs.Length; i++)
                {
                    int idx = i;
                    var b = UIFactory.Btn(tabBar, tabs[i], i == activeTab ? UIFactory.Blue : UIFactory.PanelLight, () => { activeTab = idx; Rebuild(true); }, 28);
                    if (i == activeTab) b.bg.color = UIFactory.Blue;
                }
            }
            contentHolder.offsetMax = new Vector2(-20, tabs != null && tabs.Length > 0 ? -190 : -115);

            for (int i = contentHolder.childCount - 1; i >= 0; i--) Destroy(contentHolder.GetChild(i).gameObject);
            var content = UIFactory.Scroll(contentHolder, out currentScroll);
            UIFactory.Stretch((RectTransform)currentScroll.transform);
            UIFactory.VLayout(content.gameObject, 10, UIFactory.Pad(6, 6, 6, 12), TextAnchor.UpperCenter, true, true, true, false);
            activePanel.Build(content, activeTab);
            RunRefreshers();
            if (!resetScroll)
            {
                Canvas.ForceUpdateCanvases();
                currentScroll.verticalNormalizedPosition = scrollPos;
            }
        }

        public int ActiveTab => activeTab;

        public void AddRefresher(Action a) => refreshers.Add(a);

        void RunRefreshers()
        {
            for (int i = 0; i < refreshers.Count; i++)
            {
                try { refreshers[i](); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        // ------------------------------------------------------------ Popups
        void BuildPopupLayer()
        {
            popupRoot = UIFactory.Rect("Popups", Root);
            UIFactory.Stretch(popupRoot);
            popupRoot.gameObject.SetActive(false);
        }

        /// <summary>Generic popup with up to two buttons. Queued if another popup is showing.</summary>
        public void ShowPopup(string title, string body, string okLabel, Action onOk, string cancelLabel = null, Action onCancel = null,
            Sprite icon = null, Color? okColor = null)
        {
            if (popupOpen)
            {
                popupQueue.Enqueue(() => ShowPopup(title, body, okLabel, onOk, cancelLabel, onCancel, icon, okColor));
                return;
            }
            popupOpen = true;
            for (int i = popupRoot.childCount - 1; i >= 0; i--) Destroy(popupRoot.GetChild(i).gameObject);
            popupRoot.gameObject.SetActive(true);
            popupRoot.SetAsLastSibling();
            flash.transform.SetAsLastSibling();

            var dim = UIFactory.Img(popupRoot, new Color(0, 0, 0, 0.7f), SpriteFactory.White, "Dim");
            UIFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var win = UIFactory.Img(popupRoot, UIFactory.PanelBg, SpriteFactory.RoundedRect, "Popup");
            win.raycastTarget = true;
            var wrt = win.rectTransform;
            wrt.anchorMin = wrt.anchorMax = new Vector2(0.5f, 0.5f);
            wrt.sizeDelta = new Vector2(980, 0);
            UIFactory.VLayout(win.gameObject, 22, UIFactory.Pad(50, 50, 40, 40), TextAnchor.UpperCenter, true, true, true, false);
            var fit = win.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var border = UIFactory.Img(win.transform, new Color(1f, 0.8f, 0.3f, 0.5f), SpriteFactory.RoundedFrame, "Border");
            border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UIFactory.Stretch(border.rectTransform, -2, -2, -2, -2);

            if (icon != null)
            {
                var ic = UIFactory.Icon(win.transform, icon, 120);
                ic.gameObject.AddComponent<Spin>().speed = 0;
            }
            var t = UIFactory.Txt(win.transform, title, 52, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.LE(t.gameObject, -1, 70);
            var b = UIFactory.Txt(win.transform, body, 32, UIFactory.TextMain, TextAnchor.MiddleCenter);
            UIFactory.LE(b.gameObject, -1, -1);

            var row = UIFactory.Rect("Buttons", win.transform);
            UIFactory.HLayout(row.gameObject, 24, null, TextAnchor.MiddleCenter, true, true, false, false);
            UIFactory.LE(row.gameObject, -1, 100);
            if (!string.IsNullOrEmpty(cancelLabel))
            {
                var cb = UIFactory.Btn(row, cancelLabel, UIFactory.PanelLight, () => { ClosePopup(); onCancel?.Invoke(); }, 34);
                UIFactory.LE(cb.button.gameObject, 340, 96);
            }
            var ob = UIFactory.Btn(row, okLabel, okColor ?? UIFactory.Green, () => { ClosePopup(); onOk?.Invoke(); }, 34);
            UIFactory.LE(ob.button.gameObject, 380, 96);

            win.gameObject.AddComponent<PopIn>();
        }

        public void ClosePopup()
        {
            popupOpen = false;
            popupRoot.gameObject.SetActive(false);
            if (popupQueue.Count > 0) popupQueue.Dequeue()();
        }

        public void ShowConfirm(string title, string body, string okLabel, Action onOk, Color? okColor = null) =>
            ShowPopup(title, body, okLabel, onOk, "Cancel", null, null, okColor);

        void ShowWelcome()
        {
            string controls =
                "Mine rocks, sell ore, upgrade, dig deeper!\n\n" +
                UIFactory.Colorize("PC:", UIFactory.Gold) + " A/D move, Space jump, Shift dash,\nclick/hold a rock to mine (or hold E), Q sell, 1-9 menus.\n\n" +
                UIFactory.Colorize("Mobile:", UIFactory.Gold) + " tap a rock to mine it, tap the ground to walk,\nuse the on-screen buttons to jump and dash.\n\n" +
                "Sell at the " + UIFactory.Colorize("Trading Post", UIFactory.Gold) + " for full price, or anywhere at a discount.";
            ShowPopup("Idle Mining Empire", controls, "Let's dig!", () => { gm.Save.tutorialSeen = true; }, null, null, SpriteFactory.Pickaxe(UIFactory.Gold));
        }

        public void ShowOfflineReward(double awaySeconds, double countedSeconds, double coins, double ores)
        {
            string body = "While away for " + NumberFormatter.Time(awaySeconds) + ":\n\n" +
                          UIFactory.Colorize("Coins earned: " + NumberFormatter.Format(coins), UIFactory.Gold) + "\n" +
                          "Ores mined: " + NumberFormatter.Format(ores);
            if (countedSeconds < awaySeconds - 1)
                body += "\n\n" + UIFactory.Colorize("(Capped at " + NumberFormatter.Time(countedSeconds) + " - research Night Crew or buy Night Shift to extend)", UIFactory.TextDim);
            double bonusCost = 10;
            ShowPopup("Welcome Back!", body, "Collect", null,
                gm.Currency.Gems >= bonusCost ? "Double it (10 gems)" : null,
                () =>
                {
                    if (gm.Currency.TrySpend(CurrencyType.Gems, bonusCost))
                    {
                        gm.Currency.AddCoins(coins);
                        Toast("Offline earnings doubled!");
                    }
                }, SpriteFactory.CoinIcon);
        }

        // ------------------------------------------------------------ Toasts & effects
        public void Toast(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            if (toastQueue.Count < 6) toastQueue.Enqueue(msg);
        }

        void SpawnToast(string msg)
        {
            var bg = UIFactory.Img(toastRoot, new Color(0.08f, 0.1f, 0.2f, 0.92f), SpriteFactory.RoundedRect, "Toast");
            UIFactory.HLayout(bg.gameObject, 0, UIFactory.Pad(30, 30, 14, 14), TextAnchor.MiddleCenter, true, true, false, false);
            var t = UIFactory.Txt(bg.transform, msg, 30, UIFactory.TextMain, TextAnchor.MiddleCenter, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var frame = UIFactory.Img(bg.transform, new Color(1f, 0.8f, 0.3f, 0.5f), SpriteFactory.RoundedFrame, "Frame");
            frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UIFactory.Stretch(frame.rectTransform);
            bg.gameObject.AddComponent<ToastLife>();
            bg.gameObject.AddComponent<PopIn>();
        }

        public void ScreenFlash(Color c) { if (flash != null) flash.color = c; }

        public void FlashSaved() { savedTimer = 1.5f; }

        public void PulseSellButton() => hud?.PulseSell();

        public double SellRate => gm.Zones.AtStation ? 1.0 : gm.Inventory.RemoteRate;

        public void SellPressed()
        {
            if (gm.Inventory.TotalCount <= 0)
            {
                gm.Audio.Play(Sfx.Error);
                Toast("Your backpack is empty - go mine some rocks!");
                return;
            }
            gm.Inventory.SellAll(SellRate);
            gm.Fx.Burst(gm.Player.transform.position + Vector3.up * 2f, UIFactory.Gold, 16, 6f, 0.16f, 10f, 0.8f, SpriteFactory.CoinIcon);
        }

        public bool TouchControlsVisible
        {
            get
            {
                int mode = gm.Save.touchControls;
                if (mode == 1) return true;
                if (mode == 2) return false;
                return Application.isMobilePlatform;
            }
        }

        void Update()
        {
            if (gm == null || !gm.Ready) return;
            float dt = Time.unscaledDeltaTime;

            // Portrait screens match width so menus never overflow horizontally.
            float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            scaler.matchWidthOrHeight = aspect < 1.2f ? 0f : aspect > 2.1f ? 1f : 0.5f;

            hud.Update(dt);

            refreshTimer -= dt;
            if (refreshTimer <= 0 && activePanel != null)
            {
                refreshTimer = 0.2f;
                RunRefreshers();
            }

            if (toastCooldown > 0) toastCooldown -= dt;
            if (toastCooldown <= 0 && toastQueue.Count > 0)
            {
                SpawnToast(toastQueue.Dequeue());
                toastCooldown = 0.5f;
            }

            if (flash.color.a > 0)
            {
                var c = flash.color;
                c.a = Mathf.MoveTowards(c.a, 0, dt * 1.6f);
                flash.color = c;
            }

            if (savedTimer > 0)
            {
                savedTimer -= dt;
                savedText.color = new Color(0.7f, 1f, 0.7f, Mathf.Clamp01(savedTimer));
            }

            if (Input2D.EscapeDown)
            {
                if (popupOpen) { }
                else if (activePanel != null) Close();
                else Open("more", 2);
            }
            int key = Input2D.MenuKey;
            if (key > 0 && !popupOpen)
            {
                string[] order = { "upgrades", "shop", "inventory", "research", "pets", "quests", "zones", "prestige", "more" };
                Toggle(order[key - 1]);
            }
        }
    }
}
