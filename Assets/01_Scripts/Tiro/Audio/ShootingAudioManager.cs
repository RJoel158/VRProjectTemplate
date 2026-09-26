using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tiro.Core;

namespace Tiro.Audio
{
    /// <summary>
    /// Gestor central de audio para el polígono de tiro olímpico.
    /// Garantiza la existencia y activación del AudioListener, desactiva el mute de Unity Editor,
    /// y reproduce los efectos (disparos, recargas tácticas, dry fire, impactos y platos)
    /// mediante un AudioSource 2D puro sin atenuación de distancia (stereo directo a los oídos).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class ShootingAudioManager : MonoBehaviour
    {
        private static ShootingAudioManager instance;
        public static ShootingAudioManager Instance
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

        private static AudioSource audioSource2D;
        private static AudioSource bgmAudioSource;
        private static AudioListener verifiedListener;

        private static AudioClip clipShootPop;
        private static AudioClip clipReloadRack;
        private static AudioClip clipWiiClick;
        private static AudioClip clipBgmTheme;
        private static AudioClip clipKnockdown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeSceneAudioWatcher()
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
            bool isShooting = scene.name.IndexOf("Shooting", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isShooting)
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

        public static void EnsureManagerExists()
        {
            if (instance != null) return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name.IndexOf("Shooting", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            instance = FindAnyObjectByType<ShootingAudioManager>();
            if (instance == null)
            {
                GameObject go = new GameObject("[ShootingAudioManager]");
                instance = go.AddComponent<ShootingAudioManager>();
            }

            instance.SetupAudioSource();
            LoadDefaultClips();
            EnsureAudioSystemActive();
            PlayBackgroundMusic();
        }

        private void Awake()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name.IndexOf("Shooting", StringComparison.OrdinalIgnoreCase) < 0)
            {
                Destroy(gameObject);
                return;
            }

            if (instance == null)
            {
                instance = this;
                SetupAudioSource();
                LoadDefaultClips();
                EnsureAudioSystemActive();
                PlayBackgroundMusic();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            StopBackgroundMusic();
            if (instance == this)
            {
                instance = null;
            }
        }

        private void SetupAudioSource()
        {
            if (audioSource2D == null)
            {
                audioSource2D = GetComponent<AudioSource>();
                if (audioSource2D == null)
                {
                    audioSource2D = gameObject.AddComponent<AudioSource>();
                }

                audioSource2D.spatialBlend = 0f; // 100% 2D estéreo directo
                audioSource2D.volume = 1f;
                audioSource2D.playOnAwake = false;
                audioSource2D.loop = false;
                audioSource2D.mute = false;
                audioSource2D.bypassEffects = true;
                audioSource2D.bypassListenerEffects = true;
                audioSource2D.bypassReverbZones = true;
                audioSource2D.ignoreListenerPause = true;
                audioSource2D.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource2D.minDistance = 100f;
                audioSource2D.maxDistance = 1000f;
                audioSource2D.enabled = true;
            }

            if (bgmAudioSource == null)
            {
                var sources = GetComponents<AudioSource>();
                if (sources.Length > 1) bgmAudioSource = sources[1];
                else bgmAudioSource = gameObject.AddComponent<AudioSource>();

                bgmAudioSource.spatialBlend = 0f; // 100% 2D estéreo
                bgmAudioSource.volume = 0.35f; // Volumen ambiente confortable
                bgmAudioSource.loop = true;
                bgmAudioSource.playOnAwake = false;
                bgmAudioSource.bypassEffects = true;
                bgmAudioSource.bypassListenerEffects = true;
                bgmAudioSource.bypassReverbZones = true;
                bgmAudioSource.ignoreListenerPause = true;
                bgmAudioSource.enabled = true;
            }
        }

        private static void LoadDefaultClips()
        {
#if UNITY_EDITOR
            if (clipShootPop == null)
            {
                clipShootPop = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/DemoAssets/Audio/Button Pop.wav");
            }
            if (clipReloadRack == null)
            {
                clipReloadRack = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/VRTemplateAssets/Audio/Button_22_click.wav");
            }
            if (clipWiiClick == null)
            {
                clipWiiClick = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/03_Resources/Sounds/WiiClick.mp3");
            }
            if (clipBgmTheme == null)
            {
                clipBgmTheme = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/03_Resources/Sounds/Nintendo Wii - Mii Channel Theme.mp3");
            }
            if (clipKnockdown == null)
            {
                clipKnockdown = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/03_Resources/Sounds/bowling-one-pin-free-sound-effectsmp3-160k-1_dOlvwnca.mp3");
            }
#endif
            // Fallback procedurale inmediato e infalible
            if (clipShootPop == null) clipShootPop = ShootingSoundFX.GetGunshotRifle();
            if (clipReloadRack == null) clipReloadRack = ShootingSoundFX.GetReloadSound();
            if (clipWiiClick == null) clipWiiClick = ShootingSoundFX.GetDryFireSound();
        }

        public static void PlayBackgroundMusic(AudioClip clip = null, float volume = 0.35f)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name.IndexOf("Shooting", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            EnsureManagerExists();
            if (bgmAudioSource == null) return;

            if (clip != null) clipBgmTheme = clip;
            if (clipBgmTheme == null) LoadDefaultClips();

            if (clipBgmTheme != null)
            {
                if (bgmAudioSource.clip != clipBgmTheme)
                {
                    bgmAudioSource.clip = clipBgmTheme;
                }
                bgmAudioSource.volume = volume;
                bgmAudioSource.loop = true;
                if (!bgmAudioSource.isPlaying)
                {
                    bgmAudioSource.Play();
                }
            }
        }

        public static void StopBackgroundMusic()
        {
            if (bgmAudioSource != null && bgmAudioSource.isPlaying)
            {
                bgmAudioSource.Stop();
            }
        }

        private void Update()
        {
            // Tecla de prueba rápida en PC / Editor: presionar 'T'
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
            {
                Debug.Log("[ShootingAudioManager] Test disparado con tecla 'T'");
                PlayGunshot(ShootingDiscipline.OlympicRifleDistance);
            }
#endif
        }

        public static void EnsureAudioSystemActive()
        {
            // 1. Desmutear el Editor de Unity si el usuario presionó 'Mute Audio' en la barra Game
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.audioMasterMute)
            {
                UnityEditor.EditorUtility.audioMasterMute = false;
                Debug.LogWarning("[ShootingAudioManager] 'Mute Audio' estaba activo en Unity Game View. Se ha desmuteado automaticamente.");
            }
#endif

            // 2. Despausar y garantizar volumen maestro en AudioListener
            AudioListener.pause = false;
            AudioListener.volume = 1f;

            // 3. Localizar o crear AudioListener activo en la cámara principal
            if (verifiedListener == null || !verifiedListener.enabled || !verifiedListener.gameObject.activeInHierarchy)
            {
                Camera cam = Camera.main;
                if (cam == null)
                {
                    var allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                    foreach (var c in allCams)
                    {
                        if (c.name.Contains("Main") || c.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null)
                        {
                            cam = c;
                            break;
                        }
                    }
                    if (cam == null && allCams.Length > 0) cam = allCams[0];
                }

                if (cam != null)
                {
                    verifiedListener = cam.GetComponent<AudioListener>();
                    if (verifiedListener == null)
                    {
                        verifiedListener = cam.gameObject.AddComponent<AudioListener>();
                    }
                    verifiedListener.enabled = true;
                }
                else
                {
                    // Si aún no hay cámara, asegurar listener en el propio gestor
                    if (instance != null)
                    {
                        verifiedListener = instance.GetComponent<AudioListener>();
                        if (verifiedListener == null)
                        {
                            verifiedListener = instance.gameObject.AddComponent<AudioListener>();
                        }
                        verifiedListener.enabled = true;
                    }
                }
            }

            // Asegurar que el AudioSource 2D esté siempre activo
            if (audioSource2D != null)
            {
                if (!audioSource2D.enabled) audioSource2D.enabled = true;
                if (audioSource2D.mute) audioSource2D.mute = false;
                if (audioSource2D.volume < 0.9f) audioSource2D.volume = 1f;
                if (audioSource2D.spatialBlend > 0f) audioSource2D.spatialBlend = 0f;
            }
        }

