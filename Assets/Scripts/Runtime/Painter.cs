using System;
using UnityEngine;

namespace FannyIn2Trom
{
    /// <summary>
    /// Petit moteur de dessin "en pixels" (anti-aliasé) utilisé pour générer
    /// les visuels provisoires du jeu tant que les vraies images ne sont pas fournies.
    /// Repère : (0,0) en bas à gauche, y vers le haut (comme Texture2D).
    /// </summary>
    public class Painter
    {
        public readonly int W;
        public readonly int H;
        readonly Color[] px;

        public Painter(int w, int h)
        {
            W = w;
            H = h;
            px = new Color[w * h];
        }

        // ------------------------------------------------------------------ base

        public void Fill(Color c)
        {
            for (int i = 0; i < px.Length; i++) px[i] = c;
        }

        public void VerticalGradient(float y0, float y1, Color bottom, Color top)
        {
            int a = Mathf.Clamp(Mathf.FloorToInt(y0), 0, H);
            int b = Mathf.Clamp(Mathf.CeilToInt(y1), 0, H);
            for (int y = a; y < b; y++)
            {
                float t = Mathf.InverseLerp(y0, y1, y + 0.5f);
                Color c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < W; x++) Blend(x, y, c, 1f);
            }
        }

        public void HorizontalGradientRect(float x0, float y0, float x1, float y1, Color left, Color mid, Color right)
        {
            int ya = Mathf.Clamp(Mathf.FloorToInt(y0), 0, H), yb = Mathf.Clamp(Mathf.CeilToInt(y1), 0, H);
            int xa = Mathf.Clamp(Mathf.FloorToInt(x0), 0, W), xb = Mathf.Clamp(Mathf.CeilToInt(x1), 0, W);
            for (int x = xa; x < xb; x++)
            {
                float t = Mathf.InverseLerp(x0, x1, x + 0.5f);
                Color c = t < 0.5f ? Color.Lerp(left, mid, t * 2f) : Color.Lerp(mid, right, (t - 0.5f) * 2f);
                for (int y = ya; y < yb; y++) Blend(x, y, c, 1f);
            }
        }

