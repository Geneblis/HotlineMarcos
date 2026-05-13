using UnityEngine;
using TMPro; // Se estiver usando TextMeshPro

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Configurações")]
    [SerializeField] float streakTimeout = 3.5f;

    [Header("Referências da HUD")]
    [SerializeField] private TextMeshProUGUI scoreDisplayText;
    [SerializeField] private TextMeshProUGUI comboDisplayText;

    private int totalScore = 0;
    private int streakCount = 0;
    private float lastKillTime;

    void Awake() 
    {
        Instance = this;
        UpdateUI();
    }

    void Update()
    {
        // Reseta o multiplicador se passar muito tempo
        if (streakCount > 0 && Time.time - lastKillTime > streakTimeout)
        {
            ResetStreak();
        }
    }

    public void AddKill(int basePoints)
    {
        lastKillTime = Time.time;
        streakCount++;

        // Lógica de Multiplicador: 1x, 1.5x, 2x, etc.
        float multiplier = 1f + (streakCount - 1) * 0.5f;
        int pointsToGive = Mathf.RoundToInt(basePoints * multiplier);

        totalScore += pointsToGive;
        UpdateUI();
    }

    void ResetStreak()
    {
        streakCount = 0;
        if (comboDisplayText != null)
        {
            comboDisplayText.text = "";
            comboDisplayText.gameObject.SetActive(false);
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreDisplayText != null)
        {
            scoreDisplayText.text = $"PONTOS: {totalScore}";
        }

        if (streakCount > 1)
        {
            if (comboDisplayText != null)
            {
                comboDisplayText.text = $"{streakCount}x COMBO!";
                comboDisplayText.gameObject.SetActive(true);
            }
        }
        else if (comboDisplayText != null)
        {
            comboDisplayText.gameObject.SetActive(false);
        }
    }
}