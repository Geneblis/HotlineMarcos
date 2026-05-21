using UnityEngine;
using UnityEngine.SceneManagement;

public class Credits : MonoBehaviour
{
    [Header("Configurações do Movimento")]
    [SerializeField] private float scrollSpeed = 40f; // Velocidade da subida do texto
    [SerializeField] private float exitPositionY = 1200f; // Ponto no eixo Y onde o texto some e o jogo volta pro menu

    [Header("Configurações de Cena")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private RectTransform textRectTransform;

    void Start()
    {
        // Tenta pegar o RectTransform do próprio GameObject
        textRectTransform = GetComponent<RectTransform>();

        // Se não encontrou, tenta encontrar em um componente filho
        if (textRectTransform == null)
        {
            textRectTransform = GetComponentInChildren<RectTransform>();
        }

        // Se ainda não encontrou, loga um erro
        if (textRectTransform == null)
        {
            enabled = false; // Desabilita o script para evitar erros
            return;
        }

        // Se você quiser garantir que ele SEMPRE comece embaixo, 
        // descomente a linha abaixo e mude o valor (ex: -800f)
        // textRectTransform.anchoredPosition = new Vector2(textRectTransform.anchoredPosition.x, -800f);
    }

    void Update()
    {
        // Se por algum motivo o textRectTransform for null, retorna
        if (textRectTransform == null) return;

        // Move o texto para cima baseado no tempo (DeltaTime garante que rode igual em qualquer PC)
        textRectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

        // Se o texto passar da posição limite (sumiu da tela por cima), volta pro menu sozinho
        if (textRectTransform.anchoredPosition.y >= exitPositionY)
        {
            VoltarParaOMenu();
        }

        // Atalho clássico: se o jogador apertar ESC ou Espaço, pula os créditos e volta pro menu
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
        {
            VoltarParaOMenu();
        }
    }

    // Mantido público para caso você ainda queira usar um botão físico de "Voltar" na tela
    public void VoltarParaOMenu()
    {
        Debug.Log("Créditos finalizados. Carregando o Menu Principal...");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}