        /// <summary>
        /// Reproduce cualquier AudioClip de forma estéreo directa (2D) e inmune a distancias o jerarquías desactivadas.
        /// </summary>
        public static void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            EnsureManagerExists();
            EnsureAudioSystemActive();

            if (audioSource2D != null && audioSource2D.enabled)
            {
                audioSource2D.PlayOneShot(clip, volume);
            }
            else
            {
                Camera cam = Camera.main;
                Vector3 pos = cam != null ? cam.transform.position : Vector3.zero;
                AudioSource.PlayClipAtPoint(clip, pos, volume);
            }

            Debug.Log($"[ShootingAudioManager] Sonido reproducido: {clip.name} (Vol: {volume})");
        }

        public static void PlayEasterEggSound()
        {
            AudioClip clip = ShootingSoundFX.GetEasterEggSquawk();
            PlaySound(clip, 1.0f);
        }

        public static void PlayGunshot(ShootingDiscipline discipline = ShootingDiscipline.OlympicRifleDistance, AudioClip customClip = null)
        {
            LoadDefaultClips();
            AudioClip clip = customClip;

            if (clip == null)
            {
                clip = clipShootPop;
            }

            if (clip == null)
            {
                clip = discipline switch
                {
                    ShootingDiscipline.OlympicRifleDistance => ShootingSoundFX.GetGunshotRifle(),
                    ShootingDiscipline.ClayPigeonShotgun => ShootingSoundFX.GetGunshotShotgun(),
                    _ => ShootingSoundFX.GetGunshotPistol()
                };
            }

            PlaySound(clip, 1f);
        }

        public static void PlayReload(AudioClip customClip = null)
        {
            LoadDefaultClips();
            AudioClip clip = customClip != null ? customClip : clipReloadRack;
            if (clip == null) clip = ShootingSoundFX.GetReloadSound();
            PlaySound(clip, 1f);
        }

        public static void PlayDryFire(AudioClip customClip = null)
        {
            LoadDefaultClips();
            AudioClip clip = customClip != null ? customClip : clipWiiClick;
            if (clip == null) clip = ShootingSoundFX.GetDryFireSound();
            PlaySound(clip, 0.9f);
        }

        public static void PlayTargetHit(bool isBullseye)
        {
            AudioClip clip = ShootingSoundFX.GetTargetHitSound(isBullseye);
            PlaySound(clip, isBullseye ? 1f : 0.75f);
        }

        public static void PlayTargetKnockdown()
        {
            LoadDefaultClips();
            if (clipKnockdown != null)
            {
                PlaySound(clipKnockdown, 0.95f);
            }
            else
            {
                PlayTargetHit(true);
            }
        }

        public static void PlayClayShatter()
        {
            AudioClip clip = ShootingSoundFX.GetClayShatterSound();
            PlaySound(clip, 0.9f);
        }

        public static void PlayClayLaunch()
        {
            AudioClip clip = ShootingSoundFX.GetClayLaunchSound();
            PlaySound(clip, 0.5f);
        }
    }
}
