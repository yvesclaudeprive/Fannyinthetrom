using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FannyIn2Trom
{
    /// <summary>
    /// ÉPREUVE 1 — Le lasso breton.
    /// Fanny se balade dans la rue ; le joueur touche l'écran pour lancer le lasso
    /// à cet endroit. Le lasso met un peu de temps à arriver : il faut anticiper !
    /// </summary>
    public class LassoGame : MiniGame
    {
        public override string Title { get { return "Le lasso breton"; } }
        public override string Instruction { get { return "Touche l'écran pour lancer le lasso sur Fanny.\nAnticipe : elle ne s'arrête jamais longtemps !"; } }
        protected override float BaseTimeLimit { get { return 20f; } }
        public override bool HasOwnTimer { get { return bakedHud; } }

        // --- Réglages (coordonnées en pixels de l'image du décor, origine en haut à gauche)
        const float RefW = 2576f, RefH = 1438f;     // taille de l'image fournie
        const float HudBottom = 190f;                // bas des barres du HUD dessinées dans le décor
        const float WalkMinX = 170f, WalkMaxX = 2420f;
        const float WalkMinY = 1150f, WalkMaxYRef = 1330f; // position des pieds : sur la route
        const float HeightFar = 500f, HeightNear = 600f; // taille de Fanny selon la profondeur
        const float StridePx = 85f;                  // distance parcourue par étape de marche
        const float FlightTime = 0.38f;

        // Barres du HUD dessinées dans le décor (fractions de l'image, y vers le haut)
        static readonly Rect OrangeBar = new Rect(0.0823f, 0.9096f, 0.1655f, 0.0202f);
        static readonly Rect BlueBar = new Rect(0.0970f, 0.8755f, 0.0765f, 0.0132f);

        float imgW = RefW, imgH = RefH, scale, topCrop;
        float WalkMaxY = WalkMaxYRef;
        bool bakedHud;
        RectTransform world;
        Image fanny, shadow, loop, rope;
        Image orangeFill, blueFill;
        Text lassoText;

        Vector2 feet, target;          // en pixels image
        float speed, pauseTimer, retargetTimer, sprintTimer, walkPhase, walkDist;
        int lassosLeft, lassosMax;
        bool throwing, caught;
        int lastStepFrame = -1;

        protected override void Build()
        {
            var bgSprite = Art.BgBretagne;
            if (bgSprite != null) { imgW = bgSprite.rect.width; imgH = bgSprite.rect.height; }
            bakedHud = bgSprite != null && Mathf.Approximately(imgW, RefW) && Mathf.Approximately(imgH, RefH);

            scale = Mathf.Max(Size.x / imgW, Size.y / imgH);
            float overflow = imgH * scale - Size.y;
            // on cadre en bas pour voir la route où marche Fanny
            topCrop = Mathf.Max(0f, overflow);
            // si le haut de l'image est rogné, les barres dessinées ne sont plus visibles : HUD standard
            if (topCrop / scale > HudBottom * imgH / RefH - 60f) bakedHud = false;
            WalkMaxY = Mathf.Min(WalkMaxYRef, (Size.y + topCrop) / scale - 25f);

            var bg = UIKit.Image("Decor", Root, bgSprite, new Vector2(imgW, imgH) * scale, Vector2.zero,
                bgSprite != null ? Color.white : new Color(0.55f, 0.7f, 0.85f));
            world = bg.rectTransform;
            world.anchorMin = world.anchorMax = new Vector2(0.5f, 1f);
            world.pivot = new Vector2(0.5f, 1f);
            world.anchoredPosition = new Vector2(0f, topCrop);

            lassosMax = Mathf.Max(3, 6 - Mathf.RoundToInt(Difficulty));
            lassosLeft = lassosMax;

            if (bakedHud)
            {
                orangeFill = BarOverlay("BarreTemps", OrangeBar, new Color(1f, 0.62f, 0.08f));
                blueFill = BarOverlay("BarreLassos", BlueBar, new Color(0.3f, 0.78f, 1f));
            }
            lassoText = UIKit.Text("Lassos", Root, "", 46, Color.white, new Vector2(700, 70), Vector2.zero, TextAnchor.MiddleLeft);
            lassoText.rectTransform.anchorMin = lassoText.rectTransform.anchorMax = new Vector2(0f, 0f);
            lassoText.rectTransform.pivot = new Vector2(0f, 0f);
            lassoText.rectTransform.anchoredPosition = new Vector2(60f, 30f);
            UpdateLassoUI();

            // Fanny + ombre
            shadow = UIKit.Image("Ombre", world, Art.Circle, new Vector2(100, 20), Vector2.zero, new Color(0, 0, 0, 0.28f));
            fanny = UIKit.Image("Fanny", world, Art.FannyProfil, new Vector2(100, 400), Vector2.zero);
            fanny.rectTransform.pivot = new Vector2(0.5f, 0f);
            // positions exprimées depuis le haut du décor (comme dans l'image)
            fanny.rectTransform.anchorMin = fanny.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            shadow.rectTransform.anchorMin = shadow.rectTransform.anchorMax = new Vector2(0.5f, 1f);

            // Lasso (corde + boucle)
            rope = UIKit.Image("Corde", Root, null, new Vector2(10, 8), Vector2.zero, new Color(0.62f, 0.43f, 0.2f));
            rope.rectTransform.pivot = new Vector2(0f, 0.5f);
            loop = UIKit.Image("Boucle", Root, Art.LassoLoop, new Vector2(170, 170), Vector2.zero);
            rope.gameObject.SetActive(false);
            loop.gameObject.SetActive(false);

            Sfx.PlayMusic(Sfx.MusicBretagne, 0.55f, this);
            feet = new Vector2(Random.value < 0.5f ? 600f : 1900f, Random.Range(WalkMinY, WalkMaxY));
            NewTarget();
            PlaceFanny();
        }

        Image BarOverlay(string name, Rect r, Color color)
        {
            var back = UIKit.Image(name, world, null, Vector2.zero, Vector2.zero, new Color(0.16f, 0.16f, 0.2f));
            UIKit.Anchor(back.rectTransform, r.xMin, r.yMin, r.xMax, r.yMax);
            var fill = UIKit.Image("Remplissage", back.transform, null, Vector2.zero, Vector2.zero, color);
            UIKit.Anchor(fill.rectTransform, 0, 0, 1, 1);
            return fill;
        }

        void UpdateLassoUI()
        {
            lassoText.text = "Lassos : " + new string('O', lassosLeft) + new string('·', lassosMax - lassosLeft);
            if (blueFill != null) blueFill.rectTransform.anchorMax = new Vector2((float)lassosLeft / lassosMax, 1f);
        }

        void OnDestroy()
        {
            Sfx.StopMusic(this);
        }

        protected override void OnTimerChanged(float fraction)
        {
            if (orangeFill != null)
            {
                orangeFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
                orangeFill.color = fraction > 0.3f ? new Color(1f, 0.62f, 0.08f) : new Color(0.9f, 0.15f, 0.15f);
            }
        }

        // ------------------------------------------------------------- boucle

        protected override void Tick(float dt)
        {
            if (!Locked)
            {
                Wander(dt);
                if (Pointer.Down && !throwing && lassosLeft > 0) StartCoroutine(Throw(Pointer.Pos));
            }
            if (!caught) PlaceFanny();
        }

        void NewTarget()
        {
            // elle se promène surtout le long de la rue (gauche/droite)
            target = new Vector2(Random.Range(WalkMinX, WalkMaxX), Mathf.Clamp(feet.y + Random.Range(-70f, 70f), WalkMinY, WalkMaxY));
            // pas trop près : on veut qu'elle traverse l'écran
            if (Mathf.Abs(target.x - feet.x) < 400f) target.x = feet.x + (target.x < feet.x ? -600f : 600f);
            target.x = Mathf.Clamp(target.x, WalkMinX, WalkMaxX);
            speed = Random.Range(240f, 380f) * Difficulty;
            retargetTimer = Random.Range(1.2f, 3f) / Difficulty;
        }

        void Wander(float dt)
        {
            if (pauseTimer > 0f) { pauseTimer -= dt; return; }
            retargetTimer -= dt;
            if (retargetTimer <= 0f) NewTarget();

            float s = speed * (sprintTimer > 0f ? 1.7f : 1f);
            sprintTimer -= dt;
            Vector2 d = target - feet;
            float step = s * dt;
            if (d.magnitude <= step)
            {
                feet = target;
                if (Random.value < 0.35f) pauseTimer = Random.Range(0.3f, 0.9f) / Difficulty;
                NewTarget();
            }
            else feet += d.normalized * step;
            walkPhase += dt * s / 55f;
            walkDist += step;
        }

        float HeightAt(float feetY)
        {
            return Mathf.Lerp(HeightFar, HeightNear, Mathf.InverseLerp(WalkMinY, WalkMaxY, feetY)) * imgH / RefH;
        }

        /// <summary>Pixel de l'image (origine en haut à gauche) → position dans "world" (pivot en haut au centre).</summary>
        Vector2 ImgToWorld(Vector2 p) { return new Vector2((p.x - imgW * 0.5f) * scale, -p.y * scale); }

        /// <summary>Pixel de l'image → coordonnées écran (canvas, origine au centre).</summary>
        Vector2 ImgToScreen(Vector2 p) { return ImgToWorld(p) + new Vector2(0f, Size.y * 0.5f + topCrop); }

        void PlaceFanny()
        {
            float h = HeightAt(feet.y) * scale;
            bool moving = pauseTimer <= 0f && !Locked;
            Vector2 d = target - feet;

            Sprite view = Art.FannyFace;
            float flip = 1f;
            if (moving)
            {
                if (Mathf.Abs(d.x) > Mathf.Abs(d.y) * 1.2f)
                {
                    view = Art.FannyProfil;
                    var steps = Art.FannyMarche;
                    if (steps.Length >= 6)
                    {
                        // 6 étapes pour 2 pas : appui, genou qui plie, passage… puis l'autre jambe
                        int frame = Mathf.FloorToInt(walkDist / (StridePx / 3f)) % 6;
                        view = steps[frame];
                        if (frame != lastStepFrame && (frame == 0 || frame == 3))
                            Sfx.PlayRandom(Sfx.Steps, 0.35f, Random.Range(0.9f, 1.1f)); // bruit de pas sur les pavés
                        lastStepFrame = frame;
                    }
                    else if (steps.Length >= 3)
                    {
                        int[] cycle = { 0, 1, 2, 1 };
                        view = steps[cycle[Mathf.FloorToInt(walkDist / StridePx) % 4]];
                    }
                    flip = d.x > 0f ? -1f : 1f; // le profil fourni regarde vers la gauche
                }
                else view = d.y > 0f ? Art.FannyFace : Art.FannyDos;
            }
            if (view == null) view = Art.FannyFace;
            if (fanny.sprite != view) fanny.sprite = view;

            float k = walkDist / StridePx;
            float bob = moving ? (0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * k)) * h * 0.018f : 0f;
            fanny.rectTransform.sizeDelta = UIKit.SizeForHeight(view, h);
            fanny.rectTransform.anchoredPosition = ImgToWorld(feet) + new Vector2(0f, bob);
            if (!Locked)
            {
                fanny.rectTransform.localScale = new Vector3(flip, 1f, 1f);
                fanny.rectTransform.localRotation = Quaternion.Euler(0, 0, moving ? Mathf.Sin(Mathf.PI * k * 0.5f) * 1.5f : 0f);
            }
            shadow.rectTransform.sizeDelta = new Vector2(h * 0.32f, h * 0.05f);
            shadow.rectTransform.anchoredPosition = ImgToWorld(feet) + new Vector2(0f, h * 0.005f);
        }

        Vector2 TorsoScreen()
        {
            return ImgToScreen(feet) + new Vector2(0f, HeightAt(feet.y) * scale * 0.55f);
        }

        // -------------------------------------------------------------- lasso

        Vector2 Origin { get { return new Vector2(0f, -Size.y * 0.5f - 60f); } }

        void SetRope(Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            rope.rectTransform.anchoredPosition = a;
            rope.rectTransform.sizeDelta = new Vector2(d.magnitude, 9f);
            rope.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        IEnumerator Throw(Vector2 aim)
        {
            throwing = true;
            lassosLeft--;
            UpdateLassoUI();
            Sfx.Play(Sfx.Whoosh);
            rope.gameObject.SetActive(true);
            loop.gameObject.SetActive(true);

            Vector2 from = Origin;
            for (float e = 0f; e < FlightTime; e += Time.deltaTime)
            {
                float k = e / FlightTime;
                Vector2 p = Vector2.Lerp(from, aim, 1f - (1f - k) * (1f - k)) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 140f;
                loop.rectTransform.anchoredPosition = p;
                loop.rectTransform.localRotation = Quaternion.Euler(0, 0, e * 900f);
                loop.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.15f, k);
                SetRope(from, p);
                yield return null;
            }
            loop.rectTransform.anchoredPosition = aim;
            SetRope(from, aim);

            float h = HeightAt(feet.y) * scale;
            float radius = h * 0.26f + 30f;
            if (!Locked && Vector2.Distance(aim, TorsoScreen()) < radius)
            {
                yield return Caught();
                yield break;
            }

            // Raté
            Sfx.Play(Sfx.Miss);
            Pop("Raté !", aim + Vector2.up * 60f, new Color(1f, 0.5f, 0.4f), 70);
            if (Vector2.Distance(aim, TorsoScreen()) < radius * 2.5f)
            {
                sprintTimer = 1.1f; // elle a eu peur : elle accélère
                target.x = Mathf.Clamp(feet.x + (ImgToScreen(feet).x > aim.x ? 700f : -700f), WalkMinX, WalkMaxX);
            }
            for (float e = 0f; e < 0.3f; e += Time.deltaTime)
            {
                float k = e / 0.3f;
                Vector2 p = Vector2.Lerp(aim, from, k * k);
                loop.rectTransform.anchoredPosition = p;
                loop.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.4f, k);
                SetRope(from, p);
                yield return null;
            }
            rope.gameObject.SetActive(false);
            loop.gameObject.SetActive(false);
            throwing = false;

            if (lassosLeft <= 0 && !Locked)
            {
                Locked = true;
                Pop("Plus de lasso !", Vector2.zero, new Color(1f, 0.45f, 0.4f), 100);
                yield return new WaitForSeconds(1f);
                Finish(false, 0);
            }
        }

        IEnumerator Caught()
        {
            Locked = true;
            caught = true;
            Sfx.Play(Sfx.Catch);
            Sfx.Vibrate();
            fanny.sprite = Art.FannyFace;
            fanny.rectTransform.sizeDelta = UIKit.SizeForHeight(Art.FannyFace, HeightAt(feet.y) * scale);
            fanny.rectTransform.localRotation = Quaternion.identity;
            fanny.rectTransform.localScale = Vector3.one;
            Pop("ATTRAPÉE !", TorsoScreen() + Vector2.up * 260f, new Color(1f, 0.85f, 0.2f), 110);

            Vector2 from = Origin;
            float h = HeightAt(feet.y) * scale;
            Vector2 waist = TorsoScreen() + new Vector2(0f, -h * 0.07f);
            Vector2 start = loop.rectTransform.anchoredPosition;

            // la boucle se resserre autour de la taille
            for (float e = 0f; e < 0.35f; e += Time.deltaTime)
            {
                float k = e / 0.35f;
                loop.rectTransform.anchoredPosition = Vector2.Lerp(start, waist, k);
                loop.rectTransform.localRotation = Quaternion.identity;
                loop.rectTransform.localScale = new Vector3(Mathf.Lerp(1.15f, h * 0.2f / 170f * 1.6f, k), Mathf.Lerp(1.15f, 0.35f, k), 1f);
                SetRope(from, loop.rectTransform.anchoredPosition);
                yield return null;
            }
            // elle se tortille dans le lasso : bras qui battent, jambes qui ruent, grimace
            Vector2 basePos = fanny.rectTransform.anchoredPosition;
            var poses = Art.FannyDebat;
            float faceH = Art.FannyFace != null ? Art.FannyFace.rect.height : 1f;
            if (Sfx.Scream != null) Sfx.Play(Sfx.Scream, 1f);
            if (Sfx.Breath != null) Sfx.Play(Sfx.Breath, 0.6f);
            string[] cris = { "Au secours !", "Lâchez-moi !", "Hééé !" };
            int criIndex = 0;
            float nextYelp = 0f;
            const float struggle = 2.2f;
            for (float e = 0f; e < struggle; e += Time.deltaTime)
            {
                if (poses.Length > 0)
                {
                    var pose = poses[Mathf.FloorToInt(e * 12f) % poses.Length];
                    if (fanny.sprite != pose)
                    {
                        fanny.sprite = pose;
                        fanny.rectTransform.sizeDelta = UIKit.SizeForHeight(pose, h * pose.rect.height / faceH);
                    }
                }
                // le lasso la soulève : elle se retrouve en l'air et pédale des pieds
                float lift = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, e / 0.35f)) * h * 0.32f + Mathf.Sin(e * 9f) * h * 0.025f;
                float swing = Mathf.Sin(e * 5.5f);
                Vector2 off = new Vector2(swing * 28f, lift);
                fanny.rectTransform.anchoredPosition = basePos + off;
                fanny.rectTransform.localRotation = Quaternion.Euler(0, 0, swing * 9f);
                fanny.rectTransform.localScale = Vector3.one;
                shadow.rectTransform.sizeDelta = new Vector2(h * 0.32f, h * 0.05f) * (1f - 0.45f * (lift / (h * 0.32f)));
                loop.rectTransform.anchoredPosition = waist + off;
                loop.rectTransform.localRotation = Quaternion.Euler(0, 0, swing * 9f);
                SetRope(from, loop.rectTransform.anchoredPosition);
                if (e >= nextYelp)
                {
                    if (e > 0.5f || Sfx.Scream == null) Sfx.PlayRandom(Sfx.Yelps, 0.9f, Random.Range(0.95f, 1.15f));
                    if (criIndex < cris.Length && e > 0.2f)
                    {
                        Pop(cris[criIndex], TorsoScreen() + new Vector2(criIndex % 2 == 0 ? -260f : 260f, h * 0.45f), Color.white, 64);
                        criIndex++;
                    }
                    nextYelp = e + Random.Range(0.32f, 0.5f);
                }
                yield return null;
            }
            fanny.rectTransform.localScale = Vector3.one;
            fanny.rectTransform.localRotation = Quaternion.identity;
            // on la tire vers soi !
            Vector2 worldStart = fanny.rectTransform.anchoredPosition;
            for (float e = 0f; e < 0.6f; e += Time.deltaTime)
            {
                float k = e / 0.6f;
                Vector2 pull = new Vector2(-(ImgToScreen(feet).x) * k * k, -Size.y * 0.6f * k * k);
                fanny.rectTransform.anchoredPosition = worldStart + pull;
                fanny.rectTransform.localScale = Vector3.one * (1f + k * 0.5f);
                shadow.color = new Color(0, 0, 0, 0.28f * (1f - k));
                loop.rectTransform.anchoredPosition = waist + pull + new Vector2(0f, h * 0.5f * k * 0.5f);
                SetRope(from, loop.rectTransform.anchoredPosition);
                yield return null;
            }
            int points = 500 + lassosLeft * 150 + Mathf.RoundToInt(TimeLeft * 25f);
            Finish(true, points);
        }
    }
}
