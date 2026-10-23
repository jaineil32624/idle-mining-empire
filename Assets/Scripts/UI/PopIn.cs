using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Elastic scale-in when a UI element appears.</summary>
    public class PopIn : MonoBehaviour
    {
        float t;

        void OnEnable() { t = 0; transform.localScale = Vector3.one * 0.6f; }

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime * 4.5f);
            float s = 1f + Mathf.Sin(t * Mathf.PI * 1.5f) * (1f - t) * 0.25f;
            s = Mathf.Lerp(0.6f, 1f, Mathf.Min(1f, t * 2.2f)) * s;
            transform.localScale = new Vector3(s, s, 1);
        }
    }
}
