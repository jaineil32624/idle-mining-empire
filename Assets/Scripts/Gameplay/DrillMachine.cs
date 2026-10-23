using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Background drill rig: rumbles and spits dirt; shows how many of that drill you own.</summary>
    public class DrillMachine : MonoBehaviour
    {
        Transform visual;
        float t;

        public static DrillMachine Create(GameManager gm, DrillDefinition def, int count, float x)
        {
            var go = new GameObject("Drill " + def.displayName);
            go.transform.position = new Vector3(x, 0.1f, 0);
            var d = go.AddComponent<DrillMachine>();
            d.visual = new GameObject("Visual").transform;
            d.visual.SetParent(go.transform, false);
            var sr = d.visual.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Drill(def.color, def.id.Length);
            sr.sortingOrder = -45;
            sr.color = new Color(0.85f, 0.85f, 0.9f);

            var label = new GameObject("Count").AddComponent<TextMesh>();
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0, 3.8f, 0);
            label.font = UIFactory.DefaultFont;
            label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            label.GetComponent<MeshRenderer>().sortingOrder = -44;
            label.fontSize = 64;
            label.characterSize = 0.05f;
            label.anchor = TextAnchor.MiddleCenter;
            label.fontStyle = FontStyle.Bold;
            label.text = def.displayName + " x" + count;
            label.color = new Color(1f, 0.9f, 0.6f, 0.9f);
            return d;
        }

        void Update()
        {
            t += Time.deltaTime;
            visual.localPosition = new Vector3(Mathf.Sin(t * 60f) * 0.03f, Mathf.Abs(Mathf.Sin(t * 37f)) * 0.04f, 0);
            if (Random.value < Time.deltaTime * 6f && GameManager.Instance != null)
                GameManager.Instance.Fx.Burst(transform.position + new Vector3(Random.Range(-0.6f, 0.6f), 0.1f, 0),
                    new Color(0.5f, 0.4f, 0.3f), 1, 3f, 0.1f, 15f, 0.4f);
        }
    }
}
