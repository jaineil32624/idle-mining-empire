using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Background worker crew: swings at the rock face and shows how many are hired.</summary>
    public class WorkerAvatar : MonoBehaviour
    {
        SpriteRenderer body;
        Transform pivot;
        float t, offset;

        public static WorkerAvatar Create(GameManager gm, WorkerDefinition def, int count, float x)
        {
            var go = new GameObject("Workers " + def.displayName);
            go.transform.position = new Vector3(x, 0.15f, 0);
            go.transform.localScale = Vector3.one * 0.8f;
            var w = go.AddComponent<WorkerAvatar>();
            int style = def.id == "worker_robot" ? 1 : def.id == "worker_ai" ? 2 : 0;
            w.body = go.AddComponent<SpriteRenderer>();
            w.body.sprite = SpriteFactory.Miner(def.color, Color.Lerp(def.color, Color.black, 0.5f), 0, style);
            w.body.sortingOrder = -50;
            w.body.color = new Color(0.8f, 0.8f, 0.85f);
            w.body.flipX = true;
            w.offset = Random.Range(0f, 3f);

            w.pivot = new GameObject("Pick").transform;
            w.pivot.SetParent(go.transform, false);
            w.pivot.localPosition = new Vector3(-0.45f, 0.95f, 0);
            w.pivot.localScale = new Vector3(-1, 1, 1);
            var pick = new GameObject("PickSprite").AddComponent<SpriteRenderer>();
            pick.transform.SetParent(w.pivot, false);
            pick.sprite = SpriteFactory.Pickaxe(new Color(0.6f, 0.6f, 0.65f));
            pick.sortingOrder = -49;
            pick.color = new Color(0.8f, 0.8f, 0.85f);

            var label = new GameObject("Count").AddComponent<TextMesh>();
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0, 2.6f, 0);
            label.font = UIFactory.DefaultFont;
            label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            label.GetComponent<MeshRenderer>().sortingOrder = -48;
            label.fontSize = 64;
            label.characterSize = 0.055f;
            label.anchor = TextAnchor.MiddleCenter;
            label.fontStyle = FontStyle.Bold;
            label.text = "x" + count;
            label.color = new Color(1f, 1f, 1f, 0.85f);
            return w;
        }

        void Update()
        {
            t += Time.deltaTime;
            float cycle = Mathf.Repeat(t * 1.6f + offset, 1f);
            float angle = cycle < 0.25f ? Mathf.Lerp(35f, -70f, cycle / 0.25f) : Mathf.Lerp(-70f, 35f, (cycle - 0.25f) / 0.75f);
            pivot.localRotation = Quaternion.Euler(0, 0, angle);
            if (cycle < 0.02f && GameManager.Instance != null)
                GameManager.Instance.Fx.Burst(transform.position + new Vector3(-1.4f, 0.6f, 0), new Color(0.6f, 0.55f, 0.5f), 2, 2.5f, 0.1f, 15f, 0.4f);
        }
    }
}
