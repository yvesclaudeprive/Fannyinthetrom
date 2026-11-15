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
        public override string Instruction { get { return "Pêche ton poisson à la main dans le vivier,\npuis fais-le tournoyer et colle-lui une baffe.\nVide sa barre d'énergie : K.O. !"; } }
        protected override float BaseTimeLimit { get { return 25f; } }

        // Réglages
        const float Gravity = 2400f;
        const float Damping = 0.9f;
        const float MinHitSpeed = 650f;     // vitesse minimale du poisson pour que ça compte

        bool choosing = true, ko;
        int fishType;
        float weight;
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

        // --- pêche dans le vivier
        class Swimmer { public int type; public Image img; public Vector2 pos; public float dir, speed, baseY, phase, flee; public Vector2 size; }
        RectTransform fishing;
        RawImage caust1, caust2, surface;
        readonly List<Swimmer> swimmers = new List<Swimmer>();
        readonly List<RectTransform> bubbles = new List<RectTransform>();
        RectTransform hand;
        Vector2 handPos, handPrev;
        float bubbleTimer, grabCooldown;
        Text hoverText;
        bool grabbing;

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

            // pêche du poisson dans le vivier
            PauseTimer = true;
            BuildFishing();
        }

        void OnDestroy() { Sfx.StopMusic(this); }

        // ================================================================ pêche

        void BuildFishing()
        {
            fishing = UIKit.Stretch(UIKit.Rect("Vivier", Root));
            var bgSpr = Art.Vivier;
            var bg = UIKit.Image("Eau", fishing, bgSpr, Size, Vector2.zero, bgSpr != null ? Color.white : new Color(0.1f, 0.35f, 0.5f));
            if (bgSpr != null)
            {
                float sc = Mathf.Max(Size.x / bgSpr.rect.width, Size.y / bgSpr.rect.height);
                bg.rectTransform.sizeDelta = new Vector2(bgSpr.rect.width, bgSpr.rect.height) * sc;
            }
            // reflets de lumière (caustiques) animés : deux couches qui défilent
            var tex = Art.Caustiques;
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Repeat;
                caust1 = Caustic("Caustiques1", tex, 0.22f);
                caust2 = Caustic("Caustiques2", tex, 0.16f);
            }
            // poissons qui nagent (du plus loin au plus près)
            int[] types = { 3, 0, 1, 2, 0, 1 };
            for (int i = 0; i < types.Length; i++) AddSwimmer(types[i], i);
            // surface de l'eau
            var stex = Art.SurfaceEau;
            if (stex != null)
            {
                stex.wrapMode = TextureWrapMode.Repeat;
                var rt = UIKit.Rect("Surface", fishing);
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, 150f); rt.anchoredPosition = Vector2.zero;
                surface = rt.gameObject.AddComponent<RawImage>();
                surface.texture = stex;
                surface.raycastTarget = false;
                surface.uvRect = new Rect(0f, 0f, Size.x / 1024f, 1f);
            }
            // la main (le bras plonge depuis la surface)
            hand = UIKit.Rect("Main", fishing);
            if (!Art.HandGripIsPlaceholder)
            {
                Sprite arm = Art.HandGrip;
                float w = Art.HandWidthInBars * 95f;
                var img = UIKit.Image("Bras", hand, arm, new Vector2(w, w * arm.rect.height / arm.rect.width), Vector2.zero);
                img.rectTransform.pivot = Art.HandGripPivot;
                img.rectTransform.localRotation = Quaternion.Euler(0, 0, 200f);
            }
            else UIKit.Image("Main", hand, Art.HandOpen, new Vector2(220, 275), Vector2.zero).rectTransform.localRotation = Quaternion.Euler(0, 0, 180f);
            handPos = handPrev = new Vector2(0f, Size.y * 0.1f);
            hand.anchoredPosition = handPos;

            UIKit.Text("Consigne", fishing, "Plonge la main : appuie sur un poisson pour l'attraper !", 46, Color.white, new Vector2(Size.x - 100f, 70), new Vector2(0, -Size.y * 0.5f + 60f));
            hoverText = UIKit.Text("Nom", fishing, "", 40, new Color(1f, 0.9f, 0.5f), new Vector2(700, 60), Vector2.zero);
            Sfx.PlayMusic(Sfx.Bubbles, 0.6f, this);
        }

        RawImage Caustic(string name, Texture2D tex, float alpha)
        {
            var rt = UIKit.Stretch(UIKit.Rect(name, fishing));
            var r = rt.gameObject.AddComponent<RawImage>();
            r.texture = tex;
            r.color = new Color(1f, 1f, 0.95f, alpha);
            r.raycastTarget = false;
            r.uvRect = new Rect(0f, 0f, Size.x / 700f, Size.y / 700f);
            return r;
        }

        void AddSwimmer(int type, int index)
        {
            float[] lens = { 250f, 340f, 440f, 520f };
            float[] speeds = { 380f, 280f, 190f, 130f };
            var spr = ArtGross.Fish(type);
            var sw = new Swimmer { type = type };
            sw.size = new Vector2(lens[type], lens[type] * spr.rect.height / spr.rect.width);
            sw.baseY = Mathf.Lerp(Size.y * 0.25f, -Size.y * 0.28f, (index + 0.5f) / 6f);
            sw.pos = new Vector2(Random.Range(-Size.x * 0.4f, Size.x * 0.4f), sw.baseY);
            sw.dir = Random.value < 0.5f ? -1f : 1f;
            sw.speed = speeds[type] * Random.Range(0.85f, 1.15f);
            sw.phase = Random.Range(0f, 10f);
            sw.img = UIKit.Image("Poisson" + index, fishing, spr, sw.size, sw.pos);
            // un peu bleuté par l'eau, plus sombre au fond
            float depth = Mathf.InverseLerp(Size.y * 0.3f, -Size.y * 0.3f, sw.baseY);
            sw.img.color = Color.Lerp(new Color(0.92f, 0.98f, 1f), new Color(0.62f, 0.78f, 0.88f), depth);
            swimmers.Add(sw);
        }

        void UpdateFishing(float dt)
        {
            // eau qui bouge
            if (caust1 != null)
            {
                var r1 = caust1.uvRect; r1.x += dt * 0.03f; r1.y += dt * 0.012f; caust1.uvRect = r1;
                var r2 = caust2.uvRect; r2.x -= dt * 0.022f; r2.y += dt * 0.02f; caust2.uvRect = r2;
            }
            if (surface != null) { var r = surface.uvRect; r.x += dt * 0.05f; surface.uvRect = r; }

            // bulles
            bubbleTimer -= dt;
            if (bubbleTimer <= 0f)
            {
                float sz = Random.Range(8f, 26f);
                var b = UIKit.Image("Bulle", fishing, Art.Circle, new Vector2(sz, sz), new Vector2(Random.Range(-Size.x * 0.48f, Size.x * 0.48f), -Size.y * 0.42f), new Color(0.85f, 0.95f, 1f, 0.45f));
                bubbles.Add(b.rectTransform);
                bubbleTimer = Random.Range(0.08f, 0.3f);
            }
            for (int i = bubbles.Count - 1; i >= 0; i--)
            {
                var b = bubbles[i];
                b.anchoredPosition += new Vector2(Mathf.Sin(t * 3f + i) * 25f * dt, (90f + b.sizeDelta.x * 6f) * dt);
                if (b.anchoredPosition.y > Size.y * 0.45f) { Destroy(b.gameObject); bubbles.RemoveAt(i); }
            }

            // la main suit le doigt
            if (!grabbing)
            {
                if (Pointer.Down) handPos = Pointer.Pos;
                else if (Pointer.Held) handPos = Vector2.Lerp(handPos, Pointer.Pos, 1f - Mathf.Exp(-dt * 18f));
                hand.anchoredPosition = handPos;
            }
            float handSpeed = (handPos - handPrev).magnitude / Mathf.Max(dt, 0.0001f);
            handPrev = handPos;
            grabCooldown -= dt;

            // les poissons nagent, ondulent, fuient la main
            Swimmer hover = null;
            foreach (var sw in swimmers)
            {
                if (sw.img == null) continue;
                float d = Vector2.Distance(sw.pos, handPos);
                if (!grabbing && d < 260f && handSpeed > 600f && sw.flee <= 0f)
                {
                    sw.flee = 0.8f;                                   // coup de nageoire pour s'enfuir
                    sw.dir = sw.pos.x > handPos.x ? 1f : -1f;
                }
                sw.flee -= dt;
                float spd = sw.speed * (sw.flee > 0f ? 2.6f : 1f);
                sw.phase += dt * spd / 60f;
                sw.pos.x += sw.dir * spd * dt;
                sw.pos.y = sw.baseY + Mathf.Sin(sw.phase * 0.35f) * 40f;
                if (sw.pos.x > Size.x * 0.5f + sw.size.x * 0.3f) sw.dir = -1f;
                if (sw.pos.x < -Size.x * 0.5f - sw.size.x * 0.3f) sw.dir = 1f;
                sw.img.rectTransform.anchoredPosition = sw.pos;
                // tête à gauche dans l'image : on retourne quand il nage vers la droite ; il ondule
                sw.img.rectTransform.localScale = new Vector3(-sw.dir * (1f + Mathf.Sin(sw.phase * 2f) * 0.03f), 1f, 1f);
                sw.img.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(sw.phase * 2f) * 4f);
                if (Mathf.Abs(sw.pos.x - handPos.x) < sw.size.x * 0.45f && Mathf.Abs(sw.pos.y - handPos.y) < sw.size.y * 0.6f + 20f) hover = sw;
            }
            hoverText.text = hover != null ? ArtGross.Fishes[hover.type].name + " (" + ArtGross.Fishes[hover.type].desc + ")" : "";
            hoverText.rectTransform.anchoredPosition = handPos + new Vector2(0f, -130f);

            if (!grabbing && Pointer.Down && grabCooldown <= 0f) StartCoroutine(Grab(hover));
        }

        IEnumerator Grab(Swimmer target)
        {
            grabbing = true;
            grabCooldown = 0.4f;
            Sfx.Play(Sfx.Plouf, 0.9f, Random.Range(0.9f, 1.1f));
            // ronds dans l'eau
            var ring = UIKit.Image("Rond", fishing, ArtGross.Ring, new Vector2(60, 30), handPos, new Color(1f, 1f, 1f, 0.6f));
            // la main plonge d'un coup
            Vector2 start = handPos;
            for (float e = 0f; e < 0.12f; e += Time.deltaTime)
            {
                hand.anchoredPosition = start + new Vector2(0f, -40f * (e / 0.12f));
                ring.rectTransform.sizeDelta = new Vector2(60f + e * 1500f, 30f + e * 700f);
                ring.color = new Color(1f, 1f, 1f, 0.6f * (1f - e / 0.12f));
                yield return null;
            }
            Destroy(ring.gameObject);
            if (target == null || Vector2.Distance(target.pos, hand.anchoredPosition) > target.size.x * 0.6f)
            {
                Pop("Raté !", handPos + new Vector2(0f, 120f), Color.white, 60);
                hand.anchoredPosition = start;
                grabbing = false;
                yield break;
            }
            // attrapé ! le poisson se débat dans la main puis on le sort de l'eau
            Sfx.Play(Sfx.Catch);
            Sfx.Vibrate();
            Pop("ATTRAPÉ !", target.pos + new Vector2(0f, 160f), new Color(1f, 0.85f, 0.25f), 90);
            var fishRt = target.img.rectTransform;
            Vector2 grabPoint = hand.anchoredPosition;
            for (float e = 0f; e < 0.6f; e += Time.deltaTime)
            {
                fishRt.anchoredPosition = grabPoint + new Vector2(Mathf.Sin(e * 40f) * 15f, 0f);
                fishRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(e * 35f) * 25f);
                yield return null;
            }
            Sfx.Play(Sfx.SplashOut);
            for (float e = 0f; e < 0.5f; e += Time.deltaTime)
            {
                Vector2 up = new Vector2(0f, (Size.y * 0.9f) * (e / 0.5f) * (e / 0.5f));
                hand.anchoredPosition = grabPoint + up;
                fishRt.anchoredPosition = grabPoint + up + new Vector2(Mathf.Sin(e * 40f) * 15f, 0f);
                fishRt.localRotation = Quaternion.Euler(0, 0, 90f + Mathf.Sin(e * 30f) * 20f);
                yield return null;
            }
            Pop("Tu as pêché : " + ArtGross.Fishes[target.type].name.ToLower() + " !", new Vector2(0f, 0f), Color.white, 70);
            yield return new WaitForSeconds(0.6f);
            Sfx.StopMusic(this);
            Destroy(fishing.gameObject);
            swimmers.Clear();
            bubbles.Clear();
            Sfx.PlayMusic(Sfx.MusicBretagne, 0.3f, this);
            Pick(target.type);
        }

        void Pick(int type)
        {
            if (!choosing) return;
            choosing = false;
            fishType = type;
            weight = ArtGross.Fishes[type].weight;

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

            if (choosing) { UpdateFishing(dt); return; }
            if (fish == null || ko) return;

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
