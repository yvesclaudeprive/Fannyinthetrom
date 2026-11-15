using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FannyIn2Trom
{
    /// <summary>
    /// JEU DE MARINE — Coup de poisson.
    /// On choisit un poisson, on le tient par la queue et on le fait tournoyer (il se balance
    /// comme un pendule au bout du doigt), puis on le fait claquer sur la joue de Marine.
    /// Plus le poisson va vite et plus il est lourd, plus ça fait mal. But : la mettre K.O.
    /// </summary>
    public class MarineGame : MiniGame
    {
        public override string Title { get { return "Coup de poisson !"; } }
        public override string Instruction { get { return "Choisis ton poisson, fais-le tournoyer\net colle-lui une baffe sur la joue.\nVide sa barre d'énergie : K.O. !"; } }
        protected override float BaseTimeLimit { get { return 25f; } }

        // Réglages
        const float Gravity = 2400f;
        const float Damping = 0.9f;
        const float MinHitSpeed = 650f;     // vitesse minimale du poisson pour que ça compte

        bool choosing = true, ko;
        int fishType;
        float weight;
        RectTransform choiceRoot;
        Image face, fish, energyFill;
        Text energyText;
        Vector2 headHome, headOffset, faceSize;
        float headRot, energy = 1f;
        Vector2 anchor, prevAnchor, prevVel;
        float theta, omega, fishLen, t, cooldown, swooshTimer, tauntTimer = 4f, dodgeTimer, dodgeLeft;
        Vector2 dodgeOffset;
        string view = "face";
        float viewLeft;
        Vector2 shake;
        float shakeLeft;

        protected override void Build()
        {
            float s;
            Cover("Port", Art.BgBretagne, out s);
            UIKit.Panel("Voile", Root, new Color(0.05f, 0.08f, 0.15f, 0.45f));

            headHome = new Vector2(Size.x * 0.12f, -Size.y * 0.04f);
            face = UIKit.Image("Marine", Root, Art.Marine("face"), Vector2.one, headHome);
            SetView("face");

            // barre d'énergie de Marine (en haut à gauche)
            var bar = UIKit.Image("Energie", Root, Art.RoundedRect, new Vector2(620, 54), new Vector2(-Size.x * 0.5f + 560f, Size.y * 0.5f - 95f), new Color(0, 0, 0, 0.75f));
            bar.type = Image.Type.Sliced;
            energyFill = UIKit.Image("Niveau", bar.transform, Art.RoundedRect, Vector2.zero, Vector2.zero, new Color(0.3f, 0.9f, 0.3f));
            energyFill.type = Image.Type.Sliced;
            UIKit.Anchor(energyFill.rectTransform, 0, 0, 1, 1);
            energyFill.rectTransform.offsetMin = new Vector2(7, 7);
            energyFill.rectTransform.offsetMax = new Vector2(-7, -7);
            UIKit.Image("Coeur", bar.transform, Art.Heart, new Vector2(80, 80), new Vector2(-330f, 0f));
            energyText = UIKit.Text("Texte", bar.transform, "Marine : 100 %", 36, Color.white, new Vector2(600, 60), Vector2.zero);

            // choix du poisson
            PauseTimer = true;
            choiceRoot = UIKit.Stretch(UIKit.Rect("Choix", Root));
            UIKit.Panel("Fond", choiceRoot, new Color(0.03f, 0.05f, 0.1f, 0.85f));
            UIKit.Text("Question", choiceRoot, "Choisis ton poisson !", 70, new Color(1f, 0.85f, 0.25f), new Vector2(Size.x - 80f, 100), new Vector2(0, Size.y * 0.5f - 140f));
            float cardW = Mathf.Min(420f, (Size.x - 120f) / 4f - 20f);
            for (int i = 0; i < ArtGross.Fishes.Length; i++)
            {
                int type = i;
                var info = ArtGross.Fishes[i];
                float x = (i - 1.5f) * (cardW + 24f);
                var btn = UIKit.Button("Poisson" + i, choiceRoot, "", new Vector2(cardW, 470), new Vector2(x, -40f), new Color(0.1f, 0.2f, 0.32f, 0.95f), () => Pick(type));
                var spr = ArtGross.Fish(i);
                var img = UIKit.Image("Image", btn.transform, spr, UIKit.SizeForHeight(spr, 140), new Vector2(0, 90));
                if (img.rectTransform.sizeDelta.x > cardW - 30f) img.rectTransform.sizeDelta *= (cardW - 30f) / img.rectTransform.sizeDelta.x;
                UIKit.Text("Nom", btn.transform, info.name, 40, Color.white, new Vector2(cardW - 20f, 60), new Vector2(0, -40));
                UIKit.Text("Desc", btn.transform, info.desc, 28, new Color(0.85f, 0.9f, 1f), new Vector2(cardW - 20f, 70), new Vector2(0, -105));
                int stars = Mathf.Clamp(Mathf.RoundToInt(info.weight * 2f), 1, 5);
                UIKit.Text("Force", btn.transform, "Puissance : " + new string('*', stars), 30, new Color(1f, 0.6f, 0.4f), new Vector2(cardW - 20f, 50), new Vector2(0, -175));
            }
            Sfx.PlayMusic(Sfx.MusicBretagne, 0.3f, this);
        }

        void OnDestroy() { Sfx.StopMusic(this); }

        void Pick(int type)
        {
            if (!choosing) return;
            choosing = false;
            fishType = type;
            weight = ArtGross.Fishes[type].weight;
            Destroy(choiceRoot.gameObject);

            var spr = ArtGross.Fish(type);
            fishLen = Size.y * (0.42f + 0.06f * weight);
            float fishH = fishLen * spr.rect.height / spr.rect.width;
            fish = UIKit.Image("Poisson", Root, spr, new Vector2(fishLen, fishH), Vector2.zero);
            fish.rectTransform.pivot = new Vector2(0.985f, 0.5f);   // tenu par la queue
            anchor = prevAnchor = new Vector2(-Size.x * 0.3f, Size.y * 0.25f);
            PauseTimer = false;
        }

        void SetView(string v)
        {
            var spr = Art.Marine(v) ?? Art.Marine("face");
            if (spr == null) return;
            view = v;
            face.sprite = spr;
            faceSize = UIKit.SizeForHeight(spr, Size.y * 0.78f);
            face.rectTransform.sizeDelta = faceSize;
        }

        string IdleView { get { return energy > 0.6f ? "face" : energy > 0.3f ? "pensive" : "inquiete"; } }

        protected override void Tick(float dt)
        {
            t += dt;
            dt = Mathf.Max(dt, 0.0001f);

            // tremblement de l'écran après un coup
            if (shakeLeft > 0f)
            {
                shakeLeft -= dt;
                shake = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 18f * Mathf.Clamp01(shakeLeft / 0.25f);
            }
            else shake = Vector2.zero;
            Root.anchoredPosition = shake;

            if (choosing || fish == null || ko) return;

            // --- la tête de Marine : léger balancement, esquives
            float amp = 0.5f + 0.5f * Difficulty;
            Vector2 sway = new Vector2(Mathf.Sin(t * 0.8f) * 55f, Mathf.Sin(t * 1.1f) * 22f) * amp;
            if (dodgeLeft > 0f) dodgeLeft -= dt; else dodgeOffset = Vector2.Lerp(dodgeOffset, Vector2.zero, dt * 6f);
            dodgeTimer -= dt;
            face.rectTransform.anchoredPosition = headHome + sway + dodgeOffset + headOffset;
            face.rectTransform.localRotation = Quaternion.Euler(0, 0, headRot);
            if (viewLeft > 0f) { viewLeft -= dt; if (viewLeft <= 0f) SetView(IdleView); }

            // --- le poisson, tenu par la queue au bout du doigt (pendule)
            if (!Locked && Pointer.Held) anchor = Pointer.Pos;
            Vector2 vel = (anchor - prevAnchor) / dt;
            Vector2 acc = Vector2.ClampMagnitude((vel - prevVel) / dt, 60000f);
            prevAnchor = anchor;
            prevVel = vel;
            float L = fishLen * 0.55f;
            float inertia = 1f / Mathf.Pow(weight, 0.25f);  // un gros poisson est plus lent à lancer
            float alpha = (-(Gravity + acc.y) / L * Mathf.Sin(theta) - acc.x / L * Mathf.Cos(theta)) * inertia - Damping * omega;
            omega = Mathf.Clamp(omega + alpha * dt, -40f, 40f);
            theta += omega * dt;
            fish.rectTransform.anchoredPosition = anchor;
            fish.rectTransform.localRotation = Quaternion.Euler(0, 0, 90f + theta * Mathf.Rad2Deg);

            Vector2 dir = new Vector2(Mathf.Sin(theta), -Mathf.Cos(theta));
            Vector2 perp = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
            Vector2 tipVel = vel + perp * omega * fishLen;
            float speed = tipVel.magnitude;

            swooshTimer -= dt;
            if (speed > 1600f && swooshTimer <= 0f) { Sfx.Play(Sfx.Swoosh, 0.7f, Random.Range(0.9f, 1.15f)); swooshTimer = 0.3f; }

            if (Locked) return;
            cooldown -= dt;

            // --- elle esquive parfois un poisson qui arrive vite
            Vector2 faceCenter = face.rectTransform.anchoredPosition + new Vector2(0f, faceSize.y * 0.05f);
            Vector2 tip = anchor + dir * fishLen;
            if (dodgeTimer <= 0f && speed > 1500f && Vector2.Distance(tip, faceCenter) < 380f && Random.value < 0.12f * Difficulty)
            {
                dodgeOffset = new Vector2(tip.x < faceCenter.x ? 170f : -170f, -40f);
                dodgeLeft = 0.35f;
                dodgeTimer = 1.6f;
                Pop("Raté !", faceCenter + new Vector2(0f, faceSize.y * 0.55f), Color.white, 60);
                Sfx.PlayRandom(Sfx.Taunts, 0.9f, 1f);
            }

            // --- impact sur la joue ?
            if (cooldown <= 0f && speed > MinHitSpeed)
            {
                float rx = faceSize.x * 0.3f, ry = faceSize.y * 0.32f;
                for (float k = 0.45f; k <= 1.001f; k += 0.183f)
                {
                    Vector2 pt = anchor + dir * fishLen * k;
                    Vector2 d = pt - faceCenter;
                    if ((d.x * d.x) / (rx * rx) + (d.y * d.y) / (ry * ry) <= 1f)
                    {
                        Hit(pt, tipVel, speed);
                        break;
                    }
                }
            }

            // --- elle se moque si on tarde
            tauntTimer -= dt;
            if (tauntTimer <= 0f && viewLeft <= 0f)
            {
                SetView(Random.value < 0.5f ? "sourire" : "joie");
                viewLeft = 1.2f;
                Sfx.PlayRandom(Sfx.Taunts, 0.9f, 1f);
                string[] mots = { "Même pas mal !", "C'est tout ?", "Tu vises mal !" };
                Pop(mots[Random.Range(0, mots.Length)], faceCenter + new Vector2(-faceSize.x * 0.7f, faceSize.y * 0.35f), Color.white, 60);
                tauntTimer = Random.Range(4f, 6f);
            }

            UpdateEnergyBar();
        }

        void UpdateEnergyBar()
        {
            energyFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.03f, energy), 1f);
            energyFill.color = energy > 0.5f ? Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.9f, 0.3f), (energy - 0.5f) * 2f)
                                             : Color.Lerp(new Color(0.95f, 0.2f, 0.2f), new Color(1f, 0.85f, 0.2f), energy * 2f);
            energyText.text = energy > 0f ? "Marine : " + Mathf.CeilToInt(energy * 100f) + " %" : "Marine : K.O.";
        }

        void Hit(Vector2 point, Vector2 tipVel, float speed)
        {
            float side = Mathf.Abs(tipVel.x) > 1f ? Mathf.Sign(tipVel.x) : Mathf.Sign(point.x - face.rectTransform.anchoredPosition.x);
            float dmg = Mathf.Clamp(speed / 1800f * weight * 13f, 4f, 34f) / Mathf.Pow(Difficulty, 0.3f);
            energy = Mathf.Max(0f, energy - dmg / 100f);
            cooldown = 0.45f;
            omega = -omega * 0.35f; // le poisson rebondit
            tauntTimer = Random.Range(4f, 6f);

            Sfx.PlayRandom(Sfx.Slaps, 1f, Random.Range(0.9f, 1.1f) / Mathf.Sqrt(weight));
            Sfx.PlayRandom(Sfx.Yelps, 0.9f, Random.Range(0.95f, 1.1f));
            Sfx.Vibrate();
            shakeLeft = 0.25f;
            string[] mots = { "SPLATCH !", "PAF !", "BIM !", "SCHLAK !", "FLOC !" };
            Pop(mots[Random.Range(0, mots.Length)], point + new Vector2(0f, 120f), new Color(1f, 0.9f, 0.3f), 96);
            Pop("-" + Mathf.RoundToInt(dmg), point + new Vector2(side * 150f, 20f), new Color(1f, 0.4f, 0.35f), 64);
            StartCoroutine(Droplets(point, side));
            StartCoroutine(CheekMark(point));
            StartCoroutine(HeadSnap(side, speed));
            UpdateEnergyBar();
            if (energy <= 0f) StartCoroutine(KnockOut(side));
        }

        /// <summary>La tête part sous le choc, du côté où va le poisson, puis revient.</summary>
        IEnumerator HeadSnap(float side, float speed)
        {
            bool strong = speed > 2200f || weight >= 1.5f;
            SetView(side > 0 ? (strong ? "profil_droit" : "34_droite") : (strong ? "profil_gauche" : "34_gauche"));
            viewLeft = 10f;
            float push = (strong ? 110f : 70f) * side;
            for (float e = 0f; e < 0.07f; e += Time.deltaTime)
            {
                float k = e / 0.07f;
                headOffset = new Vector2(push * k, -12f * k);
                headRot = -side * (strong ? 14f : 8f) * k;
                yield return null;
            }
            yield return new WaitForSeconds(0.14f);
            if (ko) yield break;
            SetView("surprise");
            for (float e = 0f; e < 0.3f; e += Time.deltaTime)
            {
                float k = e / 0.3f;
                headOffset = Vector2.Lerp(new Vector2(push, -12f), Vector2.zero, k * k);
                headRot = Mathf.Lerp(-side * (strong ? 14f : 8f), 0f, k);
                yield return null;
            }
            headOffset = Vector2.zero;
            headRot = 0f;
            viewLeft = 0.35f; // reste surprise un instant puis revient à l'expression du moment
        }

        IEnumerator CheekMark(Vector2 point)
        {
            var mark = UIKit.Image("Joue", face.transform, Art.Circle, new Vector2(150f, 110f), point - face.rectTransform.anchoredPosition, new Color(0.95f, 0.25f, 0.25f, 0.5f));
            for (float e = 0f; e < 2f; e += Time.deltaTime)
            {
                mark.color = new Color(0.95f, 0.25f, 0.25f, 0.5f * (1f - e / 2f));
                yield return null;
            }
            Destroy(mark.gameObject);
        }

        IEnumerator Droplets(Vector2 point, float side)
        {
            var drops = new List<Image>();
            var vels = new List<Vector2>();
            for (int i = 0; i < 14; i++)
            {
                drops.Add(UIKit.Image("Goutte", Root, Art.Circle, Vector2.one * Random.Range(10f, 26f), point, new Color(0.75f, 0.88f, 1f, 0.85f)));
                vels.Add(new Vector2(side * Random.Range(200f, 900f), Random.Range(-200f, 700f)));
            }
            for (float e = 0f; e < 0.7f; e += Time.deltaTime)
            {
                for (int i = 0; i < drops.Count; i++)
                {
                    vels[i] += Vector2.down * 2500f * Time.deltaTime;
                    drops[i].rectTransform.anchoredPosition += vels[i] * Time.deltaTime;
                    drops[i].color = new Color(0.75f, 0.88f, 1f, 0.85f * (1f - e / 0.7f));
                }
                yield return null;
            }
            foreach (var d in drops) Destroy(d.gameObject);
        }

        IEnumerator KnockOut(float side)
        {
            ko = true;
            Locked = true;
            Sfx.Play(Sfx.Bell);
            // la tête tourne sous le choc (elle voit trente-six chandelles)
            string[] spin = side > 0
                ? new[] { "34_droite", "profil_droit", "34_droite", "face", "34_gauche", "profil_gauche", "34_gauche", "face" }
                : new[] { "34_gauche", "profil_gauche", "34_gauche", "face", "34_droite", "profil_droit", "34_droite", "face" };
            var stars = new List<RectTransform>();
            for (int i = 0; i < 5; i++) stars.Add(UIKit.Image("Etoile", Root, ArtGross.Star, new Vector2(70, 70), Vector2.zero).rectTransform);
            float total = 2.6f;
            for (float e = 0f; e < total; e += Time.deltaTime)
            {
                if (e < 1.1f)
                {
                    SetView(spin[Mathf.FloorToInt(e * 14f) % spin.Length]);
                    face.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(e * 25f) * 6f);
                }
                else
                {
                    if (view != "inquiete") { SetView("inquiete"); Sfx.Play(Sfx.VoiceKO); Pop("K.O. !", new Vector2(0f, Size.y * 0.3f), new Color(1f, 0.3f, 0.25f), 150); }
                    float k = Mathf.Clamp01((e - 1.1f) / 0.9f);
                    face.rectTransform.localRotation = Quaternion.Euler(0, 0, -side * 22f * k + Mathf.Sin(e * 3f) * 4f);
                    face.rectTransform.anchoredPosition = headHome + new Vector2(side * 40f * k, -150f * k * k);
                }
                // étoiles qui tournent autour de la tête
                Vector2 c = face.rectTransform.anchoredPosition + new Vector2(0f, faceSize.y * 0.42f);
                for (int i = 0; i < stars.Count; i++)
                {
                    float a = e * 4f + i * Mathf.PI * 2f / stars.Count;
                    stars[i].anchoredPosition = c + new Vector2(Mathf.Cos(a) * faceSize.x * 0.38f, Mathf.Sin(a) * 40f);
                    stars[i].localRotation = Quaternion.Euler(0, 0, e * 200f);
                    stars[i].SetAsLastSibling();
                }
                yield return null;
            }
            Finish(true, 500 + Mathf.RoundToInt(TimeLeft * 40f) + (weight < 1f ? 250 : 0));
        }

        protected override IEnumerator OnTimeUp()
        {
            SetView("joie");
            Sfx.PlayRandom(Sfx.Taunts, 1f, 1f);
            Pop("Marine tient bon !", Vector2.zero, new Color(1f, 0.45f, 0.4f), 96);
            yield return new WaitForSeconds(1.2f);
            Finish(false, 0);
        }
    }
}
