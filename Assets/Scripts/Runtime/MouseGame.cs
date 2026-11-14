using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FannyIn2Trom
{
    /// <summary>
    /// ÉPREUVE — La souris.
    /// 1) Le joueur choisit une souris (plus elle est moche, plus ça rapporte).
    /// 2) Il la tient par la queue (elle pendouille et se balance) et doit la guider
    ///    jusque dans la bouche grande ouverte de Fanny, qui bouge la tête.
    /// 3) CROC ! Fanny la croque, mâche lentement, la queue dépasse de sa bouche… puis slurp.
    /// </summary>
    public class MouseGame : MiniGame
    {
        public override string Title { get { return "La souris"; } }
        public override string Instruction { get { return "Choisis une souris bien dégoûtante,\npuis tiens-la par la queue et fais-la tomber\ndans la bouche de Fanny !"; } }
        protected override float BaseTimeLimit { get { return 14f; } }

        // Réglages
        const float Gravity = 2600f;   // unités canvas / s²
        const float Damping = 1.6f;

        bool choosing = true, eating;
        int variant;
        RectTransform choiceRoot;
        Image face, mouse;
        FaceAnimator anim;
        Vector2 faceSize, faceHome, mouthOffset;
        Vector2 anchor, prevAnchor, prevVel;
        float theta, omega, mouseLen, t, legTimer, squeakTimer;
        int legFrame;
        readonly List<RectTransform> flies = new List<RectTransform>();

        Vector2 MouthPos { get { return face.rectTransform.anchoredPosition + mouthOffset; } }
        Vector2 HeadPos { get { return anchor + new Vector2(Mathf.Sin(theta), -Mathf.Cos(theta)) * mouseLen; } }

        protected override void Build()
        {
            float s;
            Cover("Decor", Art.BgMetro, out s);
            UIKit.Panel("Voile", Root, new Color(0.03f, 0.03f, 0.06f, 0.55f));

            // Fanny (gros plan, à droite)
            var sprite = Art.FannyVisage ?? Art.FannyFace;
            faceSize = UIKit.SizeForHeight(sprite, Size.y * 0.8f);
            faceHome = new Vector2(Size.x * 0.2f, -Size.y * 0.06f);
            face = UIKit.Image("Fanny", Root, sprite, faceSize, faceHome);
            anim = FaceAnimator.On(face);
            mouthOffset = Vector2.Scale(Art.VisageMouth - new Vector2(0.5f, 0.5f), faceSize);

            // choix de la souris
            PauseTimer = true;
            choiceRoot = UIKit.Stretch(UIKit.Rect("Choix", Root));
            UIKit.Panel("Fond", choiceRoot, new Color(0.03f, 0.04f, 0.1f, 0.8f));
            UIKit.Text("Question", choiceRoot, "Quelle souris Fanny va-t-elle manger ?", 66, new Color(1f, 0.85f, 0.25f), new Vector2(Size.x - 80f, 100), new Vector2(0, Size.y * 0.5f - 140f));
            for (int i = 0; i < ArtGross.Mice.Length; i++)
            {
                int v = i;
                var info = ArtGross.Mice[i];
                float x = (i - 1) * 520f;
                var btn = UIKit.Button("Souris" + i, choiceRoot, "", new Vector2(470, 600), new Vector2(x, -60f), new Color(0.15f, 0.12f, 0.1f, 0.95f), () => Pick(v));
                UIKit.Image("Image", btn.transform, ArtGross.Mouse(i, 0), new Vector2(170, 320), new Vector2(0, 85));
                UIKit.Text("Nom", btn.transform, info.name, 38, Color.white, new Vector2(450, 60), new Vector2(0, -130));
                UIKit.Text("Desc", btn.transform, info.desc, 28, new Color(0.85f, 0.85f, 0.85f), new Vector2(450, 50), new Vector2(0, -180));
                UIKit.Text("Degout", btn.transform, "Dégoût : " + new string('*', info.ugliness + 1), 32, new Color(0.7f, 1f, 0.4f), new Vector2(450, 50), new Vector2(0, -235));
            }
        }

        void Pick(int v)
        {
            if (!choosing) return;
            variant = v;
            choosing = false;
            Destroy(choiceRoot.gameObject);

            mouse = UIKit.Image("Souris", Root, ArtGross.Mouse(v, 0), new Vector2(160, 302), Vector2.zero);
            mouse.rectTransform.pivot = new Vector2(0.5f, 1f);          // tenue par le bout de la queue
            mouseLen = 302f * 0.94f;
            anchor = prevAnchor = new Vector2(-Size.x * 0.32f, Size.y * 0.3f);
            if (v == 2)
                for (int i = 0; i < 4; i++)
                    flies.Add(UIKit.Image("Mouche", Root, Art.Circle, new Vector2(10, 8), anchor, new Color(0.05f, 0.05f, 0.05f)).rectTransform);
            Sfx.PlayRandom(Sfx.Squeaks, 1f, 1f);
            PauseTimer = false;
        }

        protected override void Tick(float dt)
        {
            t += dt;
            if (choosing || mouse == null) return;
            if (eating) return;

            // --- la tête de Fanny bouge (elle ne facilite pas la tâche)
            float amp = 0.6f + 0.4f * Difficulty;
            Vector2 sway = new Vector2(Mathf.Sin(t * 0.9f) * 80f, Mathf.Sin(t * 1.4f + 1f) * 60f) * amp;
            if (!Locked) face.rectTransform.anchoredPosition = faceHome + sway;

            // --- la souris pend au bout de la queue (pendule), tenue par le doigt
            if (!Locked && Pointer.Held) anchor = Pointer.Pos + new Vector2(0f, 20f);
            dt = Mathf.Max(dt, 0.0001f);
            Vector2 vel = (anchor - prevAnchor) / dt;
            Vector2 acc = Vector2.ClampMagnitude((vel - prevVel) / dt, 40000f);
            prevAnchor = anchor;
            prevVel = vel;
            float L = mouseLen * 0.55f; // centre de masse
            float alpha = -(Gravity + acc.y) / L * Mathf.Sin(theta) - acc.x / L * Mathf.Cos(theta) - Damping * omega;
            omega += alpha * dt;
            theta += omega * dt;
            mouse.rectTransform.anchoredPosition = anchor;
            mouse.rectTransform.localRotation = Quaternion.Euler(0, 0, theta * Mathf.Rad2Deg);

            // pattes qui gigotent, couinements, mouches
            legTimer -= dt;
            if (legTimer <= 0f) { legFrame = 1 - legFrame; mouse.sprite = ArtGross.Mouse(variant, legFrame); legTimer = 0.11f; }
            squeakTimer -= dt;
            if (squeakTimer <= 0f) { Sfx.PlayRandom(Sfx.Squeaks, 0.7f, Random.Range(0.9f, 1.2f)); squeakTimer = Random.Range(0.6f, 1.3f); }
            for (int i = 0; i < flies.Count; i++)
            {
                float a = t * (6f + i) + i * 1.7f;
                flies[i].anchoredPosition = anchor + new Vector2(Mathf.Sin(theta), -Mathf.Cos(theta)) * mouseLen * 0.5f
                                           + new Vector2(Mathf.Cos(a) * 110f, Mathf.Sin(a * 1.3f) * 90f);
            }

            if (Locked) return;

            // --- Fanny ouvre grand la bouche quand la souris approche
            float d = Vector2.Distance(HeadPos, MouthPos);
            if (d < 420f) anim.Show(Art.VisageBouche, 0.1f);
            else anim.SetGrimace(0.4f);

            if (d < faceSize.x * 0.075f + 30f) StartCoroutine(Eat());
        }

        IEnumerator Eat()
        {
            eating = true;
            Locked = true;
            foreach (var f in flies) Destroy(f.gameObject);
            flies.Clear();

            // CROC !
            Sfx.Play(Sfx.Crunch, 1f, 1f);
            Sfx.PlayRandom(Sfx.Squeaks, 0.9f, 1.4f);
            Sfx.Vibrate();
            Pop("CROC !", MouthPos + new Vector2(-faceSize.x * 0.45f, 120f), new Color(1f, 0.5f, 0.4f), 110);
            anim.SetGrimace(0f);
            anim.Show(Art.VisageBouche, 0.12f);
            Destroy(mouse.gameObject);

            // la queue dépasse de la bouche (et suit la tête)
            var tail = UIKit.Image("Queue", face.transform, ArtGross.Tail, new Vector2(46f, 180f), mouthOffset + new Vector2(8f, -6f));
            tail.rectTransform.pivot = new Vector2(0.5f, 1f);
            Vector2 headStart = face.rectTransform.anchoredPosition;

            // elle mâche lentement…
            string[] mots = { "Miam…", "Crounch…", "Mmmh…", "Ça croustille" };
            const int chews = 5;
            for (int c = 0; c < chews; c++)
            {
                if (c < 2) Sfx.PlayRandom(Sfx.Squeaks, 0.35f, 0.7f); // couinement étouffé
                Sfx.PlayRandom(Sfx.Chews, 0.9f, Random.Range(0.9f, 1.05f));
                if (c == 1 || c == 3) Pop(mots[Random.Range(0, mots.Length)], MouthPos + new Vector2(faceSize.x * 0.45f, 80f), Color.white, 60);
                const float chewT = 0.55f;
                for (float e = 0f; e < chewT; e += Time.deltaTime)
                {
                    float k = e / chewT;
                    float jaw = Mathf.Sin(k * Mathf.PI);              // mâchoire qui s'ouvre et se ferme
                    if (k > 0.3f && k < 0.7f) anim.Show(Art.VisageBouche, 0.05f);
                    face.rectTransform.localScale = new Vector3(1f + 0.02f * jaw, 1f - 0.035f * jaw, 1f);
                    face.rectTransform.anchoredPosition = headStart + new Vector2(0f, -8f * jaw);
                    // la queue gigote (de moins en moins)
                    tail.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin((c * chewT + e) * 14f) * (25f - c * 4f));
                    yield return null;
                }
            }
            face.rectTransform.localScale = Vector3.one;

            // … et aspire la queue comme un spaghetti
            Sfx.Play(Sfx.Slurp, 1f, 1.1f);
            anim.Show(Art.VisageBouche, 0.4f);
            for (float e = 0f; e < 0.4f; e += Time.deltaTime)
            {
                float k = e / 0.4f;
                tail.rectTransform.localScale = new Vector3(1f, 1f - k, 1f);
                tail.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(e * 60f) * 30f * (1f - k));
                yield return null;
            }
            Destroy(tail.gameObject);
            Sfx.PlayRandom(Sfx.Glous, 0.9f, 1f);
            yield return new WaitForSeconds(0.25f);
            Sfx.PlayRandom(Sfx.Burps, 1f, 1.1f);
            anim.Show(Art.VisageBouche, 0.4f);
            Pop("Délicieux !", new Vector2(0f, Size.y * 0.3f), new Color(0.7f, 1f, 0.4f), 90);
            yield return new WaitForSeconds(0.8f);
            Finish(true, 400 + Mathf.RoundToInt(TimeLeft * 40f) + ArtGross.Mice[variant].ugliness * 150);
        }

        protected override IEnumerator OnTimeUp()
        {
            Pop("Elle n'a rien mangé !", Vector2.zero, new Color(1f, 0.45f, 0.4f), 96);
            yield return new WaitForSeconds(1f);
            Finish(false, 0);
        }
    }
}
