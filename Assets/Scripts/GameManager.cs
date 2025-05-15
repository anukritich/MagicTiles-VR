using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI timerText;
    public GameObject startPanel;
    public GameObject gameOverPanel;
    public Button startButton;
    public Button restartButton;
    public TextMeshProUGUI secondaryMessageText; // For the second canvas
    public GameObject levelCompleteCanvas;       // Canvas to show on level complete


    [Header("Game Reference")]
    public TileManager tileManager;

    [Header("Message Settings")]
    public float messageDisplayTime = 4f;

    [Header("Timer Settings")]
    public float baseTimeLimit = 10f;  // Base time for level 1
    public float additionalTimePerTile = 1f;  // Additional time per tile in the pattern
    public float timeReductionPerLevel = 0.2f; // Time reduction per level increase
    public float minTimePerTile = 0.8f;  // Minimum time per tile

    // Private variables
    private float currentTimeLimit;
    private float currentTimer;
    private bool timerActive = false;
    private Coroutine timerCoroutine;

    private void Start()
    {
        // Setup event listeners
        if (tileManager != null)
        {
            tileManager.OnPatternStart.AddListener(OnPatternStart);
            tileManager.OnPatternComplete.AddListener(OnPatternComplete);
            tileManager.OnPlayerSuccess.AddListener(OnPlayerSuccess);
            tileManager.OnPlayerFail.AddListener(OnPlayerFail);
            if (levelCompleteCanvas != null)
                levelCompleteCanvas.SetActive(false);

        }

        // Setup button listeners
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);
        if (restartButton != null)
            restartButton.onClick.AddListener(StartGame);

        // Show initial UI
        ShowStartPanel(true);
        ShowGameOverPanel(false);
        HideTimer();
        UpdateUI();
    }

    public void StartGame()
    {
        ShowStartPanel(false);
        ShowGameOverPanel(false);
        HideTimer();

        if (tileManager != null)
            tileManager.StartGame();

        UpdateUI();
        ShowMessage("Game Started! Watch the pattern...");
    }

    private void OnPatternStart()
    {
        // Pattern is being shown to player - make sure timer is stopped
        StopTimer();
        HideTimer();
        ShowMessage("Watch carefully...");
        UpdateUI();
    }

    private void OnPatternComplete()
    {
        // Pattern is complete, now it's player's turn
        ShowMessage("Your turn! Repeat the pattern");

        // Delay starting timer to give player time to read instructions
        StartCoroutine(DelayedTimerStart(2.0f));
    }

    private IEnumerator DelayedTimerStart(float delay)
    {
        // Wait for the specified delay time before starting the timer
        yield return new WaitForSeconds(delay);

        // Now it's officially player's turn - start the timer
        StartTimer();
    }

    private void OnPlayerSuccess()
    {
        StopTimer();
        HideTimer();
        ShowMessage($"Level {tileManager.CurrentLevel - 1} complete!");
        StartCoroutine(ShowLevelCompleteSequence());
        UpdateUI();
        if (levelCompleteCanvas != null)
            levelCompleteCanvas.SetActive(true);

    }
    private IEnumerator ShowLevelCompleteSequence()
    {
        ShowMessage($"Level {tileManager.CurrentLevel - 1} complete!");
        yield return new WaitForSeconds(messageDisplayTime); // Wait for "Level complete!" message to fade

        PromptNextLevel();
    }

    private void OnPlayerFail()
    {
        StopTimer();
        HideTimer();
        ShowMessage("Wrong tile! Game Over");
        ShowGameOverPanel(true);
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {tileManager.CurrentScore}";
        if (levelText != null)
            levelText.text = $"Level: {tileManager.CurrentLevel}";
    }

    //private void ShowMessage(string message)
    //{
     //   if (messageText != null)
     //   {
      //      messageText.text = message;
       //     messageText.gameObject.SetActive(true);

        //    // For "Your turn!" message, don't auto-hide
         //   if (!message.Contains("Your turn!"))
        //    {
         //       CancelInvoke("HideMessage");
         //       Invoke("HideMessage", messageDisplayTime);
          //  }
       // }
   // }
    private void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
        }

        if (secondaryMessageText != null)
        {
            secondaryMessageText.text = message;
            secondaryMessageText.gameObject.SetActive(true);
        }

        // For "Your turn!" message, don't auto-hide
        if (!message.Contains("Your turn!"))
        {
            CancelInvoke("HideMessage");
            Invoke("HideMessage", messageDisplayTime);
        }
    }

    public void PromptNextLevel()
    {
        ShowMessage("Enter the area to start next level");
    }

    private void HideMessage()
    {
        if (messageText != null)
            messageText.gameObject.SetActive(false);
        if (secondaryMessageText != null)
            secondaryMessageText.gameObject.SetActive(false);
    }


    private void ShowStartPanel(bool show)
    {
        if (startPanel != null)
            startPanel.SetActive(show);
    }

    private void ShowGameOverPanel(bool show)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(show);
    }

    // Timer functionality
    public void StartTimer()
    {
        // Calculate time limit based on level and pattern length
        int currentPatternLength = tileManager.CurrentPatternLength;
        int currentLevel = tileManager.CurrentLevel;

        float timePerTile = Mathf.Max(
            baseTimeLimit / 3f - (currentLevel - 1) * timeReductionPerLevel,
            minTimePerTile
        );

        currentTimeLimit = timePerTile * currentPatternLength;
        currentTimer = currentTimeLimit;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        timerCoroutine = StartCoroutine(RunTimer());
        timerActive = true;

        // Hide the instruction message when timer starts
        HideMessage();

        ShowTimer();
        UpdateTimerText();
    }

    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        timerActive = false;
    }

    private IEnumerator RunTimer()
    {
        while (currentTimer > 0)
        {
            yield return null; // Wait for next frame
            currentTimer -= Time.deltaTime;
            UpdateTimerText();
        }

        // Time's up!
        currentTimer = 0;
        UpdateTimerText();
        timerActive = false;

        // Notify the player they ran out of time
        ShowMessage("Time's up! Game Over");

        // Simulate a failure in the tile manager
        if (tileManager != null)
        {
            // Check if TileManager has a HandleTimeExpired method
            if (tileManager.GetType().GetMethod("HandleTimeExpired") != null)
            {
                tileManager.SendMessage("HandleTimeExpired", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                // Otherwise just trigger the fail event
                tileManager.OnPlayerFail?.Invoke();
            }
        }

        ShowGameOverPanel(true);
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {currentTimer:F1}s";

            // Change color based on remaining time
            if (currentTimer < currentTimeLimit * 0.25f)
            {
                timerText.color = Color.red;
            }
            else if (currentTimer < currentTimeLimit * 0.5f)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }

    private void ShowTimer()
    {
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }
    }

    private void HideTimer()
    {
        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
    }
    public float currentLevelTime = 60f;

    public void AddTime(float seconds)
    {
        //currentLevelTime += seconds;
            currentTimer += seconds;

            // Optional: Clamp to not exceed max time limit
            currentTimer = Mathf.Min(currentTimer, currentTimeLimit);

            Debug.Log($"{seconds} seconds added! New timer: {currentTimer}");

            UpdateTimerText();
    }

}