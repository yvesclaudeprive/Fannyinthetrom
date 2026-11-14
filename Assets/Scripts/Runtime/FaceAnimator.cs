using UnityEngine;
using UnityEngine.UI;

namespace FannyIn2Trom
{
    /// <summary>
    /// Anime le gros plan du visage de Fanny : elle cligne des yeux, ouvre la bouche quand elle
    /// crie/parle, et grimace progressivement (légère, puis franche) selon SetGrimace(0..1).
    /// </summary>
    public class FaceAnimator : MonoBehaviour
    {
        Image img;
        float grimace, grimaceTarget;
        float blinkIn = 2f, blinkLeft;
        float talkLeft, flapTimer;
        bool talkFlap, mouthOpen;

        public static FaceAnimator On(Image image)
        {
            var a = image.gameObject.AddComponent<FaceAnimator>();
            a.img = image;
            return a;
        }

        /// <summary>0 = visage normal, 0.5 = grimace légère, 1 = grosse grimace. Transition animée.</summary>
        public void SetGrimace(float amount) { grimaceTarget = Mathf.Clamp01(amount); }

        /// <summary>Bouche ouverte pendant "seconds" ; flap = elle articule (ouvre/ferme), sinon reste grande ouverte.</summary>
        public void Talk(float seconds, bool flap = true)
        {
            talkLeft = Mathf.Max(talkLeft, seconds);
            talkFlap = flap;
            mouthOpen = true;
            flapTimer = 0.1f;
        }

        public void TalkFor(AudioClip clip, bool flap = true) { Talk(clip != null ? clip.length : 0.4f, flap); }

        void Update()
        {
            if (img == null) return;
            float dt = Time.deltaTime;
            grimace = Mathf.MoveTowards(grimace, grimaceTarget, dt * 4f);

            // clignement des yeux
            blinkIn -= dt;
            if (blinkIn <= 0f) { blinkLeft = 0.12f; blinkIn = Random.Range(1.8f, 4.5f); }
            blinkLeft -= dt;

            // bouche
            if (talkLeft > 0f)
            {
                talkLeft -= dt;
                if (talkFlap)
                {
                    flapTimer -= dt;
                    if (flapTimer <= 0f) { mouthOpen = !mouthOpen; flapTimer = Random.Range(0.07f, 0.13f); }
                }
                else mouthOpen = true;
            }
            else mouthOpen = false;

            Sprite s = Pick(grimace > 0.7f ? 2 : grimace > 0.25f ? 1 : 0, mouthOpen, blinkLeft > 0f);
            if (s != null && img.sprite != s) img.sprite = s;
        }

        static Sprite Pick(int level, bool mouth, bool eyesClosed)
        {
            if (level == 2)
            {
                if (mouth && Art.VisageBeurkBouche != null) return Art.VisageBeurkBouche;
                if (eyesClosed && Art.VisageBeurkYeuxFermes != null) return Art.VisageBeurkYeuxFermes;
                return Art.FannyVisageBeurk ?? Art.FannyVisage;
            }
            if (level == 1)
            {
                if (mouth && Art.VisageBeurkBouche != null) return Art.VisageBeurkBouche;
                return Art.VisageGrimace1 ?? Art.FannyVisageBeurk ?? Art.FannyVisage;
            }
            if (mouth && Art.VisageBouche != null) return Art.VisageBouche;
            if (eyesClosed && Art.VisageYeuxFermes != null) return Art.VisageYeuxFermes;
            return Art.FannyVisage;
        }
    }
}
