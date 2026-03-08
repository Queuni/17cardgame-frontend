using System.Collections.Generic;
using UnityEngine;


public class TableAnimator : MonoBehaviour
{
    public Player[] playerList;
    
    private PrepareManager prepareManager;
    private AuthManager authManager;

    public RectTransform typePanelRect;

    private void Start()
    {
        // 
        prepareManager = PrepareManager.Instance;
        authManager = AuthManager.Instance;

        typePanelRect.gameObject.SetActive(false);

        SetPlayerInfo();
    }

    private void SetPlayerInfo()
    {
        if (prepareManager.gameMode == GameMode.Online)
        {
            List<string> playerNames = prepareManager.gameStartInfo.playerNames;
            List<int> playerAvatarIndexes = prepareManager.gameStartInfo.playerAvatarIndexes;
            int offset = 3 - prepareManager.gameStartInfo.myTurnIndex;

            for (int i = 0; i < playerList.Length; i++)
            {
                Player player = playerList[(i + offset) % 3];
                player.SetPlayerName(playerNames[i]);

                int avatarIndex = playerAvatarIndexes[i];
                player.SetAvatarSprite(authManager.getAvatarSprite(avatarIndex));
            }
        }
        else
        {
            // Local mode - use default name and avatar if not logged in
            Player myPlayer = playerList[0];
            string playerName = "You";
            int avatarIndex = 0;
            
            if (authManager.profileInfo != null)
            {
                playerName = authManager.profileInfo.displayName ?? "You";
                avatarIndex = authManager.profileInfo.avatarIndex;
            }
            
            myPlayer.SetPlayerName(playerName);
            myPlayer.SetAvatarSprite(authManager.getAvatarSprite(avatarIndex));
        }
    }

    public void AnimatePlayerOrder(int playerIndex)
    {
        for (int i = 0; i < playerList.Length; i++)
        {
            Player player = (Player)playerList[i];
            if (i == playerIndex)
            {
                player.ShowWaiting();
            }
            else
            {
                player.HideWaiting();
            }
        }
    }

    public void RemovePlayerOrder()
    {
        foreach (Player player in playerList)
        {
            player.HideWaiting();
        }
    }


    public void SayPass(int playerIndex)
    {
        Player player = playerList[playerIndex];
        player.ShowPass();
    }

    public void SayPlayType(int playerIndex, PlayType playType)
    {
        Player player = playerList[playerIndex];
        player.ShowPlayType(playType);
    }
}
