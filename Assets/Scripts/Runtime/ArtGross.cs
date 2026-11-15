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

        // =============================================================== poissons

        public struct FishInfo { public string name, desc; public float weight; }

        public static readonly FishInfo[] Fishes =
        {
            new FishInfo { name = "La sardine", desc = "Légère et rapide", weight = 0.6f },
            new FishInfo { name = "Le maquereau", desc = "Le classique", weight = 1.0f },
            new FishInfo { name = "Le saumon", desc = "Lourd et visqueux", weight = 1.5f },
            new FishInfo { name = "La morue", desc = "Énorme, ça va faire mal", weight = 2.1f },
        };

        /// <summary>Poisson de profil, tête à gauche, queue à droite (on le tient par la queue).</summary>
        public static Sprite Fish(int type) { return Get("poisson_" + (type + 1), () => MakeFish(type)); }

        static Sprite MakeFish(int type)
        {
            var p = new Painter(560, 220);
            float[] lens = { 380, 440, 480, 500 };
            float[] hs = { 70, 92, 118, 136 };
            Color[] backs = { new Color(0.22f, 0.35f, 0.52f), new Color(0.10f, 0.38f, 0.40f), new Color(0.32f, 0.40f, 0.45f), new Color(0.42f, 0.38f, 0.24f) };
            Color[] bellies = { new Color(0.86f, 0.89f, 0.92f), new Color(0.88f, 0.90f, 0.90f), new Color(0.93f, 0.86f, 0.86f), new Color(0.90f, 0.86f, 0.74f) };
            float len = lens[type], h = hs[type];
            Color back = backs[type], belly = bellies[type];
            Color fin = Color.Lerp(back, new Color(0.6f, 0.6f, 0.6f), 0.4f); fin.a = 0.85f;
            Color outline = Color.Lerp(back, Color.black, 0.55f);
            float x0 = 560 - len - 30, cy = 110;
            float bcx = x0 + len * 0.42f, brx = len * 0.43f, bry = h * 0.5f;
            float tailBase = x0 + len * 0.86f;
            var rnd = new System.Random(31 + type);

            // nageoire caudale (fourchue)
            p.Polygon(fin, new Vector2(tailBase - 6, cy), new Vector2(556, cy + h * 0.62f), new Vector2(520, cy));
            p.Polygon(fin, new Vector2(tailBase - 6, cy), new Vector2(520, cy), new Vector2(556, cy - h * 0.62f));
            // nageoires dorsale / anale
            p.Polygon(fin, new Vector2(bcx - brx * 0.25f, cy + bry * 0.9f), new Vector2(bcx + brx * 0.05f, cy + bry * 1.55f), new Vector2(bcx + brx * 0.35f, cy + bry * 0.8f));
            if (type >= 2) p.Polygon(fin, new Vector2(bcx + brx * 0.45f, cy + bry * 0.6f), new Vector2(bcx + brx * 0.6f, cy + bry * 1.05f), new Vector2(bcx + brx * 0.75f, cy + bry * 0.4f));
            p.Polygon(fin, new Vector2(bcx + brx * 0.35f, cy - bry * 0.75f), new Vector2(bcx + brx * 0.55f, cy - bry * 1.2f), new Vector2(bcx + brx * 0.7f, cy - bry * 0.55f));
            // pédoncule + corps (contour, ventre, dos)
            p.Polygon(outline, new Vector2(bcx + brx * 0.6f, cy + bry * 0.62f), new Vector2(tailBase + 4, cy + h * 0.17f), new Vector2(tailBase + 4, cy - h * 0.17f), new Vector2(bcx + brx * 0.6f, cy - bry * 0.62f));
            p.Ellipse(bcx, cy, brx + 3, bry + 3, outline);
            p.Polygon(belly, new Vector2(bcx + brx * 0.6f, cy + bry * 0.56f), new Vector2(tailBase, cy + h * 0.13f), new Vector2(tailBase, cy - h * 0.13f), new Vector2(bcx + brx * 0.6f, cy - bry * 0.56f));
            p.Ellipse(bcx, cy, brx, bry, belly);
            Color backA = back; backA.a = 0.92f;
            p.Ellipse(bcx + 6, cy + bry * 0.32f, brx * 0.98f, bry * 0.72f, backA);
            p.Polygon(backA, new Vector2(bcx + brx * 0.55f, cy + bry * 0.58f), new Vector2(tailBase, cy + h * 0.14f), new Vector2(tailBase, cy + h * 0.02f), new Vector2(bcx + brx * 0.55f, cy + bry * 0.05f));
            // motifs selon l'espèce
            if (type == 1)
                for (int i = 0; i < 11; i++)
                {
                    float x = bcx - brx * 0.5f + i * brx * 0.13f;
                    p.Line(x, cy + bry * 0.25f, x + 14 + rnd.Next(10), cy + bry * 0.85f, 6, new Color(0.03f, 0.12f, 0.14f, 0.85f));
                }
            if (type == 2)
                for (int i = 0; i < 26; i++)
                    p.Circle(bcx - brx * 0.4f + rnd.Next((int)(brx * 1.5f)), cy + rnd.Next((int)(bry * 0.85f)), 2.5f + rnd.Next(3), new Color(0.08f, 0.08f, 0.1f, 0.9f));
            if (type == 2) p.Ellipse(bcx, cy - bry * 0.1f, brx * 0.85f, bry * 0.12f, new Color(0.95f, 0.65f, 0.65f, 0.35f));
            if (type == 3)
                for (int i = 0; i < 40; i++)
                    p.Ellipse(bcx - brx * 0.7f + rnd.Next((int)(brx * 1.6f)), cy - bry * 0.1f + rnd.Next((int)(bry * 0.95f)), 4 + rnd.Next(7), 3 + rnd.Next(5), new Color(0.25f, 0.22f, 0.12f, 0.55f));
            // écailles
            for (int gx = 0; gx < 14; gx++)
                for (int gy = -2; gy <= 2; gy++)
                {
                    float x = bcx - brx * 0.45f + gx * brx * 0.1f, y = cy + gy * bry * 0.3f + (gx % 2) * bry * 0.15f;
                    float nx = (x - bcx) / brx, ny = (y - cy) / bry;
                    if (nx * nx + ny * ny < 0.75f) p.Arc(x, y, bry * 0.16f, 1.5f, 100, 260, new Color(1f, 1f, 1f, 0.16f));
                }
            // ligne latérale, reflet mouillé
            p.Line(bcx - brx * 0.6f, cy + bry * 0.12f, tailBase, cy + h * 0.02f, 2.5f, new Color(1f, 1f, 1f, 0.35f));
            p.Ellipse(bcx - brx * 0.1f, cy + bry * 0.55f, brx * 0.6f, bry * 0.08f, new Color(1f, 1f, 1f, 0.35f));
            // tête : opercule, bouche, nageoire pectorale, œil
            float hx = x0 + len * 0.2f;
            p.Arc(hx - bry * 0.5f, cy, bry * 0.85f, 4, -60, 60, outline); // opercule
            p.Line(x0 + 4, cy - h * 0.06f, x0 + len * 0.07f, cy - h * 0.1f, 4, outline);
            if (type == 3) p.Line(x0 + len * 0.05f, cy - bry * 0.55f, x0 + len * 0.06f, cy - bry * 0.85f, 3, outline); // barbillon
            p.Polygon(fin, new Vector2(hx + 14, cy - bry * 0.1f), new Vector2(hx + len * 0.14f, cy - bry * 0.55f), new Vector2(hx + len * 0.1f, cy - bry * 0.05f));
            float ex = x0 + len * 0.09f, ey = cy + h * 0.12f, er = h * 0.12f + 3;
            p.Circle(ex, ey, er + 2, outline);
            p.Circle(ex, ey, er, new Color(0.95f, 0.85f, 0.55f));
            p.Circle(ex, ey, er * 0.62f, new Color(0.05f, 0.05f, 0.06f));
            p.Circle(ex - er * 0.25f, ey + er * 0.3f, er * 0.22f, Color.white);
            return p.ToSprite("poisson_" + type);
        }

        /// <summary>Anneau blanc (ronds dans l'eau).</summary>
        public static Sprite Ring { get { return Get("anneau", () => { var p = new Painter(128, 128); p.Ring(64, 64, 56, 7, Color.white); return p.ToSprite("anneau"); }); } }

        /// <summary>Étoile jaune (K.O., étourdie).</summary>
        public static Sprite Star { get { return Get("etoile", MakeStar); } }

        static Sprite MakeStar()
        {
            var p = new Painter(96, 96);
            Color c = new Color(1f, 0.85f, 0.15f), o = new Color(0.7f, 0.45f, 0.05f);
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? 44f : 18f;
                pts[i] = new Vector2(48 + Mathf.Cos(a) * r, 48 + Mathf.Sin(a) * r);
            }
            foreach (var col in new[] { o, c })
            {
                float k = col == o ? 1f : 0.86f;
                var inner = new Vector2[5];
                for (int i = 0; i < 5; i++) inner[i] = Vector2.Lerp(new Vector2(48, 48), pts[i * 2 + 1], k);
                p.Polygon(col, inner);
                for (int i = 0; i < 5; i++)
                    p.Polygon(col, Vector2.Lerp(new Vector2(48, 48), pts[(i * 2 + 9) % 10], k), Vector2.Lerp(new Vector2(48, 48), pts[i * 2], k), Vector2.Lerp(new Vector2(48, 48), pts[i * 2 + 1], k));
            }
            return p.ToSprite("etoile");
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
