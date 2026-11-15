using UnityEngine;
using UnityEngine.UI;

namespace FannyIn2Trom
{
    /// <summary>
    /// Image de poisson qui ONDULE vraiment : le corps est découpé en tranches et déformé
    /// par une vague. Tenu par la queue, c'est la tête qui frétille ; en nageant, c'est la queue
    /// qui bat. "Fill" permet de faire disparaître la tête bouchée après bouchée.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class WigglyFish : MaskableGraphic
    {
        public Sprite sprite;
        /// <summary>Amplitude de la vague, en fraction de la hauteur de l'image.</summary>
        public float amplitude = 0.25f;
        /// <summary>Battements par seconde.</summary>
        public float frequency = 5f;
        /// <summary>Nombre d'ondulations sur la longueur du poisson.</summary>
        public float waves = 1.1f;
        /// <summary>Vrai : l'extrémité gauche (la tête) est libre et frétille. Faux : la queue (à droite) bat.</summary>
        public bool freeHead = true;
        /// <summary>Part visible du poisson depuis la queue (1 = entier). Sert quand Marine le mange.</summary>
        public float Fill = 1f;

        const int Segments = 28;
        float time;

        public override Texture mainTexture
        {
            get { return sprite != null ? sprite.texture : s_WhiteTexture; }
        }

        public static WigglyFish Create(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var w = go.AddComponent<WigglyFish>();
            w.sprite = sprite;
            w.raycastTarget = false;
            return w;
        }

        void Update()
        {
            time += Time.deltaTime;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (sprite == null) return;
            Rect r = GetPixelAdjustedRect();
            Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            float start = Mathf.Clamp01(1f - Fill);           // la tête (à gauche) disparaît en premier
            Color32 c = color;
            int idx = 0;
            for (int i = 0; i <= Segments; i++)
            {
                float t = Mathf.Lerp(start, 1f, i / (float)Segments);
                float x = r.xMin + t * r.width;
                // l'amplitude grandit vers l'extrémité libre
                float free = freeHead ? (1f - t) : t;
                float amp = amplitude * r.height * Mathf.Pow(free, 1.4f);
                float off = Mathf.Sin(2f * Mathf.PI * (t * waves - time * frequency)) * amp;
                float u = Mathf.Lerp(uv.x, uv.z, t);
                vh.AddVert(new Vector3(x, r.yMin + off), c, new Vector2(u, uv.y));
                vh.AddVert(new Vector3(x, r.yMax + off), c, new Vector2(u, uv.w));
                if (i > 0)
                {
                    vh.AddTriangle(idx - 2, idx - 1, idx + 1);
                    vh.AddTriangle(idx - 2, idx + 1, idx);
                }
                idx += 2;
            }
        }
    }
}
