using System.Runtime.InteropServices;
using UnityEngine;

namespace FannyIn2Trom
{
    /// <summary>
    /// Bruitages, voix et musique synthétisés par code (aucun fichier audio nécessaire).
    /// Pour utiliser de vrais sons, déposez des .wav/.mp3 dans Assets/Resources/Audio/
    /// avec le même nom (ex. "lasso.wav", "musique_bretagne.mp3") : ils remplacent les sons générés.
    /// </summary>
    public static class Sfx
    {
        const int Rate = 44100;
        const int MusicRate = 22050;
        static AudioSource source, music;
        static System.Random rnd = new System.Random(7);

        public static AudioClip Click, Whoosh, Catch, Miss, Rub, Squish, Win, Lose, Tick, Chime, GameOver;
        /// <summary>Petits cris de Fanny.</summary>
        public static AudioClip Ouille, Beurk, Yelp, Vomit, Splash, Hic, Glou, Pour;
        /// <summary>Air breton (bombarde + biniou + tambour), en boucle.</summary>
        public static AudioClip MusicBretagne;
        /// <summary>Grondement du métro (boucle).</summary>
        public static AudioClip MetroAmbiance;
        /// <summary>Variantes (vrais sons libres de droits, CC0) : une est tirée au hasard à chaque fois.</summary>
        public static AudioClip[] Yelps, Ouilles, Beurks, Glous, Rubs, Steps, Clinks;
        public static AudioClip Scream, Breath, Laugh, GlouGlou;
        public static AudioClip[] Burps;
        /// <summary>Voix de l'annonceur.</summary>
        public static AudioClip Voice3, Voice2, Voice1, VoiceGo, VoiceGameOver, VoiceRecord, VoiceLevel;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void FannySetPlaybackAudio();
#endif

        /// <summary>Sur iPhone : le son joue même en mode silencieux (catégorie audio "Playback").</summary>
        public static void ForceSoundEvenWhenMuted()
        {
#if UNITY_IOS && !UNITY_EDITOR
            FannySetPlaybackAudio();
#endif
        }

