using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gestor principal del menú de inicio (Wii Sports Style).
/// Administra la selección de minijuegos, la música de fondo continua (Wii Menu Music),
/// los efectos de sonido de clic y hover en todos los botones del menú, y asegura
/// que el sistema de audio (AudioListener y AudioSources 2D) esté siempre activo y operativo.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [Header("Datos")]
    public MinigameData selectedMinigame;

    [Header("Audio SFX")]
    [Tooltip("AudioSource para efectos de sonido (clic). Si está vacío se asigna automáticamente.")]
    public AudioSource audioSource;
    [Tooltip("Efecto de sonido de clic (Wii Click).")]
    public AudioClip clickSound;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Audio Hover (Opcional)")]
    [Tooltip("Efecto de sonido al pasar el cursor o puntero sobre un botón.")]
    public AudioClip hoverSound;
    [Range(0f, 1f)] public float hoverVolume = 0.35f;

    [Header("Música de Fondo")]
    [Tooltip("AudioSource dedicado para la música de fondo en loop.")]
    public AudioSource musicSource;
    [Tooltip("Música ambiental del menú (Wii Menu Theme).")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.45f;
    public bool playMusicOnStart = true;

    [Header("Pantalla de carga")]
    public LoadingScreenManager loadingScreen;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        EnsureAudioSystem();
    }

    private void Start()
    {
        EnsureAudioSystem();
        StartBackgroundMusic();
        HookAllButtonSounds();
    }

    /// <summary>
    /// Garantiza la existencia de un AudioListener activo en la escena, y configura los AudioSources
    /// en modo 2D estéreo puro para que se escuchen siempre con máxima claridad sin importar la posición del jugador.
    /// </summary>
    public void EnsureAudioSystem()
    {
        // 1. Desmutear el editor si el botón 'Mute Audio' en la barra de Game estaba encendido
#if UNITY_EDITOR
        if (UnityEditor.EditorUtility.audioMasterMute)
        {
            UnityEditor.EditorUtility.audioMasterMute = false;
            Debug.Log("[MainMenuManager] 🔊 Editor desmuteado automáticamente.");
        }
#endif

        // 2. Garantizar volumen global y despausar el AudioListener
        AudioListener.pause = false;
        AudioListener.volume = 1f;

        // 3. Localizar o crear el AudioListener en la cámara activa o en este objeto
        var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners == null || listeners.Length == 0)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (allCams.Length > 0) cam = allCams[0];
            }

            if (cam != null)
            {
                cam.gameObject.AddComponent<AudioListener>();
                Debug.Log($"[MainMenuManager] 🔊 AudioListener añadido a la cámara {cam.name}.");
            }
            else
            {
                gameObject.AddComponent<AudioListener>();
                Debug.Log("[MainMenuManager] 🔊 AudioListener añadido a MainMenuManager.");
            }
        }
        else
        {
            // Asegurar que el primer listener esté habilitado
            listeners[0].enabled = true;
        }

        // 4. Configurar AudioSource para SFX (2D estéreo, sin atenuación por distancia)
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0f; // 2D estéreo directo
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = sfxVolume;
        audioSource.mute = false;
        audioSource.bypassEffects = true;
        audioSource.bypassListenerEffects = true;
        audioSource.bypassReverbZones = true;

        // 5. Configurar AudioSource para Música (2D estéreo independiente en loop)
        if (musicSource == null)
        {
            var sources = GetComponents<AudioSource>();
            if (sources.Length > 1)
            {
                musicSource = sources[1];
            }
            else
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
        }
        musicSource.spatialBlend = 0f; // 2D estéreo directo
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.mute = false;
        musicSource.bypassEffects = true;
        musicSource.bypassListenerEffects = true;
        musicSource.bypassReverbZones = true;

        // 6. Cargar sonidos predeterminados si faltasen
        LoadDefaultClips();
    }

    private void LoadDefaultClips()
    {
#if UNITY_EDITOR
        if (clickSound == null)
        {
            clickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/03_Resources/Sounds/WiiClick.mp3");
        }

        if (backgroundMusic == null)
        {
            backgroundMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/03_Resources/Sounds/Wii Menu Music 4K(MP3_160K).mp3");
            if (backgroundMusic == null)
            {
                backgroundMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/03_Resources/Sounds/Wii Music Theme (From _Wii Music_)(MP3_160K) 1.mp3");
            }
        }

        if (hoverSound == null)
        {
            hoverSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/VRTemplateAssets/Audio/Button_14_hover.wav");
        }
#endif
    }

    public void StartBackgroundMusic()
    {
        if (!playMusicOnStart) return;

        if (musicSource != null && backgroundMusic != null)
        {
            if (musicSource.clip != backgroundMusic || !musicSource.isPlaying)
            {
                musicSource.clip = backgroundMusic;
                musicSource.volume = musicVolume;
                musicSource.loop = true;
                musicSource.Play();
                Debug.Log("[MainMenuManager] 🎵 Música de fondo iniciada.");
            }
        }
    }

    /// <summary>
    /// Conecta de manera automática el sonido de clic a todos los botones del Canvas.
    /// </summary>
    public void HookAllButtonSounds()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            btn.onClick.RemoveListener(PlayClickSound);
            btn.onClick.AddListener(PlayClickSound);
        }
    }

    /// <summary>
    /// Reproduce el sonido de clic de Wii de forma nítida.
    /// </summary>
    public void PlayClickSound()
    {
        EnsureAudioSystem();
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound, sfxVolume);
        }
    }

    /// <summary>
    /// Reproduce el sonido de hover al apuntar a un botón.
    /// </summary>
    public void PlayHoverSound()
    {
        if (audioSource != null && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound, hoverVolume);
        }
    }

    /// <summary>
    /// Selecciona el minijuego correspondiente y transiciona a su escena de manera limpia.
    /// </summary>
    public void SelectMinigame(MinigameData minigame)
    {
        if (minigame == null) return;
        selectedMinigame = minigame;

        if (isTransitioning) return;
        isTransitioning = true;

        PlayClickSound();

        if (loadingScreen != null)
        {
            loadingScreen.LoadSceneWithAnimation(minigame.sceneName, minigame.icon);
        }
        else
        {
            StartCoroutine(LoadSceneWithFade(minigame.sceneName));
        }
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        float duration = 0.25f;
        float elapsed = 0f;
        float startVol = musicSource != null ? musicSource.volume : 0f;

        // Breve fade out de la música para permitir que el clic se escuche completo y la transición sea suave
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (musicSource != null)
            {
                musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            }
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }
}