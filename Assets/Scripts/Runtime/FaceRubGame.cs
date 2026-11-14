using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FannyIn2Trom
{
    /// <summary>
    /// ÉPREUVE 3 — Sur le visage !
    /// Avec ses mains pleines de microbes du métro, Fanny se frotte le visage :
    /// le joueur frotte l'écran sur tout le visage jusqu'à le "contaminer" entièrement.
    /// </summary>
    public class FaceRubGame : MiniGame
    {
        public override string Title { get { return "Sur le visage !"; } }
        public override string Instruction { get { return "Les mains pleines de microbes...\nfrotte-les PARTOUT sur le visage de Fanny !"; } }
        protected override float BaseTimeLimit { get { return 11f; } }

        // Zone du visage dans l'image "fanny_visage" (fractions, y vers le haut).
        // À ajuster si l'image du visage est remplacée.
        const float ZoneCX = 0.501f, ZoneCY = 0.556f, ZoneRX = 0.175f, ZoneRY = 0.25f;
        const float Goal = 0.85f;        // part du visage à couvrir pour gagner
        const int TexW = 128;

        Image face, leftHand, rightHand, arm;
        RawImage paintImage;
        Texture2D paintTex;
        Color32[] paint;
        int texH, zoneCells, coveredCells;
        bool[] inZone, covered;
        Vector2 faceSize, facePos, lastPos;
        bool hasLast;
        Image progressFill;
        Text progressText;
        float beurkTimer, wobble;
        bool switchedFace;

        protected override void Build()
        {
            float s;
            Cover("Decor", Art.BgMetro, out s);
            UIKit.Panel("Voile", Root, new Color(0.02f, 0.03f, 0.08f, 0.6f));

            var sprite = Art.FannyVisage ?? Art.FannyFace;
            faceSize = UIKit.SizeForHeight(sprite, Size.y * 0.86f);
            facePos = new Vector2(0, -40);
            face = UIKit.Image("Visage", Root, sprite, faceSize, facePos);

            // calque de "microbes" peint par-dessus le visage
            texH = Mathf.RoundToInt(TexW * faceSize.y / faceSize.x);
            paintTex = new Texture2D(TexW, texH, TextureFormat.RGBA32, false);
            paintTex.wrapMode = TextureWrapMode.Clamp;
            paintTex.filterMode = FilterMode.Bilinear;
            paint = new Color32[TexW * texH];
            inZone = new bool[paint.Length];
            covered = new bool[paint.Length];
            for (int y = 0; y < texH; y++)
                for (int x = 0; x < TexW; x++)
                {
                    float nx = ((x + 0.5f) / TexW - ZoneCX) / ZoneRX;
                    float ny = ((y + 0.5f) / texH - ZoneCY) / ZoneRY;
                    if (nx * nx + ny * ny <= 1f) { inZone[y * TexW + x] = true; zoneCells++; }
                }
            paintTex.SetPixels32(paint);
            paintTex.Apply();
            var prt = UIKit.Rect("Microbes", face.transform);
            UIKit.Stretch(prt);
            paintImage = prt.gameObject.AddComponent<RawImage>();
            paintImage.texture = paintTex;
            paintImage.raycastTarget = false;

            // jauge de contamination
            var bar = UIKit.Image("Jauge", Root, Art.RoundedRect, new Vector2(700, 50), new Vector2(0, -Size.y * 0.5f + 70), new Color(0, 0, 0, 0.7f));
            bar.type = Image.Type.Sliced;
            progressFill = UIKit.Image("Niveau", bar.transform, Art.RoundedRect, Vector2.zero, Vector2.zero, new Color(0.5f, 0.9f, 0.2f));
            progressFill.type = Image.Type.Sliced;
            UIKit.Anchor(progressFill.rectTransform, 0, 0, 0, 1);
            progressFill.rectTransform.offsetMin = new Vector2(6, 6);
            progressFill.rectTransform.offsetMax = new Vector2(0, -6);
            progressText = UIKit.Text("Texte", bar.transform, "Visage contaminé : 0 %", 38, Color.white, new Vector2(700, 60), new Vector2(0, 55));

            // main(s)
            if (!Art.HandGripIsPlaceholder)
            {
                // le bras fourni (gros plan du métro), tenu par la paume
                Sprite a = Art.HandGrip;
                float w = Art.HandWidthInBars * 110f;
                arm = UIKit.Image("Bras", Root, a, new Vector2(w, w * a.rect.height / a.rect.width), new Vector2(0, -Size.y));
                arm.rectTransform.pivot = Art.HandGripPivot + new Vector2(-0.067f, -0.067f);
            }
            else
            {
                leftHand = UIKit.Image("MainG", Root, Art.HandOpen, new Vector2(220, 275), new Vector2(-Size.x * 0.32f, -200));
                rightHand = UIKit.Image("MainD", Root, Art.HandOpen, new Vector2(220, 275), new Vector2(Size.x * 0.32f, -200));
                leftHand.rectTransform.localScale = new Vector3(-1, 1, 1);
            }
            beurkTimer = 0.8f;
        }

        protected override void Tick(float dt)
        {
            wobble += dt;
            if (!Locked && Pointer.Held)
            {
                Vector2 p = Pointer.Pos;
                Vector2 from = hasLast ? lastPos : p;
                float dist = Vector2.Distance(from, p);
                if (dist > 1.5f)
                {
                    int steps = Mathf.Max(1, Mathf.CeilToInt(dist / 12f));
                    for (int i = 1; i <= steps; i++) Stamp(Vector2.Lerp(from, p, i / (float)steps));
                    paintTex.SetPixels32(paint);
                    paintTex.Apply();
                    beurkTimer -= dt * 2f;
                }
                lastPos = p;
                hasLast = true;
                PlaceHands(p, Pointer.Delta);
            }
            else
            {
                hasLast = false;
                if (!Locked) PlaceHands(new Vector2(arm != null ? -Size.x * 0.3f : 0f, -260 + Mathf.Sin(wobble * 3f) * 20f), Vector2.zero);
            }

            float cov = zoneCells > 0 ? coveredCells / (float)zoneCells : 0f;
            float shown = Mathf.Clamp01(cov / Goal);
            progressFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.02f, shown), 1f);
            progressText.text = "Visage contaminé : " + Mathf.RoundToInt(shown * 100f) + " %";

            // réactions de Fanny
            face.color = Color.Lerp(Color.white, new Color(0.8f, 1f, 0.72f), shown);
            face.rectTransform.anchoredPosition = facePos + new Vector2(Mathf.Sin(wobble * 37f) * 6f * shown, 0f);
            if (!switchedFace && shown > 0.5f && Art.FannyVisageBeurk != null)
            {
                face.sprite = Art.FannyVisageBeurk;
                switchedFace = true;
            }
            if (!Locked && beurkTimer <= 0f)
            {
                beurkTimer = Random.Range(0.9f, 1.6f);
                string[] words = { "Beurk !", "Pouah !", "Berk !!", "Bah non !", "Au secours !" };
                Sfx.Play(Sfx.Squish, 0.8f, Random.Range(0.9f, 1.2f));
                Pop(words[Random.Range(0, words.Length)], facePos + new Vector2(Random.value < 0.5f ? -430f : 430f, Random.Range(0f, 260f)), new Color(0.65f, 1f, 0.35f), 70);
            }

            if (!Locked && shown >= 1f) StartCoroutine(WinRoutine());
        }

        void PlaceHands(Vector2 p, Vector2 delta)
        {
            float tilt = Mathf.Clamp(delta.x * 0.8f, -25f, 25f);
            if (arm != null)
            {
                arm.rectTransform.anchoredPosition = Vector2.Lerp(arm.rectTransform.anchoredPosition, p, 0.6f);
                arm.rectTransform.localRotation = Quaternion.Euler(0, 0, tilt * 0.4f);
                return;
            }
            leftHand.rectTransform.anchoredPosition = Vector2.Lerp(leftHand.rectTransform.anchoredPosition, p + new Vector2(-95f, -20f), 0.6f);
            rightHand.rectTransform.anchoredPosition = Vector2.Lerp(rightHand.rectTransform.anchoredPosition, p + new Vector2(95f, -20f), 0.6f);
            leftHand.rectTransform.localRotation = Quaternion.Euler(0, 0, 12f + tilt);
            rightHand.rectTransform.localRotation = Quaternion.Euler(0, 0, -12f + tilt);
        }

        /// <summary>Dépose des microbes sur le visage à la position écran p (sous les deux mains).</summary>
        void Stamp(Vector2 p)
        {
            float radius = 0.085f * TexW;
            // avec le bras fourni : paume + doigts ; avec les mains dessinées : main gauche + main droite
            Vector2 off1 = arm != null ? Vector2.zero : new Vector2(-80f, 0f);
            Vector2 off2 = arm != null ? new Vector2(70f, 75f) : new Vector2(80f, 0f);
            for (int h = 0; h < 2; h++)
            {
                Vector2 hp = p + (h == 0 ? off1 : off2);
                Vector2 local = (hp - facePos) / faceSize + new Vector2(0.5f, 0.5f);
                float cx = local.x * TexW, cy = local.y * texH;
                int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radius)), x1 = Mathf.Min(TexW - 1, Mathf.CeilToInt(cx + radius));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radius)), y1 = Mathf.Min(texH - 1, Mathf.CeilToInt(cy + radius));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        int i = y * TexW + x;
                        if (!inZone[i]) continue;
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / radius;
                        if (d > 1f) continue;
                        float add = (1f - d) * 0.18f;
                        // petite texture "pustules"
                        float grain = Mathf.PerlinNoise(x * 0.35f, y * 0.35f);
                        Color32 c = paint[i];
                        byte a = (byte)Mathf.Min(170, c.a + add * 255f);
                        paint[i] = new Color32((byte)(70 + grain * 60), (byte)(150 + grain * 60), (byte)(30 + grain * 20), a);
                        if (!covered[i] && a > 95)
                        {
                            covered[i] = true;
                            coveredCells++;
                        }
                    }
            }
        }

        IEnumerator WinRoutine()
        {
            Locked = true;
            Sfx.Play(Sfx.Squish);
            Sfx.Vibrate();
            Pop("CONTAMINATION TOTALE !", new Vector2(0, Size.y * 0.3f), new Color(0.6f, 1f, 0.3f), 95);
            // quelques microbes qui dansent sur le visage
            for (int i = 0; i < 10; i++)
            {
                var g = UIKit.Image("Microbe", face.transform, Art.Germ(i), new Vector2(80, 80),
                    new Vector2(Random.Range(-ZoneRX, ZoneRX) * faceSize.x, (ZoneCY - 0.5f + Random.Range(-ZoneRY, ZoneRY)) * faceSize.y));
                g.rectTransform.localScale = Vector3.zero;
                StartCoroutine(GrowGerm(g.rectTransform, i * 0.05f));
            }
            yield return new WaitForSeconds(1.3f);
            Finish(true, 400 + Mathf.RoundToInt(TimeLeft * 60f));
        }

        IEnumerator GrowGerm(RectTransform g, float delay)
        {
            yield return new WaitForSeconds(delay);
            for (float e = 0f; e < 0.3f; e += Time.deltaTime)
            {
                g.localScale = Vector3.one * EaseOutBack(e / 0.3f);
                yield return null;
            }
            g.localScale = Vector3.one;
        }

        void OnDestroy()
        {
            if (paintTex != null) Destroy(paintTex);
        }
    }
}
