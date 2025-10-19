using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;

public class GameEndManager : MonoBehaviour
{
    [Header("End Game UI")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private UnityEngine.UI.Button returnToMenuButton;

    [Header("Settings")]
    [SerializeField] private string menuSceneName = "MainMenu";

    private bool gameEnded = false;

    private void Start()
    {
        // Hide end game panel initially
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(false);
        }

        // Setup button
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
        }
    }

    /// <summary>
    /// Shows victory screen
    /// </summary>
    public void ShowVictory()
    {
        if (gameEnded) return;
        gameEnded = true;

        Debug.Log("[GameEndManager] VICTORY!");

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text = "VICTORY!";
            resultText.color = Color.green;
        }

        // Pause game (optional)
        // Time.timeScale = 0f;

        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Shows defeat screen
    /// </summary>
    public void ShowDefeat()
    {
        if (gameEnded) return;
        gameEnded = true;

        Debug.Log("[GameEndManager] DEFEAT!");

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text = "DEFEAT";
            resultText.color = Color.red;
        }

        // Pause game (optional)
        // Time.timeScale = 0f;

        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Returns to main menu
    /// </summary>
    public void ReturnToMenu()
    {
        Debug.Log("[GameEndManager] Returning to menu...");

        // Unpause game
        Time.timeScale = 1f;

        // Disconnect from Photon
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        // Load menu scene
        SceneManager.LoadScene(menuSceneName);
    }
}
