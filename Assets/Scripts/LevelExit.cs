using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Necessário para a mensagem na tela

public class LevelExit : MonoBehaviour
{
    [Header("Configurações de Cena")]
    [SerializeField] private string endingSceneName = "EndingScene"; // Nome exato da sua cena final

    [Header("Configurações de UI")]
    [SerializeField] private TMP_Text alertText; // Texto da HUD que diz "Vá pegar o seu prêmio!"
    [SerializeField] private string clearMessage = "Vá pegar o seu prêmio!";

    [Header("Configurações do Player")]
    [SerializeField] private string playerTag = "Player"; // Tag padrão do jogador
    [SerializeField] private string enemyTag = "Enemy";   // Tag dos seus inimigos

    private bool levelCleared = false;

    void Start()
    {
        // Garante que a mensagem comece escondida no início da fase
        if (alertText != null)
        {
            alertText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Se já matou todo mundo, não precisa continuar procurando por inimigos
        if (levelCleared) return;

        // Procura na cena se ainda existe algum objeto com a tag "Enemy"
        GameObject[] remainingEnemies = GameObject.FindGameObjectsWithTag(enemyTag);

        // Se a lista estiver vazia, significa que todos morreram!
        if (remainingEnemies.Length == 0)
        {
            TriggerLevelClear();
        }
    }

    private void TriggerLevelClear()
    {
        levelCleared = true;
        Debug.Log("LevelExit: Todos os inimigos foram eliminados!");

        // PAUSA APENAS O TIMER DA PARTIDA (Chama a função nova do GameManager)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseMatchTimer(); 
        }

        // Mostra a mensagem na tela para o jogador
        if (alertText != null)
        {
            alertText.text = clearMessage;
            alertText.gameObject.SetActive(true);
        }
    }

    // Detecta quando o Player encosta no Box Collider 2D (com Is Trigger ativado)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Print de segurança para verificar a física no Console
        Debug.Log("LevelExit: Algo encostou na moto -> " + other.name + " (Tag: " + other.tag + ")");

        // Só avança se quem encostou foi o Player E se todos os inimigos já morreram
        if (other.CompareTag(playerTag))
        {
            if (levelCleared)
            {
                Debug.Log("LevelExit: Subindo na moto e carregando a cena: " + endingSceneName);
                SceneManager.LoadScene(endingSceneName);
            }
            else
            {
                Debug.Log("LevelExit: Você tentou usar a moto, mas ainda há inimigos vivos!");
            }
        }
    }
}