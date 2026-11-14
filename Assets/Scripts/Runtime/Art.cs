using System.Collections.Generic;
using UnityEngine;

namespace FannyIn2Trom
{
    /// <summary>
    /// Toutes les images du jeu.
    /// Les images fournies sont dans Assets/Resources/Art/ (voir README) :
    /// si un fichier existe il est utilisé, sinon un visuel provisoire est dessiné par code.
    /// </summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static readonly Color Skin = new Color(0.87f, 0.66f, 0.54f);
        public static readonly Color SkinDark = new Color(0.55f, 0.33f, 0.24f);

        // --------------------------------------------------- images fournies

        public static Sprite BgBretagne { get { return Load("bg_bretagne", null); } }
        public static Sprite FannyFace { get { return Load("fanny_face", null); } }
        public static Sprite FannyProfil { get { return Load("fanny_profil", null); } }
        public static Sprite FannyDos { get { return Load("fanny_dos", null); } }
        public static Sprite FannyVisage { get { return Load("fanny_visage", null); } }
        /// <summary>Optionnel : visage dégoûté. Si absent, le visage normal est teinté en vert.</summary>
        public static Sprite FannyVisageBeurk { get { return Load("fanny_visage_beurk", null); } }
        public static Sprite BgMetro { get { return Load("bg_metro", MakeMetroWall); } }
        public static Sprite HandGrip { get { return Load("main_barre", MakeHandGrip); } }
        public static Sprite HandOpen { get { return Load("main_ouverte", MakeHandOpen); } }

        /// <summary>Vrai si bg_metro est l'image provisoire (fenêtres transparentes + tunnel animé).</summary>
        public static bool MetroIsPlaceholder { get { return !Exists("bg_metro"); } }
        /// <summary>Vrai si main_barre est le dessin provisoire (deux poings) et non l'image fournie (bras + main).</summary>
        public static bool HandGripIsPlaceholder { get { return !Exists("main_barre"); } }

        // --- Repères dans les images fournies (fractions de l'image) ---
        /// <summary>bg_metro : position (x) et largeur de la barre verticale, en fraction de la largeur.</summary>
        public const float MetroPoleX = 0.6766f, MetroPoleWidth = 0.0225f;
        /// <summary>main_barre : point de contact des doigts sur la barre (x,y vers le haut) et largeur de l'image en "largeurs de barre".</summary>
        public static readonly Vector2 HandGripPivot = new Vector2(0.9656f, 0.9526f);
        public const float HandWidthInBars = 14.87f;

        static bool Exists(string key) { return LoadFile(key) != null; }

        // ----------------------------------------------------- formes de base

        public static Sprite Circle { get { return Load("~circle", () => { var p = new Painter(64, 64); p.Circle(32, 32, 31, Color.white); return p.ToSprite("circle"); }); } }

        public static Sprite RoundedRect
        {
            get
            {
                return Load("~rrect", () =>
                {
                    var p = new Painter(64, 64);
                    p.RoundRect(32, 32, 31.5f, 31.5f, 20, Color.white);
                    return p.ToSprite("rrect", new Vector4(24, 24, 24, 24));
                });
            }
        }

        public static Sprite Heart
        {
            get
            {
                return Load("~heart", () =>
                {
                    var p = new Painter(64, 64);
                    Color c = new Color(0.93f, 0.16f, 0.25f);
                    p.Circle(21, 40, 15, c);
                    p.Circle(43, 40, 15, c);
                    p.Polygon(c, new Vector2(7.5f, 35), new Vector2(56.5f, 35), new Vector2(32, 6));
                    p.Circle(19, 45, 5, new Color(1, 1, 1, 0.6f));
                    return p.ToSprite("heart");
                });
            }
        }

        /// <summary>Boucle du lasso.</summary>
        public static Sprite LassoLoop
        {
            get
            {
                return Load("~lasso", () =>
                {
                    var p = new Painter(160, 160);
                    p.Ring(80, 80, 66, 16, new Color(0.36f, 0.22f, 0.10f));
                    p.Ring(80, 80, 66, 10, new Color(0.78f, 0.58f, 0.33f));
                    for (int i = 0; i < 24; i++)
                    {
                        float a0 = i * 15f;
                        p.Arc(80, 80, 66, 3f, a0, a0 + 6f, new Color(0.45f, 0.29f, 0.13f));
                    }
                    return p.ToSprite("lasso");
                });
            }
        }

