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
        /// <summary>Assombrit le poisson près de la coupure (quand il entre dans une bouche).</summary>
        public float cutShade = 0f;
        /// <summary>Animation image par image (le poisson qui se tortille vraiment). Null = image fixe.</summary>
        public Sprite[] frames;
        /// <summary>Images par seconde de l'animation.</summary>
        public float fps = 12f;
        /// <summary>Espèce (pour la ligne médiane du corps dans FishData), -1 = inconnue.</summary>
        public int species = -1;
        /// <summary>Si &gt;= 0 : point d'attache le long du poisson (0 = tête, 1 = queue). Le pivot suit le corps
        /// d'une image à l'autre, donc la main (ou la bouche) reste accrochée au même endroit du poisson.</summary>
        public float holdAlong = -1f;
        float frameClock;
        public int Frame { get; private set; }

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
            if (frames != null && frames.Length > 1)
            {
                frameClock += Time.deltaTime * fps;
                int f = (int)frameClock % frames.Length;
                if (f != Frame || sprite != frames[f]) { Frame = f; sprite = frames[f]; SetMaterialDirty(); }
            }
            if (holdAlong >= 0f)
            {
                float y = species >= 0 && species < FishData.Centerline.Length ? FishData.CenterAt(species, Frame, holdAlong) : 0.5f;
                var p = new Vector2(holdAlong, y);
                if ((rectTransform.pivot - p).sqrMagnitude > 1e-8f) rectTransform.pivot = p;
            }
            SetVerticesDirty();
        }

        /// <summary>Change la vitesse de l'animation sans à-coup.</summary>
        public void SetFrames(Sprite[] f, float speed, int kind)
        {
            frames = f;
            fps = speed;
            species = kind;
            frameClock = Random.Range(0f, f != null ? f.Length : 1f);
            if (f != null && f.Length > 0) sprite = f[0];
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
                float span = Mathf.Max(0.0001f, 1f - start);
                float free = freeHead ? (1f - t) / span : (t - start) / span;   // 0 au point tenu, 1 au bout libre
                float amp = amplitude * r.height * Mathf.Pow(free, 1.4f);
                float off = Mathf.Sin(2f * Mathf.PI * (t * waves - time * frequency)) * amp;
                float u = Mathf.Lerp(uv.x, uv.z, t);
                Color32 cc = c;
                if (cutShade > 0f && start > 0f)
                {
                    float sh = cutShade * (1f - Mathf.Clamp01((t - start) / (0.14f * span)));
                    cc = Color32.Lerp(c, new Color32(60, 25, 25, c.a), sh);
                }
                vh.AddVert(new Vector3(x, r.yMin + off), cc, new Vector2(u, uv.y));
                vh.AddVert(new Vector3(x, r.yMax + off), cc, new Vector2(u, uv.w));
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
