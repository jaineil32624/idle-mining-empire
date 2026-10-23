using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Ore chunk that pops out of a broken rock, bounces, then magnets into the backpack.</summary>
    public class OrePickup : MonoBehaviour
    {
        public OreDefinition ore;
        public double amount;
        GameManager gm;
        SpriteRenderer sr;
        Vector2 vel;
        float age, retryTimer;
        bool grounded, magnet;

        public static OrePickup Create(GameManager gm, OreDefinition ore, double amount, Vector3 pos)
        {
            var go = new GameObject("Ore " + ore.displayName);
            go.transform.position = pos;
            var p = go.AddComponent<OrePickup>();
            p.gm = gm;
            p.ore = ore;
            p.amount = amount;
            p.sr = go.AddComponent<SpriteRenderer>();
            p.sr.sprite = SpriteFactory.OreChunk(ore);
            p.sr.sortingOrder = 20;
            float a = Random.Range(0.35f, Mathf.PI - 0.35f);
            p.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(5f, 9f);
            go.transform.localScale = Vector3.one * Mathf.Clamp(0.9f + Mathf.Log10((float)amount + 1) * 0.5f, 0.9f, 1.8f);
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            var player = gm.Player;
            var pos = transform.position;

            if (!magnet && age > 0.45f && player != null)
            {
                retryTimer -= dt;
                if (retryTimer <= 0)
                {
                    retryTimer = 0.4f;
                    bool canTake = !gm.Inventory.IsFull || gm.Stat(StatType.AutoSell) > 0;
                    float dist = Mathf.Abs(player.transform.position.x - pos.x);
                    if (canTake && (dist < 9f || age > 2.5f)) magnet = true;
                }
            }

            if (magnet && player != null)
            {
                Vector3 target = player.transform.position + Vector3.up * 1.2f;
                Vector3 d = target - pos;
                float speed = 10f + age * 6f;
                pos += d.normalized * Mathf.Min(d.magnitude, speed * dt);
                transform.position = pos;
                transform.Rotate(0, 0, 540f * dt);
                if (d.magnitude < 0.45f) Collect();
                return;
            }

            if (!grounded)
            {
                vel.y -= 22f * dt;
                pos += (Vector3)(vel * dt);
                if (pos.y <= 0.18f)
                {
                    pos.y = 0.18f;
                    if (Mathf.Abs(vel.y) > 2.5f) { vel.y *= -0.4f; vel.x *= 0.6f; }
                    else { grounded = true; vel = Vector2.zero; }
                }
                pos.x = Mathf.Clamp(pos.x, -ZoneManager.WorldHalfWidth + 1f, ZoneManager.WorldHalfWidth - 1f);
                transform.position = pos;
                transform.Rotate(0, 0, vel.x * 60f * dt);
            }
            else
            {
                // idle bob + shimmer while waiting on the floor
                sr.color = Color.Lerp(Color.white, ore.highlight, 0.3f + 0.3f * Mathf.Sin(Time.time * 5f + pos.x));
            }
        }

        void Collect()
        {
            double added = gm.Inventory.TryAdd(ore, amount);
            if (added > 0)
            {
                gm.Audio.Play(Sfx.Pickup, 0.35f, 0.15f);
                gm.Fx.FloatingText(gm.Player.transform.position + Vector3.up * 2.4f,
                    "+" + NumberFormatter.Format(added) + " " + ore.displayName, Color.Lerp(ore.color, Color.white, 0.4f), 0.7f);
            }
            amount -= added;
            if (amount <= 0.0001)
            {
                gm.Mining.RemovePickup(this);
                Destroy(gameObject);
                return;
            }
            // Backpack full: drop back down and wait.
            gm.Mining.NotifyBackpackFull();
            magnet = false;
            grounded = false;
            age = 0;
            retryTimer = 1.5f;
            vel = new Vector2(Random.Range(-3f, 3f), 4f);
        }
    }
}
