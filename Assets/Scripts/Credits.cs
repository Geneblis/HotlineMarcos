using UnityEngine;
using UnityEngine.SceneManagement; // IMPORTANTE: Sem isso o Unity não muda de cena

public class Credits : MonoBehaviour
{
    // Tem que ser PUBLIC VOID para o botão conseguir enxergar!
    public void VoltarParaOMenu()
    {
        // Troque "MainMenu" pelo nome EXATO da sua cena de menu principal
        SceneManager.LoadScene("MainMenu");
        Debug.Log("Carregando o Menu Principal...");
    }
}