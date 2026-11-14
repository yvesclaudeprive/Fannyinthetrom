using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FannyIn2Trom
{
    /// <summary>
    /// ÉPREUVE 2 — La barre du métro (gros plan, téléphone à l'horizontale).
    /// La main est posée sur la barre : le joueur la fait glisser de haut en bas
    /// le plus vite possible pour remplir le "Frottomètre".
    /// </summary>
    public class MetroBarGame : MiniGame
    {
        public override string Title { get { return "La barre du métro"; } }
        public override string Instruction { get { return "Frotte la main sur la barre :\nglisse ton doigt de haut en bas,\nTRÈS TRÈS VITE !"; } }
        protected override float BaseTimeLimit { get { return 12f; } }

        // Réglages
        const float RubGain = 0.000085f;  // remplissage par unité de déplacement du doigt
        const float Decay = 0.06f;        // le frottomètre redescend tout seul (par seconde)

        static readonly string[] Stations =
        {
            "Châtelet", "Strasbourg – Saint-Denis", "Réaumur – Sébastopol", "Barbès – Rochechouart",
            "Montparnasse – Bienvenüe", "Saint-Michel", "Gare du Nord", "Odéon", "Porte de Clignancourt"
        };

        RectTransform shakeRoot;
        float meter, rubAccum, germCount, stationTimer, shakeT;
        float barX, barWidth, handY, minY, maxY;
        RectTransform hands;
        Image gaugeFill, fannyFace;
        Text gaugeText, germText, stationText;
        readonly List<RectTransform> lights = new List<RectTransform>();
        int stuckGerms;
        FaceAnimator anim;
        float moanTimer = 1.5f;

        protected override void Build()
        {
            shakeRoot = UIKit.Stretch(UIKit.Rect("Rame", Root));
            Sprite bgSprite = Art.BgMetro;
            float W = bgSprite.rect.width, H = bgSprite.rect.height;
            float bgScale = Mathf.Max(Size.x / W, Size.y / H);

            if (Art.MetroIsPlaceholder)
            {
                // Décor provisoire : tunnel animé derrière les vitres + barre dessinée
                UIKit.Panel("Tunnel", shakeRoot, new Color(0.05f, 0.05f, 0.08f));
                for (int i = 0; i < 10; i++)
                {
                    var l = UIKit.Image("Lumiere" + i, shakeRoot, Art.RoundedRect, new Vector2(Random.Range(120, 260), 16), Vector2.zero, new Color(1f, 0.9f, 0.6f, 0.8f));
                    l.type = Image.Type.Sliced;
                    l.rectTransform.anchoredPosition = new Vector2(Random.Range(-Size.x, Size.x), Random.Range(450f, 670f) - 450f) * 1f;
                    l.rectTransform.anchoredPosition = new Vector2(l.rectTransform.anchoredPosition.x, l.rectTransform.anchoredPosition.y * bgScale);
                    lights.Add(l.rectTransform);
                }
                var bg = UIKit.Image("Decor", shakeRoot, bgSprite, new Vector2(W, H) * bgScale, Vector2.zero);
                barX = -Size.x * 0.22f;
                barWidth = 78f;
                UIKit.Image("Barre", shakeRoot, Art.MetalBar, new Vector2(barWidth, Size.y + 100), new Vector2(barX, 0));
                bg.name = "Decor";
            }
            else
            {
                // Décor fourni : gros plan sur la barre, cadré à droite de la Fanny déjà dessinée dans l'image
                float visibleW = W * (1f - Art.MetroBakedFannyRight);
                bgScale = Mathf.Max(bgScale, Size.x / visibleW);
                var bg = UIKit.Image("Decor", shakeRoot, bgSprite, new Vector2(W, H) * bgScale, Vector2.zero);
                float leftImg = W - Size.x / bgScale;               // bord gauche visible (pixels image)
                const float centerY = 0.56f;                        // hauteur de l'image placée au centre de l'écran
                bg.rectTransform.anchoredPosition = new Vector2(Size.x * 0.5f - W * bgScale * 0.5f, (centerY - 0.5f) * H * bgScale);
                barX = (Art.MetroPoleX * W - leftImg) * bgScale - Size.x * 0.5f;
                barWidth = Art.MetroPoleWidth * W * bgScale;
            }

            // Main(s) sur la barre
            hands = UIKit.Rect("Main", shakeRoot);
            hands.anchoredPosition = new Vector2(barX, 0);
            if (Art.HandGripIsPlaceholder)
            {
                UIKit.Image("MainHaut", hands, Art.HandGrip, new Vector2(260, 260), new Vector2(-32, 125));
                var low = UIKit.Image("MainBas", hands, Art.HandGrip, new Vector2(260, 260), new Vector2(32, -125));
                low.rectTransform.localScale = new Vector3(-1, 1, 1);
                minY = -Size.y * 0.5f + 270f;
                maxY = Size.y * 0.5f - 270f;
            }
            else
            {
                Sprite arm = Art.HandGrip;
                // assez grand pour que le bras sorte de l'écran à gauche
                float w = Mathf.Max(Art.HandWidthInBars * barWidth, (barX + Size.x * 0.5f) / Art.HandGripPivot.x + 60f);
                var img = UIKit.Image("Bras", hands, arm, new Vector2(w, w * arm.rect.height / arm.rect.width), Vector2.zero);
                img.rectTransform.pivot = Art.HandGripPivot;
                img.rectTransform.anchoredPosition = Vector2.zero;
                minY = -Size.y * 0.5f + 120f;
                maxY = Size.y * 0.5f - 200f;
            }
            handY = Mathf.Lerp(minY, maxY, 0.4f);

            float top = Size.y * 0.5f;
            float right = Size.x * 0.5f;

            // Fanny (portrait qui réagit)
            var face = Art.FannyVisage;
            float frameSize = 440f;
            // à droite de la barre (le bras arrive par la gauche)
            Vector2 framePos = new Vector2(Mathf.Max(barX + 420f, right - 560f), -90f);
            var frame = UIKit.Image("Cadre", shakeRoot, Art.RoundedRect, new Vector2(frameSize, frameSize), framePos, new Color(0.12f, 0.16f, 0.32f, 0.9f));
            frame.type = Image.Type.Sliced;
            fannyFace = UIKit.Image("Fanny", frame.transform, face, UIKit.SizeForHeight(face, frameSize * 0.9f), new Vector2(0, -frameSize * 0.02f));
            anim = FaceAnimator.On(fannyFace);
            Sfx.PlayMusic(Sfx.MetroAmbiance, 0.5f, this);
            UIKit.Text("Nom", frame.transform, "Fanny", 40, Color.white, new Vector2(400, 60), new Vector2(0, frameSize * 0.5f + 32f));

            // Frottomètre (à droite)
            float gaugeH = 600f;
            var gauge = UIKit.Image("Frottometre", shakeRoot, Art.RoundedRect, new Vector2(110, gaugeH), new Vector2(right - 190f, -80f), new Color(0, 0, 0, 0.7f));
            gauge.type = Image.Type.Sliced;
            gaugeFill = UIKit.Image("Niveau", gauge.transform, Art.RoundedRect, Vector2.zero, Vector2.zero, new Color(0.3f, 0.9f, 0.3f));
            gaugeFill.type = Image.Type.Sliced;
            UIKit.Anchor(gaugeFill.rectTransform, 0, 0, 1, 0);
            gaugeFill.rectTransform.offsetMin = new Vector2(12, 12);
            gaugeFill.rectTransform.offsetMax = new Vector2(-12, 0);
            UIKit.Text("Titre", gauge.transform, "FROTTO-\nMÈTRE", 36, new Color(1f, 0.85f, 0.25f), new Vector2(300, 100), new Vector2(0, gaugeH * 0.5f + 60f));
            gaugeText = UIKit.Text("Pourcent", gauge.transform, "0 %", 46, Color.white, new Vector2(300, 70), new Vector2(0, -gaugeH * 0.5f - 45f));

            // Bandeau "Prochaine station"
            float bannerW = Mathf.Min(980f, Size.x - 60f);
            var banner = UIKit.Image("Bandeau", shakeRoot, Art.RoundedRect, new Vector2(bannerW, 90), new Vector2(0, top - 160f), new Color(0.06f, 0.12f, 0.33f, 0.95f));
            banner.type = Image.Type.Sliced;
            stationText = UIKit.Text("Station", banner.transform, "", 38, Color.white, new Vector2(bannerW - 20f, 80), Vector2.zero, TextAnchor.MiddleCenter, false);
            germText = UIKit.Text("Microbes", shakeRoot, "Microbes ramassés : 0", 44, new Color(0.7f, 1f, 0.4f), new Vector2(Mathf.Min(900f, Size.x - 40f), 60), new Vector2(framePos.x, -top + 60f));
            NextStation();
        }

        void OnDestroy() { Sfx.StopMusic(this); }

        void NextStation()
        {
            stationTimer = 3.5f;
            stationText.text = "Prochaine station : " + Stations[Random.Range(0, Stations.Length)];
            Sfx.Play(Sfx.Chime, 0.5f);
        }

        protected override void Tick(float dt)
        {
            // le métro roule : lumières du tunnel + secousses
            foreach (var l in lights)
            {
                var p = l.anchoredPosition;
                p.x -= 2600f * dt;
                if (p.x < -Size.x * 0.7f) p.x += Size.x * 1.4f + Random.Range(0f, 400f);
                l.anchoredPosition = p;
            }
            shakeT += dt;
            shakeRoot.anchoredPosition = new Vector2(Mathf.Sin(shakeT * 23f) * 3f, Mathf.Sin(shakeT * 31f) * 4f + Mathf.Sin(shakeT * 2f) * 3f);
            stationTimer -= dt;
            if (stationTimer <= 0f && !Locked) NextStation();

            if (!Locked)
            {
                float travel = 0f;
                if (Pointer.Held)
                {
                    float targetY = Mathf.Clamp(Pointer.Pos.y, minY, maxY);
                    float newY = Mathf.Lerp(handY, targetY, 1f - Mathf.Exp(-dt * 30f));
                    travel = Mathf.Abs(newY - handY);
                    handY = newY;
                }
                meter += travel * RubGain / Mathf.Pow(Difficulty, 0.35f);
                meter -= Decay * Difficulty * dt;
                meter = Mathf.Clamp01(meter);

                rubAccum += travel;
                germCount += travel * 2.7f;
                if (rubAccum > 110f)
                {
                    rubAccum -= 110f;
                    Sfx.PlayRandom(Sfx.Rubs, 0.7f, Random.Range(0.9f, 1.2f));
                    SpawnGerm();
                }
                if (meter >= 1f) StartCoroutine(WinRoutine());
            }

            hands.anchoredPosition = new Vector2(barX, handY);
            gaugeFill.rectTransform.anchorMax = new Vector2(1f, meter);
            gaugeFill.color = Color.Lerp(new Color(0.3f, 0.9f, 0.3f), new Color(1f, 0.3f, 0.15f), meter);
            gaugeText.text = Mathf.RoundToInt(meter * 100f) + " %";
            germText.text = "Microbes ramassés : " + Mathf.RoundToInt(germCount).ToString("N0").Replace(',', ' ');
            fannyFace.color = Color.Lerp(Color.white, new Color(0.78f, 1f, 0.65f), meter);
            // Fanny grimace de plus en plus à mesure que ses mains se couvrent de microbes
            anim.SetGrimace(meter);
            moanTimer -= dt;
            if (!Locked && meter > 0.35f && moanTimer <= 0f)
            {
                anim.TalkFor(Sfx.PlayRandom(Sfx.Beurks, 0.6f, Random.Range(0.95f, 1.1f)), false);
                moanTimer = Random.Range(1.6f, 2.6f);
            }
            fannyFace.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(shakeT * 20f) * 3f * meter);
        }

        void SpawnGerm()
        {
            Vector2 at = new Vector2(barX + Random.Range(-1.5f, 1.5f) * barWidth, handY + Random.Range(-150f, 60f));
            var g = UIKit.Image("Microbe", shakeRoot, Art.Germ(Random.Range(0, 3)), new Vector2(64, 64), at);
            if (stuckGerms < 24)
            {
                // il se colle sur la main
                g.rectTransform.SetParent(hands, true);
                g.rectTransform.localScale = Vector3.one * Random.Range(0.6f, 1f);
                stuckGerms++;
            }
            else StartCoroutine(FlyAway(g));
        }

        IEnumerator FlyAway(Image g)
        {
            Vector2 v = new Vector2(Random.Range(-500f, 500f), Random.Range(200f, 600f));
            for (float e = 0f; e < 0.8f; e += Time.deltaTime)
            {
                g.rectTransform.anchoredPosition += v * Time.deltaTime;
                v.y -= 1200f * Time.deltaTime;
                g.rectTransform.localRotation = Quaternion.Euler(0, 0, e * 400f);
                g.color = new Color(1, 1, 1, 1f - e / 0.8f);
                yield return null;
            }
            Destroy(g.gameObject);
        }

        IEnumerator WinRoutine()
        {
            Locked = true;
            Sfx.Play(Sfx.Squish);
            Sfx.Vibrate();
            stationText.text = "Terminus : tout le monde descend !";
            Pop("MAIN BIEN CRADE !", new Vector2(0, 120), new Color(0.6f, 1f, 0.3f), 100);
            for (float e = 0f; e < 1.1f; e += Time.deltaTime)
            {
                hands.localScale = Vector3.one * (1f + Mathf.Sin(e * 20f) * 0.03f);
                yield return null;
            }
            Finish(true, 400 + Mathf.RoundToInt(TimeLeft * 60f) + Mathf.RoundToInt(germCount / 100f));
        }
    }
}
