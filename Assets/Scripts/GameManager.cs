using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum GameState { Playing, Paused, Dead }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("HUD — Points")]
    [SerializeField] TMP_Text pointsText;
    [SerializeField] string pointsSuffix = "";

    [Header("HUD — Ammo")]
    [SerializeField] TMP_Text ammoText;
    [SerializeField] string ammoSeparator = " / ";
    [SerializeField] string ammoEmpty = "---";

    [Header("HUD — Timer")]
    [SerializeField] TMP_Text timerText;
    [SerializeField] bool countDown = false;
    [SerializeField] float timerDuration = 60f;

    [Header("Screens")]
    [SerializeField] GameObject gameOverScreen;
    [SerializeField] TMP_Text gameOverText;
    [SerializeField] string gameOverMessage = "VOCE MORREU!\n\nAperte R para tentar novamente...";
    [SerializeField] GameObject pauseMenuRoot;

    [Header("Main Menu Scene")]
    [SerializeField] string mainMenuScene = "MainMenu";

    [Header("Restart")]
    [SerializeField] KeyCode restartKey = KeyCode.R;

    GameState state = GameState.Playing;
    int currentPoints = 0;
    float elapsedTime = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (gameOverText != null)
            gameOverText.text = gameOverMessage;

        RefreshPointsUI();
        RefreshAmmoUI(ammoEmpty);
        RefreshTimerUI(countDown ? timerDuration : 0f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (state == GameState.Playing)
                Pause();
            else if (state == GameState.Paused)
                Resume();
        }

        if (state == GameState.Dead)
        {
            if (Input.GetKeyDown(restartKey))
                RestartLevel();

            return;
        }

        if (state != GameState.Playing)
            return;

        elapsedTime += Time.deltaTime;

        float display = countDown ? Mathf.Max(0f, timerDuration - elapsedTime) : elapsedTime;
        RefreshTimerUI(display);

        if (countDown && display <= 0f)
        {
            OnTimerEnd();
        }
    }

    public GameState CurrentState => state;

    public void Pause()
    {
        if (state != GameState.Playing)
            return;

        state = GameState.Paused;
        Time.timeScale = 0f;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(true);
    }

    public void Resume()
    {
        if (state != GameState.Paused)
            return;

        state = GameState.Playing;
        Time.timeScale = 1f;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
    }

    public void OnPlayerDied()
    {
        if (state == GameState.Dead)
            return;

        state = GameState.Dead;

        if (gameOverText != null)
            gameOverText.text = gameOverMessage;

        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void AddPoints(int amount)
    {
        if (state != GameState.Playing)
            return;

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

    public void ResetTimer()
    {
        elapsedTime = 0f;
    }

    void RefreshPointsUI()
    {
        if (pointsText == null)
            return;

        pointsText.text = $"{currentPoints}{pointsSuffix}";
    }

    void RefreshAmmoUI(string content)
    {
        if (ammoText == null)
            return;

        ammoText.text = content;
    }

    void RefreshTimerUI(float seconds)
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(seconds / 60f);
        int remainingSeconds = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"{minutes:00}:{remainingSeconds:00}";
    }

    void OnTimerEnd()
    {
        Debug.Log("GameManager: Timer ended.");
        OnPlayerDied();
    }
}