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
        // Pega o componente de UI que controla a posição do texto
        textRectTransform = GetComponent<RectTransform>();

        // Se você quiser garantir que ele SEMPRE comece embaixo, 
        // descomente a linha abaixo e mude o valor (ex: -800f)
        // textRectTransform.anchoredPosition = new Vector2(textRectTransform.anchoredPosition.x, -800f);
    }

    void Update()
    {
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