using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IdleMiningEmpire
{
    /// <summary>
    /// One input API for keyboard, mouse, touch and on-screen buttons. Works whether the project uses the
    /// old Input Manager, the new Input System, or both.
    /// </summary>
    public static class Input2D
    {
        // On-screen (mobile) controls write here.
        public static float VirtualAxis;
        static bool virtualJump, virtualDash;
        public static void PressVirtualJump() => virtualJump = true;
        public static void PressVirtualDash() => virtualDash = true;

        static int lastFrame = -1;
        static bool jumpDown, dashDown, pointerDown, pointerHeld, pointerOverUi, escDown;
        static Vector2 pointerPos;
        static float axis;
        static int menuKey;
        static bool mineHeld, sellDown;
        static readonly List<RaycastResult> hits = new List<RaycastResult>();

        public static void Init() { VirtualAxis = 0; virtualJump = virtualDash = false; lastFrame = -1; }

        public static float MoveAxis { get { Poll(); return axis; } }
        public static bool JumpDown { get { Poll(); return jumpDown; } }
        public static bool DashDown { get { Poll(); return dashDown; } }
        public static bool PointerDown { get { Poll(); return pointerDown; } }
        public static bool PointerHeld { get { Poll(); return pointerHeld; } }
        public static bool PointerOverUI { get { Poll(); return pointerOverUi; } }
        public static Vector2 PointerPosition { get { Poll(); return pointerPos; } }
        public static bool MineHeld { get { Poll(); return mineHeld; } }
        public static bool SellDown { get { Poll(); return sellDown; } }
        public static bool EscapeDown { get { Poll(); return escDown; } }
        /// <summary>1-9 when a number key was pressed this frame, else 0.</summary>
        public static int MenuKey { get { Poll(); return menuKey; } }

        public static bool IsOverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            hits.Clear();
            es.RaycastAll(data, hits);
            return hits.Count > 0;
        }

        static void Poll()
        {
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            axis = 0; jumpDown = dashDown = pointerDown = pointerHeld = mineHeld = sellDown = escDown = false;
            menuKey = 0;
            bool foundPointer = false;
            bool pressedThisFrame = false;
            Vector2 pos = pointerPos;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) axis -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) axis += 1;
            jumpDown = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            dashDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            mineHeld = Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.F);
            sellDown = Input.GetKeyDown(KeyCode.Q);
            escDown = Input.GetKeyDown(KeyCode.Escape);
            for (int i = 1; i <= 9; i++) if (Input.GetKeyDown(KeyCode.Alpha0 + i)) menuKey = i;

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (IsOverUI(t.position)) continue;
                    foundPointer = true;
                    pos = t.position;
                    pressedThisFrame = t.phase == TouchPhase.Began;
                    break;
                }
            }
            else if (Input.mousePresent)
            {
                pos = Input.mousePosition;
                if (Input.GetMouseButton(0)) { foundPointer = true; pressedThisFrame = Input.GetMouseButtonDown(0); }
            }
#elif ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) axis -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) axis += 1;
                jumpDown = kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
                dashDown = kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame;
                mineHeld = kb.eKey.isPressed || kb.fKey.isPressed;
                sellDown = kb.qKey.wasPressedThisFrame;
                escDown = kb.escapeKey.wasPressedThisFrame;
                if (kb.digit1Key.wasPressedThisFrame) menuKey = 1;
                if (kb.digit2Key.wasPressedThisFrame) menuKey = 2;
                if (kb.digit3Key.wasPressedThisFrame) menuKey = 3;
                if (kb.digit4Key.wasPressedThisFrame) menuKey = 4;
                if (kb.digit5Key.wasPressedThisFrame) menuKey = 5;
                if (kb.digit6Key.wasPressedThisFrame) menuKey = 6;
                if (kb.digit7Key.wasPressedThisFrame) menuKey = 7;
                if (kb.digit8Key.wasPressedThisFrame) menuKey = 8;
                if (kb.digit9Key.wasPressedThisFrame) menuKey = 9;
            }
            var ts = Touchscreen.current;
            bool anyTouch = false;
            if (ts != null)
            {
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed) continue;
                    anyTouch = true;
                    var p = t.position.ReadValue();
                    if (IsOverUI(p)) continue;
                    foundPointer = true;
                    pos = p;
                    pressedThisFrame = t.press.wasPressedThisFrame;
                    break;
                }
            }
            var mouse = Mouse.current;
            if (!anyTouch && mouse != null)
            {
                pos = mouse.position.ReadValue();
                if (mouse.leftButton.isPressed) { foundPointer = true; pressedThisFrame = mouse.leftButton.wasPressedThisFrame; }
            }
#endif
            axis = Mathf.Clamp(axis + VirtualAxis, -1f, 1f);
            if (virtualJump) { jumpDown = true; virtualJump = false; }
            if (virtualDash) { dashDown = true; virtualDash = false; }

            pointerPos = pos;
            if (foundPointer)
            {
                pointerOverUi = IsOverUI(pos);
                pointerHeld = !pointerOverUi;
                pointerDown = pressedThisFrame && !pointerOverUi;
            }
            else pointerOverUi = false;
        }
    }
}