        /// <summary>Barre verticale métallique (dégradé horizontal).</summary>
        public static Sprite MetalBar
        {
            get
            {
                return Load("~bar", () =>
                {
                    var p = new Painter(64, 8);
                    p.HorizontalGradientRect(0, 0, 64, 8, new Color(0.45f, 0.48f, 0.52f), new Color(0.97f, 0.98f, 1f), new Color(0.35f, 0.37f, 0.42f));
                    p.Rect(16, 0, 22, 8, new Color(1, 1, 1, 0.8f));
                    return p.ToSprite("bar");
                });
            }
        }

        public static Sprite Germ(int variant)
        {
            variant = Mathf.Abs(variant) % 3;
            return Load("~germ" + variant, () => MakeGerm(variant));
        }

        // -------------------------------------------------------- chargement

        static readonly Dictionary<string, Sprite> files = new Dictionary<string, Sprite>();

        /// <summary>Image fournie dans Resources/Art (ou null si absente).</summary>
        static Sprite LoadFile(string key)
        {
            Sprite s;
            if (files.TryGetValue(key, out s)) return s;
            var tex = Resources.Load<Texture2D>("Art/" + key);
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                s.name = key;
            }
            files[key] = s;
            return s;
        }

        static Sprite Load(string key, System.Func<Sprite> fallback)
        {
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            s = key.StartsWith("~") ? null : LoadFile(key);
            if (s == null && fallback != null) s = fallback();
            cache[key] = s;
            return s;
        }

        // ---------------------------------------------- visuels provisoires

        static Sprite MakeGerm(int v)
        {
            var p = new Painter(128, 128);
            Color[] bodies = { new Color(0.45f, 0.82f, 0.25f), new Color(0.62f, 0.85f, 0.15f), new Color(0.30f, 0.70f, 0.45f) };
            Color body = bodies[v];
            Color dark = Color.Lerp(body, Color.black, 0.45f);
            int spikes = 9 + v * 2;
            for (int i = 0; i < spikes; i++)
            {
                float a = i * Mathf.PI * 2f / spikes;
                p.Line(64, 64, 64 + Mathf.Cos(a) * 54, 64 + Mathf.Sin(a) * 54, 9, dark);
                p.Circle(64 + Mathf.Cos(a) * 54, 64 + Mathf.Sin(a) * 54, 7, dark);
            }
            p.Ellipse(64, 64, 42, 38, dark);
            p.Ellipse(64, 64, 38, 34, body);
            p.Circle(52, 78, 5, Color.Lerp(body, Color.white, 0.5f));
            // yeux
            p.Circle(50, 68, 11, Color.white);
            p.Circle(78, 68, 11, Color.white);
            p.Circle(52 + v, 66, 5, Color.black);
            p.Circle(80 + v, 66, 5, Color.black);
            // bouche
            p.Arc(64, 52, 12, 4, 200, 340, new Color(0.15f, 0.25f, 0.1f));
            return p.ToSprite("germ" + v);
        }

        /// <summary>Poing qui agrippe une barre verticale (la barre passe au milieu).</summary>
        static Sprite MakeHandGrip()
        {
            var p = new Painter(256, 256);
            Color o = SkinDark, s = Skin, l = Color.Lerp(Skin, Color.white, 0.25f);
            // contour
            p.RoundRect(96, 128, 70, 92, 40, o);
            for (int i = 0; i < 4; i++) p.Line(110, 70 + i * 40, 200, 70 + i * 40, 46, o);
            p.Line(80, 190, 150, 225, 46, o);
            // remplissage
            p.RoundRect(96, 128, 64, 86, 36, s);
            for (int i = 0; i < 4; i++)
            {
                float y = 70 + i * 40;
                p.Line(110, y, 200, y, 38, s);
                p.Line(120, y + 8, 196, y + 8, 8, l);
                p.Circle(204, y, 6, new Color(1f, 0.85f, 0.8f)); // ongle
            }
            p.Line(80, 190, 150, 225, 38, s);
            p.Line(90, 200, 145, 228, 8, l);
            return p.ToSprite("main_barre");
        }

        /// <summary>Main ouverte (paume vers l'écran).</summary>
        static Sprite MakeHandOpen()
        {
            var p = new Painter(256, 320);
            Color o = SkinDark, s = Skin, l = Color.Lerp(Skin, Color.white, 0.3f);
            float[] fx = { 70, 108, 146, 184 };
            float[] fl = { 95, 120, 115, 90 };
            p.RoundRect(128, 110, 86, 92, 46, o);
            for (int i = 0; i < 4; i++) p.Line(fx[i], 170, fx[i] + (i - 1.5f) * 10, 170 + fl[i], 42, o);
            p.Line(50, 120, 10, 205, 44, o);
            p.RoundRect(128, 110, 80, 86, 42, s);
            for (int i = 0; i < 4; i++)
            {
                p.Line(fx[i], 170, fx[i] + (i - 1.5f) * 10, 170 + fl[i], 34, s);
                p.Circle(fx[i] + (i - 1.5f) * 10, 170 + fl[i] - 4, 8, new Color(1f, 0.86f, 0.82f));
            }
            p.Line(50, 120, 10, 205, 36, s);
            p.Ellipse(128, 100, 45, 40, l);
            p.Arc(128, 120, 50, 4, 200, 330, new Color(0.7f, 0.45f, 0.38f));
            return p.ToSprite("main_ouverte");
        }

        /// <summary>
        /// Intérieur de rame de métro parisien. Les vitres sont transparentes :
        /// le tunnel défile derrière (animé dans le jeu).
        /// </summary>
        static Sprite MakeMetroWall()
        {
            const int W = 1600, H = 900;
            var p = new Painter(W, H);
            // parois
            p.VerticalGradient(0, H, new Color(0.80f, 0.80f, 0.76f), new Color(0.93f, 0.93f, 0.90f));
            // plafond + néons
            p.Rect(0, 830, W, H, new Color(0.88f, 0.89f, 0.88f));
            for (int i = 0; i < 4; i++) p.RoundRect(200 + i * 400, 862, 150, 12, 10, new Color(1f, 1f, 0.94f));
            // plan de ligne
            p.RoundRect(800, 780, 760, 26, 8, Color.white);
            p.Line(80, 780, 1520, 780, 10, new Color(0.73f, 0.30f, 0.62f));
            for (int i = 0; i < 13; i++) p.Circle(90 + i * 118, 780, 9, i == 6 ? new Color(0.95f, 0.75f, 0.1f) : Color.white);
            for (int i = 0; i < 13; i++) p.Ring(90 + i * 118, 780, 9, 4, new Color(0.73f, 0.30f, 0.62f));
            // fenêtres (trous) + cadres
            float[] wx = { 290, 800, 1310 };
            foreach (float cx in wx)
            {
                p.RoundRect(cx, 560, 230, 140, 46, new Color(0.62f, 0.64f, 0.66f));
                p.RoundRect(cx, 560, 216, 126, 38, new Color(0.38f, 0.40f, 0.44f));
            }
            // on perce les vitres
            foreach (float cx in wx) p.Erase(cx, 560, 210, 120, 34);
            // ceinture verte RATP
            p.Rect(0, 395, W, 412, new Color(0.15f, 0.55f, 0.48f));
            // sièges
            for (int i = 0; i < 4; i++)
            {
                float cx = 200 + i * 400;
                Color seat = new Color(0.16f, 0.42f, 0.62f);
                p.RoundRect(cx, 300, 150, 80, 30, Color.Lerp(seat, Color.black, 0.35f));
                p.RoundRect(cx, 302, 142, 72, 26, seat);
                for (int k = 0; k < 18; k++)
                {
                    float px = cx - 120 + (k % 6) * 48, py = 255 + (k / 6) * 40;
                    p.Circle(px, py, 6, new Color(0.95f, 0.55f, 0.2f, 0.8f));
                    p.Circle(px + 24, py + 20, 5, new Color(0.3f, 0.8f, 0.75f, 0.8f));
                }
                p.RoundRect(cx, 205, 160, 26, 14, Color.Lerp(seat, Color.black, 0.45f));
            }
            // sol
            p.Rect(0, 0, W, 170, new Color(0.30f, 0.31f, 0.35f));
            var rnd = new System.Random(4);
            for (int i = 0; i < 900; i++) p.Circle(rnd.Next(W), rnd.Next(165), 1.6f, new Color(0.5f, 0.5f, 0.55f, 0.7f));
            p.Rect(0, 165, W, 178, new Color(0.65f, 0.66f, 0.68f));
            return p.ToSprite("bg_metro");
        }

    }
}
