
// Constans for Card

using System;
using System.Collections.Generic;
using UnityEngine;

public enum Suit { Spades = 0, Clubs, Diamonds, Hearts }
public enum Rank { Three = 3, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace, Two }

// Single : 3
// Pair : 33
// Run : 34567
// SuitedRun : 34567 of same suit
// Set : 333
// Bomb : 3333
// PairedRun(Deuce Destroyer): 334455 

public enum PlayType { None, Single, Pair, Run, SuitedRun, Set, PairedRun, Bomb }

public enum CPUDifficulty { Normal, Hard }

public enum GameMode { Online, Local }

// 
public static class  Constants
{
    public const string WEBSITE_URL = "https://17cardgame.com/";

    public const string SERVER_HTTPS_URL = "https://www.17-cardgame.com/api/";
    public const string SERVER_WS_URL = "https://www.17-cardgame.com/";


    public const string LOCAL_HTTP_URL = "http://localhost:5001/api/";
    public const string LOCAL_WS_URL = "http://localhost:5001";

    public const string firebaseWebApiKey = "AIzaSyDgtXKJmlneK0jzVl9d6TBOSHAmGop-UeI";

    public const string firebaseSignInUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=";
    public const string firebaseSignUpUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=";
    public const string firebaseForgotUrl = "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=";
}

public static class GameOptionsKeys
{
    public const string AutoSuggest = "AutoSuggest";
    public const string AutoPass = "AutoPass";
}

[Serializable]
public class NewPlayerInfo
{
    public string email;
    public string displayName;
}

[Serializable]
public class ServerError
{
    public string message;
    public string code;
}

[Serializable]
public class WebGLEventWrapper
{
    public string @event;
    public string data;
    public string eventName => @event;
}

[Serializable]
public class ProfileInfo
{
    public string email;
    public string playerId;
    public string displayName;
    public int token;
    public int avatarIndex;
}

[Serializable]
public class AddPlayerResult
{
    public string status;
    public string playerId;
    public string msg;
}

[Serializable]
public class GamePlayerInfo
{
    public string email;
    public string name;
    public bool isCPU;
    public CPUDifficulty difficulty;
    public int tokens; 
    public int avatarIndex;
    public int wins;
}

[Serializable]
public class GameInfo
{
    public string gameId;
    public string gameName;
    public int betAmount;
    public bool hasCPU;
    public GamePlayerInfo player0; // creator
    public GamePlayerInfo player1; // invitee1
    public GamePlayerInfo player2; // invitee2
}

[Serializable]
public class GameListInfo
{
    public List<GameInfo> gameList;
}

[Serializable]
public class InvitedGameInfo
{
    public string gameId;
    public GameInfo gameInfo;
}

[Serializable]
public class GameCreateResult
{
    public string gameId;
}

[Serializable]
public class ParamInfo
{
    public string param;
}

[Serializable]
public class ResultInfo
{
    public string result;
    public string status;
    public string msg;
}

[Serializable]
public class GameStartInfo
{
    public string gameName;
    public string gameId;
    public int betAmount;
    public bool hasCPU;
    public List<string> playerHands;
    public List<string> playerNames;
    public List<int> playerTokens;
    public List<int> playerAvatarIndexes;
    public int myTurnIndex;
    public int currentPlayerIndex;
    public bool firstTrick;
}

[Serializable]
public class WinnerInfo
{
    public string winnerEmail;
    public string winnerName;
    public int wonToken;
    public int winnerIndex;
}

[Serializable]
public class TurnInfo
{
    public string gameId;
    public bool isPassed;
    public int passesInRow;
    public List<string> currentTopCards;
    public int currentPlayerIndex;
    public bool firstTrick;
    public WinnerInfo winnerInfo;
}

[Serializable]
public class DealEndedInfo
{
    public string gameId;
    public int starterIndex;
}

[Serializable]
public class UpdatedTokenInfo
{
    public int[] updatedTokens;
}

[Serializable]
public class UpdateProfileInfo
{
    public string playerId;
    public int avatarIndex;
    public string displayName;
}

[Serializable]
public class BuyTokenInfo
{
    public string email;
    public string playerId;
    public int tokenAmount;
    public int price;
}

[Serializable]
public class GameScoreInfo
{
    public string name;
    public int wins;
}

[Serializable]
public class GameFinishedInfo
{
    public List<GameScoreInfo> scoreInfoList;
}

[Serializable]
public class LeaderBoardRowInfo
{
    public int rank;
    public string name;
    public int wins;
    public int tokens;
}

[Serializable]
public class LeaderBoardListInfo
{
    public List<LeaderBoardRowInfo> leaderBoardRows;
}

[Serializable]
public class FirebaseSignPayload
{
    public string email;
    public string password;
    public bool returnSecureToken;
}

[Serializable]
public class FirebasePasswordResetPayload
{
    public string requestType;
    public string email;
}

[Serializable]
public class FirebaseSignResponse
{
    public string idToken;
    public string email;
    public string refreshToken;
    public string localId;
    public FirebaseError error;
}

[Serializable]
public class FirebaseError
{
    public int code;
    public string message;
}

[Serializable]
public class FirebaseRefreshResponse
{
    public string id_token;
    public string refresh_token;
    public string access_token;
    public string expires_in;
    public string token_type;
    public string user_id;
}

[Serializable]
public class ServerHealthInfo
{
    public string status;
    public string message;
    public string timestamp;
}

[Serializable]
public class PlayerDisconnectedInfo
{
    public string gameId;
    public string playerName;
    public string message;
}

[Serializable]
public class TokenRefreshResponse
{
    public bool success;
    public string error;
}