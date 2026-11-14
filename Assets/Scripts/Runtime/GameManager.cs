using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FannyIn2Trom
{
    /// <summary>
    /// Point d'entrée du jeu "Fanny in 2 Trom".
    /// Il se crée tout seul au lancement (aucune configuration de scène nécessaire),
    /// construit l'interface et enchaîne : menu → 3 épreuves en boucle → game over.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const string GameTitle = "Fanny in 2 Trom";
        const int StartLives = 3;
        const string BestKey = "fanny_best_score";

        public static GameManager Instance { get; private set; }

        Canvas canvas;
        CanvasScaler scaler;
        RectTransform stageLayer, hudLayer, overlayLayer;
        Hud hud;

        int score, lives, level;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance == null) new GameObject("FannyIn2Trom").AddComponent<GameManager>();
        }

        /// <summary>Taille de l'écran en unités canvas : le petit côté vaut toujours 1080.</summary>
        public static Vector2 CanvasSize
        {
            get
            {
                float w = Mathf.Max(1f, Screen.width), h = Mathf.Max(1f, Screen.height);
                return w >= h ? new Vector2(1080f * w / h, 1080f) : new Vector2(1080f, 1080f * h / w);
            }
        }

        static bool IsPortrait { get { return Screen.height > Screen.width; } }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
#if !(ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER)
            Input.multiTouchEnabled = true;
