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
        Image face, energyFill;
        WigglyFish fish;
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
        class Swimmer { public int type; public WigglyFish img; public Vector2 pos; public float dir, speed, baseY, phase, flee; public Vector2 size; }
        RectTransform fishing;
        RawImage caust1, caust2, surface;
        readonly List<Swimmer> swimmers = new List<Swimmer>();
        readonly List<RectTransform> bubbles = new List<RectTransform>();
        HandRig hand;
        Vector2 handPos, handPrev;
        float bubbleTimer, grabCooldown;
        Text hoverText;
        bool grabbing;

        // --- la main qui tient le poisson (gifles et repas)
        HandRig holder;
        float handRot, armLean;
        float gripFrac = 0.8f;                 // le poisson est pincé au pédoncule, juste avant la nageoire de queue
        int hits;

        // --- final : elle mange le poisson
        bool eatPhase, biting;
        // le poisson dans la bouche
        RectTransform mouthClip;               // le poisson n'est visible qu'à droite de la commissure des lèvres
        Vector2 mouthCur, chewOfs;
        float hangAngle = -20f, mouthAlong, tailWhip;
        bool inMouth;
        Coroutine seqAnim;

        // repères dans les images de Marine (fractions de l'image)
        static readonly Vector2 FaceCenterOfs = new Vector2(0.006f, 0.04f);   // depuis le centre de l'image
        static readonly Vector2 MouthOfs = new Vector2(0.005f, -0.085f);

        /// <summary>
        /// Une main qui SERRE un poisson : le bout des doigts, le poisson et le reste de la main
        /// sont trois calques. Doigts derrière / main devant : la main l'attrape par-dessus ;
        /// l'inverse : les doigts se referment sur la queue.
        /// </summary>
        class HandRig
        {
            public RectTransform root, palm, tips;
            Vector3 tipsScale = Vector3.one;

            public static HandRig Create(Transform parent, float width, bool shortArm)
            {
                var h = new HandRig();
                h.root = UIKit.Rect("MainQuiTient", parent);
                h.root.sizeDelta = Vector2.zero;
                Sprite palmSpr = (shortArm ? Art.HandPalmShort : null) ?? Art.HandPalm, tipsSpr = Art.HandFingers;
                if (Art.HandGripIsPlaceholder || palmSpr == null || tipsSpr == null)
                {
                    var img = UIKit.Image("Main", h.root, Art.HandOpen, new Vector2(220, 275), Vector2.zero);
                    h.palm = img.rectTransform;
                    return h;
                }
                Vector2 size = new Vector2(width, width * palmSpr.rect.height / palmSpr.rect.width);
                h.tips = UIKit.Image("BoutDesDoigts", h.root, tipsSpr, size, Vector2.zero).rectTransform;
                h.palm = UIKit.Image("Main", h.root, palmSpr, size, Vector2.zero).rectTransform;
                h.tips.pivot = h.palm.pivot = Art.FishGripPivot;
                return h;
            }

            /// <summary>Place la main : pos = point de prise, rot = rotation de l'image, curl = 1 doigts tendus, &lt;1 serrés.</summary>
            public void Set(Vector2 pos, float rot, float curl)
            {
                palm.anchoredPosition = pos;
                palm.localRotation = Quaternion.Euler(0, 0, rot);
                if (tips == null) return;
                tips.anchoredPosition = pos;
                tips.localRotation = palm.localRotation;
                tips.localScale = new Vector3(curl, Mathf.Lerp(1f, curl, 0.6f), 1f);
            }

            /// <summary>Glisse le poisson entre les calques de la main.</summary>
            public void Hold(Transform fish, bool fingersInFront)
            {
                fish.SetParent(root, false);
                if (tips == null) { fish.SetSiblingIndex(0); return; }
                if (fingersInFront) { palm.SetSiblingIndex(0); fish.SetSiblingIndex(1); tips.SetSiblingIndex(2); }
                else { tips.SetSiblingIndex(0); fish.SetSiblingIndex(1); palm.SetSiblingIndex(2); }
            }
        }

        /// <summary>Rotation de l'image de la main pour que l'avant-bras parte dans la direction donnée (degrés).</summary>
        static float RotForArm(float armDir) { return armDir - Art.ArmAngleInImage; }

        protected override void Build()
        {
            float s;
            Cover("Port", Art.BgBretagne, out s);
            UIKit.Panel("Voile", Root, new Color(0.05f, 0.08f, 0.15f, 0.45f));

            headHome = new Vector2(Size.x * 0.12f, -Size.y * 0.04f);
            face = UIKit.Image("Marine", Root, Art.Marine("face"), Vector2.one, headHome);
            SetView("degout_1");

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
            int[] types = { 2, 0, 1, 0, 2, 1 };
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
            // la main (le bras plonge depuis la surface, en haut à droite)
            hand = HandRig.Create(fishing, Art.HandWidthInBars * 95f, false);
            handPos = handPrev = new Vector2(0f, Size.y * 0.1f);
            hand.Set(handPos, FishingHandRot, 1f);

            UIKit.Text("Consigne", fishing, "Plonge la main : appuie sur un poisson pour l'attraper !", 46, Color.white, new Vector2(Size.x - 100f, 70), new Vector2(0, -Size.y * 0.5f + 60f));
            hoverText = UIKit.Text("Nom", fishing, "", 40, new Color(1f, 0.9f, 0.5f), new Vector2(700, 60), Vector2.zero);
            Sfx.PlayMusic(Sfx.Bubbles, 0.6f, this);
        }

        const float FishingHandRot = 200f;      // doigts vers le bas, avant-bras vers le haut à droite

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
            float[] lens = { 400f, 330f, 480f };
            float[] speeds = { 300f, 220f, 150f };
            var spr = ArtGross.Fish(type);
            var sw = new Swimmer { type = type };
            sw.size = new Vector2(lens[type], lens[type] * spr.rect.height / spr.rect.width);
            sw.baseY = Mathf.Lerp(Size.y * 0.25f, -Size.y * 0.28f, (index + 0.5f) / 6f);
            sw.pos = new Vector2(Random.Range(-Size.x * 0.4f, Size.x * 0.4f), sw.baseY);
            sw.dir = Random.value < 0.5f ? -1f : 1f;
            sw.speed = speeds[type] * Random.Range(0.85f, 1.15f);
            sw.phase = Random.Range(0f, 10f);
            sw.img = WigglyFish.Create("Poisson" + index, fishing, spr, sw.size, sw.pos);
            // en nageant, le corps ondule (vraie animation image par image) et la queue bat
            sw.img.SetFrames(ArtGross.FishFrames(type), ArtGross.Fishes[type].fps * 0.6f, type);
            sw.img.freeHead = false;
            sw.img.amplitude = 0.03f;
            sw.img.frequency = 2.5f;
            sw.img.waves = 1.0f;
            // un peu bleuté par l'eau, plus sombre au fond
            float depth = Mathf.InverseLerp(Size.y * 0.3f, -Size.y * 0.3f, sw.baseY);
            sw.img.color = Color.Lerp(new Color(0.92f, 0.98f, 1f), new Color(0.62f, 0.78f, 0.88f), depth);
            swimmers.Add(sw);
        }

        void UpdateFishing(float dt)
        {
            UpdateFishingWater(dt);
            UpdateFishingHand(dt);
        }

        void UpdateFishingWater(float dt)
        {
            if (fishing == null) return;
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
        }

        void UpdateFishingHand(float dt)
        {

            // la main suit le doigt
            if (!grabbing)
            {
                if (Pointer.Down) handPos = Pointer.Pos;
                else if (Pointer.Held) handPos = Vector2.Lerp(handPos, Pointer.Pos, 1f - Mathf.Exp(-dt * 18f));
                // la main penche un peu dans le sens du mouvement
                float lean = Mathf.Clamp((handPos.x - handPrev.x) / Mathf.Max(dt, 0.0001f) * -0.006f, -12f, 12f);
                hand.Set(handPos, FishingHandRot + lean, 1f);
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
                sw.img.fps = ArtGross.Fishes[sw.type].fps * (sw.flee > 0f ? 1.5f : 0.6f);
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
            // la main plonge d'un coup, doigts écartés, puis se referme
            Vector2 start = handPos;
            Vector2 goal = target != null ? Vector2.Lerp(start, target.pos, 0.7f) : start + new Vector2(0f, -40f);
            for (float e = 0f; e < 0.14f; e += Time.deltaTime)
            {
                float k = e / 0.14f;
                hand.Set(Vector2.Lerp(start, goal, k), FishingHandRot, 1f + 0.12f * k);
                ring.rectTransform.sizeDelta = new Vector2(60f + e * 1500f, 30f + e * 700f);
                ring.color = new Color(1f, 1f, 1f, 0.6f * (1f - k));
                yield return null;
            }
            Destroy(ring.gameObject);
            Vector2 grabPoint = goal;
            if (target == null || Vector2.Distance(target.pos, grabPoint) > target.size.x * 0.6f)
            {
                // la main se referme… sur rien
                for (float e = 0f; e < 0.12f; e += Time.deltaTime) { hand.Set(grabPoint, FishingHandRot, Mathf.Lerp(1.12f, 0.8f, e / 0.12f)); yield return null; }
                Pop("Raté !", grabPoint + new Vector2(0f, 120f), Color.white, 60);
                yield return new WaitForSeconds(0.15f);
                hand.Set(start, FishingHandRot, 1f);
                grabbing = false;
                yield break;
            }

            // ATTRAPÉ : le poisson glisse sous la paume, les doigts se referment derrière lui
            Sfx.Play(Sfx.Catch);
            Sfx.Vibrate();
            Pop("ATTRAPÉ !", grabPoint + new Vector2(0f, 200f), new Color(1f, 0.85f, 0.25f), 90);
            var caught = target.img;
            swimmers.Remove(target);
            var fishRt = caught.rectTransform;
            Vector2 p0 = fishRt.anchoredPosition;
            float r0 = fishRt.localEulerAngles.z;
            fishRt.localScale = Vector3.one;
            caught.color = Color.white;
            hand.Hold(fishRt, false);
            caught.holdAlong = 0.55f;                               // pris un peu en arrière du milieu (le pivot suit le corps)
            float holdRot = FishingHandRot + Art.FishGripAngle;      // axe du poisson dans la main
            caught.freeHead = true;
            caught.amplitude = 0.12f;
            caught.frequency = 9f;
            caught.fps = ArtGross.Fishes[target.type].fps * 1.7f;   // il se tortille comme un fou
            caught.waves = 0.8f;
            for (float e = 0f; e < 0.12f; e += Time.deltaTime)
            {
                float k = e / 0.12f;
                fishRt.anchoredPosition = Vector2.Lerp(p0, grabPoint, k);
                fishRt.localRotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(r0, holdRot, k));
                hand.Set(grabPoint, FishingHandRot, Mathf.Lerp(1.12f, 0.86f, k));
                yield return null;
            }
            // il se débat dans la main : la main tremble, des bulles s'échappent
            for (int i = 0; i < 16; i++)
            {
                float sz = Random.Range(10f, 30f);
                var b = UIKit.Image("Bulle", fishing, Art.Circle, new Vector2(sz, sz), grabPoint + Random.insideUnitCircle * 120f, new Color(0.85f, 0.95f, 1f, 0.55f));
                bubbles.Add(b.rectTransform);
            }
            for (float e = 0f; e < 0.75f; e += Time.deltaTime)
            {
                Vector2 jolt = new Vector2(Mathf.Sin(e * 47f) * 14f, Mathf.Sin(e * 31f) * 9f);
                hand.Set(grabPoint + jolt, FishingHandRot + Mathf.Sin(e * 37f) * 5f, 0.86f + Mathf.Sin(e * 23f) * 0.03f);
                fishRt.anchoredPosition = grabPoint + jolt;
                fishRt.localRotation = Quaternion.Euler(0, 0, holdRot + Mathf.Sin(e * 29f) * 16f);
                yield return null;
            }
            // la main remonte et sort le poisson de l'eau
            Sfx.Play(Sfx.SplashOut);
            float lift = 0.75f;
            for (float e = 0f; e < lift; e += Time.deltaTime)
            {
                float k = e / lift;
                Vector2 up = new Vector2(Size.x * 0.06f * k, (Size.y * 1.1f) * k * k);
                Vector2 jolt = new Vector2(Mathf.Sin(e * 47f) * 10f, 0f);
                hand.Set(grabPoint + up + jolt, FishingHandRot + Mathf.Sin(e * 30f) * 4f, 0.86f);
                fishRt.anchoredPosition = grabPoint + up + jolt;
                fishRt.localRotation = Quaternion.Euler(0, 0, holdRot + Mathf.Sin(e * 26f) * 22f);
                if (Random.value < 0.5f)
                {
                    float sz = Random.Range(8f, 22f);
                    var b = UIKit.Image("Bulle", fishing, Art.Circle, new Vector2(sz, sz), fishRt.anchoredPosition + Random.insideUnitCircle * 80f, new Color(0.85f, 0.95f, 1f, 0.5f));
                    bubbles.Add(b.rectTransform);
                }
                yield return null;
            }
            // remous à la surface
            for (int i = 0; i < 4; i++)
            {
                var rr = UIKit.Image("Remous", fishing, ArtGross.Ring, new Vector2(80, 30), new Vector2(grabPoint.x + Size.x * 0.06f + Random.Range(-90f, 90f), Size.y * 0.42f), new Color(1f, 1f, 1f, 0.7f));
                StartCoroutine(Ripple(rr));
            }
            Pop("Tu as pêché : " + ArtGross.Fishes[target.type].name.ToLower() + " !", new Vector2(0f, 0f), Color.white, 70);
            yield return new WaitForSeconds(0.9f);
            Sfx.StopMusic(this);
            Destroy(fishing.gameObject);
            swimmers.Clear();
            bubbles.Clear();
            Sfx.PlayMusic(Sfx.MusicBretagne, 0.3f, this);
            Pick(target.type);
        }

        IEnumerator Ripple(Image ring)
        {
            float delay = Random.Range(0f, 0.25f);
            ring.color = Color.clear;
            yield return new WaitForSeconds(delay);
            for (float e = 0f; e < 0.6f && ring != null; e += Time.deltaTime)
            {
                ring.rectTransform.sizeDelta = new Vector2(80f + e * 700f, 30f + e * 160f);
                ring.color = new Color(1f, 1f, 1f, 0.7f * (1f - e / 0.6f));
                yield return null;
            }
            if (ring != null) Destroy(ring.gameObject);
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
            fish = WigglyFish.Create("Poisson", Root, spr, new Vector2(fishLen, fishH), Vector2.zero);
            // tenu par la queue, il se tortille (animation image par image) et la tête fouette l'air
            fish.SetFrames(ArtGross.FishFrames(type), ArtGross.Fishes[type].fps, type);
            fish.freeHead = true;
            fish.amplitude = 0.1f;
            fish.frequency = 6.5f;
            fish.waves = 0.9f;
            gripFrac = Mathf.Clamp(FishData.Peduncle[Mathf.Min(type, FishData.Peduncle.Length - 1)] + 0.02f, 0.7f, 0.9f);
            fish.holdAlong = gripFrac;                               // pincé au pédoncule, le pivot suit le corps
            // la main du joueur (qui arrive d'en bas à droite) : les doigts se referment sur la queue
            holder = HandRig.Create(Root, Art.HandWidthInBars * 80f, true);
            holder.Hold(fish.rectTransform, true);
            anchor = prevAnchor = new Vector2(-Size.x * 0.3f, Size.y * 0.25f);
            handRot = RotForArm(-50f);
            holder.Set(anchor, handRot, 0.9f);
            fish.rectTransform.anchoredPosition = anchor;
            fish.rectTransform.localRotation = Quaternion.Euler(0, 0, 90f);
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

        string IdleView { get { return energy > 0.6f ? "degout_1" : energy > 0.3f ? "degout_3" : "degout_6"; } }

        /// <summary>Joue une séquence d'images de Marine (ex. "rire" 1..10), puis revient à l'expression du moment.</summary>
        void PlaySeq(string seq, int[] frames, float fps, float hold = 0.3f)
        {
            if (seqAnim != null) StopCoroutine(seqAnim);
            seqAnim = StartCoroutine(SeqRoutine(seq, frames, fps, hold));
        }

        IEnumerator SeqRoutine(string seq, int[] frames, float fps, float hold)
        {
            viewLeft = frames.Length / fps + hold + 0.05f;
            foreach (int f in frames)
            {
                SetView(seq + "_" + f);
                yield return new WaitForSeconds(1f / fps);
            }
            seqAnim = null;
        }

        static int[] Range(int a, int b)
        {
            var r = new int[Mathf.Abs(b - a) + 1];
            for (int i = 0; i < r.Length; i++) r[i] = a + (b >= a ? i : -i);
            return r;
        }

        Vector2 FaceCenter { get { return face.rectTransform.anchoredPosition + Vector2.Scale(FaceCenterOfs, faceSize); } }
        Vector2 Mouth { get { return face.rectTransform.anchoredPosition + MouthLocal(view); } }

        /// <summary>Décalage (en pixels de l'image 760×740) de la bouche selon l'expression : sur certaines images
        /// la tête bouge un peu, la bouche n'est pas toujours au même endroit.</summary>
        static readonly Dictionary<string, Vector2> MouthShift = new Dictionary<string, Vector2>
        {
            { "mache_1", new Vector2(-5, 0) }, { "mache_2", new Vector2(0, 12) }, { "mache_4", new Vector2(0, -3) },
            { "mache_5", new Vector2(-5, 2) }, { "mache_6", new Vector2(0, 20) }, { "mache_7", new Vector2(5, 15) },
            { "mache_8", new Vector2(0, 18) }, { "mache_9", new Vector2(0, 22) }, { "mache_10", new Vector2(0, 20) },
            { "degout_5", new Vector2(0, 15) }, { "degout_6", new Vector2(0, 25) }, { "degout_7", new Vector2(0, 25) },
            { "degout_8", new Vector2(0, 20) }, { "nausee_1", new Vector2(0, 10) }, { "nausee_2", new Vector2(0, 5) },
            { "nausee_7", new Vector2(0, 22) }, { "nausee_8", new Vector2(40, 25) },
        };

        /// <summary>Position de la bouche dans le repère du visage pour une expression donnée.</summary>
        Vector2 MouthLocal(string v)
        {
            Vector2 px;
            if (!MouthShift.TryGetValue(v, out px)) px = Vector2.zero;
            return Vector2.Scale(new Vector2(MouthOfs.x + px.x / 760f, MouthOfs.y - px.y / 740f), faceSize);
        }

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
            if (fish == null) return;
            if (ko && !eatPhase) return;
            if (biting) return;                       // le poisson est dans sa bouche : l'animation s'occupe de tout

            // --- la tête de Marine : léger balancement, esquives
            float amp = 0.5f + 0.5f * Difficulty;
            Vector2 sway = new Vector2(Mathf.Sin(t * 0.8f) * 55f, Mathf.Sin(t * 1.1f) * 22f) * amp;
            if (dodgeLeft > 0f) dodgeLeft -= dt; else dodgeOffset = Vector2.Lerp(dodgeOffset, Vector2.zero, dt * 6f);
            dodgeTimer -= dt;
            if (!eatPhase)
            {
                face.rectTransform.anchoredPosition = headHome + sway + dodgeOffset + headOffset;
                face.rectTransform.localRotation = Quaternion.Euler(0, 0, headRot);
            }
            if (viewLeft > 0f) { viewLeft -= dt; if (viewLeft <= 0f) SetView(IdleView); }

            // --- le poisson, tenu par la queue au bout du doigt (pendule)
            if ((!Locked || eatPhase) && !biting && Pointer.Held) anchor = Pointer.Pos;
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
            // l'avant-bras vient d'en bas à droite et suit un peu le mouvement
            armLean = Mathf.Lerp(armLean, Mathf.Clamp(-vel.x * 0.006f, -18f, 18f) + anchor.x / Size.x * 18f, 1f - Mathf.Exp(-dt * 8f));
            handRot = RotForArm(-50f + armLean);
            if (holder != null) holder.Set(anchor, handRot, 0.9f);

            Vector2 dir = new Vector2(Mathf.Sin(theta), -Mathf.Cos(theta));
            Vector2 perp = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
            Vector2 tipVel = vel + perp * omega * fishLen * gripFrac;
            float speed = tipVel.magnitude;

            swooshTimer -= dt;
            if (speed > 1600f && swooshTimer <= 0f) { Sfx.Play(Sfx.Swoosh, 0.7f, Random.Range(0.9f, 1.15f)); swooshTimer = 0.3f; }

            if (eatPhase) { EatTick(dir); return; }
            if (Locked) return;
            cooldown -= dt;

            // --- elle esquive parfois un poisson qui arrive vite
            Vector2 faceCenter = FaceCenter;
            Vector2 tip = anchor + dir * fishLen * gripFrac;
            if (dodgeTimer <= 0f && speed > 1500f && Vector2.Distance(tip, faceCenter) < 380f && Random.value < 0.12f * Difficulty)
            {
                dodgeOffset = new Vector2(tip.x < faceCenter.x ? 170f : -170f, -40f);
                dodgeLeft = 0.35f;
                dodgeTimer = 1.6f;
                Pop("Raté !", faceCenter + new Vector2(0f, faceSize.y * 0.45f), Color.white, 60);
                Sfx.PlayRandom(Sfx.Taunts, 0.9f, 1f);
                PlaySeq("danse", new[] { 1, 5, 6, 7, 8, 9, 10 }, 14f);
            }

            // --- impact sur la joue ?
            if (cooldown <= 0f && speed > MinHitSpeed)
            {
                float rx = faceSize.x * 0.21f, ry = faceSize.y * 0.3f;
                for (float k = 0.45f; k <= 1.001f; k += 0.183f)
                {
                    Vector2 pt = anchor + dir * fishLen * gripFrac * k;
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
                PlaySeq("rire", Range(1, 10), 12f, 0.5f);
                Sfx.PlayRandom(Sfx.Taunts, 0.9f, 1f);
                if (Sfx.Laugh != null && Random.value < 0.5f) Sfx.Play(Sfx.Laugh, 0.7f, 1.2f);
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

        /// <summary>La tête part sous le choc, du côté où va le poisson, puis revient… avec une vraie grimace de dégoût.</summary>
        IEnumerator HeadSnap(float side, float speed)
        {
            bool strong = speed > 2200f || weight >= 1.5f;
            hits++;
            if (seqAnim != null) { StopCoroutine(seqAnim); seqAnim = null; }
            SetView(side > 0 ? "nausee_6" : "nausee_4");          // la tête tourne sous l'impact
            viewLeft = 10f;
            float push = (strong ? 110f : 70f) * side;
            for (float e = 0f; e < 0.07f; e += Time.deltaTime)
            {
                float k = e / 0.07f;
                headOffset = new Vector2(push * k, -12f * k);
                headRot = -side * (strong ? 14f : 8f) * k;
                yield return null;
            }
            yield return new WaitForSeconds(0.12f);
            if (ko) yield break;
            // le poisson visqueux sur la joue : dégoût, de plus en plus fort au fil des coups
            string[] soft = { "degout_5", "nausee_1", "degout_7", "nausee_2", "degout_6" };
            string[] hard = { "degout_8", "nausee_7", "degout_8", "nausee_2" };
            string[] seq = strong ? hard : soft;
            SetView(seq[(hits - 1) % seq.Length]);
            if (Random.value < 0.55f || strong)
            {
                Sfx.PlayRandom(Sfx.Beurks, 0.85f, Random.Range(0.95f, 1.08f));
                string[] mots = { "Beurk !", "Ça pue !", "C'est gluant !", "Berk, ça colle !", "Pouah !" };
                Pop(mots[Random.Range(0, mots.Length)], FaceCenter + new Vector2(-side * faceSize.x * 0.62f, faceSize.y * 0.3f), new Color(0.75f, 1f, 0.45f), 62);
            }
            for (float e = 0f; e < 0.3f; e += Time.deltaTime)
            {
                float k = e / 0.3f;
                headOffset = Vector2.Lerp(new Vector2(push, -12f), Vector2.zero, k * k);
                headRot = Mathf.Lerp(-side * (strong ? 14f : 8f), 0f, k);
                yield return null;
            }
            headOffset = Vector2.zero;
            headRot = 0f;
            // elle frissonne de dégoût puis revient à l'expression du moment
            yield return new WaitForSeconds(0.25f);
            if (ko || viewLeft < 5f) yield break;
            SetView(strong ? "nausee_1" : "degout_7");
            for (float e = 0f; e < 0.3f; e += Time.deltaTime)
            {
                headOffset = new Vector2(Mathf.Sin(e * 70f) * 6f, 0f);
                yield return null;
            }
            headOffset = Vector2.zero;
            viewLeft = 0.25f;
        }

        IEnumerator CheekMark(Vector2 point)
        {
            Vector2 local = point - face.rectTransform.anchoredPosition;
            var mark = UIKit.Image("Joue", face.transform, Art.Circle, new Vector2(150f, 110f), local, new Color(0.95f, 0.25f, 0.25f, 0.5f));
            // de la bave de poisson qui coule sur la joue
            var slime = UIKit.Image("Bave", face.transform, Art.Circle, new Vector2(70f, 46f), local + new Vector2(0f, -10f), new Color(0.85f, 0.95f, 0.8f, 0.55f));
            var drip = UIKit.Image("Goutte", face.transform, Art.Circle, new Vector2(22f, 30f), local + new Vector2(8f, -30f), new Color(0.85f, 0.95f, 0.8f, 0.6f));
            for (float e = 0f; e < 2f; e += Time.deltaTime)
            {
                float k = e / 2f;
                mark.color = new Color(0.95f, 0.25f, 0.25f, 0.5f * (1f - k));
                slime.color = new Color(0.85f, 0.95f, 0.8f, 0.55f * (1f - k));
                slime.rectTransform.sizeDelta = new Vector2(70f, 46f + 60f * k);
                slime.rectTransform.anchoredPosition = local + new Vector2(0f, -10f - 30f * k);
                drip.color = new Color(0.85f, 0.95f, 0.8f, 0.6f * (1f - k));
                drip.rectTransform.anchoredPosition = local + new Vector2(8f, -30f - 170f * k * k);
                yield return null;
            }
            Destroy(mark.gameObject);
            Destroy(slime.gameObject);
            Destroy(drip.gameObject);
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
            if (seqAnim != null) { StopCoroutine(seqAnim); seqAnim = null; }
            string[] spin = side > 0
                ? new[] { "nausee_6", "danse_9", "nausee_4", "danse_7", "nausee_6", "danse_1", "nausee_4", "nausee_7" }
                : new[] { "nausee_4", "danse_7", "nausee_6", "danse_9", "nausee_4", "danse_1", "nausee_6", "nausee_7" };
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
                    if (view != "nausee_10") { SetView("nausee_10"); Sfx.Play(Sfx.VoiceKO); Pop("K.O. !", new Vector2(0f, Size.y * 0.3f), new Color(1f, 0.3f, 0.25f), 150); }
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
            foreach (var st in stars) Destroy(st.gameObject);
            // … et pour finir, on lui fait manger le poisson !
            yield return EatFish();
            Finish(true, 700 + Mathf.RoundToInt(TimeLeft * 40f) + (weight < 1f ? 250 : 0));
        }

        // ============================================================== repas

        IEnumerator EatFish()
        {
            // elle reprend ses esprits
            Vector2 p0 = face.rectTransform.anchoredPosition;
            Quaternion q0 = face.rectTransform.localRotation;
            for (float e = 0f; e < 0.6f; e += Time.deltaTime)
            {
                float k = e / 0.6f;
                face.rectTransform.anchoredPosition = Vector2.Lerp(p0, headHome, k);
                face.rectTransform.localRotation = Quaternion.Lerp(q0, Quaternion.identity, k);
                yield return null;
            }
            face.rectTransform.anchoredPosition = headHome;
            face.rectTransform.localRotation = Quaternion.identity;
            SetView("degout_1");
            viewLeft = 0f;
            Pop("Maintenant, donne-lui le poisson à manger !", new Vector2(0f, Size.y * 0.36f), new Color(1f, 0.85f, 0.25f), 64);
            omega *= 0.3f;
            eatPhase = true;
            while (eatPhase) yield return null;     // EatTick lance EatSequence quand la tête du poisson arrive à sa bouche
            while (biting) yield return null;
            yield return new WaitForSeconds(0.4f);
        }

        void EatTick(Vector2 dir)
        {
            if (biting) return;
            Vector2 tip = anchor + dir * fishLen * gripFrac;   // la tête du poisson
            float d = Vector2.Distance(tip, Mouth);
            // plus le poisson approche, plus elle fait la grimace (elle recule même un peu)
            float near = Mathf.InverseLerp(560f, 110f, d);
            int frame = 1 + Mathf.Clamp(Mathf.RoundToInt(near * 7f), 0, 7);
            SetView(near > 0.93f ? "nausee_1" : "degout_" + frame);
            face.rectTransform.anchoredPosition = headHome + new Vector2(-near * 30f, near * 12f) + new Vector2(Mathf.Sin(t * 30f), 0f) * near * 4f;
            if (d < faceSize.x * 0.1f + 30f) StartCoroutine(EatSequence());
        }

        /// <summary>Tant que le poisson est dans sa bouche : il sort par la commissure des lèvres (tout ce qui est
        /// à gauche est « dans la bouche », effacé en fondu), suit la bouche d'une image à l'autre, la mâchoire
        /// le secoue et la queue bat.</summary>
        IEnumerator MouthFollow()
        {
            var frt = fish.rectTransform;
            while (inMouth && fish != null)
            {
                float dt = Time.deltaTime;
                mouthCur = Vector2.Lerp(mouthCur, MouthLocal(view), 1f - Mathf.Exp(-dt * 30f));
                PlaceInMouth(frt);
                tailWhip += dt * (5f + fish.fps * 0.5f);
                frt.localRotation = Quaternion.Euler(0, 0, hangAngle + Mathf.Sin(tailWhip) * (3f + fish.fps * 0.25f));
                yield return null;
            }
        }

        void PlaceInMouth(RectTransform frt)
        {
            mouthClip.anchoredPosition = mouthCur;
            mouthClip.sizeDelta = faceSize;
            fish.holdAlong = mouthAlong;
            frt.anchoredPosition = chewOfs;
        }

        /// <summary>Coup de mâchoire : le poisson est tiré vers l'intérieur d'un coup sec.</summary>
        IEnumerator JawPull(float amount, float dur)
        {
            float a0 = mouthAlong, a1 = mouthAlong + amount;
            for (float e = 0f; e < dur; e += Time.deltaTime)
            {
                float k = e / dur;
                mouthAlong = Mathf.Lerp(a0, a1, 1f - (1f - k) * (1f - k));
                chewOfs = new Vector2(-5f * Mathf.Sin(k * Mathf.PI), -7f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            mouthAlong = a1;
            chewOfs = Vector2.zero;
        }

        /// <summary>Elle croque la tête, la main lâche, le poisson glisse dans sa bouche ; elle mâche
        /// (il est avalé par à-coups, la queue frétille de moins en moins), l'aspire, l'avale… et recrache l'arête.</summary>
        IEnumerator EatSequence()
        {
            biting = true;
            var frt = fish.rectTransform;

            // 1. elle ouvre la bouche et CROC : elle attrape la tête
            SetView("mache_2");
            Sfx.Play(Sfx.Crunch, 1f, Random.Range(0.9f, 1.05f));
            Sfx.Vibrate();
            shakeLeft = 0.12f;
            Pop("CROC !", Mouth + new Vector2(-faceSize.x * 0.5f, 140f), new Color(1f, 0.5f, 0.4f), 100);

            // le poisson passe dans sa bouche : il est désormais tenu par la bouche
            float rot0 = frt.localEulerAngles.z;
            Vector3 s0 = frt.localScale;
            mouthCur = MouthLocal(view);
            mouthClip = UIKit.Rect("Bouche", face.transform);
            mouthClip.pivot = new Vector2(0f, 0.5f);
            mouthClip.sizeDelta = faceSize;
            mouthClip.anchoredPosition = mouthCur;
            var clip = mouthClip.gameObject.AddComponent<RectMask2D>();
#if UNITY_2020_1_OR_NEWER
            clip.softness = new Vector2Int(Mathf.RoundToInt(faceSize.x * 0.02f), 0);   // le poisson sort des lèvres en fondu
#endif
            frt.SetParent(mouthClip, false);
            fish.Fill = 1f;
            mouthAlong = 0f;
            fish.holdAlong = 0f;
            frt.anchoredPosition = Vector2.zero;
            frt.localRotation = Quaternion.Euler(0, 0, rot0);
            fish.freeHead = false;               // tenu par la bouche : c'est la queue qui bat
            fish.amplitude = 0.06f;
            fish.frequency = 8f;
            fish.waves = 0.8f;
            fish.fps = ArtGross.Fishes[fishType].fps * 1.8f;   // il se débat
            inMouth = true;
            StartCoroutine(HandLetsGo());

            // elle l'engloutit : le corps disparaît dans sa bouche (joues gonflées), la queue bascule et pend
            float[] lens = { 0.46f, 0.42f, 0.42f };
            float scaleIn = Mathf.Clamp(faceSize.y * lens[Mathf.Min(fishType, 2)] / fishLen, 0.45f, 1f);
            for (float e = 0f; e < 0.45f; e += Time.deltaTime)
            {
                float k = e / 0.45f, ease = 1f - (1f - k) * (1f - k);
                mouthAlong = Mathf.Lerp(0f, 0.62f, ease);
                frt.localScale = Vector3.Lerp(s0, Vector3.one * scaleIn, ease);
                float bounce = Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k) * 14f;
                frt.localRotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(rot0, hangAngle, ease) + bounce);
                face.rectTransform.localScale = new Vector3(1f + 0.03f * Mathf.Sin(k * Mathf.PI), 1f, 1f);
                face.rectTransform.anchoredPosition = headHome + new Vector2(0f, -16f * Mathf.Sin(k * Mathf.PI));
                PlaceInMouth(frt);
                yield return null;
            }
            face.rectTransform.localScale = Vector3.one;
            face.rectTransform.anchoredPosition = headHome;
            StartCoroutine(MouthFollow());
            SetView("nausee_1");
            Sfx.PlayRandom(Sfx.Beurks, 0.8f, 1.1f);
            Pop("Mmmpf !!", Mouth + new Vector2(-faceSize.x * 0.5f, 60f), Color.white, 64);
            yield return new WaitForSeconds(0.45f);

            // 2. elle mâche (3 fois) : à chaque coup de mâchoire le poisson rentre un peu, il se débat de moins en moins
            string[][] lines = {
                new[] { "Beurk…", "C'est cru !" },
                new[] { "Il bouge encore !!", "Ça gigote…" },
                new[] { "Ça croque…", "Les arêtes !" } };
            int[] chew = { 1, 3, 4, 3, 1, 2, 1, 3, 4, 5 };   // la bouche s'ouvre et se ferme
            for (int round = 0; round < 3; round++)
            {
                fish.fps = ArtGross.Fishes[fishType].fps * (1.6f - round * 0.4f);
                fish.amplitude = 0.08f - round * 0.02f;
                for (int i = 0; i < chew.Length; i++)
                {
                    bool flash = (round == 0 && i == 6) || (round == 1 && i == 4) || (round == 2 && i == 7);
                    SetView(flash ? (round == 2 ? "degout_8" : "degout_7") : "mache_" + chew[i]);
                    if (chew[i] == 1 && i > 0)
                    {
                        // la mâchoire se referme : CRONCH, le poisson est tiré vers l'intérieur
                        Sfx.PlayRandom(Sfx.Chews, 0.95f, Random.Range(0.9f, 1.05f));
                        if (round > 0) Sfx.Play(Sfx.Crunch, 0.45f, Random.Range(1.2f, 1.4f));
                        StartCoroutine(JawPull(0.022f, 0.1f));
                    }
                    if (i == 3) Pop(lines[round][0], Mouth + new Vector2(faceSize.x * 0.55f, 90f), Color.white, 58);
                    if (i == 8) Pop(lines[round][1], Mouth + new Vector2(-faceSize.x * 0.55f, 40f), Color.white, 54);
                    face.rectTransform.anchoredPosition = headHome + new Vector2(0f, (chew[i] == 1 ? -6f : 2f));
                    face.rectTransform.localScale = new Vector3(1f + (chew[i] == 1 ? 0.012f : 0f), 1f - (chew[i] == 1 ? 0.01f : 0f), 1f);
                    yield return new WaitForSeconds(0.11f);
                }
                face.rectTransform.localScale = Vector3.one;
                face.rectTransform.anchoredPosition = headHome;
                // elle déglutit entre deux bouchées, la tête secouée de dégoût
                Sfx.PlayRandom(Sfx.Glous, 0.8f, Random.Range(0.95f, 1.05f));
                SetView(round == 1 ? "nausee_2" : "nausee_1");
                for (float e = 0f; e < 0.3f; e += Time.deltaTime)
                {
                    face.rectTransform.anchoredPosition = headHome + new Vector2(Mathf.Sin(e * 60f) * 5f, 0f);
                    yield return null;
                }
                face.rectTransform.anchoredPosition = headHome;
            }

            // 3. elle aspire la queue comme un spaghetti : la queue fouette dans tous les sens
            SetView("mache_5");
            Sfx.Play(Sfx.Slurp, 1f, 1f);
            fish.fps = ArtGross.Fishes[fishType].fps * 2.4f;
            fish.amplitude = 0.16f;
            fish.frequency = 16f;
            Pop("SLUUURP !", Mouth + new Vector2(faceSize.x * 0.5f, 120f), new Color(1f, 0.85f, 0.4f), 80);
            float a0 = mouthAlong;
            for (float e = 0f; e < 0.5f; e += Time.deltaTime)
            {
                float k = e / 0.5f;
                mouthAlong = Mathf.Lerp(a0, 1.08f, k * k);
                chewOfs = new Vector2(Mathf.Sin(e * 70f) * 4f * (1f - k), 0f);
                yield return null;
            }
            inMouth = false;
            fish.gameObject.SetActive(false);
            if (mouthClip != null) Destroy(mouthClip.gameObject);
            Sfx.PlayRandom(Sfx.Glous, 1f, 0.9f);
            // la gorge : gloups
            SetView("mache_3");
            for (float e = 0f; e < 0.3f; e += Time.deltaTime)
            {
                float k = e / 0.3f;
                face.rectTransform.localScale = new Vector3(1f + 0.015f * Mathf.Sin(k * Mathf.PI), 1f - 0.012f * Mathf.Sin(k * Mathf.PI), 1f);
                yield return null;
            }
            face.rectTransform.localScale = Vector3.one;
            SetView("nausee_1");
            yield return new WaitForSeconds(0.45f);

            // 4. il reste quelque chose… les joues se gonflent et elle recrache l'arête
            SetView("mache_6");
            yield return new WaitForSeconds(0.22f);
            SetView("mache_7");
            yield return new WaitForSeconds(0.2f);
            SetView("mache_5");
            yield return new WaitForSeconds(0.15f);
            SetView("nausee_8");
            for (float e = 0f; e < 0.08f; e += Time.deltaTime)
            {
                face.rectTransform.anchoredPosition = headHome + new Vector2(18f * e / 0.08f, 6f);
                yield return null;
            }
            Sfx.Play(Sfx.Spit, 1f, 1f);
            Pop("PTOU !", Mouth + new Vector2(faceSize.x * 0.5f, 110f), Color.white, 96);
            yield return SpitBone();
            face.rectTransform.anchoredPosition = headHome;

            // 5. l'arrière-goût : dégoût intense, haut-le-cœur
            Sfx.PlayRandom(Sfx.Beurks, 1f, 1.05f);
            Pop("BEURK !!!", Mouth + new Vector2(-faceSize.x * 0.45f, 160f), new Color(0.7f, 1f, 0.35f), 100);
            foreach (int f in Range(1, 10))
            {
                SetView("nausee_" + f);
                if (f == 5) Sfx.PlayRandom(Sfx.Beurks, 0.9f, 0.95f);
                face.rectTransform.anchoredPosition = headHome + new Vector2(Mathf.Sin(f * 2.1f) * 12f, f % 3 == 0 ? 10f : 0f);
                yield return new WaitForSeconds(0.13f);
            }
            face.rectTransform.anchoredPosition = headHome;
            eatPhase = false;
            biting = false;
        }

        /// <summary>La main desserre les doigts et sort de l'écran.</summary>
        IEnumerator HandLetsGo()
        {
            if (holder == null) yield break;
            Vector2 p0 = anchor;
            float r0 = handRot;
            for (float e = 0f; e < 0.12f; e += Time.deltaTime) { holder.Set(p0, r0, Mathf.Lerp(0.9f, 1.15f, e / 0.12f)); yield return null; }
            Vector2 away = new Vector2(Size.x * 0.75f, -Size.y * 0.9f);
            for (float e = 0f; e < 0.5f; e += Time.deltaTime)
            {
                float k = e / 0.5f;
                holder.Set(Vector2.Lerp(p0, away, k * k), r0 - 10f * k, 1.15f);
                yield return null;
            }
            holder.root.gameObject.SetActive(false);
        }

        /// <summary>L'arête sort de sa bouche, vole en tournoyant et retombe par terre.</summary>
        IEnumerator SpitBone()
        {
            var spr = Art.FishBone;
            if (spr == null) yield break;
            float len = fishLen * 0.8f;
            var bone = UIKit.Image("Arete", Root, spr, new Vector2(len, len * spr.rect.height / spr.rect.width), Mouth);
            var rt = bone.rectTransform;
            Vector2 pos = Mouth;
            Vector2 v = new Vector2(620f, 760f);
            float rot = -20f, spin = 620f;
            float ground = -Size.y * 0.43f;
            int bounces = 0;
            for (float e = 0f; e < 2.2f; e += Time.deltaTime)
            {
                float dt = Time.deltaTime;
                float grow = Mathf.Clamp01(e / 0.22f);
                rt.localScale = Vector3.one * Mathf.Lerp(0.15f, 1f, grow);
                v += Vector2.down * 2600f * dt;
                pos += v * dt;
                rot += spin * dt;
                if (pos.y < ground && v.y < 0f)
                {
                    pos.y = ground;
                    if (bounces < 2)
                    {
                        Sfx.Play(Sfx.BoneDrop, bounces == 0 ? 0.9f : 0.5f, Random.Range(0.95f, 1.1f));
                        v = new Vector2(v.x * 0.45f, -v.y * 0.32f);
                        spin *= -0.4f;
                        bounces++;
                    }
                    else { v = Vector2.zero; spin = 0f; }
                }
                rt.anchoredPosition = pos;
                rt.localRotation = Quaternion.Euler(0, 0, rot);
                if (bounces >= 2 && v == Vector2.zero) break;
                yield return null;
            }
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Round(rot / 180f) * 180f + 4f);
        }

        protected override IEnumerator OnTimeUp()
        {
            PlaySeq("rire", Range(1, 10), 10f, 1f);
            Sfx.PlayRandom(Sfx.Taunts, 1f, 1f);
            Pop("Marine tient bon !", Vector2.zero, new Color(1f, 0.45f, 0.4f), 96);
            yield return new WaitForSeconds(1.2f);
            Finish(false, 0);
        }
    }
}
