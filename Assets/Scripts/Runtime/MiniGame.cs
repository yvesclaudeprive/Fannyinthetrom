using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FannyIn2Trom
{
    /// <summary>Base commune des épreuves.</summary>
    public abstract class MiniGame : MonoBehaviour
    {
        public abstract string Title { get; }
        public abstract string Instruction { get; }
        protected abstract float BaseTimeLimit { get; }
        /// <summary>Vrai si l'épreuve affiche elle-même son chrono (barre du décor breton).</summary>
        public virtual bool HasOwnTimer { get { return false; } }
        /// <summary>Vrai si l'épreuve se joue téléphone à la verticale.</summary>
        public virtual bool WantsPortrait { get { return false; } }

        public bool Done { get; private set; }
        public bool Won { get; private set; }
        public int Points { get; private set; }

        protected RectTransform Root;
        protected Vector2 Size;          // taille de l'écran en unités canvas
        protected float Difficulty = 1f; // 1 = niveau 1, augmente à chaque tour
        protected float TimeLimit;
        protected float TimeLeft;
        protected bool Locked;           // vrai pendant les animations de fin

        public void Setup(RectTransform root, Vector2 size, float difficulty)
        {
            Root = root;
            Size = size;
            Difficulty = difficulty;
            TimeLimit = Mathf.Max(BaseTimeLimit * 0.5f, BaseTimeLimit / Mathf.Sqrt(difficulty));
            TimeLeft = TimeLimit;
            Build();
        }

        protected abstract void Build();
        protected abstract void Tick(float dt);
        protected virtual void OnTimerChanged(float fraction) { }

        public IEnumerator Run(Hud hud)
        {
            while (!Done)
            {
                float dt = Time.deltaTime;
                if (!Locked)
                {
                    TimeLeft -= dt;
                    if (TimeLeft <= 0f)
                    {
                        TimeLeft = 0f;
                        Locked = true;
                        StartCoroutine(OnTimeUp());
                    }
                }
                float frac = TimeLimit > 0f ? TimeLeft / TimeLimit : 0f;
                hud.SetTimer(frac, !HasOwnTimer);
                OnTimerChanged(frac);
                Tick(dt);
                yield return null;
            }
        }

        /// <summary>Par défaut, la fin du temps = épreuve ratée.</summary>
        protected virtual IEnumerator OnTimeUp()
        {
            Pop("Trop tard !", Vector2.zero, new Color(1f, 0.4f, 0.35f), 110);
            yield return new WaitForSeconds(0.9f);
            Finish(false, 0);
        }

        protected void Finish(bool won, int points)
        {
            Locked = true;
            Won = won;
            Points = won ? points : 0;
            Done = true;
        }

        // ------------------------------------------------------------ effets

        /// <summary>Texte qui apparaît en grossissant puis s'envole.</summary>
        protected void Pop(string text, Vector2 pos, Color color, int size = 90)
        {
            var t = UIKit.Text("Pop", Root, text, size, color, new Vector2(Mathf.Min(1400f, Size.x - 40f), 200), pos);
            StartCoroutine(PopAnim(t));
        }

        IEnumerator PopAnim(Text t)
        {
            var rt = t.rectTransform;
            Vector2 start = rt.anchoredPosition;
            float d = 1.1f;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                float k = e / d;
                float s = k < 0.15f ? Mathf.Lerp(0.3f, 1.2f, k / 0.15f) : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((k - 0.15f) / 0.15f));
                rt.localScale = Vector3.one * s;
                rt.anchoredPosition = start + Vector2.up * (k * 80f);
                var c = t.color;
                c.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                t.color = c;
                yield return null;
            }
            Destroy(t.gameObject);
        }

        protected static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>Place une image de façon à remplir l'écran sans la déformer (rognage centré).</summary>
        protected Image Cover(string name, Sprite sprite, out float scale)
        {
            var img = UIKit.Image(name, Root, sprite, Size, Vector2.zero);
            scale = 1f;
            if (sprite != null)
            {
                scale = Mathf.Max(Size.x / sprite.rect.width, Size.y / sprite.rect.height);
                img.rectTransform.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height) * scale;
            }
            return img;
        }
    }
}
