using UnityEngine;

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
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                screen = t.position;
                down = t.phase == TouchPhase.Began;
                up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                held = !up;
                screen = ToCanvas(screen, canvasScale);
            }
            else if (Input.mousePresent || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
            {
                down = Input.GetMouseButtonDown(0);
                held = Input.GetMouseButton(0);
                up = Input.GetMouseButtonUp(0);
                screen = ToCanvas(Input.mousePosition, canvasScale);
            }

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
