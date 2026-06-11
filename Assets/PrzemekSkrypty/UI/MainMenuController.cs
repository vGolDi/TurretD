using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ElementumDefense.UI;


namespace ElementumDefense.UI
{
public class MainMenuController : MonoBehaviour
{
    [Header("Panel Tokens (dummy GameObjects for ShowPanel routing)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject deckbuilderPanel;
    [SerializeField] private GameObject lootboxPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject battlePassPanel;

    [Header("UI Toolkit Panels")]
    [SerializeField] private ArtDecoTarotOverlay artDecoOverlay;
    [SerializeField] private DeckbuilderUI deckbuilderUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private LootboxUI lootboxUI;
    [SerializeField] private MultiplayerUI multiplayerUI;
    [SerializeField] private SettingsUI settingsUI;
    [SerializeField] private CreditsUI creditsUI;
    [SerializeField] private ProfileUI profileUI;
    [SerializeField] private ElementumDefense.BattlePass.BattlePassUI battlePassUI;

    [Header("Main Menu Buttons (UGUI � legacy)")]
    [SerializeField] private Button multiPlayerButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button deckbuilderButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button lootboxButton;
    [SerializeField] private Button shopButton;

    [Header("Version")]
    [SerializeField] private string gameVersion = "0.1.0 Alpha";

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip menuMusic;

    private AudioSource audioSource;

    private void Start()
    {
        InitializeMenu();
        PlayMenuMusic();
    }

    private void InitializeMenu()
    {
        ShowPanel(mainMenuPanel);

        if (multiPlayerButton != null)
            multiPlayerButton.onClick.AddListener(() =>
            { PlayButtonSound(); OpenMultiplayer(); });

        if (deckbuilderButton != null)
            deckbuilderButton.onClick.AddListener(() =>
            { PlayButtonSound(); OpenDeckbuilder(); });

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() =>
            { PlayButtonSound(); OpenSettings(); });

        if (creditsButton != null)
            creditsButton.onClick.AddListener(() =>
            { PlayButtonSound(); OpenCredits(); });

        if (quitButton != null)
            quitButton.onClick.AddListener(() =>
            { PlayButtonSound(); QuitGame(); });

        if (lootboxButton != null)
            lootboxButton.onClick.AddListener(() =>
            { PlayButtonSound(); OpenLootboxMenu(); });

        if (shopButton != null)
            shopButton.onClick.AddListener(() =>
            { PlayButtonSound(); OpenShop(); });

        audioSource = gameObject.AddComponent<AudioSource>();

        Debug.Log(
            $"[MainMenu] Initialized - v{gameVersion}");
    }

    #region Panel Management

    public void OpenMultiplayer()
    {
        ShowPanel(multiplayerPanel);
        Debug.Log("[MainMenu] Opened multiplayer");
    }

    public void OpenDeckbuilder()
    {
        ShowPanel(deckbuilderPanel);
        Debug.Log("[MainMenu] Opened deckbuilder");
    }

    public void OpenSettings()
    {
        ShowPanel(settingsPanel);
        Debug.Log("[MainMenu] Opened settings");
    }

    public void OpenCredits()
    {
        ShowPanel(creditsPanel);
        Debug.Log("[MainMenu] Opened credits");
    }

    public void OpenLootboxMenu()
    {
        ShowPanel(lootboxPanel);
        Debug.Log("[MainMenu] Opened lootbox");
    }

    public void OpenShop()
    {
        ShowPanel(shopPanel);
        Debug.Log("[MainMenu] Opened shop");
    }
    public void OpenProfile()
    {
        ShowPanel(profilePanel);
        Debug.Log("[MainMenu] Opened profile");
    }
    public void OpenBattlePass()
    {
        ShowPanel(battlePassPanel);
        Debug.Log("[MainMenu] Opened battle pass");
    }
    public void BackToMainMenu()
    {
        ShowPanel(mainMenuPanel);
    }

    private void ShowPanel(GameObject panel)
    {
        // Stary UGUI main menu panel
        mainMenuPanel?.SetActive(
            panel == mainMenuPanel);

        // WA�NE: NIE r�b SetActive na dummy tokenach!
        // Tylko por�wnuj referencje.

        // UI Toolkit: Art Deco main overlay
        if (artDecoOverlay != null)
        {
            if (panel == mainMenuPanel)
                artDecoOverlay.Show();
            else
                artDecoOverlay.Hide();
        }

        if (deckbuilderUI != null)
        {
            if (panel == deckbuilderPanel)
                deckbuilderUI.Show();
            else
                deckbuilderUI.Hide();
        }

        if (shopUI != null)
        {
            if (panel == shopPanel)
                shopUI.Show();
            else
                shopUI.Hide();
        }

        if (lootboxUI != null)
        {
            if (panel == lootboxPanel)
                lootboxUI.Show();
            else
                lootboxUI.Hide();
        }

        if (multiplayerUI != null)
        {
            if (panel == multiplayerPanel)
                multiplayerUI.Show();
            else
                multiplayerUI.Hide();
        }

        if (settingsUI != null)
        {
            if (panel == settingsPanel)
                settingsUI.Show();
            else
                settingsUI.Hide();
        }

        if (creditsUI != null)
        {
            if (panel == creditsPanel)
                creditsUI.Show();
            else
                creditsUI.Hide();
        }

        if (profileUI != null)
        {
            if (panel == profilePanel)
                profileUI.Show();
            else
                profileUI.Hide();
        }

        if (battlePassUI != null)
        {
            if (panel == battlePassPanel)
                battlePassUI.Show();
            else
                battlePassUI.Hide();
        }
    }


    #endregion

    #region Audio

    private void PlayMenuMusic()
    {
        if (menuMusic != null && audioSource != null)
        {
            audioSource.clip = menuMusic;
            audioSource.loop = true;
            audioSource.volume = 0.5f;
            audioSource.Play();
        }
    }

    private void PlayButtonSound()
    {
        if (buttonClickSound != null && audioSource != null)
            audioSource.PlayOneShot(buttonClickSound, 0.7f);
    }

    #endregion

    #region Application

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Quitting...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}
}
