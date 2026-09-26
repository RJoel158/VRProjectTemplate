using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Golf.Audio
{
    /// <summary>
    /// Gestor central de audio para el minijuego de Minigolf VR.
    /// Garantiza la reproduccion estéreo 2D de efectos de impacto, embocado en el hoyo,
    /// rebotes contra maderas, penalizaciones y musica ambiental relajante en loop.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class GolfAudioManager : MonoBehaviour
    {
        private static GolfAudioManager instance;
        public static GolfAudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    EnsureManagerExists();
                }
                return instance;
            }
        }

        private static AudioSource sfxSource;
        private static AudioSource bgmSource;
        private static AudioListener verifiedListener;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip customBgmClip;
        [SerializeField] private AudioClip customPutterHitClip;
        [SerializeField] private AudioClip customCupSinkClip;
        [SerializeField] private AudioClip customWoodBounceClip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeGolfAudioWatcher()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            CheckSceneAndManageAudio(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckSceneAndManageAudio(scene);
        }

        private static void CheckSceneAndManageAudio(Scene scene)
        {
            bool isGolf = scene.name.IndexOf("Golf", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isGolf)
            {
                EnsureManagerExists();
                EnsureAudioSystemActive();
                PlayBackgroundMusic();
            }
            else
            {
                StopBackgroundMusic();
                if (instance != null)
                {
                    Destroy(instance.gameObject);
                    instance = null;
                }
            }
        }

        private void Awake()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name.IndexOf("Golf", StringComparison.OrdinalIgnoreCase) < 0)
            {
                Destroy(gameObject);
                return;
            }

            if (instance == null)
            {
                instance = this;
            }
            SetupAudioSources();
            EnsureAudioSystemActive();
        }

        private void Start()
        {
            PlayBackgroundMusic();
        }

        private void OnDestroy()
        {
            StopBackgroundMusic();
            if (instance == this)
            {
                instance = null;
            }
        }

        public static void EnsureManagerExists()
        {
            if (instance != null) return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name.IndexOf("Golf", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            instance = FindAnyObjectByType<GolfAudioManager>();
            if (instance == null)
            {
                GameObject go = new GameObject("[GolfAudioManager]");
                instance = go.AddComponent<GolfAudioManager>();
            }

            instance.SetupAudioSources();
            EnsureAudioSystemActive();
        }

        private void SetupAudioSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.spatialBlend = 0f; // 2D estereo directo
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.volume = 1f;
                sfxSource.bypassEffects = true;
                sfxSource.bypassListenerEffects = true;
                sfxSource.bypassReverbZones = true;
            }

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.spatialBlend = 0f;
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
                bgmSource.volume = 0.35f;
                bgmSource.bypassEffects = true;
                bgmSource.bypassListenerEffects = true;
                bgmSource.bypassReverbZones = true;
            }

            LoadDefaultClips();
        }

        private void LoadDefaultClips()
        {
#if UNITY_EDITOR
            if (customBgmClip == null)
            {
                // Cargar musica del canal Mii / Wii para la escena de Golf
                customBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/03_Resources/Sounds/Nintendo Wii - Mii Channel Theme.mp3");
                if (customBgmClip == null)
                {
                    customBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/03_Resources/Sounds/Wii Menu Music 4K(MP3_160K).mp3");
                }
            }
            if (customWoodBounceClip == null)
            {
                customWoodBounceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/03_Resources/Sounds/WiiClick.mp3");
            }
#endif
        }

        public static void EnsureAudioSystemActive()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.audioMasterMute)
            {
                UnityEditor.EditorUtility.audioMasterMute = false;
                Debug.Log("[GolfAudioManager] Editor desmuteado automaticamente.");
            }
#endif
            AudioListener.pause = false;
            AudioListener.volume = 1f;

            if (verifiedListener == null || !verifiedListener.enabled)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    verifiedListener = cam.GetComponent<AudioListener>();
                    if (verifiedListener == null)
                    {
                        verifiedListener = cam.gameObject.AddComponent<AudioListener>();
                    }
                    verifiedListener.enabled = true;
                }
            }
        }

        public static void PlayBackgroundMusic(AudioClip clip = null, float volume = 0.35f)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name.IndexOf("Golf", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            EnsureManagerExists();
            if (bgmSource == null) return;

            AudioClip toPlay = clip != null ? clip : instance.customBgmClip;
            if (toPlay != null)
            {
                if (bgmSource.clip != toPlay)
                {
                    bgmSource.clip = toPlay;
                }
                bgmSource.volume = volume;
                bgmSource.loop = true;
                if (!bgmSource.isPlaying)
                {
                    bgmSource.Play();
                }
            }
        }

        public static void StopBackgroundMusic()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Stop();
            }
        }

        public static void PlayPutterHit(float normalizedPower = 0.5f)
        {
            EnsureManagerExists();
            EnsureAudioSystemActive();

            AudioClip clip = instance.customPutterHitClip != null ? instance.customPutterHitClip : GolfSoundFX.GetPutterHitSound();
            float volume = Mathf.Clamp(0.35f + normalizedPower * 0.65f, 0.2f, 1f);
            sfxSource.pitch = UnityEngine.Random.Range(0.96f, 1.05f);
            sfxSource.PlayOneShot(clip, volume);
            sfxSource.pitch = 1f;
        }

        public static void PlayCupSink()
        {
            EnsureManagerExists();
            EnsureAudioSystemActive();

            AudioClip clip = instance.customCupSinkClip != null ? instance.customCupSinkClip : GolfSoundFX.GetCupSinkSound();
            sfxSource.PlayOneShot(clip, 1f);
        }

        public static void PlayWoodBounce(float impactSpeed = 1f)
        {
            EnsureManagerExists();
            EnsureAudioSystemActive();

            AudioClip clip = instance.customWoodBounceClip != null ? instance.customWoodBounceClip : GolfSoundFX.GetWoodBounceSound();
            float vol = Mathf.Clamp(impactSpeed * 0.25f, 0.15f, 0.8f);
            sfxSource.PlayOneShot(clip, vol);
        }

        public static void PlayOutOfBounds()
        {
            EnsureManagerExists();
            EnsureAudioSystemActive();

            AudioClip clip = GolfSoundFX.GetOutOfBoundsSound();
            sfxSource.PlayOneShot(clip, 0.8f);
        }

        public static void PlayVictoryFanfare()
        {
            EnsureManagerExists();
            EnsureAudioSystemActive();

            AudioClip clip = GolfSoundFX.GetVictoryFanfare();
            sfxSource.PlayOneShot(clip, 0.9f);
        }
    }
}
