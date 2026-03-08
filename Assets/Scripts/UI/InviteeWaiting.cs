using UnityEngine;
using TMPro;
using System.Collections;

public class InviteeWaiting : MonoBehaviour
{
    public TMP_Text gameNameText;
    public Player myPlayer;

    private bool isReady = false;
    private SocketManager socketManager;
    private AuthManager authManager;
    private PrepareManager prepareManager;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait; // Set screen orientation to portrait
    }

    // Start is called before the first frame update
    void Start()
    {
        authManager = AuthManager.Instance;
        socketManager = SocketManager.Instance;
        prepareManager = PrepareManager.Instance;

        // Validate all required managers are available
        if (authManager == null || socketManager == null || prepareManager == null)
        {
            Debug.LogError("InviteeWaiting: Required managers are null!");
            return;
        }

        // Validate prepareManager has gameInfo
        if (prepareManager.gameInfo == null)
        {
            Debug.LogError("InviteeWaiting: prepareManager.gameInfo is null!");
            return;
        }
 
        gameNameText.text = prepareManager.gameInfo.gameName;


        myPlayer.SetPlayerName(authManager.profileInfo.displayName);
        myPlayer.SetToken(authManager.profileInfo.token);
        myPlayer.SetAvatarSprite(authManager.getAvatarSprite(authManager.profileInfo.avatarIndex));
        

        socketManager.On("game_starting", HandleGameStarting);
        socketManager.On("game_canceled", (string json) =>
        {
            StartCoroutine(HandleGameCanceled(json));
        });
    }

    // Update is called once per frame
    private void HandleGameStarting(string json)
    {
        // Check if MonoBehaviour is still valid
        prepareManager.gameStartInfo = Utils.JsonToObject<GameStartInfo>(json);

        Utils.LoadScene("GamePlay");
    }

    private IEnumerator HandleGameCanceled(string json)
    {
        // Check if MonoBehaviour and dependencies are still valid
        if (this == null || prepareManager == null || prepareManager.gameInfo == null)
        {
            yield break;
        }

        string gameId = prepareManager.gameInfo.gameId;
        ResultInfo resultInfo = Utils.JsonToObject<ResultInfo>(json);
        if (resultInfo != null && resultInfo.result == gameId)
        {
            if (AlertBar.Instance != null)
            {
                AlertBar.Instance.ShowMessage("The game is canceled by creator.");
            }
            yield return new WaitForSeconds(1f);

            Utils.LoadScene("Games");
        }
    }

    public void OnBackButtonClicked()
    {
        Utils.LoadScene("Games");
    }

    public void OnReadyToggleChanged()
    {
        isReady = !isReady;
        if (isReady)
        {
            prepareManager.AcceptInvite();
        }
        else
        {
            prepareManager.RejectInvite();
        }
    }

    private void OnDestroy()
    {
        // Unregister socket handlers to prevent callbacks on destroyed object
        if (socketManager != null)
        {
            socketManager.Off("game_starting");
            socketManager.Off("game_canceled");
        }
    }
}
