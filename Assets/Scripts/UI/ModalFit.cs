using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Sizes the menu window to fit any screen (phone, tablet, desktop, portrait or landscape).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class ModalFit : MonoBehaviour
    {
        public Vector2 max = new Vector2(1400, 900);
        public Vector2 margin = new Vector2(40, 60);

        void LateUpdate()
        {
            var parent = transform.parent as RectTransform;
            if (parent == null) return;
            var size = parent.rect.size;
            var rt = (RectTransform)transform;
            float w = Mathf.Min(max.x, size.x - margin.x);
            float h = Mathf.Min(max.y, size.y - 60f);
            if (size.y > size.x) h = size.y - 260f; // portrait: use the tall space, keep HUD visible
            rt.sizeDelta = new Vector2(Mathf.Max(400, w), Mathf.Max(400, h));
        }
    }
}
