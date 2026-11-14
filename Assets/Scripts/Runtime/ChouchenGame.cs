using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FannyIn2Trom
{
    /// <summary>
    /// ÉPREUVE — Le chouchen.
    /// Une rangée de verres à shot sur le comptoir : le joueur garde le doigt sur un verre
    /// (ou glisse d'un verre à l'autre) pour verser le chouchen. Fanny boit très vite :
    /// plus on sert vite, plus elle est bourrée… et à 100 % d'ivresse, elle vomit.
    /// </summary>
    public class ChouchenGame : MiniGame
    {
        public override string Title { get { return "Le chouchen"; } }
        public override string Instruction { get { return "Tape sur les verres (ou garde le doigt dessus) pour servir le chouchen.\nFanny boit cul sec : remplis-les plus vite qu'elle les vide !"; } }
        protected override float BaseTimeLimit { get { return 22f; } }

        // Réglages
        const int GlassCount = 5;
        const float TapFill = 0.5f;         // un tapotement remplit un demi-verre
        const float PourRate = 4f;          // doigt maintenu : verres remplis par seconde
        const float DrunkPerShot = 0.12f;   // ivresse gagnée par shot plein (environ 9 shots pour finir)
        const float SoberWhenWaiting = 0.012f;
        const float FullEnough = 0.6f;      // elle boit les verres au moins à moitié pleins

        class Glass
        {
            public RectTransform rt;
            public Image liquid;
            public Vector2 home;
            public float fill;
            public bool busy;               // en train d'être bu
        }

        Glass[] glasses;
        Image face, ghost, bottle, stream;
        Vector2 faceSize, facePos, mouth;
        Image drunkFill;
        Text drunkText, shotsText;
        Sprite normalFace, sickFace;
        FaceAnimator anim;
        float drunk, drinkTimer, hicTimer, t, pourSoundTimer, thirstyPopTimer;
        int shots;
        float glassW, glassH, counterTop, spacing;
        bool drinking;

        static readonly Color Chouchen = new Color(0.88f, 0.58f, 0.12f, 0.95f);

        protected override void Build()
        {
            float s;
            Cover("Decor", Art.BgBretagne, out s);
            UIKit.Panel("Ambiance", Root, new Color(0.18f, 0.08f, 0.02f, 0.55f)); // crêperie tamisée

            // Fanny derrière le comptoir
            normalFace = Art.FannyVisage ?? Art.FannyFace;
            sickFace = Art.FannyVisageBeurk;
            counterTop = -Size.y * 0.5f + Size.y * 0.34f;
            faceSize = UIKit.SizeForHeight(normalFace, Size.y * 0.64f);
            facePos = new Vector2(Size.x * 0.12f, counterTop + faceSize.y * 0.40f);
            ghost = UIKit.Image("VisionDouble", Root, normalFace, faceSize, facePos, new Color(1f, 1f, 1f, 0f));
            face = UIKit.Image("Fanny", Root, normalFace, faceSize, facePos);
            anim = FaceAnimator.On(face);
            mouth = facePos + Vector2.Scale(Art.VisageMouth - new Vector2(0.5f, 0.5f), faceSize);

            // comptoir
            var counter = UIKit.Image("Comptoir", Root, Art.Counter, new Vector2(Size.x + 40f, Size.y * 0.34f + 30f), Vector2.zero);
            counter.rectTransform.anchorMin = counter.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            counter.rectTransform.pivot = new Vector2(0.5f, 0f);
            counter.rectTransform.anchoredPosition = new Vector2(0f, -10f);

            // verres
            glassW = 132f; glassH = 176f;
            glasses = new Glass[GlassCount];
            float span = Mathf.Min(Size.x * 0.62f, 1350f);
            spacing = span / (GlassCount - 1);
            for (int i = 0; i < GlassCount; i++)
            {
                float x = -span * 0.5f + span * i / (GlassCount - 1) - Size.x * 0.04f;
                var g = new Glass();
                var img = UIKit.Image("Verre" + i, Root, Art.ShotGlass, new Vector2(glassW, glassH), new Vector2(x, counterTop - 70f));
                g.rt = img.rectTransform;
                g.home = g.rt.anchoredPosition;
                g.liquid = UIKit.Image("Chouchen", img.transform, null, Vector2.zero, Vector2.zero, Chouchen);
                g.liquid.transform.SetAsFirstSibling();
                UIKit.Anchor(g.liquid.rectTransform, 0.11f, 0.2f, 0.89f, 0.2f);
                glasses[i] = g;
            }

            // bouteille + filet de chouchen
            stream = UIKit.Image("Filet", Root, null, new Vector2(14f, 10f), Vector2.zero, Chouchen);
            stream.rectTransform.pivot = new Vector2(0.5f, 1f);
            stream.gameObject.SetActive(false);
            bottle = UIKit.Image("Bouteille", Root, Art.Bottle, new Vector2(150f, 367f), new Vector2(-Size.x * 0.38f, counterTop + 230f));
            bottle.rectTransform.pivot = new Vector2(0.5f, 0.97f);
            UIKit.Text("Etiquette", bottle.transform, "CHOUCHEN", 20, new Color(0.35f, 0.16f, 0.03f), new Vector2(140, 40), new Vector2(0f, -62f), TextAnchor.MiddleCenter, false);

            // jauge d'ivresse (en haut à gauche)
            var bar = UIKit.Image("Ivresse", Root, Art.RoundedRect, new Vector2(620, 54), new Vector2(-Size.x * 0.5f + 560f, Size.y * 0.5f - 95f), new Color(0, 0, 0, 0.75f));
            bar.type = Image.Type.Sliced;
            drunkFill = UIKit.Image("Niveau", bar.transform, Art.RoundedRect, Vector2.zero, Vector2.zero, new Color(1f, 0.7f, 0.15f));
            drunkFill.type = Image.Type.Sliced;
            UIKit.Anchor(drunkFill.rectTransform, 0, 0, 0.03f, 1);
            drunkFill.rectTransform.offsetMin = new Vector2(7, 7);
            drunkFill.rectTransform.offsetMax = new Vector2(-7, -7);
            drunkText = UIKit.Text("Texte", bar.transform, "Ivresse : 0 %", 36, Color.white, new Vector2(600, 60), Vector2.zero);
            shotsText = UIKit.Text("Shots", Root, "Shots : 0", 40, new Color(1f, 0.85f, 0.4f), new Vector2(400, 60), new Vector2(-Size.x * 0.5f + 360f, Size.y * 0.5f - 160f), TextAnchor.MiddleLeft);

            drinkTimer = 1.2f;
            hicTimer = 2f;
            Sfx.PlayMusic(Sfx.MusicBretagne, 0.55f, this);
        }

        void OnDestroy() { Sfx.StopMusic(this); }

        /// <summary>Pause entre deux shots (elle enchaîne de plus en plus vite).</summary>
        float DrinkPause { get { return Mathf.Lerp(0.35f, 0.12f, drunk) / Mathf.Pow(Difficulty, 0.3f); } }

        protected override void Tick(float dt)
        {
            t += dt;

            // ---- service
            Glass target = null;
            if (!Locked && Pointer.Held)
            {
                float best = spacing * 0.5f;
                foreach (var g in glasses)
                {
                    if (g.busy) continue;
                    float d = Mathf.Abs(Pointer.Pos.x - g.home.x);
                    if (d < best) { best = d; target = g; }
                }
            }
            if (target != null)
            {
                // un tapotement = une bonne rasade ; le doigt maintenu continue de verser
                if (Pointer.Down) { target.fill = Mathf.Min(1f, target.fill + TapFill); Sfx.Play(Sfx.Pour, 0.8f, Random.Range(0.9f, 1.15f)); pourSoundTimer = 0.15f; }
                target.fill = Mathf.Min(1f, target.fill + PourRate * dt);
                Vector2 top = target.home + new Vector2(0f, glassH * 0.5f + 175f);
                bottle.rectTransform.anchoredPosition = Vector2.Lerp(bottle.rectTransform.anchoredPosition, top + new Vector2(28f, 0f), 1f - Mathf.Exp(-dt * 25f));
                bottle.rectTransform.localRotation = Quaternion.Lerp(bottle.rectTransform.localRotation, Quaternion.Euler(0, 0, 145f), 1f - Mathf.Exp(-dt * 20f));
                // filet de chouchen jusqu'au verre
                Vector2 spout = bottle.rectTransform.anchoredPosition;
                float level = target.home.y - glassH * 0.5f + glassH * (0.2f + 0.72f * target.fill);
                stream.gameObject.SetActive(true);
                stream.rectTransform.anchoredPosition = spout;
                stream.rectTransform.sizeDelta = new Vector2(14f, Mathf.Max(10f, spout.y - level));
                pourSoundTimer -= dt;
                if (pourSoundTimer <= 0f) { Sfx.Play(Sfx.Pour, 0.7f, Random.Range(0.9f, 1.15f)); pourSoundTimer = 0.15f; }
            }
            else
            {
                stream.gameObject.SetActive(false);
                if (!Locked)
                {
                    Vector2 rest = Pointer.Held ? new Vector2(Pointer.Pos.x, counterTop + 230f) : new Vector2(-Size.x * 0.38f, counterTop + 230f);
                    bottle.rectTransform.anchoredPosition = Vector2.Lerp(bottle.rectTransform.anchoredPosition, rest, 1f - Mathf.Exp(-dt * 12f));
                }
                bottle.rectTransform.localRotation = Quaternion.Lerp(bottle.rectTransform.localRotation, Quaternion.identity, 1f - Mathf.Exp(-dt * 12f));
            }
            foreach (var g in glasses) g.liquid.rectTransform.anchorMax = new Vector2(0.89f, 0.2f + 0.72f * g.fill);

            // ---- Fanny boit
            if (!Locked)
            {
                drinkTimer -= dt;
                thirstyPopTimer -= dt;
                if (drinkTimer <= 0f && !drinking)
                {
                    Glass full = null;
                    foreach (var g in glasses)
                        if (!g.busy && g.fill >= FullEnough && (full == null || g.fill > full.fill)) full = g;
                    if (full != null)
                    {
                        StartCoroutine(Drink(full));
                    }
                    else
                    {
                        // personne ne la sert : elle dessoûle un peu
                        drunk = Mathf.Max(0f, drunk - SoberWhenWaiting);
                        if (thirstyPopTimer <= 0f)
                        {
                            string[] mots = { "Plus vite !", "J'ai soif !", "Encore !", "Allez, sers !" };
                            Pop(mots[Random.Range(0, mots.Length)], facePos + new Vector2(-faceSize.x * 0.55f, faceSize.y * 0.25f), Color.white, 60);
                            anim.Talk(0.5f);
                            thirstyPopTimer = 1.4f;
                        }
                        drinkTimer = 0.3f;
                    }
                }

                // hoquet
                if (drunk > 0.3f && !drinking)
                {
                    hicTimer -= dt;
                    if (hicTimer <= 0f)
                    {
                        Sfx.Play(Sfx.Hic, 1f, Random.Range(0.9f, 1.15f));
                        anim.Talk(0.15f, false);
                        Pop("Hic !", facePos + new Vector2(faceSize.x * 0.45f, faceSize.y * 0.3f), new Color(1f, 0.8f, 0.8f), 64);
                        face.rectTransform.anchoredPosition += new Vector2(0f, 18f);
                        hicTimer = Random.Range(1.2f, 2.6f) * (1.4f - drunk);
                    }
                }
                if (drunk >= 1f) StartCoroutine(WinRoutine());
            }

            ApplyDrunkLook(dt);
            drunkFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.03f, drunk), 1f);
            drunkFill.color = Color.Lerp(new Color(1f, 0.75f, 0.2f), new Color(0.95f, 0.2f, 0.25f), drunk);
            drunkText.text = "Ivresse : " + Mathf.RoundToInt(drunk * 100f) + " %";
            shotsText.text = "Shots : " + shots;
        }

        /// <summary>Joues rouges, tête qui tangue, vision double, salle qui tourne.</summary>
        void ApplyDrunkLook(float dt)
        {
            if (Locked) return;
            face.color = Color.Lerp(Color.white, new Color(1f, 0.8f, 0.78f), drunk);
            if (ghost.sprite != face.sprite) ghost.sprite = face.sprite;
            ghost.color = new Color(1f, 0.85f, 0.85f, 0.35f * drunk);
            Root.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.9f) * 2.5f * drunk);
            if (drinking) return; // pendant qu'elle boit, c'est Drink() qui anime la tête
            float sway = Mathf.Sin(t * 1.7f) * drunk;
            Vector2 target = facePos + new Vector2(Mathf.Sin(t * 1.1f) * 30f * drunk, Mathf.Sin(t * 2.3f) * 8f * drunk);
            face.rectTransform.anchoredPosition = Vector2.Lerp(face.rectTransform.anchoredPosition, target, 1f - Mathf.Exp(-dt * 10f));
            face.rectTransform.localRotation = Quaternion.Euler(0, 0, sway * 10f);
            face.color = Color.Lerp(Color.white, new Color(1f, 0.8f, 0.78f), drunk);
            anim.SetGrimace(drunk > 0.85f ? 1f : drunk > 0.6f ? 0.4f : 0f);
            if (ghost.sprite != face.sprite) ghost.sprite = face.sprite;
            ghost.rectTransform.anchoredPosition = face.rectTransform.anchoredPosition + new Vector2(Mathf.Sin(t * 2.6f) * 45f * drunk, 0f);
            ghost.rectTransform.localRotation = face.rectTransform.localRotation;
            ghost.color = new Color(1f, 0.85f, 0.85f, 0.35f * drunk);
            Root.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.9f) * 2.5f * drunk);
        }

        /// <summary>Elle attrape le verre, renverse la tête, glou glou glou cul sec… et rote.</summary>
        IEnumerator Drink(Glass g)
        {
            drinking = true;
            g.busy = true;
            RectTransform head = face.rectTransform;
            Vector2 headStart = head.anchoredPosition;
            Vector2 lip = mouth + new Vector2(0f, -glassH * 0.32f);

            // 1) le verre monte jusqu'à sa bouche, elle ouvre la bouche
            anim.Show(Art.VisageBouche, 0.14f);
            for (float e = 0f; e < 0.12f; e += Time.deltaTime)
            {
                g.rt.anchoredPosition = Vector2.Lerp(g.home, lip, e / 0.12f);
                yield return null;
            }
            // 2) cul sec : tête en arrière, yeux fermés, glou glou glou (la gorge déglutit 3 fois)
            Sfx.Play(Sfx.GlouGlou, 1f, Random.Range(0.95f, 1.08f));
            const float gulp = 0.8f;
            anim.Show(Art.VisageBoit, gulp);
            float start = g.fill;
            for (float e = 0f; e < gulp; e += Time.deltaTime)
            {
                float k = e / gulp;
                float tilt = Mathf.Sin(Mathf.Min(1f, k * 2.5f) * Mathf.PI * 0.5f);            // la tête part en arrière
                float swallow = Mathf.Max(0f, Mathf.Sin(e * 2f * Mathf.PI * 3.6f)) * 7f;       // 3 gorgées
                head.anchoredPosition = headStart + new Vector2(0f, 30f * tilt - swallow);
                head.localRotation = Quaternion.Euler(0, 0, 6f * tilt);
                head.localScale = new Vector3(1f, 1f - 0.03f * tilt, 1f);
                g.rt.anchoredPosition = lip + new Vector2(-10f * tilt, 30f * tilt - swallow);
                g.rt.localRotation = Quaternion.Euler(0, 0, -95f * tilt);
                g.fill = Mathf.Lerp(start, 0f, Mathf.Clamp01((k - 0.1f) / 0.8f));
                yield return null;
            }
            g.fill = 0f;
            shots++;
            drunk = Mathf.Min(1f, drunk + DrunkPerShot * Mathf.Clamp01(start) / Mathf.Pow(Difficulty, 0.15f));

            // 3) le verre redescend et claque sur le comptoir
            Vector2 from = g.rt.anchoredPosition;
            for (float e = 0f; e < 0.12f; e += Time.deltaTime)
            {
                float k = e / 0.12f;
                g.rt.anchoredPosition = Vector2.Lerp(from, g.home, k);
                g.rt.localRotation = Quaternion.Euler(0, 0, -95f * (1f - k));
                head.anchoredPosition = Vector2.Lerp(head.anchoredPosition, headStart, 0.35f);
                head.localRotation = Quaternion.Lerp(head.localRotation, Quaternion.identity, 0.35f);
                yield return null;
            }
            g.rt.anchoredPosition = g.home;
            g.rt.localRotation = Quaternion.identity;
            Sfx.PlayRandom(Sfx.Clinks, 0.7f, Random.Range(0.95f, 1.1f));
            g.busy = false;

            // 4) ROT !
            anim.Show(Art.VisageBouche, 0.4f);
            Sfx.PlayRandom(Sfx.Burps, 1f, Random.Range(0.95f, 1.15f));
            if (Random.value < 0.5f) Pop(Random.value < 0.5f ? "BEURP !" : "BRÔÔÔ !", mouth + new Vector2(faceSize.x * 0.5f, 40f), new Color(0.9f, 1f, 0.6f), 62);
            for (float e = 0f; e < 0.4f; e += Time.deltaTime)
            {
                float k = e / 0.4f;
                head.anchoredPosition = headStart + new Vector2(Mathf.Sin(e * 45f) * 5f * (1f - k), -12f * Mathf.Sin(k * Mathf.PI));
                head.localScale = new Vector3(1f + 0.03f * Mathf.Sin(k * Mathf.PI), 1f, 1f);
                head.localRotation = Quaternion.identity;
                yield return null;
            }
            head.localScale = Vector3.one;
            // 5) frisson : ça brûle !
            anim.Show(Art.VisageGrimace1, 0.25f);
            if (Random.value < 0.25f) Pop("Yec'hed mat !", mouth + new Vector2(-faceSize.x * 0.55f, 80f), new Color(1f, 0.85f, 0.3f), 58);
            else if (drunk > 0.45f && Random.value < 0.35f && Sfx.Laugh != null)
            {
                Sfx.Play(Sfx.Laugh, 0.8f, Random.Range(1.0f, 1.15f)); // rire d'ivrogne
                anim.TalkFor(Sfx.Laugh);
            }
            drinking = false;
            drinkTimer = DrinkPause;
        }

        IEnumerator WinRoutine()
        {
            Locked = true;
            while (drinking) yield return null;
            stream.gameObject.SetActive(false);
            Pop("COMPLÈTEMENT BOURRÉE !", new Vector2(0, Size.y * 0.32f), new Color(1f, 0.6f, 0.3f), 92);
            anim.SetGrimace(1f);
            // elle tangue, hoquette...
            for (float e = 0f; e < 1.3f; e += Time.deltaTime)
            {
                t += Time.deltaTime;
                face.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 3f) * 16f);
                face.rectTransform.anchoredPosition = facePos + new Vector2(Mathf.Sin(t * 2f) * 50f, 0f);
                ghost.rectTransform.anchoredPosition = face.rectTransform.anchoredPosition + new Vector2(Mathf.Sin(t * 5f) * 60f, 0f);
                ghost.rectTransform.localRotation = face.rectTransform.localRotation;
                ghost.sprite = face.sprite;
                Root.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.4f) * 4f);
                if (e < 0.05f || (e > 0.6f && e < 0.65f)) { Sfx.Play(Sfx.Hic); anim.Talk(0.15f, false); }
                yield return null;
            }
            face.rectTransform.localRotation = Quaternion.identity;
            ghost.color = new Color(1, 1, 1, 0);
            Root.localRotation = Quaternion.identity;
            // ... et vomit
            yield return Vomit(mouth, face.rectTransform, facePos);
            Finish(true, 500 + Mathf.RoundToInt(TimeLeft * 50f) + shots * 20);
        }

        protected override IEnumerator OnTimeUp()
        {
            Root.localRotation = Quaternion.identity;
            Pop("Pas assez bourrée !", Vector2.zero, new Color(1f, 0.45f, 0.4f), 100);
            yield return new WaitForSeconds(1f);
            Finish(false, 0);
        }
    }
}
