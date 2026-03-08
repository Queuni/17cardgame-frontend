using UnityEngine;
using TMPro;

public class NewGame : MonoBehaviour
{
    public TMP_InputField gameNameInput;

    public TMP_Dropdown betAmountInput;

    public TMP_InputField inviteeInput1;
    private TextMeshProUGUI inviteePlaceholder1;

    public TMP_InputField inviteeInput2;
    private TextMeshProUGUI inviteePlaceholder2;

    private bool isInvitePlayer1;
    private bool isInvitePlayer2;

    private string gameName;
    private int betAmount;
    private string inviteeInfo1;  // emall or display name
    private string inviteeInfo2; // email or display name

    private AuthManager authManager;
    private RequestManager requestManager;
    private PrepareManager prepareManager;
    private SocketManager socketManager;

    private void Start()
    {
        Screen.orientation = ScreenOrientation.Portrait;

        authManager = AuthManager.Instance;
        requestManager = RequestManager.Instance;
        prepareManager = PrepareManager.Instance;
        socketManager = SocketManager.Instance;

        isInvitePlayer1 = true;
        isInvitePlayer2 = true;

        if (Debug.isDebugBuild)
            inviteeInput1.text = "kerry@123.com";
        inviteePlaceholder1 = inviteeInput1.placeholder.GetComponent<TextMeshProUGUI>();
        inviteePlaceholder1.text = "Email | Display name";

        if (Debug.isDebugBuild)
            inviteeInput2.text = "cherry@123.com";
        inviteePlaceholder2 = inviteeInput2.placeholder.GetComponent<TextMeshProUGUI>();
        inviteePlaceholder2.text = "Email | Display name";

        gameNameInput.text = "New Game";

        socketManager.On("game_created", OnGameCreated);
    }

    public void OnPlayerToggle1Changed(bool isOn)
    {
        isInvitePlayer1 = !isInvitePlayer1;
        inviteeInput1.interactable = isInvitePlayer1;
        inviteeInput1.text = isInvitePlayer1 ? "" : "CPU Player 1";
    }

    public void OnPlayerToggle2Changed(bool isOn)
    {
        isInvitePlayer2 = !isInvitePlayer2;
        inviteeInput2.interactable = isInvitePlayer2;
        inviteeInput2.text = isInvitePlayer2 ? "" : "CPU Player 2";
    }

    public void OnBackButtonClicked()
    {
        Utils.LoadScene("MainMenu");
    }

    public async void OnStartButtonClicked()
    {
        gameName = gameNameInput.text;
        if (string.IsNullOrEmpty(gameName))
        {
            AlertBar.Instance.ShowMessage("Please enter a game name.");
            return;
        }

        betAmount = int.Parse(betAmountInput.options[betAmountInput.value].text);


        inviteeInfo1 = inviteeInput1.text;
        if (isInvitePlayer1)
        {
            if (string.IsNullOrEmpty(inviteeInfo1))
            {
                AlertBar.Instance.ShowMessage("Please enter email or display name for Player 1.");
                return;
            }
            else
            {
                bool isExist1 = await authManager.checkPlayerExist(inviteeInfo1);
                if (!isExist1)
                {
                    AlertBar.Instance.ShowMessage("Player 1 does not exist.");
                    return;
                }
            }
        }

        inviteeInfo2 = inviteeInput2.text;
        if (isInvitePlayer2)
        {
            if (string.IsNullOrEmpty(inviteeInfo2))
            {
                AlertBar.Instance.ShowMessage("Please enter email or display name for Player 2.");
                return;
            }
            else
            {
                bool isExist2 = await authManager.checkPlayerExist(inviteeInfo2);
                if (!isExist2)
                {
                    AlertBar.Instance.ShowMessage("Player 2 does not exist.");
                    return;
                }
            }
        }

        // Creator Info
        GamePlayerInfo player0Info = new GamePlayerInfo
        {
            name = authManager.profileInfo.displayName,
            email = authManager.profileInfo.email,
            isCPU = false,
        };


        // Player 1 Info
        string player1Name = isInvitePlayer1 ? "" : "CPU Player 1";

        GamePlayerInfo player1Info = new GamePlayerInfo
        {
            name = player1Name,
            email = inviteeInfo1,
            isCPU = !isInvitePlayer1,
            difficulty = CPUDifficulty.Hard
        };


        // Player 2 Info
        string player2Name = isInvitePlayer2 ? "" : "CPU Player 2";

        GamePlayerInfo player2Info = new GamePlayerInfo
        {
            name = player2Name,
            email = inviteeInfo2,
            isCPU = !isInvitePlayer2,
            difficulty = CPUDifficulty.Normal
        };
        bool hasCPU = !isInvitePlayer1 || !isInvitePlayer2;

        GameInfo gameInfo = new GameInfo
        {
            gameName = gameName,
            betAmount = betAmount,
            player0 = player0Info,
            player1 = player1Info,
            player2 = player2Info,
            hasCPU = hasCPU
        };

        if (!isInvitePlayer1 && !isInvitePlayer2)
        {
            prepareManager.gameMode = GameMode.Local;
        }
        else
        {
            prepareManager.gameMode = GameMode.Online;
        }

        
        if (!hasCPU && betAmount > authManager.profileInfo.token)
        {
            AlertBar.Instance.ShowMessage("You don't have enough tokens.");
            return;
        }

        if (prepareManager.gameMode == GameMode.Local)
        {
            prepareManager.gameInfo = gameInfo;

            Utils.LoadScene("GamePlay");
        }
        else
        {
            // GameId will be generated by the backend socket handler
            // No need to call HTTP POST endpoint anymore
            prepareManager.CreateGame(gameInfo);

            Spinner.Instance.Show();
        }
    }

    private void OnGameCreated(string json)
    {
        Spinner.Instance.Hide();
        prepareManager.gameInfo = Utils.JsonToObject<GameInfo>(json);
        AlertBar.Instance.ShowMessage(prepareManager.gameInfo.gameName + " created.");
        Utils.LoadScene("CreatorWaiting");
    }
}