        public static void Init(GameObject host)
        {
            ForceSoundEvenWhenMuted();
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
            music = host.AddComponent<AudioSource>();
            music.spatialBlend = 0f;
            music.playOnAwake = false;
            music.loop = true;
            music.volume = 0.55f;

            Click = Load("click") ?? Tone("click", 0.06f, t => Sq(t, 1200) * Env(t, 0.06f, 0.002f));
            Whoosh = Load("lasso") ?? Tone("lasso", 0.4f, t => Noise() * Mathf.Sin(Mathf.PI * t / 0.4f) * 0.5f * (0.5f + 0.5f * Mathf.Sin(t * 60f)));
            Catch = Load("attrape") ?? Tone("attrape", 0.5f, t =>
            {
                float f = t < 0.12f ? 523f : t < 0.24f ? 659f : 784f;
                return (Tri(t, f) * 0.6f + Sq(t, f * 2f) * 0.1f) * Env(t, 0.5f, 0.01f);
            });
            Miss = Load("rate") ?? Tone("rate", 0.35f, t => Tri(t, 330f - 400f * t) * Env(t, 0.35f, 0.01f) * 0.7f);
            Rub = Load("frotte") ?? Tone("frotte", 0.09f, t => Noise() * Env(t, 0.09f, 0.01f) * 0.35f);
            Squish = Load("squish") ?? Tone("squish", 0.25f, t => (Mathf.Sin(2 * Mathf.PI * (120f + 300f * t) * t) * 0.6f + Noise() * 0.2f) * Env(t, 0.25f, 0.005f));
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
            GameOver = Load("gameover") ?? Lose;

            // --- voix de Fanny (synthèse par formants) ---
            // "Hmpf !" quand on lui touche le visage
            Ouille = Load("ouille") ?? VoiceClip("ouille", 0.28f, t => 330f - 160f * t, new[] { 320f, 850f, 2500f }, t => Env(t, 0.28f, 0.02f), t => 0.05f);
            // "Beurk !"
            Beurk = Load("beurk") ?? VoiceClip("beurk", 0.5f, t => 290f - 90f * t, new[] { 420f, 1450f, 2450f },
                t => t < 0.03f ? 0f : Env(t - 0.03f, 0.42f, 0.02f), t => t > 0.36f ? 0.9f : 0.04f);
            // "Hé !" (elle se débat dans le lasso)
            Yelp = Load("cri") ?? VoiceClip("cri", 0.2f, t => 380f + 180f * t, new[] { 480f, 2100f, 2900f }, t => Env(t, 0.2f, 0.01f), t => 0.05f);
            // "Bleuaaargh"
            Vomit = Load("vomi") ?? VoiceClip("vomi", 1.3f, t => 170f - 50f * t + 12f * Mathf.Sin(t * 30f), new[] { 650f, 1050f, 2400f },
                t => Env(t, 1.3f, 0.05f) * (0.55f + 0.45f * Mathf.Sin(t * 2f * Mathf.PI * 22f)), t => 0.25f + 0.3f * t);
            Splash = Load("splash") ?? SplashClip();
            // "Hic !"
            Hic = Load("hic") ?? VoiceClip("hic", 0.14f, t => 330f + 900f * t, new[] { 300f, 2300f, 3000f },
                t => t < 0.008f ? t / 0.008f : Mathf.Exp(-(t - 0.008f) * 22f), t => 0.15f);
            // "Glou glou glou"
            Glou = Load("glou") ?? VoiceClip("glou", 0.42f, t => 210f - 40f * t, new[] { 300f, 750f, 2300f },
                t => Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 2f * 7f)), 2f) * Env(t, 0.42f, 0.01f), t => 0.1f);
            // liquide qui coule
            Pour = Load("verser") ?? PourClip();

            MusicBretagne = Load("musique_bretagne") ?? BretonTune();
            MetroAmbiance = Load("metro_ambiance");

            Yelps = Variants("cri", Yelp);
            Ouilles = Variants("ouille", Ouille);
            Beurks = Variants("beurk", Beurk);
            Glous = Variants("glou", Glou);
            Rubs = Variants("frotte", Rub);
            Steps = Variants("pas", null);
            Clinks = Variants("verre", null);
            Scream = Load("au_secours");
            Breath = Load("souffle");
            Laugh = Load("rire");
            GlouGlou = Load("glouglou") ?? Glou;
            Burps = Variants("rot", Squish);
            Voice3 = Load("voix_3"); Voice2 = Load("voix_2"); Voice1 = Load("voix_1"); VoiceGo = Load("voix_go");
            VoiceGameOver = Load("voix_gameover"); VoiceRecord = Load("voix_record"); VoiceLevel = Load("voix_niveau");
        }

        public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (source == null || clip == null) return;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        /// <summary>Joue une des variantes au hasard ; renvoie le son choisi (pour animer la bouche).</summary>
        public static AudioClip PlayRandom(AudioClip[] clips, float volume = 1f, float pitch = 1f)
        {
            if (clips == null || clips.Length == 0) return null;
            var c = clips[rnd.Next(clips.Length)];
            Play(c, volume, pitch);
            return c;
        }

        static AudioClip[] Variants(string name, AudioClip fallback)
        {
            var list = new System.Collections.Generic.List<AudioClip>();
            for (int i = 1; i <= 12; i++)
            {
                var c = Load(name + "_" + i);
                if (c == null) break;
                list.Add(c);
            }
            if (list.Count == 0)
            {
                var single = Load(name);
                if (single != null) list.Add(single);
                else if (fallback != null) list.Add(fallback);
            }
            return list.ToArray();
        }

        static object musicOwner;

        /// <summary>Lance une musique/ambiance en boucle. Si elle joue déjà, elle continue sans coupure.</summary>
        public static void PlayMusic(AudioClip clip, float volume = 0.55f, object owner = null)
        {
            if (music == null || clip == null) return;
            musicOwner = owner;
            music.volume = volume;
            if (music.clip == clip && music.isPlaying) return;
            music.clip = clip;
            music.Play();
        }

        /// <summary>Arrête la musique, seulement si c'est bien "owner" qui l'a lancée en dernier.</summary>
        public static void StopMusic(object owner = null)
        {
            if (music == null) return;
            if (owner != null && owner != musicOwner) return;
            music.Stop();
            musicOwner = null;
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
            return Make(name, data, Rate);
        }

        static AudioClip Make(string name, float[] data, int rate)
        {
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Résonateur (filtre passe-bande à deux pôles).</summary>
        struct Reso
        {
            float a1, a2, g, y1, y2;
            public Reso(float freq, float bw, int rate)
            {
                float r = Mathf.Exp(-Mathf.PI * bw / rate);
                a1 = -2f * r * Mathf.Cos(2f * Mathf.PI * freq / rate);
                a2 = r * r;
                g = 1f - r;
                y1 = y2 = 0f;
            }
            public float Step(float x)
            {
                float y = g * x - a1 * y1 - a2 * y2;
                y2 = y1; y1 = y;
                return y;
            }
        }

        /// <summary>Voix synthétique : impulsions glottiques filtrées par 3 formants (+ souffle).</summary>
        static AudioClip VoiceClip(string name, float duration, System.Func<float, float> pitch, float[] formants,
            System.Func<float, float> amp, System.Func<float, float> breath)
        {
            int n = Mathf.CeilToInt(duration * Rate);
            var data = new float[n];
            var res = new Reso[formants.Length];
            for (int k = 0; k < formants.Length; k++) res[k] = new Reso(formants[k], 80f + formants[k] * 0.06f, Rate);
            float phase = 0f, peak = 0.0001f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                phase += pitch(t) / Rate;
                if (phase >= 1f) phase -= 1f;
                float glottal = phase < 0.4f ? Mathf.Sin(phase / 0.4f * Mathf.PI) : 0f; // impulsion
                float b = breath(t);
                float x = glottal * (1f - b) + Noise() * b * 0.6f;
                float y = 0f;
                for (int k = 0; k < res.Length; k++) y += res[k].Step(x) * (k == 0 ? 1f : 0.6f / k);
                y *= amp(t);
                data[i] = y;
                peak = Mathf.Max(peak, Mathf.Abs(y));
            }
            for (int i = 0; i < n; i++) data[i] = data[i] / peak * 0.8f;
            return Make(name, data, Rate);
        }

        static AudioClip PourClip()
        {
            int n = Mathf.CeilToInt(0.2f * Rate);
            var data = new float[n];
            var bp = new Reso(1100f, 700f, Rate);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float gurgle = 0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * (38f + 20f * Mathf.Sin(t * 50f)));
                data[i] = bp.Step(Noise()) * gurgle * Mathf.Sin(Mathf.PI * t / 0.2f) * 10f;
            }
            Normalize(data);
            for (int i = 0; i < n; i++) data[i] *= 0.6f;
            return Make("verser", data, Rate);
        }

        static AudioClip SplashClip()
        {
            int n = Mathf.CeilToInt(0.7f * Rate);
            var data = new float[n];
            var lp = new Reso(700f, 900f, Rate);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float e = Mathf.Exp(-t * 6f) * (0.6f + 0.4f * Mathf.Sin(t * 90f));
                data[i] = lp.Step(Noise()) * e * 14f;
            }
            Normalize(data);
            return Make("splash", data, Rate);
        }

        /// <summary>
        /// Air original dans le style d'un an-dro breton : bombarde (mélodie),
        /// biniou (bourdon ré/la) et tambour régulier. Boucle de 8 mesures.
        /// </summary>
        static AudioClip BretonTune()
        {
            const float bpm = 132f;
            float eighth = 60f / bpm / 2f;
            // notes (demi-tons au-dessus de ré 4) et durées en croches
            int D = 0, E = 2, Fs = 4, G = 5, A = 7, B = 9, C5 = 10, D5 = 12;
            int[][] tune =
            {
                new[] { A,1, A,1, B,1, A,1, G,1, Fs,1, E,2 },
                new[] { Fs,1, G,1, A,1, Fs,1, E,1, D,1, D,2 },
                new[] { A,1, A,1, B,1, A,1, G,1, Fs,1, E,2 },
                new[] { Fs,1, G,1, A,1, B,1, A,1, Fs,1, E,2 },
                new[] { D5,1, D5,1, C5,1, A,1, B,1, A,1, G,2 },
                new[] { A,1, B,1, A,1, Fs,1, E,1, Fs,1, D,2 },
                new[] { D5,1, D5,1, C5,1, A,1, B,1, A,1, G,2 },
                new[] { A,1, Fs,1, E,1, Fs,1, D,4 },
            };
            int totalEighths = 64;
            int n = Mathf.CeilToInt(totalEighths * eighth * MusicRate);
            var data = new float[n];
            const float d4 = 293.66f;

            // mélodie (bombarde : timbre nasal, légère ornementation)
            float pos = 0f;
            float phase = 0f;
            var nasal = new Reso(1250f, 500f, MusicRate);
            foreach (var bar in tune)
            {
                for (int k = 0; k < bar.Length; k += 2)
                {
                    float f = d4 * Mathf.Pow(2f, bar[k] / 12f);
                    float dur = bar[k + 1] * eighth;
                    int s0 = Mathf.RoundToInt(pos * MusicRate), s1 = Mathf.Min(n, Mathf.RoundToInt((pos + dur) * MusicRate));
                    for (int i = s0; i < s1; i++)
                    {
                        float lt = (i - s0) / (float)MusicRate;
                        // petit "coup de doigt" (grace note) au début de chaque note
                        float ff = lt < 0.025f ? f * 1.12f : f * (1f + 0.004f * Mathf.Sin(lt * 33f));
                        phase += ff / MusicRate;
                        if (phase >= 1f) phase -= 1f;
                        float raw = (2f * phase - 1f) * 0.6f + (phase < 0.3f ? 0.5f : -0.2f);
                        float env = Mathf.Min(1f, lt / 0.01f) * (lt > dur * 0.88f ? Mathf.Max(0f, 1f - (lt - dur * 0.88f) / (dur * 0.12f)) : 1f);
                        data[i] += (raw * 0.35f + nasal.Step(raw) * 2.2f) * env * 0.42f;
                    }
                    pos += dur;
                }
            }

            // bourdon du biniou (ré 3 + la 3) et tambour
            float pd = 0f, pa = 0f;
            float beat = eighth * 2f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)MusicRate;
                pd += 146.83f / MusicRate; if (pd >= 1f) pd -= 1f;
                pa += 220f / MusicRate; if (pa >= 1f) pa -= 1f;
                float drone = ((2f * pd - 1f) * 0.5f + (2f * pa - 1f) * 0.3f) * 0.09f;
                float bt = t % beat;
                float kick = Mathf.Sin(2f * Mathf.PI * (55f + 70f * Mathf.Exp(-bt * 30f)) * bt) * Mathf.Exp(-bt * 14f) * 0.3f;
                float off = (t + eighth) % beat;
                float tap = Noise() * Mathf.Exp(-off * 60f) * 0.12f;
                data[i] += drone + kick + tap;
            }
            Normalize(data);
            var clip = AudioClip.Create("musique_bretagne", n, 1, MusicRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void Normalize(float[] data)
        {
            float peak = 0.0001f;
            for (int i = 0; i < data.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            for (int i = 0; i < data.Length; i++) data[i] = data[i] / peak * 0.85f;
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
