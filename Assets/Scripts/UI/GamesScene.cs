using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamesScene : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform contentParent;  // assign the "Content" object in Inspector

    private SocketManager socketManager;
    private AuthManager authManager;
    private PrepareManager prepareManager;
    private List<GameObject> instantiatedButtons = new List<GameObject>();
    private bool isInitialized = false;

    private List<GameInfo> invitedGameList = new List<GameInfo>();

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait; // Set screen orientation to portrait
    }

    void Start()
    {
        authManager = AuthManager.Instance;
        socketManager = SocketManager.Instance;
        prepareManager = PrepareManager.Instance;

        // Register listeners FIRST to avoid race condition
        // If we load games before registering, the response might arrive before listener is set up
        RegisterSocketListeners();
        LoadInvitedGames();

        isInitialized = true;
    }

    void OnEnable()
    {
        // Refresh games list when scene becomes active (e.g., returning from another scene)
        // Only refresh if already initialized to avoid double-loading on first start
        if (isInitialized)
        {
            LoadInvitedGames();
        }
    }

    void OnDestroy()
    {
        // Unregister socket listeners to prevent memory leaks
        if (socketManager != null)
        {
            socketManager.Off("invited_games");
        }

        // Clean up instantiated buttons
        ClearGameButtons();
    }

    private void RegisterSocketListeners()
    {
        if (socketManager == null) return;

        // Listen for the initial games list response
        socketManager.On("invited_games", HandleInvitedGames);

        // Listen for new game invitations to automatically refresh the list
        socketManager.On("invited_to_game", HandleNewInvitation);
    }

    private void LoadInvitedGames()
    {
        if (authManager == null || authManager.profileInfo == null)
        {
            Debug.LogError("GamesScene: authManager or profileInfo is null");
            Spinner.Instance.Hide();
            AlertBar.Instance.ShowMessage("Unable to load games. Please try again.");
            return;
        }

        if (prepareManager == null)
        {
            Debug.LogError("GamesScene: prepareManager is null");
            Spinner.Instance.Hide();
            AlertBar.Instance.ShowMessage("Unable to load games. Please try again.");
            return;
        }

        Spinner.Instance.Show();
        string email = authManager.profileInfo.email;
        prepareManager.GetInvitedGames(email);
    }

    private void HandleNewInvitation(string json)
    {
        // When a new invitation arrives, refresh the games list
        InvitedGameInfo data = Utils.JsonToObject<InvitedGameInfo>(json);
        AlertBar.Instance.ShowMessage($"You're invited to {data.gameInfo.gameName}");

        LoadInvitedGames();
    }

    private void HandleInvitedGames(string json)
    {
        Spinner.Instance.Hide();

        // Clear existing buttons before showing new list
        ClearGameButtons();

        GameListInfo resultInfo = Utils.JsonToObject<GameListInfo>(json);
        invitedGameList = resultInfo?.gameList;

        if (invitedGameList == null || invitedGameList.Count == 0)
        {
            AlertBar.Instance.ShowMessage("You haven't been invited yet.");
        }
        else
        {
            if (buttonPrefab != null && contentParent != null)
            {
                foreach (var game in invitedGameList)
                {
                    GameObject obj = Instantiate(buttonPrefab, contentParent);
                    instantiatedButtons.Add(obj); // Track for cleanup

                    Button btn = obj.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.name = "button_" + game.gameId;

                        TextMeshProUGUI textComponent = btn.GetComponentInChildren<TextMeshProUGUI>();
                        if (textComponent != null)
                        {
                            textComponent.text = game.gameName;
                        }

                        GameObject capturedBtn = obj;
                        btn.onClick.AddListener(() => OnButtonClicked(capturedBtn));
                    }
                }
            }
            else
            {
                Debug.LogWarning("buttonPrefab or contentParent is null");
            }
        }
    }

    void OnButtonClicked(GameObject button)
    {
        if (button == null) return;

        string clickedGameId = button.name.Replace("button_", "");

        TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
        string gameName = textComponent != null ? textComponent.text : "";

        GameInfo clickedGame = invitedGameList.FirstOrDefault<GameInfo>(a => a.gameId == clickedGameId);
        if (clickedGame != null)
        {
            int betAmount = clickedGame.betAmount;
            if (!clickedGame.hasCPU && betAmount > authManager.profileInfo.token)
            {
                AlertBar.Instance.ShowMessage("You don't have enough tokens.");
                return;
            }
            else
            {
                PrepareManager.Instance.SetJoinInfo(clickedGameId, gameName);

                Utils.LoadScene("InviteeWaiting");
            }
        }
        else
        {
            Debug.LogWarning($"Game with ID {clickedGameId} not found in invited games list. The game may have been removed or expired.");
            AlertBar.Instance.ShowMessage("This game is no longer available.");
        }
    }

    public void OnBackButtonClicked()
    {
        Utils.LoadScene("MainMenu");
    }

    private void ClearGameButtons()
    {
        // Remove all instantiated game buttons
        foreach (var button in instantiatedButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        instantiatedButtons.Clear();
    }
}
