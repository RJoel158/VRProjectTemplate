using System;
using UnityEngine;

namespace Tiro.Audio
{
    /// <summary>
    /// Generador procedural y gestor de efectos de sonido de alta fidelidad para Tiro Deportivo Olímpico.
    /// Garantiza que el disparo, la recarga, el percutor vacío y el estallido de platos siempre suenen
    /// con total contundencia y nitidez sin depender de archivos de audio externos.
    /// </summary>
    public static class ShootingSoundFX
    {
        private static AudioClip cachedPistolShot;
        private static AudioClip cachedRifleShot;
        private static AudioClip cachedShotgunShot;
        private static AudioClip cachedReloadSound;
        private static AudioClip cachedDryFireSound;
        private static AudioClip cachedClayShatter;
        private static AudioClip cachedClayLaunch;
        private static AudioClip cachedBullseyeHit;
        private static AudioClip cachedStandardHit;

        /// <summary>
        /// Disparo potente y seco de Pistola Olímpica 9mm.
        /// </summary>
        public static AudioClip GetGunshotPistol()
        {
            if (cachedPistolShot != null) return cachedPistolShot;

            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * 0.28f);
            float[] samples = new float[sampleCount];
            var rand = new System.Random(101);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float noise = (float)rand.NextDouble() * 2f - 1f;
                // Explosión inicial agresiva
                float blast = noise * Mathf.Exp(-t * 28f);
                // Cuerpo de graves (80Hz -> 38Hz)
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(82f, 38f, t / 0.28f) * t) * Mathf.Exp(-t * 16f);
                // Reverberación de recámara metálica
                float body = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-t * 32f);

                samples[i] = Mathf.Clamp(blast * 0.72f + sub * 0.55f + body * 0.3f, -1f, 1f);
            }

            cachedPistolShot = AudioClip.Create("FX_PistolShot", sampleCount, 1, sampleRate, false);
            cachedPistolShot.SetData(samples, 0);
            return cachedPistolShot;
        }

        /// <summary>
        /// Chasquido ultrasónico de alta velocidad de Rifle Olímpico .22 LR Match.
        /// </summary>
        public static AudioClip GetGunshotRifle()
        {
            if (cachedRifleShot != null) return cachedRifleShot;

            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * 0.34f);
            float[] samples = new float[sampleCount];
            var rand = new System.Random(202);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float noise = (float)rand.NextDouble() * 2f - 1f;
                // Crack supersónico penetrante
                float crack = noise * Mathf.Exp(-t * 36f);
                // Resonancia de cañón largo match grade
                float ring = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(140f, 52f, t / 0.34f) * t) * Mathf.Exp(-t * 14f);
                float punch = Mathf.Sin(2f * Mathf.PI * 75f * t) * Mathf.Exp(-t * 22f);

                samples[i] = Mathf.Clamp(crack * 0.78f + ring * 0.45f + punch * 0.4f, -1f, 1f);
            }

            cachedRifleShot = AudioClip.Create("FX_RifleShot", sampleCount, 1, sampleRate, false);
            cachedRifleShot.SetData(samples, 0);
            return cachedRifleShot;
        }

        /// <summary>
        /// Cañonazo atronador de Escopeta Olímpica de doble cañón Calibre 12.
        /// </summary>
        public static AudioClip GetGunshotShotgun()
        {
            if (cachedShotgunShot != null) return cachedShotgunShot;

            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * 0.42f);
            float[] samples = new float[sampleCount];
            var rand = new System.Random(303);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float noise = (float)rand.NextDouble() * 2f - 1f;
                // Deflagración de pólvora de alta densidad
                float blast = noise * Mathf.Exp(-t * 16f);
                // Subgrave masivo de 12-gauge
                float subBoom = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(68f, 26f, t / 0.42f) * t) * Mathf.Exp(-t * 10f);
                float punch = Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-t * 24f);

                samples[i] = Mathf.Clamp(blast * 0.88f + subBoom * 0.65f + punch * 0.4f, -1f, 1f);
            }

            cachedShotgunShot = AudioClip.Create("FX_ShotgunShot", sampleCount, 1, sampleRate, false);
            cachedShotgunShot.SetData(samples, 0);
            return cachedShotgunShot;
        }

        /// <summary>
        /// Sonido mecánico táctico de corredera / cerrojo metálico (estilo Pistol Whip / John Wick).
        /// </summary>
        public static AudioClip GetReloadSound()
        {
            if (cachedReloadSound != null) return cachedReloadSound;

            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * 0.38f);
            float[] samples = new float[sampleCount];
            var rand = new System.Random(404);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float val = 0f;

                // Clic 1: Liberación / apertura de corredera (t = 0.03s a 0.12s)
                if (t >= 0.03f && t < 0.12f)
                {
                    float t1 = t - 0.03f;
                    float clickNoise = ((float)rand.NextDouble() * 2f - 1f) * Mathf.Exp(-t1 * 55f);
                    float metalTone = Mathf.Sin(2f * Mathf.PI * 1450f * t1) * Mathf.Exp(-t1 * 40f);
                    val += clickNoise * 0.65f + metalTone * 0.35f;
                }

                // Clic 2: Inserción de cargador / amartillado firme (t = 0.19s a 0.35s)
                if (t >= 0.19f && t < 0.35f)
                {
                    float t2 = t - 0.19f;
                    float slamNoise = ((float)rand.NextDouble() * 2f - 1f) * Mathf.Exp(-t2 * 45f);
                    float steelPing = Mathf.Sin(2f * Mathf.PI * 1950f * t2) * Mathf.Exp(-t2 * 35f);
                    float snapBass = Mathf.Sin(2f * Mathf.PI * 220f * t2) * Mathf.Exp(-t2 * 60f);
                    val += slamNoise * 0.75f + steelPing * 0.45f + snapBass * 0.4f;
                }

                samples[i] = Mathf.Clamp(val, -1f, 1f);
            }

            cachedReloadSound = AudioClip.Create("FX_ReloadRack", sampleCount, 1, sampleRate, false);
            cachedReloadSound.SetData(samples, 0);
            return cachedReloadSound;
        }

        /// <summary>
        /// Clic seco de percutor sobre recámara vacía (Dry Fire).
        /// </summary>
        public static AudioClip GetDryFireSound()
        {
            if (cachedDryFireSound != null) return cachedDryFireSound;

            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * 0.09f);
            float[] samples = new float[sampleCount];
            var rand = new System.Random(505);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float noise = ((float)rand.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 85f);
                float ping = Mathf.Sin(2f * Mathf.PI * 2600f * t) * Mathf.Exp(-t * 65f);
                samples[i] = Mathf.Clamp(noise * 0.55f + ping * 0.5f, -1f, 1f);
            }

            cachedDryFireSound = AudioClip.Create("FX_DryFire", sampleCount, 1, sampleRate, false);
            cachedDryFireSound.SetData(samples, 0);
            return cachedDryFireSound;
        }

        /// <summary>
        /// Estallido cerámico de plato de arcilla al recibir el impacto de perdigones.
        /// </summary>
        public static AudioClip GetClayShatterSound()
        {
            if (cachedClayShatter != null) return cachedClayShatter;

            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * 0.32f);
            float[] samples = new float[sampleCount];
            var rand = new System.Random(606);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float noise = ((float)rand.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 22f);
                float ceramicCrunch = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(3400f, 800f, t / 0.32f) * t) * Mathf.Exp(-t * 28f);
                float pop = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-t * 40f);

                samples[i] = Mathf.Clamp(noise * 0.7f + ceramicCrunch * 0.5f + pop * 0.4f, -1f, 1f);
            }

            cachedClayShatter = AudioClip.Create("FX_ClayShatter", sampleCount, 1, sampleRate, false);
            cachedClayShatter.SetData(samples, 0);
            return cachedClayShatter;
        }

        /// <summary>
        /// Sonido de expulsión mecánica del lanzador de platos (fosa olímpica).
        /// </summary>
        public static AudioClip GetClayLaunchSound()
        {
            if (cachedClayLaunch != null) return cachedClayLaunch;

            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * 0.22f);
            float[] samples = new float[sampleCount];
            var rand = new System.Random(707);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 60f, t / 0.22f) * t) * Mathf.Exp(-t * 24f);
                float whoosh = ((float)rand.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 18f);
                samples[i] = Mathf.Clamp(thud * 0.6f + whoosh * 0.45f, -1f, 1f);
            }

            cachedClayLaunch = AudioClip.Create("FX_ClayLaunch", sampleCount, 1, sampleRate, false);
            cachedClayLaunch.SetData(samples, 0);
            return cachedClayLaunch;
        }

        /// <summary>
        /// Sonido de impacto en diana (Campana de metal en 10X o perforación en zona estándar).
        /// </summary>
        public static AudioClip GetTargetHitSound(bool isBullseye)
        {
            if (isBullseye)
            {
                if (cachedBullseyeHit != null) return cachedBullseyeHit;

                int sampleRate = 44100;
                int sampleCount = (int)(sampleRate * 0.55f);
                float[] samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    // Campana olímpica dorada de 10X (2400Hz y armónicos limpios)
                    float bell = Mathf.Sin(2f * Mathf.PI * 2400f * t) * Mathf.Exp(-t * 6f);
                    float harmonic = Mathf.Sin(2f * Mathf.PI * 4800f * t) * Mathf.Exp(-t * 9f);
                    float punch = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-t * 30f);
                    samples[i] = Mathf.Clamp(bell * 0.65f + harmonic * 0.35f + punch * 0.3f, -1f, 1f);
                }

                cachedBullseyeHit = AudioClip.Create("FX_BullseyeHit", sampleCount, 1, sampleRate, false);
                cachedBullseyeHit.SetData(samples, 0);
                return cachedBullseyeHit;
            }
            else
            {
                if (cachedStandardHit != null) return cachedStandardHit;

                int sampleRate = 44100;
                int sampleCount = (int)(sampleRate * 0.18f);
                float[] samples = new float[sampleCount];
                var rand = new System.Random(808);

                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float impactNoise = ((float)rand.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 40f);
                    float thud = Mathf.Sin(2f * Mathf.PI * 150f * t) * Mathf.Exp(-t * 30f);
                    samples[i] = Mathf.Clamp(impactNoise * 0.6f + thud * 0.5f, -1f, 1f);
                }

                cachedStandardHit = AudioClip.Create("FX_StandardHit", sampleCount, 1, sampleRate, false);
                cachedStandardHit.SetData(samples, 0);
                return cachedStandardHit;
            }
        }

        private static AudioClip cachedEasterEggSquawk;

        /// <summary>
        /// Sonido cómico de Easter Egg: graznido rápido arcade con campana triunfal.
        /// </summary>
        public static AudioClip GetEasterEggSquawk()
        {
            if (cachedEasterEggSquawk != null) return cachedEasterEggSquawk;

            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * 0.45f);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Tono cómico de pájaro arcade: modulación rápida de 1100Hz a 420Hz
                float freq = Mathf.Lerp(1100f, 420f, t / 0.45f) + Mathf.Sin(2f * Mathf.PI * 32f * t) * 80f;
                float birdTone = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 7f);

                // Chime triunfal agudo
                float bell = Mathf.Sin(2f * Mathf.PI * 1850f * t) * Mathf.Exp(-t * 5f) * 0.4f;

                samples[i] = Mathf.Clamp(birdTone * 0.65f + bell, -1f, 1f);
            }

            cachedEasterEggSquawk = AudioClip.Create("FX_EasterEgg_Squawk", sampleCount, 1, sampleRate, false);
            cachedEasterEggSquawk.SetData(samples, 0);
            return cachedEasterEggSquawk;
        }
    }
}
