using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioClip backgroundMusic;
    [SerializeField] bool playOnStart = true;
    [SerializeField] bool loopMusic = true;
    [SerializeField] float musicVolume = 1f;
    [SerializeField] string requiredSceneName = ""; // Cena onde a música deve tocar

    void Awake()
    {
        // Verificar se a cena atual é a configurada
        if (!string.IsNullOrEmpty(requiredSceneName) && SceneManager.GetActiveScene().name != requiredSceneName)
        {
            Destroy(gameObject);
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Registrar listener para mudanças de cena
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (playOnStart)
            PlayBackgroundMusic();
    }

    void OnDestroy()
    {
        // Remover listener para evitar erros
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Parar música se a cena não for a configurada
        if (!string.IsNullOrEmpty(requiredSceneName) && scene.name != requiredSceneName)
        {
            StopMusic();
        }
    }

    public void PlayBackgroundMusic()
    {
        if (musicSource == null || backgroundMusic == null)
            return;

        musicSource.clip = backgroundMusic;
        musicSource.loop = loopMusic;
        musicSource.volume = musicVolume;

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void SetVolume(float volume)
    {
        musicVolume = volume;

        if (musicSource != null)
            musicSource.volume = musicVolume;
    }
}