#endif
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            LockOrientation(false);
            Screen.orientation = ScreenOrientation.AutoRotation;

            if (Camera.main == null)
            {
                var cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.transform.SetParent(transform);
            }
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.08f, 0.09f, 0.16f);
            Camera.main.orthographic = true;

            BuildCanvas();
            Sfx.Init(gameObject);
            StartCoroutine(MainLoop());
        }

        void BuildCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            UpdateScaler();
            go.AddComponent<GraphicRaycaster>();

            stageLayer = UIKit.Stretch(UIKit.Rect("Stage", go.transform));
            var safe = UIKit.Stretch(UIKit.Rect("SafeArea", go.transform));
            safe.gameObject.AddComponent<SafeArea>();
            hudLayer = UIKit.Stretch(UIKit.Rect("HUD", safe));
            overlayLayer = UIKit.Stretch(UIKit.Rect("Overlay", go.transform));
            hud = new Hud(hudLayer);
            hud.Show(false);

            if (FindObjectOfTypeCompat() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform);
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        static EventSystem FindObjectOfTypeCompat()
        {
            return EventSystem.current;
        }

        void Update()
        {
            UpdateScaler();
            Pointer.Update(canvas != null ? canvas.scaleFactor : 1f);
        }

        /// <summary>Le petit côté de l'écran = 1080 unités, en paysage comme en portrait.</summary>
        void UpdateScaler()
        {
            if (scaler == null) return;
            bool portrait = IsPortrait;
            scaler.referenceResolution = portrait ? new Vector2(1080, 1920) : new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = portrait ? 0f : 1f;
        }

        // ========================================================= orientation

        static void LockOrientation(bool portrait)
        {
            Screen.autorotateToPortrait = portrait;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = !portrait;
            Screen.autorotateToLandscapeRight = !portrait;
        }

        /// <summary>
        /// Passe l'iPhone en portrait ou en paysage et attend que l'écran ait tourné.
        /// (Dans l'éditeur, l'écran ne tourne pas : c'est la vue Game qui décide.)
        /// </summary>
        IEnumerator SetOrientation(bool portrait)
        {
            if (Application.isEditor || IsPortrait == portrait) yield break;
            LockOrientation(portrait);
            Screen.orientation = portrait ? ScreenOrientation.Portrait : ScreenOrientation.LandscapeLeft;
            float timeout = 2f;
            while (IsPortrait != portrait && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (!portrait) Screen.orientation = ScreenOrientation.AutoRotation; // paysage gauche OU droite
            yield return null;
            UpdateScaler();
            yield return null;
        }

        // ================================================================ flux

        IEnumerator MainLoop()
        {
            yield return null; // laisse le CanvasScaler s'initialiser
            while (true)
            {
                yield return TitleScreen();
                yield return PlayGame();
                yield return GameOverScreen();
            }
        }

        IEnumerator PlayGame()
        {
            score = 0;
            lives = StartLives;
            level = 1;
            hud.Show(true);
            hud.SetScore(score);
            hud.SetLives(lives);

            var stages = new Type[] { typeof(LassoGame), typeof(MetroBarGame), typeof(FaceRubGame) };
            while (lives > 0)
            {
                hud.SetLevel(level);
                if (level > 1) yield return Banner("NIVEAU " + level, "Ça accélère !", new Color(1f, 0.8f, 0.2f), 1.6f);

                for (int i = 0; i < stages.Length && lives > 0; i++)
                {
                    float difficulty = 1f + 0.3f * (level - 1);
                    var root = UIKit.Stretch(UIKit.Rect("Epreuve" + (i + 1), stageLayer));
                    var game = (MiniGame)root.gameObject.AddComponent(stages[i]);
                    bool rotated = IsPortrait != game.WantsPortrait;
                    yield return SetOrientation(game.WantsPortrait);
                    game.Setup(root, CanvasSize, difficulty);

                    yield return Intro(i + 1, game, rotated && !Application.isEditor);
                    yield return game.Run(hud);

                    if (game.Won)
                    {
                        score += game.Points;
                        hud.SetScore(score);
                        Sfx.Play(Sfx.Win);
                        yield return Banner("BRAVO !", "+" + game.Points + " points", new Color(0.45f, 1f, 0.45f), 1.3f);
                    }
                    else
                    {
                        lives--;
                        hud.SetLives(lives);
                        Sfx.Play(Sfx.Lose);
                        Sfx.Vibrate();
                        yield return Banner("RATÉ !", lives > 0 ? "Encore " + lives + (lives > 1 ? " vies" : " vie") : "Plus de vies...", new Color(1f, 0.45f, 0.4f), 1.6f);
                    }
                    Destroy(root.gameObject);
                }
                level++;
            }
            hud.Show(false);
            yield return SetOrientation(false);
        }

        // ============================================================== écrans

        IEnumerator TitleScreen()
        {
            var root = UIKit.Stretch(UIKit.Rect("Titre", overlayLayer));
            Vector2 size = CanvasSize;
            Sfx.PlayMusic(Sfx.MusicBretagne);

            var bgSprite = Art.BgBretagne;
            var bg = UIKit.Image("Fond", root, bgSprite, size, Vector2.zero);
            if (bgSprite != null)
            {
                float s = Mathf.Max(size.x / bgSprite.rect.width, size.y / bgSprite.rect.height);
                bg.rectTransform.sizeDelta = new Vector2(bgSprite.rect.width, bgSprite.rect.height) * s;
            }
            UIKit.Panel("Voile", root, new Color(0.05f, 0.07f, 0.2f, 0.55f));

            var fanny = UIKit.Image("Fanny", root, Art.FannyFace, UIKit.SizeForHeight(Art.FannyFace, 900), new Vector2(-size.x * 0.27f, -40));
            var title = UIKit.Text("Titre", root, "FANNY IN 2 TROM", 150, new Color(1f, 0.85f, 0.2f), new Vector2(1300, 200), new Vector2(size.x * 0.12f, 260));
            title.GetComponent<Outline>().effectDistance = new Vector2(6, -6);
            UIKit.Text("SousTitre", root, "Lasso breton, barre de métro\net microbes en folie !", 56, Color.white, new Vector2(1200, 160), new Vector2(size.x * 0.12f, 90));

            bool go = false;
            UIKit.Button("Jouer", root, "JOUER", new Vector2(460, 140), new Vector2(size.x * 0.12f, -110), new Color(0.9f, 0.25f, 0.3f), () => go = true);
            int best = PlayerPrefs.GetInt(BestKey, 0);
            UIKit.Text("Record", root, best > 0 ? "Record : " + best : "Touche JOUER pour commencer", 44, new Color(0.85f, 0.9f, 1f), new Vector2(1000, 80), new Vector2(size.x * 0.12f, -250));

            float t = 0f;
            while (!go)
            {
                t += Time.deltaTime;
                fanny.rectTransform.anchoredPosition = new Vector2(-size.x * 0.27f, -40 + Mathf.Abs(Mathf.Sin(t * 3f)) * 25f);
                fanny.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 3f) * 2f);
                title.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.2f) * 0.03f);
                yield return null;
            }
            Sfx.StopMusic();
            Destroy(root.gameObject);
        }

        IEnumerator GameOverScreen()
        {
            int best = PlayerPrefs.GetInt(BestKey, 0);
            bool record = score > best;
            if (record)
            {
                PlayerPrefs.SetInt(BestKey, score);
                PlayerPrefs.Save();
            }

            var root = UIKit.Stretch(UIKit.Rect("GameOver", overlayLayer));
            UIKit.Panel("Voile", root, new Color(0.04f, 0.04f, 0.1f, 0.88f));
            UIKit.Text("Titre", root, "GAME OVER", 150, new Color(1f, 0.4f, 0.35f), new Vector2(1400, 200), new Vector2(0, 270));
            UIKit.Text("Score", root, "Score : " + score + "\nNiveau atteint : " + level, 64, Color.white, new Vector2(1200, 200), new Vector2(0, 80));
            if (record) UIKit.Text("Record", root, "NOUVEAU RECORD !", 60, new Color(1f, 0.85f, 0.2f), new Vector2(1200, 100), new Vector2(0, -60));
            else UIKit.Text("Record", root, "Record : " + best, 48, new Color(0.8f, 0.85f, 1f), new Vector2(1200, 100), new Vector2(0, -60));

            bool done = false;
            UIKit.Button("Rejouer", root, "REJOUER", new Vector2(420, 130), new Vector2(0, -230), new Color(0.9f, 0.25f, 0.3f), () => done = true);
            while (!done) yield return null;
            Destroy(root.gameObject);
        }

        IEnumerator Intro(int number, MiniGame game, bool rotateHint)
        {
            var root = UIKit.Stretch(UIKit.Rect("Intro", overlayLayer));
            float w = Mathf.Min(1700f, CanvasSize.x - 60f);
            var veil = UIKit.Panel("Voile", root, new Color(0.03f, 0.04f, 0.12f, 0.7f));
            UIKit.Text("Num", root, "ÉPREUVE " + number, 60, new Color(1f, 0.8f, 0.25f), new Vector2(w, 100), new Vector2(0, 260));
            UIKit.Text("Titre", root, game.Title, 100, Color.white, new Vector2(w, 160), new Vector2(0, 150));
            UIKit.Text("Consigne", root, game.Instruction, 50, new Color(0.85f, 0.95f, 1f), new Vector2(w, 220), new Vector2(0, -20));
            if (rotateHint)
                UIKit.Text("Tourne", root, game.WantsPortrait ? ">> Tiens ton téléphone à la VERTICALE <<" : ">> Remets ton téléphone à l'HORIZONTALE <<",
                    44, new Color(1f, 0.55f, 0.5f), new Vector2(w, 120), new Vector2(0, 360));
            var count = UIKit.Text("Compte", root, "", 140, new Color(1f, 0.85f, 0.2f), new Vector2(800, 200), new Vector2(0, -250));

            yield return new WaitForSeconds(rotateHint ? 2.2f : 1.2f);
            string[] steps = { "3", "2", "1", "GO !" };
            foreach (var s in steps)
            {
                count.text = s;
                Sfx.Play(Sfx.Tick, 1f, s == "GO !" ? 1.5f : 1f);
                for (float e = 0; e < 0.45f; e += Time.deltaTime)
                {
                    count.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, e / 0.2f);
                    yield return null;
                }
            }
            // fondu
            var texts = root.GetComponentsInChildren<Text>();
            for (float e = 0; e < 0.2f; e += Time.deltaTime)
            {
                float a = 1f - e / 0.2f;
                veil.color = new Color(veil.color.r, veil.color.g, veil.color.b, 0.7f * a);
                foreach (var t in texts) t.color = new Color(t.color.r, t.color.g, t.color.b, a);
                yield return null;
            }
            Destroy(root.gameObject);
        }

        IEnumerator Banner(string title, string sub, Color color, float duration)
        {
            var root = UIKit.Stretch(UIKit.Rect("Banniere", overlayLayer));
            var band = UIKit.Image("Bande", root, null, new Vector2(4000, 330), Vector2.zero, new Color(0.03f, 0.04f, 0.12f, 0.85f));
            float w = Mathf.Min(1800f, CanvasSize.x - 40f);
            var t1 = UIKit.Text("Titre", root, title, 130, color, new Vector2(w, 180), new Vector2(0, 50));
            UIKit.Text("Sous", root, sub, 56, Color.white, new Vector2(w, 100), new Vector2(0, -90));
            for (float e = 0; e < duration; e += Time.deltaTime)
            {
                float k = Mathf.Clamp01(e / 0.25f);
                band.rectTransform.localScale = new Vector3(1f, k, 1f);
                t1.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, k);
                yield return null;
            }
            Destroy(root.gameObject);
        }
    }

    // ==================================================================== HUD

    /// <summary>Score, vies, niveau et chrono affichés pendant la partie.</summary>
    public class Hud
    {
        readonly RectTransform root;
        readonly Text scoreText, levelText;
        readonly Image[] hearts;
        readonly RectTransform timerRoot;
        readonly Image timerFill;

        public Hud(RectTransform parent)
        {
            root = UIKit.Stretch(UIKit.Rect("HudRoot", parent));

            // Score + niveau (en haut à droite)
            scoreText = UIKit.Text("Score", root, "0", 64, Color.white, new Vector2(600, 90), Vector2.zero, TextAnchor.MiddleRight);
            Corner(scoreText.rectTransform, new Vector2(1, 1), new Vector2(-30, -55));
            levelText = UIKit.Text("Niveau", root, "Niveau 1", 38, new Color(1f, 0.85f, 0.3f), new Vector2(600, 60), Vector2.zero, TextAnchor.MiddleRight);
            Corner(levelText.rectTransform, new Vector2(1, 1), new Vector2(-30, -120));

            // Vies (cœurs) sous le score
            hearts = new Image[5];
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = UIKit.Image("Coeur" + i, root, Art.Heart, new Vector2(64, 64), Vector2.zero);
                Corner(hearts[i].rectTransform, new Vector2(1, 1), new Vector2(-60 - i * 72, -185));
            }

            // Chrono (en haut au centre)
            timerRoot = UIKit.Rect("Chrono", root);
            Corner(timerRoot, new Vector2(0.5f, 1), new Vector2(0, -50));
            timerRoot.sizeDelta = new Vector2(620, 44);
            var back = UIKit.Image("Fond", timerRoot, Art.RoundedRect, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.55f));
            back.type = Image.Type.Sliced;
            UIKit.Stretch(back.rectTransform);
            timerFill = UIKit.Image("Remplissage", timerRoot, Art.RoundedRect, Vector2.zero, Vector2.zero, new Color(1f, 0.7f, 0.1f));
            timerFill.type = Image.Type.Sliced;
            UIKit.Anchor(timerFill.rectTransform, 0, 0, 1, 1);
            timerFill.rectTransform.offsetMin = new Vector2(6, 6);
            timerFill.rectTransform.offsetMax = new Vector2(-6, -6);
        }

        static void Corner(RectTransform rt, Vector2 anchor, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
        }

        public void Show(bool on) { root.gameObject.SetActive(on); }
        public void SetScore(int s) { scoreText.text = s.ToString("N0").Replace(',', ' ') + " pts"; }
        public void SetLevel(int l) { levelText.text = "Niveau " + l; }

        public void SetLives(int n)
        {
            for (int i = 0; i < hearts.Length; i++) hearts[i].gameObject.SetActive(i < n);
        }

        public void SetTimer(float fraction, bool visible)
        {
            timerRoot.gameObject.SetActive(visible);
            fraction = Mathf.Clamp01(fraction);
            timerFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.04f, fraction), 1f);
            timerFill.color = fraction > 0.3f ? new Color(1f, 0.7f, 0.1f) : new Color(1f, 0.25f, 0.2f);
        }
    }

    /// <summary>Garde le HUD hors de l'encoche et des coins arrondis de l'iPhone.</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        RectTransform rt;

        void Awake() { rt = (RectTransform)transform; Apply(); }
        void Update() { if (last != Screen.safeArea) Apply(); }

        void Apply()
        {
            last = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            rt.anchorMin = new Vector2(last.xMin / Screen.width, last.yMin / Screen.height);
            rt.anchorMax = new Vector2(last.xMax / Screen.width, last.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
