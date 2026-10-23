using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Orthographic camera that follows the player within the zone bounds, adapts to any aspect ratio and shakes.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Camera Camera { get; private set; }
        GameManager gm;
        float x;
        public const float FloorScreenFraction = 0.24f; // floor sits this far up from the bottom of the screen

        public void Init(GameManager g)
        {
            gm = g;
            Camera = GetComponent<Camera>();
            Camera.orthographic = true;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            Camera.nearClipPlane = 0.1f;
            Camera.farClipPlane = 100f;
            transform.rotation = Quaternion.identity;
            UpdateSize();
        }

        void UpdateSize()
        {
            // Show ~13 world units vertically on landscape, more on tall/portrait screens so the playfield stays readable.
            float aspect = Camera.aspect;
            float size = 6.6f;
            if (aspect < 1.3f) size = 6.6f * Mathf.Clamp(1.3f / Mathf.Max(0.4f, aspect), 1f, 2.2f) * 0.75f;
            Camera.orthographicSize = Mathf.Max(5.5f, size);
        }

        public float HalfWidth => Camera.orthographicSize * Camera.aspect;

        public void Snap()
        {
            if (gm.Player != null) x = gm.Player.transform.position.x;
            LateUpdate();
        }

        void LateUpdate()
        {
            if (gm == null || gm.Player == null) return;
            UpdateSize();
            float target = gm.Player.transform.position.x + gm.Player.Facing * 1.5f;
            x = Mathf.Lerp(x, target, 1f - Mathf.Exp(-Time.deltaTime * 5f));
            float half = HalfWidth;
            float bound = ZoneManager.WorldHalfWidth;
            float cx = half >= bound ? 0 : Mathf.Clamp(x, -bound + half, bound - half);
            float size = Camera.orthographicSize;
            float cy = size - size * 2f * FloorScreenFraction;
            transform.position = new Vector3(cx, cy, -10f) + gm.Fx.ShakeOffset;
        }
    }
}
