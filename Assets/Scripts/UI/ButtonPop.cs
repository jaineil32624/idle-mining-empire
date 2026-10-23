using UnityEngine;
using UnityEngine.EventSystems;

namespace IdleMiningEmpire
{
    /// <summary>Juicy squash when a button is pressed.</summary>
    public class ButtonPop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        float target = 1f, current = 1f;

        public void OnPointerDown(PointerEventData e) => target = 0.93f;
        public void OnPointerUp(PointerEventData e) => target = 1f;
        public void OnPointerExit(PointerEventData e) => target = 1f;

        public void Pulse() { current = 1.15f; }

        void Update()
        {
            current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f));
            transform.localScale = new Vector3(current, current, 1);
        }
    }
}
