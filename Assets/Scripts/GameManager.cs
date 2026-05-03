using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Header("HUD — Pontuação")]
    [SerializeField] TMP_Text pointsText;
    [SerializeField] string   pointsSuffix = "";

    int currentPoints = 0;

    [Header("HUD — Munição")]
    [SerializeField] TMP_Text ammoText;
    [SerializeField] string   ammoSeparator = " / ";
    [SerializeField] string   ammoEmpty     = "---";

    [Header("HUD — Timer")]
    [SerializeField] TMP_Text timerText;
    [SerializeField] bool     countDown     = false;
    [SerializeField] float    timerDuration = 60f;
    [SerializeField] bool     timerRunning  = true;

    float elapsedTime = 0f;

    void Start()
    {
        RefreshPointsUI();
        RefreshAmmoUI(ammoEmpty);
        RefreshTimerUI(countDown ? timerDuration : 0f);
    }

    void Update()
    {
        if (!timerRunning) return;

        elapsedTime += Time.deltaTime;

        float display = countDown
            ? Mathf.Max(0f, timerDuration - elapsedTime)
            : elapsedTime;

        RefreshTimerUI(display);

        if (countDown && display <= 0f)
        {
            timerRunning = false;
            OnTimerEnd();
        }
    }

    public void AddPoints(int amount)
    {
        currentPoints += amount;
        RefreshPointsUI();
    }

    public void ResetPoints()
    {
        currentPoints = 0;
        RefreshPointsUI();
    }

    public void UpdateAmmo(int current, int max)
    {
        RefreshAmmoUI($"{current}{ammoSeparator}{max}");
    }

    public void ClearAmmo()
    {
        RefreshAmmoUI(ammoEmpty);
    }

    public void PauseTimer()  => timerRunning = false;
    public void ResumeTimer() => timerRunning = true;
    public void ResetTimer()  { elapsedTime = 0f; timerRunning = true; }

    void RefreshPointsUI()
    {
        if (pointsText == null) return;
        pointsText.text = $"{currentPoints}{pointsSuffix}";
    }

    void RefreshAmmoUI(string content)
    {
        if (ammoText == null) return;
        ammoText.text = content;
    }

    void RefreshTimerUI(float seconds)
    {
        if (timerText == null) return;
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"{m:00}:{s:00}";
    }

    void OnTimerEnd()
    {
        Debug.Log("GameManager: Timer encerrado.");
    }
}