        void Blend(int x, int y, Color c, float coverage)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            float a = c.a * coverage;
            if (a <= 0f) return;
            int i = y * W + x;
            Color d = px[i];
            float outA = a + d.a * (1f - a);
            if (outA <= 0.0001f) { px[i] = Color.clear; return; }
            float k = d.a * (1f - a);
            px[i] = new Color(
                (c.r * a + d.r * k) / outA,
                (c.g * a + d.g * k) / outA,
                (c.b * a + d.b * k) / outA,
                outA);
        }

        /// <summary>Dessine une forme décrite par une fonction de distance signée (négatif = intérieur).</summary>
        public void Shape(float minX, float minY, float maxX, float maxY, Func<float, float, float> sdf, Color c)
        {
            int xa = Mathf.Max(0, Mathf.FloorToInt(minX) - 1), xb = Mathf.Min(W, Mathf.CeilToInt(maxX) + 1);
            int ya = Mathf.Max(0, Mathf.FloorToInt(minY) - 1), yb = Mathf.Min(H, Mathf.CeilToInt(maxY) + 1);
            for (int y = ya; y < yb; y++)
                for (int x = xa; x < xb; x++)
                {
                    float d = sdf(x + 0.5f, y + 0.5f);
                    float cov = Mathf.Clamp01(0.5f - d);
                    if (cov > 0f) Blend(x, y, c, cov);
                }
        }

        // ------------------------------------------------------------ primitives

        public void Circle(float cx, float cy, float r, Color c)
        {
            Shape(cx - r, cy - r, cx + r, cy + r, (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color c)
        {
            float m = Mathf.Min(rx, ry);
            Shape(cx - rx, cy - ry, cx + rx, cy + ry, (x, y) =>
            {
                float nx = (x - cx) / rx, ny = (y - cy) / ry;
                return (Mathf.Sqrt(nx * nx + ny * ny) - 1f) * m;
            }, c);
        }

        public void Ring(float cx, float cy, float r, float thickness, Color c)
        {
            float o = r + thickness * 0.5f;
            Shape(cx - o, cy - o, cx + o, cy + o, (x, y) =>
                Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - thickness * 0.5f, c);
        }

        /// <summary>Arc de cercle (épaisseur donnée) entre deux angles en degrés (0 = droite, sens trigo).</summary>
        public void Arc(float cx, float cy, float r, float thickness, float fromDeg, float toDeg, Color c)
        {
            float o = r + thickness;
            float a0 = fromDeg * Mathf.Deg2Rad, a1 = toDeg * Mathf.Deg2Rad;
            Vector2 p0 = new Vector2(cx + Mathf.Cos(a0) * r, cy + Mathf.Sin(a0) * r);
            Vector2 p1 = new Vector2(cx + Mathf.Cos(a1) * r, cy + Mathf.Sin(a1) * r);
            Shape(cx - o, cy - o, cx + o, cy + o, (x, y) =>
            {
                float ang = Mathf.Atan2(y - cy, x - cx);
                if (ang < a0) ang += Mathf.PI * 2f;
                bool inside = ang >= a0 && ang <= a1 || ang + Mathf.PI * 2f <= a1;
                if (inside)
                    return Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - thickness * 0.5f;
                // extrémités arrondies
                float d0 = Vector2.Distance(new Vector2(x, y), p0);
                float d1 = Vector2.Distance(new Vector2(x, y), p1);
                return Mathf.Min(d0, d1) - thickness * 0.5f;
            }, c);
        }

        public void Rect(float x0, float y0, float x1, float y1, Color c)
        {
            RoundRect((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, Mathf.Abs(x1 - x0) * 0.5f, Mathf.Abs(y1 - y0) * 0.5f, 0f, c);
        }

        public void RoundRect(float cx, float cy, float hw, float hh, float radius, Color c)
        {
            Shape(cx - hw, cy - hh, cx + hw, cy + hh, (x, y) =>
            {
                float qx = Mathf.Abs(x - cx) - hw + radius;
                float qy = Mathf.Abs(y - cy) - hh + radius;
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            }, c);
        }

        /// <summary>Segment épais à bouts ronds (capsule).</summary>
        public void Line(float x0, float y0, float x1, float y1, float thickness, Color c)
        {
            float r = thickness * 0.5f;
            Vector2 a = new Vector2(x0, y0), b = new Vector2(x1, y1);
            Vector2 ba = b - a;
            float len2 = Mathf.Max(ba.sqrMagnitude, 0.0001f);
            Shape(Mathf.Min(x0, x1) - r, Mathf.Min(y0, y1) - r, Mathf.Max(x0, x1) + r, Mathf.Max(y0, y1) + r, (x, y) =>
            {
                Vector2 pa = new Vector2(x, y) - a;
                float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / len2);
                return (pa - ba * h).magnitude - r;
            }, c);
        }

        /// <summary>Polygone convexe (sommets dans n'importe quel sens).</summary>
        public void Polygon(Color c, params Vector2[] pts)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            float area = 0f;
            for (int i = 0; i < pts.Length; i++)
            {
                Vector2 p = pts[i], q = pts[(i + 1) % pts.Length];
                area += p.x * q.y - q.x * p.y;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            float sign = area >= 0f ? 1f : -1f;
            Shape(minX, minY, maxX, maxY, (x, y) =>
            {
                float d = float.MinValue;
                for (int i = 0; i < pts.Length; i++)
                {
                    Vector2 p = pts[i], q = pts[(i + 1) % pts.Length];
                    Vector2 e = (q - p).normalized;
                    // normale extérieure
                    Vector2 n = new Vector2(e.y, -e.x) * sign;
                    d = Mathf.Max(d, Vector2.Dot(new Vector2(x, y) - p, n));
                }
                return d;
            }, c);
        }

        /// <summary>Rend transparent un rectangle arrondi (pour percer des fenêtres).</summary>
        public void Erase(float cx, float cy, float hw, float hh, float radius)
        {
            int xa = Mathf.Max(0, Mathf.FloorToInt(cx - hw) - 1), xb = Mathf.Min(W, Mathf.CeilToInt(cx + hw) + 1);
            int ya = Mathf.Max(0, Mathf.FloorToInt(cy - hh) - 1), yb = Mathf.Min(H, Mathf.CeilToInt(cy + hh) + 1);
            for (int y = ya; y < yb; y++)
                for (int x = xa; x < xb; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - cx) - hw + radius;
                    float qy = Mathf.Abs(y + 0.5f - cy) - hh + radius;
                    float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                    float d = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float cov = Mathf.Clamp01(0.5f - d);
                    int i = y * W + x;
                    Color c = px[i];
                    c.a *= 1f - cov;
                    px[i] = c;
                }
        }

        // ---------------------------------------------------------------- export

        public Texture2D ToTexture(string name = "painted")
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(px);
            tex.Apply(false, false);
            return tex;
        }

        public Sprite ToSprite(string name = "painted", Vector4 border = default(Vector4))
        {
            var tex = ToTexture(name);
            var s = Sprite.Create(tex, new UnityEngine.Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            s.name = name;
            return s;
        }
    }
}
