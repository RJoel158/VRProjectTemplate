using System;
using UnityEngine;

namespace Golf.Audio
{
    /// <summary>
    /// Generador procedural de efectos de sonido sinteticos para minigolf.
    /// Garantiza que siempre existan sonidos nitidos de impacto, rodamiento, hoyo y rebotes
    /// incluso si no hay archivos de audio externos cargados en el proyecto.
    /// </summary>
    public static class GolfSoundFX
    {
        private static AudioClip putterHitClip;
        private static AudioClip cupSinkClip;
        private static AudioClip woodBounceClip;
        private static AudioClip outOfBoundsClip;
        private static AudioClip victoryFanfareClip;

        public static AudioClip GetPutterHitSound()
        {
            if (putterHitClip != null) return putterHitClip;

            // Golpe metalico/solido seco: onda sinusoidal con decaimiento exponencial rapido
            int sampleRate = 44100;
            float duration = 0.12f;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 45f);
                // Armonicos de impacto metalico tipo putter
                float tone = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.5f +
                             Mathf.Sin(2f * Mathf.PI * 3200f * t) * 0.3f +
                             Mathf.Sin(2f * Mathf.PI * 450f * t) * 0.2f;
                data[i] = tone * decay;
            }

            putterHitClip = AudioClip.Create("Proc_PutterHit", samples, 1, sampleRate, false);
            putterHitClip.SetData(data, 0);
            return putterHitClip;
        }

        public static AudioClip GetCupSinkSound()
        {
            if (cupSinkClip != null) return cupSinkClip;

            // Sonido hueco de "plop" y repique en el fondo del vaso de golf
            int sampleRate = 44100;
            float duration = 0.28f;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 18f);
                // Descenso de frecuencia tipo "plop"
                float freq = Mathf.Lerp(480f, 160f, t / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                // Repique plastico/metalico
                if (t > 0.08f)
                {
                    float rattle = Mathf.Sin(2f * Mathf.PI * 820f * (t - 0.08f)) * Mathf.Exp(-(t - 0.08f) * 35f) * 0.4f;
                    wave += rattle;
                }
                data[i] = wave * decay;
            }

            cupSinkClip = AudioClip.Create("Proc_CupSink", samples, 1, sampleRate, false);
            cupSinkClip.SetData(data, 0);
            return cupSinkClip;
        }

        public static AudioClip GetWoodBounceSound()
        {
            if (woodBounceClip != null) return woodBounceClip;

            // Rebote seco contra bordes de madera
            int sampleRate = 44100;
            float duration = 0.09f;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 50f);
                float tone = Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.7f +
                             Mathf.Sin(2f * Mathf.PI * 680f * t) * 0.3f;
                data[i] = tone * decay;
            }

            woodBounceClip = AudioClip.Create("Proc_WoodBounce", samples, 1, sampleRate, false);
            woodBounceClip.SetData(data, 0);
            return woodBounceClip;
        }

        public static AudioClip GetOutOfBoundsSound()
        {
            if (outOfBoundsClip != null) return outOfBoundsClip;

            // Tono descendente de penalizacion
            int sampleRate = 44100;
            float duration = 0.35f;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = 1f - (t / duration);
                float freq = Mathf.Lerp(300f, 120f, t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * decay * 0.6f;
            }

            outOfBoundsClip = AudioClip.Create("Proc_OutOfBounds", samples, 1, sampleRate, false);
            outOfBoundsClip.SetData(data, 0);
            return outOfBoundsClip;
        }

        public static AudioClip GetVictoryFanfare()
        {
            if (victoryFanfareClip != null) return victoryFanfareClip;

            // Arpegio triunfal de 3 notas (Do - Mi - Sol mayor)
            int sampleRate = 44100;
            float duration = 0.65f;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            float[] notes = { 523.25f, 659.25f, 783.99f }; // C5, E5, G5
            float noteDuration = duration / 3f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t / noteDuration), 0, 2);
                float noteT = t - (noteIndex * noteDuration);
                float decay = Mathf.Exp(-noteT * 6f);
                float freq = notes[noteIndex];
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * decay * 0.7f;
            }

            victoryFanfareClip = AudioClip.Create("Proc_VictoryFanfare", samples, 1, sampleRate, false);
            victoryFanfareClip.SetData(data, 0);
            return victoryFanfareClip;
        }
    }
}
