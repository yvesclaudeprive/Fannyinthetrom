using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace FannyIn2Trom
{
    /// <summary>
    /// Doigt (iPhone) ou souris (éditeur), en coordonnées "canvas" :
    /// origine au centre de l'écran, 1080 unités de haut.
    /// </summary>
    public static class Pointer
    {
        public static bool Down { get; private set; }
        public static bool Held { get; private set; }
        public static bool Up { get; private set; }
        public static Vector2 Pos { get; private set; }
        /// <summary>Déplacement depuis l'image précédente (0 au moment de l'appui).</summary>
        public static Vector2 Delta { get; private set; }

        static int lastFrame = -1;

        public static void Update(float canvasScale)
        {
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;

            bool down = false, held = false, up = false;
            Vector2 screen = Pos;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // Nouveau système d'entrées (Input System)
            var ts = Touchscreen.current;
            if (ts != null && (ts.primaryTouch.press.isPressed || ts.primaryTouch.press.wasReleasedThisFrame))
            {
                var touch = ts.primaryTouch;
                screen = ToCanvas(touch.position.ReadValue(), canvasScale);
                down = touch.press.wasPressedThisFrame;
                up = touch.press.wasReleasedThisFrame;
                held = touch.press.isPressed;
            }
            else if (Mouse.current != null)
            {
                var m = Mouse.current;
                screen = ToCanvas(m.position.ReadValue(), canvasScale);
                down = m.leftButton.wasPressedThisFrame;
                held = m.leftButton.isPressed;
                up = m.leftButton.wasReleasedThisFrame;
            }
#else
            // Ancien système d'entrées (Input Manager)
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                down = t.phase == TouchPhase.Began;
                up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                held = !up;
                screen = ToCanvas(t.position, canvasScale);
            }
            else if (Input.mousePresent || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
            {
                down = Input.GetMouseButtonDown(0);
                held = Input.GetMouseButton(0);
                up = Input.GetMouseButtonUp(0);
                screen = ToCanvas(Input.mousePosition, canvasScale);
            }
#endif

            Delta = down || !Held ? Vector2.zero : screen - Pos;
            Pos = screen;
            Down = down;
            Held = held;
            Up = up;
        }

        static Vector2 ToCanvas(Vector2 screenPos, float scale)
        {
            if (scale <= 0f) scale = 1f;
            return (screenPos - new Vector2(Screen.width, Screen.height) * 0.5f) / scale;
        }
    }
}
