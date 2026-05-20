using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Necessário para a mensagem na tela

public class LevelExit : MonoBehaviour
{
    [Header("Configurações de Cena")]
    [SerializeField] private string endingSceneName = "EndingScene"; // Corrigido para o nome da sua cena final

    [Header("Configurações de UI")]
    [SerializeField] private TMP_Text alertText; // Arraste o texto da HUD para cá
    [SerializeField] private string clearMessage = "Vá pegar o seu prêmio!";

    [Header("Configurações do Player")]
    [SerializeField] private string playerTag = "Player"; // Garanta que seu Player tem essa Tag
    [SerializeField] private string enemyTag = "Enemy";   // Garanta que seus inimigos têm essa Tag

    private bool levelCleared = false;

    void Start()
    {
        // Garante que a mensagem comece escondida
        if (alertText != null)
        {
            alertText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Se a fase já foi limpa, não precisa continuar procurando inimigos
        if (levelCleared) return;

        // Procura na cena se ainda existe algum objeto com a tag de inimigo
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
        Debug.Log("Todos os inimigos foram eliminados!");

        // PAUSA APENAS O TIMER DA PARTIDA (O relógio da morte congela na HUD)
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
        // Só avança se quem encostou foi o Player E se todos os inimigos já morreram
        if (other.CompareTag(playerTag) && levelCleared)
        {
            Debug.Log("Carregando os painéis finais...");
            
            // Carrega a cena com os painéis de história finais
            SceneManager.LoadScene(endingSceneName);
        }
        else if (other.CompareTag(playerTag) && !levelCleared)
        {
            Debug.Log("Você não pode sair ainda! Ainda existem inimigos vivos.");
        }
    }
}