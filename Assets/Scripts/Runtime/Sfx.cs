using UnityEngine;

namespace FannyIn2Trom
{
    /// <summary>
    /// Bruitages synthétisés par code (aucun fichier audio nécessaire).
    /// Pour utiliser de vrais sons, déposez des .wav/.mp3 dans Assets/Resources/Audio/
    /// avec le même nom (ex. "lasso.wav") : ils remplacent automatiquement les sons générés.
    /// </summary>
    public static class Sfx
    {
        const int Rate = 44100;
        static AudioSource source;
        static System.Random rnd = new System.Random(7);

        public static AudioClip Click, Whoosh, Catch, Miss, Rub, Squish, Win, Lose, Tick, Chime, GameOver;

        public static void Init(GameObject host)
        {
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;

            Click = Load("click") ?? Tone("click", 0.06f, t => Sq(t, 1200) * Env(t, 0.06f, 0.002f));
            Whoosh = Load("lasso") ?? Tone("lasso", 0.4f, t => Noise() * Mathf.Sin(Mathf.PI * t / 0.4f) * 0.5f * (0.5f + 0.5f * Mathf.Sin(t * 60f)));
            Catch = Load("attrape") ?? Tone("attrape", 0.5f, t =>
            {
                float f = t < 0.12f ? 523f : t < 0.24f ? 659f : 784f;
                return (Tri(t, f) * 0.6f + Sq(t, f * 2f) * 0.1f) * Env(t, 0.5f, 0.01f);
            });
            Miss = Load("rate") ?? Tone("rate", 0.35f, t => Tri(t, 330f - 400f * t) * Env(t, 0.35f, 0.01f) * 0.7f);
            Rub = Load("frotte") ?? Tone("frotte", 0.09f, t => Noise() * Env(t, 0.09f, 0.01f) * 0.35f);
            Squish = Load("beurk") ?? Tone("beurk", 0.25f, t => (Mathf.Sin(2 * Mathf.PI * (120f + 300f * t) * t) * 0.6f + Noise() * 0.2f) * Env(t, 0.25f, 0.005f));
            Win = Load("victoire") ?? Tone("victoire", 0.9f, t =>
            {
                float[] notes = { 523f, 659f, 784f, 1046f };
                int i = Mathf.Min(3, (int)(t / 0.13f));
                float lt = t - i * 0.13f;
                float d = i == 3 ? 0.5f : 0.13f;
                return (Tri(t, notes[i]) * 0.55f + Sq(t, notes[i]) * 0.08f) * Env(lt, d, 0.005f);
            });
            // "wah wah wah wahhh" de trombone (le "trom" !)
            Lose = Load("perdu") ?? Tone("perdu", 1.6f, t =>
            {
                float[] notes = { 311f, 294f, 277f, 262f };
                int i = Mathf.Min(3, (int)(t / 0.3f));
                float lt = t - i * 0.3f;
                float d = i == 3 ? 0.7f : 0.3f;
                float f = notes[i] * (i == 3 ? 1f + 0.03f * Mathf.Sin(lt * 35f) : 1f);
                float wah = 0.5f + 0.5f * Mathf.Sin(Mathf.Min(lt / d, 1f) * Mathf.PI);
                return (Saw(t, f) * 0.35f + Saw(t, f * 1.005f) * 0.25f) * wah * Env(lt, d, 0.02f);
            });
            Tick = Load("tic") ?? Tone("tic", 0.08f, t => Tri(t, 880f) * Env(t, 0.08f, 0.002f) * 0.6f);
            Chime = Load("carillon") ?? Tone("carillon", 1.0f, t =>
            {
                float f = t < 0.35f ? 784f : 523f;
                float lt = t < 0.35f ? t : t - 0.35f;
                return Mathf.Sin(2 * Mathf.PI * f * t) * 0.5f * Mathf.Exp(-lt * 4f);
            });
            GameOver = Lose;
        }

        public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (source == null || clip == null) return;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        public static void Vibrate()
        {
#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }

        // ------------------------------------------------------------ synthèse

        static AudioClip Load(string name)
        {
            return Resources.Load<AudioClip>("Audio/" + name);
        }

        static AudioClip Tone(string name, float duration, System.Func<float, float> f)
        {
            int n = Mathf.CeilToInt(duration * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f) * 0.8f;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Env(float t, float dur, float attack)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / attack;
            return Mathf.Clamp01(1f - (t - attack) / Mathf.Max(0.0001f, dur - attack));
        }

        static float Sq(float t, float f) { return Mathf.Repeat(t * f, 1f) < 0.5f ? 1f : -1f; }
        static float Tri(float t, float f) { return 1f - 4f * Mathf.Abs(Mathf.Repeat(t * f + 0.25f, 1f) - 0.5f); }
        static float Saw(float t, float f) { return 2f * Mathf.Repeat(t * f, 1f) - 1f; }
        static float Noise() { return (float)(rnd.NextDouble() * 2.0 - 1.0); }
    }
}
