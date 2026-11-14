using System.Collections.Generic;
using UnityEngine;

namespace FannyIn2Trom
{
    /// <summary>
    /// Dessins (générés par code) des épreuves "La souris" et "Que va-t-elle frotter ?" :
    /// souris pustuleuses, queue, pigeon, ascenseur, toilettes publiques.
    /// Chaque image peut être remplacée par un fichier du même nom dans Resources/Art.
    /// </summary>
    public static class ArtGross
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Get(string key, System.Func<Sprite> make)
        {
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            var tex = Resources.Load<Texture2D>("Art/" + key);
            s = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect)
                : make();
            cache[key] = s;
            return s;
        }

        // ================================================================ souris

        public struct MouseInfo { public string name; public string desc; public int ugliness; }

        public static readonly MouseInfo[] Mice =
        {
            new MouseInfo { name = "Gégé la galeuse", desc = "Pelée, un peu de pus", ugliness = 1 },
            new MouseInfo { name = "Pustulette", desc = "Couverte de pustules", ugliness = 2 },
            new MouseInfo { name = "Morveuse la borgne", desc = "Borgne, verdâtre, mouches", ugliness = 3 },
        };

        /// <summary>Souris pendue par la queue (queue en haut, tête en bas). frame 0/1 = pattes qui gigotent.</summary>
        public static Sprite Mouse(int variant, int frame)
        {
            return Get("souris_" + (variant + 1) + "_" + (frame + 1), () => MakeMouse(variant, frame));
        }

        /// <summary>Bout de queue qui dépasse de la bouche (le haut est dans la bouche).</summary>
        public static Sprite Tail { get { return Get("queue_souris", MakeTail); } }

        static Sprite MakeMouse(int v, int frame)
        {
            var p = new Painter(180, 340);
            Color[] furs = { new Color(0.52f, 0.47f, 0.42f), new Color(0.48f, 0.36f, 0.27f), new Color(0.45f, 0.50f, 0.38f) };
            Color fur = furs[v], dark = Color.Lerp(fur, Color.black, 0.5f), light = Color.Lerp(fur, Color.white, 0.25f);
            Color pink = new Color(0.93f, 0.62f, 0.62f), pinkDark = new Color(0.65f, 0.35f, 0.38f);
            var rnd = new System.Random(11 + v * 7);

            // queue (de l'accroche en haut jusqu'au corps)
            for (int i = 0; i < 12; i++)
            {
                float t0 = i / 12f, t1 = (i + 1) / 12f;
                float x0 = 90 + Mathf.Sin(t0 * 5f) * 10f, x1 = 90 + Mathf.Sin(t1 * 5f) * 10f;
                p.Line(x0, 336 - t0 * 105, x1, 336 - t1 * 105, 9, pinkDark);
                p.Line(x0, 336 - t0 * 105, x1, 336 - t1 * 105, 6, pink);
            }
            // pattes (gigotent selon la frame)
            float a = frame == 0 ? 1f : -1f;
            Leg(p, 50, 205, 22, 228 + 14 * a, dark, pink);
            Leg(p, 130, 205, 158, 228 - 14 * a, dark, pink);
            Leg(p, 52, 120, 20, 104 - 16 * a, dark, pink);
            Leg(p, 128, 120, 160, 104 + 16 * a, dark, pink);
            // corps + tête (contour puis pelage)
            p.Ellipse(90, 165, 58, 82, dark);
            p.Ellipse(90, 78, 46, 50, dark);
            p.Ellipse(90, 165, 54, 78, fur);
            p.Ellipse(90, 78, 42, 46, fur);
            p.Ellipse(75, 185, 18, 40, light);
            // plaques pelées
            for (int i = 0; i < 2 + v * 2; i++)
                p.Ellipse(60 + rnd.Next(60), 120 + rnd.Next(90), 9 + rnd.Next(10), 7 + rnd.Next(8), new Color(0.85f, 0.58f, 0.58f, 0.9f));
            // oreilles
            foreach (float ex in new[] { 52f, 128f })
            {
                p.Circle(ex, 104, 24, dark);
                p.Circle(ex, 104, 21, fur);
                p.Circle(ex, 104, 13, pink);
            }
            // museau
            p.Ellipse(90, 36, 21, 24, dark);
            p.Ellipse(90, 36, 18, 21, fur);
            p.Circle(90, 16, 9, pinkDark);
            p.Circle(88, 18, 3, Color.white);
            for (int i = -1; i <= 1; i++)
            {
                p.Line(80, 30, 40, 30 + i * 9, 2, new Color(0.15f, 0.12f, 0.1f));
                p.Line(100, 30, 140, 30 + i * 9, 2, new Color(0.15f, 0.12f, 0.1f));
            }
            // yeux (la 3e est borgne)
            p.Circle(72, 70, 8, Color.black);
            p.Circle(70, 72, 2.5f, Color.white);
            if (v == 2)
            {
                p.Line(100, 62, 116, 78, 4, Color.black);
                p.Line(100, 78, 116, 62, 4, Color.black);
            }
            else
            {
                p.Circle(108, 70, 8, Color.black);
                p.Circle(106, 72, 2.5f, Color.white);
            }
            // pustules
            int n = 3 + v * 7;
            for (int i = 0; i < n; i++)
            {
                bool onHead = rnd.NextDouble() < 0.3;
                float x = onHead ? 60 + rnd.Next(60) : 45 + rnd.Next(90);
                float y = onHead ? 50 + rnd.Next(50) : 105 + rnd.Next(120);
                float r = 4 + rnd.Next(5 + v * 2);
                p.Circle(x, y, r + 2, new Color(0.65f, 0.15f, 0.15f));
                p.Circle(x, y, r, new Color(0.92f, 0.35f, 0.3f));
                p.Circle(x, y + r * 0.2f, r * 0.55f, new Color(0.98f, 0.9f, 0.45f));
                p.Circle(x - r * 0.3f, y + r * 0.4f, r * 0.22f, Color.white);
            }
            if (v == 2) // morve verte
                p.Ellipse(92, 4, 6, 10, new Color(0.55f, 0.85f, 0.25f, 0.95f));
            return p.ToSprite("souris_" + v + "_" + frame);
        }

        static void Leg(Painter p, float x0, float y0, float x1, float y1, Color dark, Color pink)
        {
            p.Line(x0, y0, x1, y1, 13, dark);
            p.Line(x0, y0, x1, y1, 9, pink);
            p.Circle(x1, y1, 7, pink);
        }

        static Sprite MakeTail()
        {
            var p = new Painter(60, 230);
            Color pink = new Color(0.93f, 0.62f, 0.62f), pinkDark = new Color(0.65f, 0.35f, 0.38f);
            for (int i = 0; i < 14; i++)
            {
                float t0 = i / 14f, t1 = (i + 1) / 14f;
                float x0 = 30 + Mathf.Sin(t0 * 6f) * 12f * t0, x1 = 30 + Mathf.Sin(t1 * 6f) * 12f * t1;
                float w = Mathf.Lerp(10f, 4f, t0);
                p.Line(x0, 226 - t0 * 220, x1, 226 - t1 * 220, w + 3, pinkDark);
                p.Line(x0, 226 - t0 * 220, x1, 226 - t1 * 220, w, pink);
            }
            return p.ToSprite("queue_souris");
        }

        // ================================================================ pigeon

        /// <summary>Pigeon parisien (de profil, tourné vers la gauche). frame 1 = il roucoule.</summary>
        public static Sprite Pigeon(int frame) { return Get("pigeon_" + (frame + 1), () => MakePigeon(frame)); }

        static Sprite MakePigeon(int frame)
        {
            var p = new Painter(320, 280);
            Color body = new Color(0.55f, 0.58f, 0.66f), dark = new Color(0.25f, 0.27f, 0.32f), wing = new Color(0.47f, 0.50f, 0.58f);
            // pattes
            p.Line(150, 70, 140, 14, 9, new Color(0.85f, 0.45f, 0.45f));
            p.Line(190, 70, 198, 14, 9, new Color(0.85f, 0.45f, 0.45f));
            p.Line(140, 14, 118, 8, 6, new Color(0.85f, 0.45f, 0.45f));
            p.Line(198, 14, 176, 8, 6, new Color(0.85f, 0.45f, 0.45f));
            // queue
            p.Polygon(dark, new Vector2(240, 120), new Vector2(316, 92), new Vector2(316, 62), new Vector2(236, 82));
            // corps
            p.Ellipse(170, 115, 112, 74, dark);
            p.Ellipse(170, 115, 106, 68, body);
            p.Ellipse(195, 122, 78, 46, wing);
            p.Line(160, 120, 245, 108, 10, new Color(0.15f, 0.15f, 0.18f));
            p.Line(165, 100, 245, 92, 10, new Color(0.15f, 0.15f, 0.18f));
            // cou irisé (vert / violet)
            float hx = frame == 1 ? 72 : 80, hy = frame == 1 ? 168 : 182;
            float neck = frame == 1 ? 52 : 44;
            p.Ellipse(105, 150, neck, 50, new Color(0.30f, 0.55f, 0.40f));
            p.Ellipse(112, 140, neck * 0.8f, 36, new Color(0.50f, 0.35f, 0.60f, 0.7f));
            // tête
            p.Circle(hx, hy, 40, dark);
            p.Circle(hx, hy, 36, body);
            p.Polygon(new Color(0.2f, 0.2f, 0.2f), new Vector2(hx - 32, hy + 4), new Vector2(hx - 62, hy - 6), new Vector2(hx - 32, hy - 8));
            p.Ellipse(hx - 30, hy + 4, 8, 5, new Color(0.95f, 0.95f, 0.92f));
            p.Circle(hx - 8, hy + 10, 9, new Color(0.95f, 0.5f, 0.1f));
            p.Circle(hx - 8, hy + 10, 4, Color.black);
            // saletés
            var rnd = new System.Random(5);
            for (int i = 0; i < 9; i++)
                p.Circle(120 + rnd.Next(110), 70 + rnd.Next(80), 4 + rnd.Next(6), new Color(0.35f, 0.28f, 0.15f, 0.7f));
            return p.ToSprite("pigeon_" + frame);
        }

        // ============================================================ ascenseur

        public static Sprite Elevator { get { return Get("bg_ascenseur", MakeElevator); } }

        static Sprite MakeElevator()
        {
            const int W = 1600, H = 900;
            var p = new Painter(W, H);
            p.HorizontalGradientRect(0, 0, W, H, new Color(0.55f, 0.57f, 0.60f), new Color(0.80f, 0.82f, 0.85f), new Color(0.52f, 0.54f, 0.57f));
            for (int x = 0; x < W; x += 6) p.Rect(x, 0, x + 1, H, new Color(1f, 1f, 1f, 0.05f)); // inox brossé
            // panneaux
            p.RoundRect(800, 470, 520, 360, 10, new Color(0.62f, 0.64f, 0.67f));
            p.RoundRect(800, 470, 505, 345, 8, new Color(0.74f, 0.76f, 0.79f));
            // afficheur d'étage
            p.RoundRect(800, 845, 110, 40, 8, new Color(0.08f, 0.08f, 0.08f));
            // boutons
            p.RoundRect(1430, 470, 70, 220, 14, new Color(0.45f, 0.47f, 0.5f));
            for (int i = 0; i < 6; i++) p.Circle(1430, 340 + i * 44, 15, i == 3 ? new Color(1f, 0.75f, 0.2f) : new Color(0.85f, 0.86f, 0.88f));
            // sol
            p.Rect(0, 0, W, 90, new Color(0.25f, 0.24f, 0.26f));
            // traces de doigts et taches
            var rnd = new System.Random(3);
            for (int i = 0; i < 40; i++) p.Ellipse(rnd.Next(W), 150 + rnd.Next(600), 8 + rnd.Next(14), 6 + rnd.Next(10), new Color(0.3f, 0.28f, 0.25f, 0.12f));
            return p.ToSprite("bg_ascenseur");
        }

        // ============================================================ toilettes

        public static Sprite ToiletWall { get { return Get("bg_toilettes", MakeToiletWall); } }
        public static Sprite ToiletSeat { get { return Get("cuvette", MakeToiletSeat); } }

        static Sprite MakeToiletWall()
        {
            const int W = 1600, H = 900;
            var p = new Painter(W, H);
            p.Fill(new Color(0.55f, 0.58f, 0.55f));
            for (int y = 0; y < H; y += 60)
                for (int x = 0; x < W; x += 60)
                    p.Rect(x + 2, y + 2, x + 58, y + 58, ((x / 60 + y / 60) % 7 == 0) ? new Color(0.78f, 0.85f, 0.78f) : new Color(0.88f, 0.9f, 0.86f));
            var rnd = new System.Random(9);
            for (int i = 0; i < 30; i++)
                p.Ellipse(rnd.Next(W), rnd.Next(H), 15 + rnd.Next(40), 10 + rnd.Next(30), new Color(0.55f, 0.48f, 0.2f, 0.18f));
            for (int i = 0; i < 6; i++)
                p.Line(200 + i * 230, 900, 210 + i * 230 + rnd.Next(30), 500 + rnd.Next(200), 6, new Color(0.45f, 0.38f, 0.2f, 0.25f)); // coulures
            p.Rect(0, 0, W, 160, new Color(0.35f, 0.33f, 0.3f));
            return p.ToSprite("bg_toilettes");
        }

        static Sprite MakeToiletSeat()
        {
            var p = new Painter(700, 520);
            p.Ellipse(350, 260, 340, 250, new Color(0.62f, 0.62f, 0.6f));
            p.Ellipse(350, 260, 330, 240, new Color(0.95f, 0.95f, 0.93f));
            p.Ellipse(350, 245, 210, 160, new Color(0.62f, 0.62f, 0.6f));
            p.Ellipse(350, 245, 200, 150, new Color(0.62f, 0.75f, 0.78f));
            p.Ellipse(350, 225, 120, 80, new Color(0.55f, 0.68f, 0.72f));
            var rnd = new System.Random(21);
            for (int i = 0; i < 26; i++)
            {
                float ang = (float)(rnd.NextDouble() * Mathf.PI * 2);
                float r = 270 + rnd.Next(40);
                p.Ellipse(350 + Mathf.Cos(ang) * r, 260 + Mathf.Sin(ang) * r * 0.72f, 8 + rnd.Next(16), 5 + rnd.Next(10),
                    i % 3 == 0 ? new Color(0.45f, 0.30f, 0.12f, 0.7f) : new Color(0.85f, 0.75f, 0.25f, 0.55f));
            }
            p.Line(150, 90, 230, 140, 3, new Color(0.4f, 0.4f, 0.4f, 0.6f)); // fêlure
            return p.ToSprite("cuvette");
        }
    }
}
