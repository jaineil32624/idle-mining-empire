using System.Collections.Generic;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>
    /// The miner: walk / jump / dash, tap-or-click a rock to walk over and mine it, hold E/F to mine
    /// the nearest rock, tap the ground to walk there (mobile), and auto-mine once researched.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        const float Gravity = 32f;
        const float DashTime = 0.18f;
        const float DashSpeedMult = 3.8f;

        GameManager gm;
        SpriteRenderer body, pick, shadow, lamp;
        Transform pickPivot, visual;
        Vector2 vel;
        bool grounded = true;
        float dashTimer, dashCooldown, swingTimer, swingAnim, walkAnim, idleTimer;
        int walkFrame;
        Rock target;
        bool hasMoveTarget;
        float moveTargetX;
        readonly List<PetFollower> pets = new List<PetFollower>();
        string cosmeticKey;

        public float Facing { get; private set; } = 1f;
        public Rock Target => target;
        public float DashCooldownRemaining => dashCooldown;

        public static PlayerController Create(GameManager gm)
        {
            var go = new GameObject("Player");
            var p = go.AddComponent<PlayerController>();
            p.gm = gm;
            p.Build();
            GameEvents.StatsChanged += p.RefreshLook;
            GameEvents.LevelUp += l => p.RefreshLook();
            GameEvents.ZoneEntered += z => p.RefreshLamp();
            return p;
        }

        void Build()
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);

            shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(transform, false);
            shadow.sprite = SpriteFactory.Circle;
            shadow.color = new Color(0, 0, 0, 0.3f);
            shadow.transform.localScale = new Vector3(1.3f, 0.3f, 1);
            shadow.transform.localPosition = new Vector3(0, 0.08f, 0);
            shadow.sortingOrder = 25;

            lamp = new GameObject("Lamp").AddComponent<SpriteRenderer>();
            lamp.transform.SetParent(visual, false);
            lamp.sprite = SpriteFactory.SoftGlow;
            lamp.color = new Color(1f, 0.9f, 0.6f, 0.18f);
            lamp.transform.localScale = Vector3.one * 7f;
            lamp.transform.localPosition = new Vector3(0.4f, 1.6f, 0);
            lamp.sortingOrder = 5;

            body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(visual, false);
            body.sortingOrder = 30;

            pickPivot = new GameObject("PickPivot").transform;
            pickPivot.SetParent(visual, false);
            pickPivot.localPosition = new Vector3(0.45f, 0.95f, 0);
            pick = new GameObject("Pickaxe").AddComponent<SpriteRenderer>();
            pick.transform.SetParent(pickPivot, false);
            pick.sortingOrder = 31;

            RefreshLook();
            RefreshLamp();
        }

        void RefreshLamp()
        {
            if (lamp != null) lamp.enabled = gm.Save.currentZone > 0;
        }

        public void RefreshLook()
        {
            if (body == null) return;
            var c = gm.Levels.CurrentCosmetic;
            string key = c.name + walkFrame;
            if (key != cosmeticKey)
            {
                cosmeticKey = key;
                body.sprite = SpriteFactory.Miner(c.helmet, c.suit, walkFrame);
                pick.sprite = SpriteFactory.Pickaxe(c.head);
            }
            RefreshPets();
        }

        void RefreshPets()
        {
            var equipped = new List<PetInstance>(gm.Pets.Equipped);
            bool same = equipped.Count == pets.Count;
            for (int i = 0; same && i < equipped.Count; i++) same = pets[i] != null && pets[i].Instance == equipped[i];
            if (same) return;
            foreach (var p in pets) if (p != null) Destroy(p.gameObject);
            pets.Clear();
            for (int i = 0; i < equipped.Count; i++)
            {
                var def = gm.Pets.Def(equipped[i]);
                if (def == null) continue;
                pets.Add(PetFollower.Create(this, equipped[i], def, i));
            }
        }

        public void Teleport(Vector3 pos)
        {
            transform.position = pos;
            vel = Vector2.zero;
            target = null;
            hasMoveTarget = false;
            foreach (var p in pets) if (p != null) p.Snap();
        }

        void Update()
        {
            if (gm == null || !gm.Ready) return;
            if (gm.UI != null && gm.UI.IsModalOpen) { ApplyPhysics(0, Time.deltaTime); return; }
            float dt = Time.deltaTime;

            float axis = Input2D.MoveAxis;
            if (Mathf.Abs(axis) > 0.1f) { target = null; hasMoveTarget = false; idleTimer = 0; }

            // Pointer: click/tap a rock to mine it, tap ground to walk.
            if (Input2D.PointerHeld)
            {
                idleTimer = 0;
                var cam = gm.Cam.Camera;
                Vector2 world = cam.ScreenToWorldPoint(Input2D.PointerPosition);
                var rock = gm.Mining.RockAtPoint(world);
                if (rock != null)
                {
                    if (rock != target) { target = rock; hasMoveTarget = false; }
                    if (Input2D.PointerDown && InRange(rock) && swingTimer <= 0.5f / Speed) Swing();
                }
                else if (Input2D.PointerDown && world.y < cam.transform.position.y + cam.orthographicSize * 0.4f)
                {
                    target = null;
                    hasMoveTarget = true;
                    moveTargetX = Mathf.Clamp(world.x, -ZoneManager.WorldHalfWidth + 1, ZoneManager.WorldHalfWidth - 1);
                }
            }

            if (Input2D.MineHeld)
            {
                idleTimer = 0;
                if (target == null || target.Dead) target = gm.Mining.NearestRock(transform.position.x, gm.Stat(StatType.MineRange) * 2.5f);
            }

            if (Input2D.SellDown) gm.UI.SellPressed();

            // Auto-mine (research) after a short idle period.
            idleTimer += dt;
            if ((target == null || target.Dead) && !hasMoveTarget && gm.Stat(StatType.AutoMine) > 0 && idleTimer > 1.2f && Mathf.Abs(axis) < 0.1f)
                target = gm.Mining.NearestRock(transform.position.x, 14f);

            if (target != null && target.Dead) target = null;

            // Decide horizontal intent.
            float move = axis;
            if (Mathf.Abs(axis) < 0.1f)
            {
                if (target != null)
                {
                    float dx = target.transform.position.x - transform.position.x;
                    if (!InRange(target)) move = Mathf.Sign(dx);
                    else Facing = Mathf.Sign(dx) == 0 ? Facing : Mathf.Sign(dx);
                }
                else if (hasMoveTarget)
                {
                    float dx = moveTargetX - transform.position.x;
                    if (Mathf.Abs(dx) < 0.2f) hasMoveTarget = false;
                    else move = Mathf.Sign(dx);
                }
            }

            // Jump & dash
            if (Input2D.JumpDown && grounded)
            {
                vel.y = Mathf.Sqrt(2f * Gravity * (float)gm.Stat(StatType.JumpHeight));
                grounded = false;
                gm.Save.stats.jumps++;
                gm.Audio.Play(Sfx.Jump, 0.35f);
            }
            dashCooldown -= dt;
            if (Input2D.DashDown && dashCooldown <= 0)
            {
                dashTimer = DashTime;
                dashCooldown = (float)gm.Stat(StatType.DashCooldown);
                gm.Audio.Play(Sfx.Dash, 0.4f);
                gm.Fx.Burst(transform.position + Vector3.up * 0.3f, new Color(1, 1, 1, 0.6f), 8, 3f, 0.15f, 2f, 0.4f);
            }

            ApplyPhysics(move, dt);

            // Mining swings
            swingTimer -= dt;
            if (target != null && InRange(target) && swingTimer <= 0) Swing();
            AnimateSwing(dt);

            // Walk animation
            bool moving = Mathf.Abs(vel.x) > 0.2f && grounded;
            walkAnim += moving ? dt : 0;
            int frame = moving ? (int)(walkAnim / 0.14f) % 2 : 0;
            if (frame != walkFrame) { walkFrame = frame; RefreshLook(); }
            float bob = moving ? Mathf.Abs(Mathf.Sin(walkAnim * 22f)) * 0.06f : 0;
            float squash = grounded ? 1f : 1.06f;
            visual.localPosition = new Vector3(0, bob, 0);
            visual.localScale = new Vector3(Facing * (2f - squash), squash, 1);
            shadow.transform.localScale = new Vector3(1.3f, 0.3f, 1) * Mathf.Clamp01(1.2f - transform.position.y * 0.15f);
            shadow.transform.position = new Vector3(transform.position.x, 0.08f, 0);
        }

        double Speed => gm.Stat(StatType.MiningSpeed);

        bool InRange(Rock r)
        {
            float d = Mathf.Abs(r.transform.position.x - transform.position.x) - r.radius;
            return d <= gm.Stat(StatType.MineRange) * 0.55f;
        }

        void Swing()
        {
            if (target == null) return;
            swingTimer = 1f / (float)Speed;
            swingAnim = 1f;
            Facing = target.transform.position.x >= transform.position.x ? 1f : -1f;
            gm.Mining.Hit(target);
        }

        void AnimateSwing(float dt)
        {
            float speed = Mathf.Max(6f, (float)Speed * 2.5f);
            swingAnim = Mathf.MoveTowards(swingAnim, 0, dt * speed);
            // swingAnim 1 -> 0 : strike happens quickly then returns to rest
            float angle;
            if (swingAnim > 0.7f) angle = Mathf.Lerp(-70f, 95f, (swingAnim - 0.7f) / 0.3f);
            else angle = Mathf.Lerp(35f, -70f, swingAnim / 0.7f);
            if (swingAnim <= 0.001f) angle = 35f + Mathf.Sin(Time.time * 2f) * 3f;
            pickPivot.localRotation = Quaternion.Euler(0, 0, angle);
        }

        void ApplyPhysics(float move, float dt)
        {
            float speed = (float)gm.Stat(StatType.WalkSpeed);
            if (dashTimer > 0)
            {
                dashTimer -= dt;
                vel.x = Facing * speed * DashSpeedMult;
            }
            else
            {
                float targetVx = move * speed;
                vel.x = Mathf.MoveTowards(vel.x, targetVx, speed * 10f * dt);
                if (Mathf.Abs(move) > 0.1f) Facing = Mathf.Sign(move);
            }
            vel.y -= Gravity * dt;
            var p = transform.position + (Vector3)(vel * dt);
            if (p.y <= 0) { p.y = 0; vel.y = 0; grounded = true; }
            else grounded = false;
            p.x = Mathf.Clamp(p.x, -ZoneManager.WorldHalfWidth + 0.8f, ZoneManager.WorldHalfWidth - 0.8f);
            transform.position = p;
        }
    }
}
