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

        // ------------------------------------------------------------- vomi

        class Drop { public RectTransform rt; public Vector2 v; }

        /// <summary>Fanny vomit depuis la position "mouth" ; "head" (optionnel) est secouée pendant le jet.</summary>
        protected IEnumerator Vomit(Vector2 mouth, RectTransform head, Vector2 headPos)
        {
            float floorY = -Size.y * 0.5f + 60f;
            Color[] colors =
            {
                new Color(0.62f, 0.72f, 0.18f), new Color(0.78f, 0.74f, 0.25f),
                new Color(0.50f, 0.62f, 0.15f), new Color(0.85f, 0.80f, 0.40f)
            };
            var puddle = UIKit.Image("Flaque", Root, Art.Circle, Vector2.zero, new Vector2(mouth.x, floorY), new Color(0.62f, 0.7f, 0.2f, 0.95f));
            var drops = new System.Collections.Generic.List<Drop>();
            float puddleW = 0f;
            bool splashed = false;

            Sfx.Play(Sfx.Vomit);
            Sfx.Vibrate();
            var fa = head != null ? head.GetComponent<FaceAnimator>() : null;
            if (fa != null) { fa.SetGrimace(1f); fa.Talk(1.6f, false); }
            Pop("BLEUUAARGH !", mouth + new Vector2(430f, 180f), new Color(0.7f, 1f, 0.3f), 96);

            const float spew = 1.3f, total = 2.3f;
            float spawnAcc = 0f;
            for (float e = 0f; e < total; e += Time.deltaTime)
            {
                float dt = Time.deltaTime;
                if (e < spew)
                {
                    spawnAcc += dt * 110f;
                    while (spawnAcc >= 1f)
                    {
                        spawnAcc -= 1f;
                        float size = Random.Range(26f, 66f);
                        var img = UIKit.Image("Vomi", Root, Art.Circle, new Vector2(size * 1.3f, size), mouth + new Vector2(Random.Range(-25f, 25f), 0f),
                            colors[Random.Range(0, colors.Length)]);
                        drops.Add(new Drop { rt = img.rectTransform, v = new Vector2(Random.Range(-160f, 160f), Random.Range(-420f, 80f)) });
                    }
                    if (head != null) head.anchoredPosition = headPos + new Vector2(Mathf.Sin(e * 30f) * 8f, -Mathf.Sin(e * 9f) * 10f);
                }
                for (int i = drops.Count - 1; i >= 0; i--)
                {
                    var d = drops[i];
                    d.v.y -= 2600f * dt;
                    d.rt.anchoredPosition += d.v * dt;
                    d.rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.v.y, d.v.x) * Mathf.Rad2Deg);
                    if (d.rt.anchoredPosition.y <= floorY)
                    {
                        puddleW = Mathf.Min(820f, puddleW + 9f);
                        if (!splashed) { Sfx.Play(Sfx.Splash); splashed = true; }
                        Destroy(d.rt.gameObject);
                        drops.RemoveAt(i);
                    }
                }
                puddle.rectTransform.sizeDelta = new Vector2(puddleW, puddleW * 0.17f);
                yield return null;
            }
            foreach (var d in drops) Destroy(d.rt.gameObject);
            if (head != null) head.anchoredPosition = headPos;
            Pop("Pauvre Fanny...", new Vector2(0, -Size.y * 0.25f), Color.white, 70);
            yield return new WaitForSeconds(0.6f);
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
