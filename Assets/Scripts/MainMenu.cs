using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Configurações de Cena")]
    [SerializeField] private string gameSceneName = "IntroScene"; // Nome da cena da história

    [Header("Painéis do Menu")]
    public GameObject mainPanel;         // Painel principal (Iniciar, Opções, Sair)
    public GameObject optionsPanel;      // Painel de Opções (Volume, Voltar)
    public GameObject difficultyPanel;   // NOVO: Painel de Dificuldade (Fácil, Difícil, Voltar)

    [Header("Configurações de Áudio")]
    public Slider volumeSlider;     
    public AudioClip clickSound;    
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Inicializa o estado de todos os painéis
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (difficultyPanel != null) difficultyPanel.SetActive(false); // Começa escondido

        // Carrega o volume salvo e aplica ao jogo
        float savedVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
        }
        AudioListener.volume = savedVolume;
    }

    // --- FUNÇÕES DE NAVEGAÇÃO ---

    public void OpenDifficultySelection()
    {
        // Quando clica em "Iniciar Jogo", esconde o principal e abre a dificuldade
        mainPanel.SetActive(false);
        difficultyPanel.SetActive(true);
    }

    public void CloseDifficultySelection()
    {
        // Botão voltar da tela de dificuldade
        mainPanel.SetActive(true);
        difficultyPanel.SetActive(false);
    }

    public void SelectEasy()
    {
        // Salva a dificuldade como 0 (Fácil) e inicia o jogo
        PlayerPrefs.SetInt("GameDifficulty", 0); 
        Debug.Log("Dificuldade selecionada: FÁCIL");
        StartGame();
    }

    public void SelectHard()
    {
        // Salva a dificuldade como 1 (Difícil) e inicia o jogo
        PlayerPrefs.SetInt("GameDifficulty", 1); 
        Debug.Log("Dificuldade selecionada: DIFÍCIL");
        StartGame();
    }

    private void StartGame()
    {
        // Carrega a cena inicial do projeto (a intro que configuramos)
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        mainPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }

    public void OpenCredits()
    {
        Debug.Log("Abrindo Créditos...");
    }

    public void QuitGame()
    {
        Debug.Log("Saindo do Jogo...");
        Application.Quit();
    }

    // --- FUNÇÕES DE ÁUDIO ---

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("GameVolume", volume);
    }

    public void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}