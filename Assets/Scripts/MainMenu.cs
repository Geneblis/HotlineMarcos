using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Configurações de Cena")]
    [SerializeField] private string gameSceneName = "IntroScene"; // Nome da cena da história

    [Header("Painéis do Menu")]
    public GameObject mainPanel;    // Painel com Iniciar, Opções, Sair
    public GameObject optionsPanel; // Painel com o Slider de Volume e botão Voltar

    [Header("Configurações de Áudio")]
    public Slider volumeSlider;     // Arraste o componente Slider aqui
    public AudioClip clickSound;    // Arraste o arquivo de áudio (.wav/.mp3) aqui
    private AudioSource audioSource;

    void Start()
    {
        // Obtém o AudioSource necessário para tocar o som de clique
        audioSource = GetComponent<AudioSource>();

        // Inicializa o estado dos painéis
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        // Carrega o volume salvo (ou 1.0 como padrão) e aplica ao jogo
        float savedVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
        }
        AudioListener.volume = savedVolume;
    }

    // --- FUNÇÕES DE NAVEGAÇÃO ---

    public void StartGame()
    {
        // Carrega a cena inicial do projeto
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
        // Placeholder para funcionalidade de créditos
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
        // Ajusta o volume global e salva para a próxima sessão
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("GameVolume", volume);
    }

    public void PlayClickSound()
    {
        // Toca o som de forma independente (não é cortado ao fechar painéis)
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}