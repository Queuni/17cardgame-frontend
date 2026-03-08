using System.Collections.Generic;
using UnityEngine;

public class PrepareManager : MonoBehaviour
{
    public static PrepareManager Instance;

    private SocketManager socketManager;

    public GameInfo gameInfo = new();

    public GameStartInfo gameStartInfo = new();

    public GameMode gameMode;

    public WinnerInfo winnerInfo = new();

    public List<GameScoreInfo> scoreInfoList = new();


    private void Awake()
    {

        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        socketManager = SocketManager.Instance;

        if (socketManager == null)
        {
            Debug.Log("SocketManager instance is null in prepareManager.");
            return;
        }

        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        socketManager.On("invited_to_game", HandleInvitedToGame);
        socketManager.On("player_disconnected", HandlePlayerDisconnected);
    }

    private void HandlePlayerDisconnected(string json)
    {
        PlayerDisconnectedInfo disconnectInfo = Utils.JsonToObject<PlayerDisconnectedInfo>(json);
        // Show alert bar with disconnect message
        if (AlertBar.Instance != null)
        {
            AlertBar.Instance.ShowMessage(disconnectInfo.message);
        }
        Debug.Log($"Player disconnected: {disconnectInfo.playerName} from game {disconnectInfo.gameId}");
    }

    private void HandleInvitedToGame(string json)
    {
        InvitedGameInfo data = Utils.JsonToObject<InvitedGameInfo>(json);
        AlertBar.Instance.ShowMessage($"You're invited to {data.gameInfo.gameName}");
    }


    public void InitGameResultInfo(PlayerController[] players)
    {
        scoreInfoList.Clear();
        foreach (var player in players)
        {
            scoreInfoList.Add(new GameScoreInfo { name = player.Name, wins = 0 });
        }
    }

    public void UpdateGameResultInfo()
    {
        scoreInfoList[winnerInfo.winnerIndex].wins++;
    }


    public void CreateGame(GameInfo info)
    {
        AlertBar.Instance.ShowMessage("Creating game...");
        socketManager.Emit("create_game", info);
    }

    public void AcceptInvite()
    {
        AlertBar.Instance.ShowMessage("Joining Game...");

        socketManager.Emit("accept_invite", new ParamInfo { param = gameInfo.gameId });
    }

    public void RejectInvite()
    {
        AlertBar.Instance.ShowMessage("Rejecting Invite...");
        socketManager.Emit("reject_invite", new ParamInfo { param = gameInfo.gameId });
    }

    public void GetInvitedGames(string email)
    {
        socketManager.Emit("get_invited_games", new ParamInfo { param = email });
    }

    public void SendPlayAgain()
    {
        socketManager.Emit("send_play_again", new ParamInfo { param = gameInfo.gameId });
    }

    public void SetJoinInfo(string gameId, string gameName)
    {
        gameInfo.gameId = gameId;
        gameInfo.gameName = gameName;
    }

    public void SendPlayerTurnInfo(List<Card> cardList, bool passed, int passesInRow)
    {
        List<string> playedCards = new List<string>();
        foreach (var card in cardList)
        {
            playedCards.Add(card.name);
        }

        TurnInfo playedInfo = new TurnInfo
        {
            gameId = gameStartInfo.gameId,
            currentTopCards = playedCards,
            isPassed = passed,
            currentPlayerIndex = gameStartInfo.myTurnIndex,
            passesInRow = passesInRow
        };

        socketManager.Emit("send_player_turn", playedInfo);
    }

    public void SendDealEnded(int currentPlayerIndex)
    {
        // Wrap async call
        string gameId = gameInfo.gameId;
        DealEndedInfo paramInfo = new DealEndedInfo
        {
            gameId = gameId,
            starterIndex = currentPlayerIndex
        };

        socketManager.Emit("deal_ended", paramInfo);
    }

    public void SendOutOfGame()
    {
        if (gameMode == GameMode.Online)
        {
            socketManager.Emit("player_out_of_game", new ParamInfo { param = gameInfo.gameId });
        }
    }
}
