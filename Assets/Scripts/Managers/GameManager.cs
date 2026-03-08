using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    private AuthManager authManager;
    private PrepareManager prepareManager;
    private SocketManager socketManager;

    [Header("Class References")]
    [SerializeField] public SelectionManager selectionManager;
    [SerializeField] public TableAnimator tableAnimator;
    [SerializeField] public CardAnimator cardAnimator;

    [Header("UI References")]
    public GameObject playButtonObject;
    public GameObject passButtonObject;
    public GameObject menuButtonObject;

    public TextMeshProUGUI potTextObject;
    public Image potTokenImage;
    public Player[] playerList;
    public TMP_Text gameNameText;

    private Button playButton;
    private Button passButton;
    private Button menuButton;

    public GameState state;
    private PlayerController[] players;
    private bool waitingForHuman;

    private Coroutine gameLoopRoutine;

    void Awake()
    {
        if (Dealer.frontSpriteMap.Count == 0)
        {
            Dealer.LoadSprites();
        }

        Screen.orientation = ScreenOrientation.LandscapeLeft; // Set screen orientation to portrait
    }

    void Start()
    {
        authManager = AuthManager.Instance;
        socketManager = SocketManager.Instance;
        prepareManager = PrepareManager.Instance;

        AddEventListener();
        

        // Initialize game state
        state = new GameState();
        SetTokenColor();

        if (prepareManager.gameMode == GameMode.Online)
        {
            StartCoroutine(StartOnlineMode());
        }
        else
        {
            StartCoroutine(StartLocalMode());
        }
    }

    private IEnumerator StartOnlineMode()
    {
        gameNameText.text = prepareManager.gameStartInfo.gameName;
        state.FirstTrick = prepareManager.gameStartInfo.firstTrick;
        state.myTurnIndex = prepareManager.gameStartInfo.myTurnIndex;
        state.currentPlayerIndex = prepareManager.gameStartInfo.currentPlayerIndex;
        state.CurrentBet = prepareManager.gameStartInfo.betAmount;

        for (int i = 0; i < state.PlayerTokens.Length; i++)
        {
            int localIndex = state.GetPlayerLocalIndex(i);
            state.PlayerTokens[localIndex] = prepareManager.gameStartInfo.playerTokens[i];
        }

        state.CaculatePot();
        UpdatePlayerTokenAndPot();

        Dealer.Deal(state, prepareManager.gameStartInfo.playerHands);

        players = new PlayerController[]
        {
            new HumanPlayer { Index = 0 },
        };

        socketManager.On("receive_player_turn", (string json) =>
        {
            TurnInfo playerTurnInfo = Utils.JsonToObject<TurnInfo>(json);

            StartCoroutine(HandelReceiveOnlineTurn(playerTurnInfo));
        });

        socketManager.On("game_continued", (string json) =>
        {
            GameStartInfo continueInfo = Utils.JsonToObject<GameStartInfo>(json);
            StartCoroutine(HandleGameContinued(continueInfo));
        });

        socketManager.On("game_finished", (string json) =>
        {
            GameFinishedInfo finishedInfo = Utils.JsonToObject<GameFinishedInfo>(json);
            StartCoroutine(HandleGameFinished(finishedInfo));
        });

        yield return StartCoroutine(cardAnimator.DealCardsAnimated(state));

        int localPlayerIndex = state.GetPlayerLocalIndex(state.currentPlayerIndex);
        tableAnimator.AnimatePlayerOrder(localPlayerIndex);
        
        selectionManager.UpdateGameState(state);
        
        // Show auto-suggest after UpdateGameState so card GameObjects are initialized
        ShowAutoSuggestIfEnabled(localPlayerIndex);

        // check if CPU turn order is first on the server
        if (state.myTurnIndex == 0)
        {
            prepareManager.SendDealEnded(state.currentPlayerIndex);
        }
    }

    private IEnumerator HandleGameFinished(GameFinishedInfo finishedInfo)
    {
        prepareManager.scoreInfoList = finishedInfo.scoreInfoList;

        tableAnimator.RemovePlayerOrder();

        yield return new WaitForSeconds(1.5f);
        Utils.LoadSceneAsDialog("GameFinished");
    }

    private IEnumerator HandleGameContinued(GameStartInfo continueInfo)
    {
        string gameId = continueInfo.gameId;
        if (gameId == prepareManager.gameStartInfo.gameId)
        {
            Spinner.Instance.Hide();

            prepareManager.gameStartInfo.firstTrick = continueInfo.firstTrick;
            prepareManager.gameStartInfo.myTurnIndex = continueInfo.myTurnIndex;
            prepareManager.gameStartInfo.currentPlayerIndex = continueInfo.currentPlayerIndex;
            prepareManager.gameStartInfo.playerHands = continueInfo.playerHands;
            prepareManager.gameStartInfo.playerTokens = continueInfo.playerTokens;


            state.FirstTrick = prepareManager.gameStartInfo.firstTrick;
            state.myTurnIndex = prepareManager.gameStartInfo.myTurnIndex;
            state.currentPlayerIndex = prepareManager.gameStartInfo.currentPlayerIndex;

            for (int i = 0; i < state.PlayerTokens.Length; i++)
            {
                int localIndex = state.GetPlayerLocalIndex(i);
                state.PlayerTokens[localIndex] = prepareManager.gameStartInfo.playerTokens[i];
            }


            state.CaculatePot();
            UpdatePlayerTokenAndPot();

            Dealer.Deal(state, prepareManager.gameStartInfo.playerHands);

            yield return StartCoroutine(cardAnimator.DealCardsAnimated(state));

            int localPlayerIndex = state.GetPlayerLocalIndex(state.currentPlayerIndex);
            tableAnimator.AnimatePlayerOrder(localPlayerIndex);

            selectionManager.UpdateGameState(state);

            ShowAutoSuggestIfEnabled(localPlayerIndex);

            // check if CPU turn order is first on the server
            if (state.myTurnIndex == 0)
            {
                prepareManager.SendDealEnded(state.currentPlayerIndex);
            }
        }
    }

    private IEnumerator HandelReceiveOnlineTurn(TurnInfo result)
    {
        // Defensive null checks for WebGL
        if (result == null)
        {
            Debug.LogError("HandelReceiveOnlineTurn: result is null");
            yield break;
        }

        List<string> currentTopCards = result.currentTopCards ?? new List<string>();

        if (passButton != null)
        {
            passButton.interactable = result.currentPlayerIndex == state.myTurnIndex
                && currentTopCards != null && currentTopCards.Count != 0;
        }

        // Calculate which player just played (currentPlayerIndex from server is the NEXT player)
        int previousPlayerIndex = (result.currentPlayerIndex - 1 + 3) % 3;
        int playerWhoJustPlayed = state.GetPlayerLocalIndex(previousPlayerIndex);

        state.currentPlayerIndex = result.currentPlayerIndex;
        state.PassesInRow = result.passesInRow;
        state.FirstTrick = result.firstTrick;

        // If server indicates pile should be cleared (two consecutive passes),
        // mirror local behaviour: clear selection, reset top play and run visual clear.
        if (result.passesInRow >= 2)
        {
            state.CurrentTopPlay = null;
            state.PassesInRow = 0;

            if (selectionManager != null)
                selectionManager.ClearSelection();

            if (cardAnimator != null)
                StartCoroutine(cardAnimator.ClearTable());
        }

        int localPlayerIndex = state.GetPlayerLocalIndex(state.currentPlayerIndex);
        if (tableAnimator != null)
        {
            tableAnimator.AnimatePlayerOrder(localPlayerIndex);
        }

        List<Card> playedCards = Dealer.CreateCardsFromNames(currentTopCards);
        Play act = null;
        if (playedCards != null && playedCards.Count > 0)
        {
            act = Rules.BuildPlay(playedCards);
        }
        state.CurrentTopPlay = act;

        if (result.isPassed)
        {
            if (tableAnimator != null)
            {
                tableAnimator.SayPass(playerWhoJustPlayed);
            }
            if (selectionManager != null)
            {
                selectionManager.ClearSelection();
            }
        }
        else
        {
            // Check if this is the local player - if so, we already animated when they clicked Play
            // Only animate for other players
            bool isLocalPlayerPlaying = (previousPlayerIndex == state.myTurnIndex);

            if (!isLocalPlayerPlaying && act != null && cardAnimator != null)
            {
                // Animate other player's cards appearing on table
                StartCoroutine(cardAnimator.AnimatePlay(playerWhoJustPlayed, act, previousPlayerIndex));
            }
            // If local player, animation was already done in OnPlayButtonClicked
        }

        if (selectionManager != null)
        {
            selectionManager.UpdateGameState(state);
        }

        ShowAutoSuggestIfEnabled(localPlayerIndex);

        WinnerInfo winnerInfo = result.winnerInfo;
        if (winnerInfo != null && !string.IsNullOrEmpty(winnerInfo.winnerName))
        {
            prepareManager.winnerInfo.winnerName = winnerInfo.winnerName;
            prepareManager.winnerInfo.wonToken = winnerInfo.wonToken;

            tableAnimator.RemovePlayerOrder();

            yield return new WaitForSeconds(1.5f);
            Utils.LoadSceneAsDialog("RoundFinished");
        }
    }

    /// Shows auto-suggested cards if it's the human player's turn and auto-suggest is enabled.
    /// <param name="localPlayerIndex">The local index of the current player (0 for human player)</param>
    private void ShowAutoSuggestIfEnabled(int localPlayerIndex)
    {
        if (localPlayerIndex == 0)
        {
            bool isAutoSuggested = PlayerPrefs.GetInt(GameOptionsKeys.AutoSuggest, 1) == 1;
            if (isAutoSuggested)
            {
                var human = players[0] as HumanPlayer;
                var suggestedPlay = human.SuggestPlay(state);
                selectionManager.HighlightSuggestedCards(suggestedPlay);
            }
        }
    }


    private IEnumerator StartLocalMode()
    {
        Dealer.Deal(state);

        state.ChooseStarter();

        state.CurrentBet = prepareManager.gameInfo.betAmount;
        gameNameText.text = prepareManager.gameInfo.gameName;

        // Initialize players
        players = new PlayerController[]
        {
            new HumanPlayer { Index = 0, Name = "You" },
            new AIPlayer(CPUDifficulty.Normal) { Index = 1, Name = "CPU Player 1" },
            new AIPlayer(CPUDifficulty.Hard)   { Index = 2, Name = "CPU Player 2" }
        };

        prepareManager.InitGameResultInfo(players);

        // Subtract tokens and calculate pot
        state.CaculatePot();

        // display updated token and pot values
        UpdatePlayerTokenAndPot();

        selectionManager.UpdateGameState(state);

        // deal cards to players with animation
        yield return StartCoroutine(cardAnimator.DealCardsAnimated(state));

        // Start the main game loop
        gameLoopRoutine = StartCoroutine(LocalGameLoop());
    }

    private void AddEventListener()
    {
        //Add listener to play and pass buttons
        playButton = playButtonObject.GetComponent<Button>();
        playButton.interactable = false;
        playButton.onClick.AddListener(OnPlayButtonClicked);

        passButton = passButtonObject.GetComponent<Button>();
        passButton.interactable = false;
        passButton.onClick.AddListener(OnPassButtonClicked);

        menuButton = menuButtonObject.GetComponent<Button>();
        menuButton.onClick.AddListener(OnMenuButtonClicked);
    }

    private void SetTokenColor()
    {
        bool hasCPU = prepareManager.gameMode == GameMode.Local || prepareManager.gameStartInfo.hasCPU;
        foreach (var player in playerList)
        {
            player.SetTokenColor(hasCPU);
        }

        if (!hasCPU)
        {
            potTokenImage.sprite = Resources.Load<Sprite>("images/coin__150");
        }
        else
        {
            potTokenImage.sprite = Resources.Load<Sprite>("images/coin_150_silver");
        }
    }

    private void UpdatePlayerTokenAndPot()
    {
        potTextObject.text = state.Pot.ToString();
        for (int i = 0; i < state.PlayerTokens.Length; i++)
        {
            playerList[i].SetToken(state.PlayerTokens[i]);
        }
    }

    private IEnumerator LocalGameLoop()
    {
        bool win = false;
        while (!win)
        {
            int p = state.currentPlayerIndex;
            
            // Validate player index
            if (p < 0 || p >= 3)
            {
                Debug.LogError($"Invalid currentPlayerIndex: {p}. Resetting to 0.");
                p = 0;
                state.currentPlayerIndex = 0;
            }
            
            selectionManager.UpdateGameState(state);

            tableAnimator.AnimatePlayerOrder(p);

            if (p == 0) // User turn
            {
                // Human turn
                waitingForHuman = true;
                // Only enable pass button when previous cards are on the table 
                passButton.interactable = state.CurrentTopPlay != null;

                ShowAutoSuggestIfEnabled(p);

                yield return new WaitUntil(() => waitingForHuman == false);

                // You win
                if (state.Hands[0].Count == 0)
                {
                    win = true;
                    prepareManager.winnerInfo.winnerName = authManager.profileInfo.displayName;
                    prepareManager.winnerInfo.winnerIndex = p;
                    prepareManager.winnerInfo.wonToken = state.Pot;
                    break;
                }
            }
            else // AI turn
            {
                // Disable user's play and pass buttons
                playButton.interactable = false;
                passButton.interactable = false;
                
                // Clear suggestions during CPU turn
                selectionManager.ClearSelection();

                // Waiting for AI thinking
                yield return new WaitForSeconds(2.0f);
                
                // Validate player index
                if (p < 0 || p >= players.Length || players[p] == null)
                {
                    Debug.LogError($"Invalid player index: {p}");
                    break;
                }
                
                var ai = players[p];
                var act = ai.ChoosePlay(state);

                if (act == null || act.Cards == null || act.Cards.Count == 0)
                {
                    tableAnimator.SayPass(p);
                    state.PassesInRow++;
                    HandlePlayerPassed();
                }
                else
                {
                    // Use safe removal method that matches by suit/rank
                    state.RemoveCardsFromHand(p, act.Cards);

                    state.CurrentTopPlay = act;
                    state.PassesInRow = 0;

                    yield return StartCoroutine(cardAnimator.AnimatePlay(p, act));
                    if (state.Hands[p].Count == 0)
                    {
                        win = true;
                        prepareManager.winnerInfo.winnerName = $"CPU Player {p}";
                        prepareManager.winnerInfo.winnerIndex = p;
                        prepareManager.winnerInfo.wonToken = state.Pot;
                        break;
                    }
                }

                state.currentPlayerIndex = (p + 1) % 3;
            }
        }


        // Game finished when win = true
        state.giveTokensToWinner(prepareManager.winnerInfo.winnerIndex);
        prepareManager.UpdateGameResultInfo();

        yield return new WaitForSeconds(1.5f);

        tableAnimator.RemovePlayerOrder();
        if (IsAnyPlayerOutOfBet())
        {
            Utils.LoadSceneAsDialog("GameFinished");
        }
        else
        {
            Utils.LoadSceneAsDialog("RoundFinished");
        }
    }

    // Called by Play button
    public void OnPlayButtonClicked()
    {
        var human = players[0] as HumanPlayer;
        human.Selected = new List<Card>(selectionManager.Selected);

        var act = human.ChoosePlay(state);

        // Check if act is null (invalid play)
        if (act == null || act.Cards == null || act.Cards.Count == 0)
        {
            Debug.LogWarning("Invalid play - cannot proceed");
            return;
        }


        // Use safe removal method that matches by suit/rank
        state.RemoveCardsFromHand(0, act.Cards);

        // Remove from selection manager
        if (selectionManager != null)
        {
            selectionManager.RemoveCardsFromSelection(act.Cards);
        }

        playButton.interactable = false;
        passButton.interactable = false;

        if (state.FirstTrick == true)
        {
            state.FirstTrick = false;
        }

        state.CurrentTopPlay = act;
        state.PassesInRow = 0;

        if (prepareManager.gameMode == GameMode.Local)
        {
            waitingForHuman = false;
            state.currentPlayerIndex = 1; // move to AI

            // In local mode, show animation immediately - cards will be moved from hand
            StartCoroutine(cardAnimator.AnimatePlay(0, act));
        }
        else
        {
            // In online mode, animate cards moving from hand immediately
            // Server will also broadcast, but we handle local player's cards here
            StartCoroutine(cardAnimator.AnimatePlay(0, act, state.myTurnIndex));

            // Also send to server so others see it
            prepareManager.SendPlayerTurnInfo(act.Cards, false, state.PassesInRow);
        }
    }

    // Called by Pass button
    public void OnPassButtonClicked()
    {
        playButton.interactable = false;
        passButton.interactable = false;

        if (prepareManager.gameMode == GameMode.Local)
        {
            // In local mode, show animations immediately
            tableAnimator.SayPass(0);
            state.PassesInRow++;
            HandlePlayerPassed();
            selectionManager.ClearSelection();

            waitingForHuman = false;
            state.currentPlayerIndex = 1; // move to AI
        }
        else
        {
            // In online mode, wait for server broadcast for synchronized animations
            // Server will handle passesInRow >= 2 and broadcast clearTable flag
            // Check if CurrentTopPlay exists before accessing its Cards
            if (state.CurrentTopPlay != null && state.CurrentTopPlay.Cards != null)
            {
                prepareManager.SendPlayerTurnInfo(state.CurrentTopPlay.Cards, true, state.PassesInRow);
            }
            else
            {
                // Pass with empty cards list when there's no current top play
                prepareManager.SendPlayerTurnInfo(new List<Card>(), true, state.PassesInRow);
            }
        }
    }

    private void HandlePlayerPassed()
    {
        if (state.PassesInRow >= 2)
        {
            state.CurrentTopPlay = null;
            state.PassesInRow = 0;
            StartCoroutine(cardAnimator.ClearTable());
        }
    }

    public void OnMenuButtonClicked()
    {
        PauseGame();
        YesNoDialog.Instance.Show("Are you sure to quit?",
            () =>
            {
                DOTween.KillAll();
                if (gameLoopRoutine != null)
                {
                    StopCoroutine(gameLoopRoutine);
                    gameLoopRoutine = null;
                }

                Time.timeScale = 1f;
                selectionManager = null;
                tableAnimator = null;
                cardAnimator = null;

                prepareManager.SendOutOfGame();

                Utils.LoadScene("MainMenu");
            },
            () =>
            {
                ResumeGame();
            });
    }

    private bool IsAnyPlayerOutOfBet()
    {
        foreach (int tokens in state.PlayerTokens)
        {
            if (tokens < state.CurrentBet)
            {
                return true;
            }
        }
        return false;
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }


    // restart the round
    public IEnumerator RestartRound()
    {
        // stop all ongoing tweens/animations
        DOTween.KillAll();

        // stop game loop coroutine
        if (gameLoopRoutine != null)
        {
            StopCoroutine(gameLoopRoutine);
            gameLoopRoutine = null;
        }

        // Disable buttons during reset
        playButton.interactable = false;
        passButton.interactable = false;

        // Clear all cards from previous round immediately
        cardAnimator.ClearAllCardsImmediate();

        state.ClearState();

        if (prepareManager.gameMode == GameMode.Local)
        {
            // save previous tokens and bet
            int[] savedTokens = (int[])state.PlayerTokens.Clone();
            int savedBet = state.CurrentBet;

            // creates a new state with previous tokens and bet
            state = new GameState();
            state.PlayerTokens = (int[])savedTokens.Clone();
            state.CurrentBet = savedBet;

            // contribute cards to players
            Dealer.Deal(state);

            // choose the player who has spade 3
            state.ChooseStarter();

            // subtract players token and add to pot
            state.CaculatePot();

            // diplay updated token and pot values
            UpdatePlayerTokenAndPot();

            // 8) Re-deal visuals, then relaunch the loop
            yield return StartCoroutine(cardAnimator.DealCardsAnimated(state));

            // Restart game loop
            gameLoopRoutine = StartCoroutine(LocalGameLoop());
        }
        else
        {
            prepareManager.SendPlayAgain();
            Spinner.Instance.Show();
        }
    }
}
