using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button profileButton;
    public Player myProfile;

    public Button newGameButton;
    public Button gamesButton;
    public Button optionsButton;
    public Button buyTokenButton;
    public Button rulesButton;
    public Button exitButton;
    public Button playOnlineButton;

    private SocketManager socketManager;
    private AuthManager authManager;

    private bool backHanded = false;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait;

        if (FindObjectOfType<MainThread>() == null)
        {
            new GameObject("MainThread").AddComponent<MainThread>();
        }

#if UNITY_WEBGL
        if (exitButton != null) exitButton.gameObject.SetActive(false);
#endif
    }

    private void Update()
    {
        if (Keyboard.current != null &&
        (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.f4Key.wasPressedThisFrame))
        {
            if (!backHanded)
            {
                backHanded = true;
                OnExitButtonClicked();
            }
        }
        else
        {
            backHanded = false;
        }
    }

    private async void Start()
    {
        // Wait for managers to be initialized
        authManager = AuthManager.Instance;

        AddEventListener();

        if (authManager.gameMode == GameMode.Local)
        {
            gamesButton.gameObject.SetActive(false);
            buyTokenButton.gameObject.SetActive(false);
            myProfile.gameObject.SetActive(false);

            return;
        }

        playOnlineButton.gameObject.SetActive(false);

        socketManager = SocketManager.Instance;

        // Check if profileInfo exists and has playerId
        if (authManager.profileInfo != null && !string.IsNullOrEmpty(authManager.profileInfo.playerId))
        {
            SetProfileInfo();

            if (Utils.previousSceneName != "Login")
            {
                bool profileLoaded = await authManager.GetProfileInfo();
                if (profileLoaded)
                {
                    SetProfileInfo();
                }
                else
                {
                    Debug.LogWarning("Failed to refresh profile info in MainMenu");
                }
            }

            // Install the socket after get profile (only if not already connected or connecting)
            // Give a small delay to allow automatic reconnection to start first (if socket was disconnected)
            
            if (socketManager != null)
            {
                socketManager.installSocket();
            }
            else
            {
                AlertBar.Instance.ShowMessage("SocketManager instance is null. Please log in again.");
            }
        }
        else
        {
            AlertBar.Instance.ShowMessage("Error: Profile information is missing. Please log in again.");
        }
    }

    private void AddEventListener()
    {
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameButtonClicked);
        if (gamesButton != null) gamesButton.onClick.AddListener(OnGamesButtonClicked);
        if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsButtonClicked);
        if (rulesButton != null) rulesButton.onClick.AddListener(OnRulesButtonClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonClicked);
        if (buyTokenButton != null) buyTokenButton.onClick.AddListener(OnBuyTokenButtonClicked);
        if (profileButton != null) profileButton.onClick.AddListener(OnProfileButtonClicked);
        if (playOnlineButton != null) playOnlineButton.onClick.AddListener(OnPlayOnlineButtonClicked);
    }

    private void SetProfileInfo()
    {
        if (authManager.profileInfo == null) return;

        if (!string.IsNullOrEmpty(authManager.profileInfo.displayName))
        {
            myProfile.SetPlayerName(authManager.profileInfo.displayName);
        }

        myProfile.SetToken(authManager.profileInfo.token);

        int avatarIndex = authManager.profileInfo.avatarIndex;
        myProfile.SetAvatarSprite(authManager.getAvatarSprite(avatarIndex));
    }

    public void OnProfileButtonClicked()
    {
        Utils.LoadScene("ProfileEdit");
    }

    public void OnNewGameButtonClicked()
    {
        if (authManager.gameMode == GameMode.Local)
        {
            PrepareManager.Instance.gameMode = GameMode.Local;
            PrepareManager.Instance.gameInfo.gameName = "Local Play";
            PrepareManager.Instance.gameInfo.betAmount = 1;

            Utils.LoadScene("GamePlay");
        }
        else
        {
            Utils.LoadScene("NewGame");
        }
    }

    public void OnGamesButtonClicked()
    {
        Utils.LoadScene("Games");
    }

    public void OnOptionsButtonClicked()
    {
        Utils.LoadScene("Options");
    }

    public void OnBuyTokenButtonClicked()
    {
        Utils.LoadScene("BuyTokens");
    }

    public void OnRulesButtonClicked()
    {
        Utils.LoadScene("Rules");
    }

    public void OnPlayOnlineButtonClicked()
    {
        Utils.LoadScene("Login");
    }

    public void OnExitButtonClicked()
    {
        YesNoDialog.Instance.Show("Are you sure you want to exit the game?", 
        () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        },
        () =>
        {
        });
    }
}
