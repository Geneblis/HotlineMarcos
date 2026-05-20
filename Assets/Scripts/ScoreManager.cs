using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Configurações")]
    [SerializeField] float streakTimeout = 3.5f;
    [SerializeField] float bonusTimePerKill = 5f; // segundos adicionados ao timer

    [Header("Referências da HUD")]
    [SerializeField] private TextMeshProUGUI scoreDisplayText;
    [SerializeField] private TextMeshProUGUI comboDisplayText;

    private int totalScore = 0;
    private int streakCount = 0;
    private float lastKillTime;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        UpdateUI();
    }

    void Update()
    {
        if (streakCount > 0 && Time.time - lastKillTime > streakTimeout)
            ResetStreak();
    }

    public void AddKill(int basePoints)
    {
        lastKillTime = Time.time;
        streakCount++;

        float multiplier = 1f + (streakCount - 1) * 0.5f;
        int pointsToGive = Mathf.RoundToInt(basePoints * multiplier);
        totalScore += pointsToGive;

        // ── Integração com GameManager ──────────────────────────────
        GameManager.Instance?.AddPoints(pointsToGive); // atualiza HUD de pontos
        GameManager.Instance?.AddTime(bonusTimePerKill); // +5s no timer
        // ────────────────────────────────────────────────────────────

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
            scoreDisplayText.text = $"PONTOS: {totalScore}";

        if (comboDisplayText != null)
        {
            if (streakCount > 1)
            {
                float multiplier = 1f + (streakCount - 1) * 0.5f;
                comboDisplayText.text = $"{streakCount}x COMBO!  {multiplier:0.#}x";
                comboDisplayText.gameObject.SetActive(true);
            }
            else
            {
                comboDisplayText.gameObject.SetActive(false);
            }
        }
    }
}