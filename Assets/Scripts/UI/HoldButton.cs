using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IdleMiningEmpire
{
    /// <summary>On-screen control that reports press / hold / release (used for mobile movement).</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action onDown;
        public Action onUp;
        public bool Held { get; private set; }

        public void OnPointerDown(PointerEventData e)
        {
            Held = true;
            onDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        void Release()
        {
            if (!Held) return;
            Held = false;
            onUp?.Invoke();
        }

        void OnDisable() => Release();
    }
}
