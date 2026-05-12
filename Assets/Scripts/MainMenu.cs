using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Nome da cena que você quer carregar ao iniciar
    [SerializeField] private string gameSceneName = "IntroScene";

    public void StartGame()
    {
        // Carrega a próxima cena
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        Debug.Log("Abrindo Opções...");
        // Futuramente você pode ativar um painel de opções aqui
    }

    public void OpenCredits()
    {
        Debug.Log("Abrindo Créditos...");
    }

    public void QuitGame()
    {
        Debug.Log("Sair do Jogo");
        // Funciona apenas no jogo buildado (executável)
        Application.Quit();
    }
}