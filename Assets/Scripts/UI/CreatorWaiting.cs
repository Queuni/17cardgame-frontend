using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreatorWaiting : MonoBehaviour
{
    public TMP_Text headerText;

    public Player playerInvitee1;
    public Player playerInvitee2;

    public Toggle readyToggle1;
    public Toggle readyToggle2;

    public Button startButton;

    private PrepareManager prepareManager;
    private SocketManager socketManager;
    private AuthManager authManager;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait; // Set screen orientation to portrait
    }

    // Start is called before the first frame update
    void Start()
    {
        prepareManager = PrepareManager.Instance;
        socketManager = SocketManager.Instance;
        authManager = AuthManager.Instance;

        // 
        SetInviteesInfo();

        // 
        SetSocketListener();

        startButton.interactable = false;

        playerInvitee1.HideToken();
        playerInvitee2.HideToken();
    }

    public void SetInviteesInfo()
    {
        GameInfo gameInfo = prepareManager.gameInfo;

        headerText.text = gameInfo.gameName;

        GamePlayerInfo invitedPlayer1 = gameInfo.player1;
        if (invitedPlayer1.isCPU == true)
        {
            readyToggle1.isOn = true;
        }
        playerInvitee1.SetPlayerName(invitedPlayer1.name);
        playerInvitee1.SetToken(invitedPlayer1.tokens);
        int avatarIndex1 = invitedPlayer1.avatarIndex;
        playerInvitee1.SetAvatarSprite(authManager.getAvatarSprite(avatarIndex1));

        GamePlayerInfo invitedPlayer2 = gameInfo.player2;
        if (invitedPlayer2.isCPU == true)
        {
            readyToggle2.isOn = true;
        }
        playerInvitee2.SetPlayerName(invitedPlayer2.name);
        playerInvitee2.SetToken(invitedPlayer2.tokens);
        int avatarIndex2 = invitedPlayer2.avatarIndex;
        playerInvitee2.SetAvatarSprite(authManager.getAvatarSprite(avatarIndex2));
    }

    public void SetSocketListener()
    {
        socketManager.On("player_ready", HandlePlayerReady);
        socketManager.On("player_not_ready", HandlePlayerNotReady);
        socketManager.On("game_starting", HandleGameStarting);
        socketManager.On("game_canceled", HandleGameCanceled);
    }

    private void HandlePlayerReady(string json)
    {
        ResultInfo resultInfo = Utils.JsonToObject<ResultInfo>(json);

        GameInfo gameInfo = prepareManager.gameInfo;

        if (resultInfo.result == gameInfo.player1.email)
        {
            AlertBar.Instance.ShowMessage(gameInfo.player1.name + " is ready.");
            readyToggle1.isOn = true;
        }
        else if (resultInfo.result == gameInfo.player2.email)
        {
            AlertBar.Instance.ShowMessage(gameInfo.player2.name + " is ready.");
            readyToggle2.isOn = true;
        }

        if (readyToggle1.isOn && readyToggle2.isOn)
        {
            startButton.interactable = true;
        }
    }

    private void HandlePlayerNotReady(string json)
    {
        ResultInfo resultInfo = Utils.JsonToObject<ResultInfo>(json);

        startButton.interactable = false;

        GameInfo gameInfo = prepareManager.gameInfo;

        if (resultInfo.result == gameInfo.player1.email)
        {
            AlertBar.Instance.ShowMessage(gameInfo.player1.name + " is not ready.");
            readyToggle1.isOn = false;
        }
        else if (resultInfo.result == gameInfo.player2.email)
        {
            AlertBar.Instance.ShowMessage(gameInfo.player2.name + " is not ready.");
            readyToggle2.isOn = false;
        }
    }

    private void HandleGameStarting(string json)
    {
        Spinner.Instance.Hide();
        prepareManager.gameStartInfo = Utils.JsonToObject<GameStartInfo>(json);

        Utils.LoadScene("GamePlay");
    }

    private void HandleGameCanceled(string json)
    {
        Spinner.Instance.Hide();
        string gameId = prepareManager.gameInfo.gameId;
        ResultInfo resultInfo = Utils.JsonToObject<ResultInfo>(json);

        if (resultInfo.result == gameId)
        {
            Utils.LoadScene("NewGame");
        }
    }

    public void OnStartClicked()
    {
        string gameId = prepareManager.gameInfo.gameId;

        socketManager.Emit("start_game", new ParamInfo { param = gameId });
        
        Spinner.Instance.Show();
        AlertBar.Instance.ShowMessage("Game starting...");
    }

    public void OnBackClicked()
    {
        string gameId = prepareManager.gameInfo.gameId;
        socketManager.Emit("cancel_creating", new ParamInfo { param = gameId });
    }
}
