using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioClip backgroundMusic;
    [SerializeField] bool playOnStart = true;
    [SerializeField] bool loopMusic = true;
    [SerializeField] float musicVolume = 1f;

    void Awake()
    {
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
        if (playOnStart)
            PlayBackgroundMusic();
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