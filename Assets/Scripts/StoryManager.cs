using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class StoryManager : MonoBehaviour
{
    [Header("Configurações da História")]
    public StoryData storyData;      // O arquivo ScriptableObject com os textos/fotos
    public Image displayImage;       // Componente de Image que mostra a foto da história
    public TextMeshProUGUI displayText; // Componente de Texto (TextMeshPro)
    public float typingSpeed = 0.02f; // Velocidade "metralhada" estilo Hotline Miami
    public string nextSceneName = "Map01";

    [Header("Cyberpunk Transition")]
    public CanvasGroup flashCanvasGroup; // Arraste a imagem "FlashOverlay" (Roxa) aqui
    public float flashInDuration = 0.1f;
    public float flashOutDuration = 0.4f;

    [Header("Componentes de Visibilidade")]
    public CanvasGroup imageCanvasGroup; // Arraste a imagem da história aqui (para o fade suave)

    private AudioSource audioSource;
    private int currentStep = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Coroutine transitionCoroutine;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Busca automática do CanvasGroup na imagem principal se esquecer de arrastar
        if (imageCanvasGroup == null && displayImage != null)
            imageCanvasGroup = displayImage.GetComponent<CanvasGroup>();

        // Garante que o Flash comece invisível e a imagem visível
        if (flashCanvasGroup != null) flashCanvasGroup.alpha = 0f;
        if (imageCanvasGroup != null) imageCanvasGroup.alpha = 1f;

        ShowStep();
    }

    void Update()
    {
        // Avança no clique ou Espaço
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // Completa o texto instantaneamente
                StopCoroutine(typingCoroutine);
                displayText.text = storyData.steps[currentStep].text;
                isTyping = false;
            }
            else if (transitionCoroutine == null) // Só avança se não estiver no meio de um flash
            {
                AdvanceStory();
            }
        }
    }

    void AdvanceStory()
    {
        currentStep++;

        if (currentStep < storyData.steps.Length)
        {
            // Se a imagem mudar, faz o flash neon. Se for a mesma, só troca o texto.
            if (storyData.steps[currentStep].image != storyData.steps[currentStep - 1].image)
            {
                transitionCoroutine = StartCoroutine(NeonFlashTransition());
            }
            else
            {
                ShowStep();
            }
        }
        else
        {
            // Fim da história, vai para o jogo
            SceneManager.LoadScene(nextSceneName);
        }
    }

    IEnumerator NeonFlashTransition()
    {
        // 1. FLASH IN (Brilho Roxo)
        float time = 0;
        while (time < flashInDuration)
        {
            time += Time.deltaTime;
            flashCanvasGroup.alpha = Mathf.Lerp(0f, 1f, time / flashInDuration);
            yield return null;
        }

        // 2. TROCA OS DADOS (Enquanto está tudo roxo)
        ShowStep();
        yield return new WaitForSeconds(0.05f);

        // 3. FLASH OUT (Revela a nova imagem com zoom suave)
        time = 0;
        while (time < flashOutDuration)
        {
            time += Time.deltaTime;
            float t = time / flashOutDuration;
            flashCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            
            // Efeito de leve zoom in para dar impacto
            displayImage.transform.localScale = Vector3.Lerp(new Vector3(1.05f, 1.05f, 1f), Vector3.one, t);
            
            yield return null;
        }

        transitionCoroutine = null;
    }

    void ShowStep()
    {
        displayImage.sprite = storyData.steps[currentStep].image;
        
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(storyData.steps[currentStep].text));
    }

    IEnumerator TypeSentence(string sentence)
    {
        displayText.text = "";
        isTyping = true;

        foreach (char letter in sentence.ToCharArray())
        {
            displayText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }
}