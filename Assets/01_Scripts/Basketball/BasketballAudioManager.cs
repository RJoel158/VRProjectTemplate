using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Basketball.Audio
{
    public class BasketballAudioManager : MonoBehaviour
    {
        private static BasketballAudioManager instance;
        private static AudioSource bgmAudioSource;
        private static AudioClip clipBgm;

        public static BasketballAudioManager Instance
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
            bool isBasket = scene.name.IndexOf("Basket", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isBasket)
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
            if (activeScene.name.IndexOf("Basket", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            instance = FindAnyObjectByType<BasketballAudioManager>();
            if (instance == null)
            {
                GameObject host = new GameObject("[BasketballAudioManager]");
                instance = host.AddComponent<BasketballAudioManager>();
                DontDestroyOnLoad(host);
            }

            if (bgmAudioSource == null)
            {
                bgmAudioSource = instance.gameObject.AddComponent<AudioSource>();
                bgmAudioSource.spatialBlend = 0f; // 2D Direct Stereo
                bgmAudioSource.loop = true;
                bgmAudioSource.volume = 0.35f;
                bgmAudioSource.playOnAwake = false;
                bgmAudioSource.bypassEffects = true;
                bgmAudioSource.bypassListenerEffects = true;
                bgmAudioSource.bypassReverbZones = true;
            }

            LoadDefaultClips();
        }

        private static void LoadDefaultClips()
        {
#if UNITY_EDITOR
            if (clipBgm == null)
            {
                clipBgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/03_Resources/Sounds/Nintendo DSi Shop Theme (High Quality_ 2019 Remastered)(MP3_160K).mp3");
            }
#endif
        }

        public static void EnsureAudioSystemActive()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.audioMasterMute)
            {
                UnityEditor.EditorUtility.audioMasterMute = false;
            }
#endif
            AudioListener.pause = false;
        }

        public static void PlayBackgroundMusic(AudioClip clip = null, float volume = 0.35f)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name.IndexOf("Basket", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            EnsureManagerExists();
            if (bgmAudioSource == null) return;

            if (clip != null) clipBgm = clip;
            if (clipBgm == null) LoadDefaultClips();

            if (clipBgm != null)
            {
                if (bgmAudioSource.clip != clipBgm)
                {
                    bgmAudioSource.clip = clipBgm;
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
    }
}
