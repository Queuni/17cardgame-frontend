#if !UNITY_WEBGL
using Firebase;
using Firebase.Auth;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance;
    private string firebaseIdToken;
    private string firebaseRefreshToken; // Store refresh token for WebGL
    private RequestManager requestManager;

    [HideInInspector] public bool isServerRunning;
    [HideInInspector] public ProfileInfo profileInfo = new ProfileInfo();
    [HideInInspector] public List<Sprite> avatarSprites = new List<Sprite>();
    [HideInInspector] public Sprite cpuAvatarSprite;
    [HideInInspector] public int avatarCount;
    [HideInInspector] public GameMode gameMode;

#if !UNITY_WEBGL
    private FirebaseUser User;
    private FirebaseAuth auth;
#endif
    [HideInInspector] public bool isFirebaseReady;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        requestManager = RequestManager.Instance;

        LoadAvatarSprites();
        await InitializeServer();
    }

    private void LoadAvatarSprites()
    {
        cpuAvatarSprite = Resources.Load<Sprite>("images/avatars/cpu_avatar");
        avatarSprites = Resources.LoadAll<Sprite>("images/avatars").ToList();

        avatarSprites.Remove(cpuAvatarSprite);
        avatarCount = avatarSprites.Count;
    }

    public Sprite getAvatarSprite(int index)
    {
        if (index == -1) return cpuAvatarSprite;
        try
        {
            return avatarSprites[index];
        }
        catch (Exception)
        {
            return avatarSprites[0];
        }
    }

    public async Task InitializeServer()
    {
#if UNITY_WEBGL
        isFirebaseReady = true;
#else
        await InitializeFirebase();
#endif

        await CheckServerRunning();
    }

#if !UNITY_WEBGL
    private async Task InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (dependencyStatus == DependencyStatus.Available)
        {
            auth = FirebaseAuth.DefaultInstance;
            isFirebaseReady = true;
            Debug.Log("✅ Firebase initialized and ready.");
        }
        else
        {
            Debug.Log($"❌ Firebase dependencies not available: {dependencyStatus}");
            isFirebaseReady = false;
        }
    }
#endif

    private async Task CheckServerRunning()
    {
        string url = "health";
        string result = await requestManager.GetRequestAsync(url);

        try
        {
            ServerHealthInfo healthInfo = Utils.JsonToObject<ServerHealthInfo>(result);
            if (healthInfo.status == "success")
            {
                isServerRunning = true;
            }
        }
        catch (Exception)
        {
            isServerRunning = false;
        }
    }

    public async Task<string> SignIn(string email, string password)
    {
#if UNITY_WEBGL
        return await WebGL_SignIn(email, password);
#else
        return await SDK_SignIn(email, password);
#endif
    }

#if !UNITY_WEBGL
    private async Task<string> SDK_SignIn(string email, string password)
    {
        try
        {
            var userCredential = await auth.SignInWithEmailAndPasswordAsync(email, password);
            User = userCredential.User;
            profileInfo.email = User.Email;

            firebaseIdToken = await User.TokenAsync(true);
            // Note: Native SDK handles refresh token internally

            // Wait for profile info to load completely
            bool profileLoaded = await GetProfileInfo();
            if (!profileLoaded)
            {
                return "Login successful but failed to load profile. Please try again.";
            }

            return "Login successful!";
        }
        catch (FirebaseException firebaseEx)
        {
            var authError = (AuthError)firebaseEx.ErrorCode;
            Debug.LogError($"Firebase Auth Error: {firebaseEx.Message}");

            switch (authError)
            {
                case AuthError.UserNotFound:
                    return "No account found with this email.";
                case AuthError.InvalidEmail:
                    return "The email address is invalid.";
                case AuthError.WrongPassword:
                    return "Incorrect password.";
                case AuthError.UserDisabled:
                    return "This account has been disabled.";
                default:
                    return "Login failed. Try again.";
            }
            
        }
        catch (Exception ex)
        {
            return $"Other Error: {ex.Message}";
        }
    }
#endif

    public async Task<string> SignUp(string email, string password, string displayName)
    {
#if UNITY_WEBGL
    return await WebGL_SignUp(email, password, displayName);
#else
        return await SDK_SignUp(email, password, displayName);
#endif
    }

    private async Task AddNewPlayer(string email, string displayName)
    {
        try
        {
            // add new player
            NewPlayerInfo newPlayer = new NewPlayerInfo
            {
                email = email,
                displayName = displayName
            };

            string jsonData = Utils.ObjectToJson(newPlayer);
            await requestManager.PostRequestAsync("auth/add-player", jsonData, firebaseIdToken);
        }
        catch (Exception e)
        {
            Debug.Log("Error in auth/add-player:" + e.ToString());
        }
    }

    public async Task<bool> checkUsernameExist(string displayName)
    {
        try
        {
            string url = $"auth/username-exists?username={displayName}";
            string resultJson = await requestManager.GetRequestAsync(url);
            ResultInfo resultInfo = Utils.JsonToObject<ResultInfo>(resultJson);
            
            return resultInfo.result == "true";
        }
        catch (Exception e)
        {
            Debug.Log("Error in auth/username-exists:" + e.ToString());
            return false;
        }
    }

    public async Task<bool> checkPlayerExist(string playerInfo)
    {
        try
        {
            string token = await GetIdToken();
            string url = $"auth/player-exists?playerInfo={playerInfo}";
            string resultJson = await requestManager.GetRequestAsync(url, token);
            ResultInfo resultInfo = Utils.JsonToObject<ResultInfo>(resultJson);
            
            return resultInfo.result == "true";
        }
        catch (Exception e)
        {
            Debug.Log("Error in auth/player-exists:" + e.ToString());
            return false;
        }
    }

#if !UNITY_WEBGL
    private async Task<string> SDK_SignUp(string email, string password, string displayName)
    {
        try
        {
            if (await checkUsernameExist(displayName))
            {
                return "This display name is already taken.";
            }

            var userCredential = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            User = userCredential.User;
            profileInfo.email = User.Email;

            firebaseIdToken = await User.TokenAsync(true);
            // Note: Native SDK handles refresh token internally

            await AddNewPlayer(email, displayName);

            return "Registration successful!";
        }
        catch (FirebaseException firebaseEx)
        {
            Spinner.Instance.Hide();

            var authError = (AuthError)firebaseEx.ErrorCode;
            switch (authError)
            {
                case AuthError.EmailAlreadyInUse:
                    return "This email is already in use.";
                case AuthError.InvalidEmail:
                    return "The email address is invalid.";
                case AuthError.WeakPassword:
                    return "The password is too weak (min 6 characters).";
                default:
                    return $"Firebase Auth Error: {firebaseEx.Message}";
            }
        }
        catch (Exception ex)
        {
            Spinner.Instance.Hide();
            return $"Other Error: {ex.Message}";
        }
    }
#endif

#if UNITY_WEBGL
    private async Task<string> WebGL_SignUp(string email, string password, string displayName)
    {
        try {
            if (await checkUsernameExist(displayName))
            {
                return "This display name is already taken.";
            }

            string url = Constants.firebaseSignUpUrl + Constants.firebaseWebApiKey;

            var payload = new FirebaseSignPayload
            {
                email = email,
                password = password,
                returnSecureToken = true
            };

            var json = Utils.ObjectToJson<FirebaseSignPayload>(payload);

            var result = await requestManager.FirebasePost(url, json);

            // Check if we got a valid token (primary indicator of success)
            if (string.IsNullOrEmpty(result.idToken))
            {
                // If no token, check for error message
                if (result.error != null && !string.IsNullOrEmpty(result.error.message))
                {
                    Debug.Log($"Firebase sign-up error: {result.error.message}");
                    return result.error.message;
                }
                Debug.Log("Firebase sign-up failed: No idToken in response");
                return "Account creation failed: No authentication token received.";
            }

            // Success - we have a valid token
            firebaseIdToken = result.idToken;
            firebaseRefreshToken = result.refreshToken;
            profileInfo.email = result.email;

            await AddNewPlayer(email, displayName);

            return "Registration successful!";
        }
        catch (Exception e)
        {
            return ("WebGL SignUp error: " + e.ToString());
        }
    }

    private async Task<string> WebGL_SendPasswordReset(string email)
    {
        string url = Constants.firebaseForgotUrl + Constants.firebaseWebApiKey;

        var payload = new FirebasePasswordResetPayload
        {
            requestType = "PASSWORD_RESET",
            email = email
        };

        var json = Utils.ObjectToJson(payload);

        var result = await requestManager.FirebasePost(url, json);

        // Firebase password reset success returns {"email": "..."} with no error field
        // For password reset, success is indicated by presence of email field, not absence of error
        // Check for email field first as the primary success indicator
        if (!string.IsNullOrEmpty(result.email))
        {
            Debug.Log($"✅ Password reset email sent successfully to {result.email}");
            return "Check your inbox or spam folder.";
        }

        // If no email field, check for error
        if (result.error != null)
        {
            Debug.LogError($"❌ Password reset error: {result.error.message} (code: {result.error.code})");
            return result.error.message ?? "Password reset failed.";
        }

        // Edge case: no email and no error (shouldn't happen)
        Debug.LogWarning("⚠️ Password reset response missing both email and error fields");
        return "Password reset failed: Invalid response from server.";
    }

    private async Task<string> WebGL_SignIn(string email, string password)
    {
        string url = Constants.firebaseSignInUrl + Constants.firebaseWebApiKey;

        var payload = new FirebaseSignPayload
        {
            email = email,
            password = password,
            returnSecureToken = true
        };

        var json = Utils.ObjectToJson<FirebaseSignPayload>(payload);
        var result = await requestManager.FirebasePost(url, json);

        // Check if we got a valid token (primary indicator of success)
        if (string.IsNullOrEmpty(result.idToken))
        {
            // If no token, check for error message
            if (result.error != null && !string.IsNullOrEmpty(result.error.message))
            {
                Debug.Log($"Firebase sign-in error: {result.error.message}");
                return result.error.message;
            }
            Debug.Log("Firebase sign-in failed: No idToken in response");
            return "Login failed: No authentication token received.";
        }

        // Success - we have a valid token

        firebaseIdToken = result.idToken;
        firebaseRefreshToken = result.refreshToken;
        profileInfo.email = result.email;

        // Wait for profile info to load completely
        bool profileLoaded = await GetProfileInfo();
        if (!profileLoaded)
        {
            return "Login successful but failed to load profile. Please try again.";
        }

        return "Login successful!";
    }


#endif

    public async Task<string> GetIdToken()
    {
#if UNITY_WEBGL
        return await Task.FromResult(firebaseIdToken);
#else
        if (User == null) return null;

        // TokenAsync(false) returns cached token if valid, or auto-refreshes if expired
        // Update firebaseIdToken to keep cache in sync (token may have been auto-refreshed)
        firebaseIdToken = await User.TokenAsync(false);
        return firebaseIdToken;
#endif
    }

    /// <summary>
    /// Force refresh the Firebase ID token. Use this when token is about to expire or has expired.
    /// </summary>
    public async Task<string> RefreshIdToken()
    {
#if UNITY_WEBGL
        // For WebGL, use refresh token to get new ID token
        if (string.IsNullOrEmpty(firebaseRefreshToken))
        {
            Debug.LogWarning("No refresh token available for WebGL. User needs to sign in again.");
            return null;
        }

        string url = $"https://securetoken.googleapis.com/v1/token?key={Constants.firebaseWebApiKey}";
        
        // Firebase token refresh endpoint expects form-encoded data, not JSON
        string formData = $"grant_type=refresh_token&refresh_token={UnityWebRequest.EscapeURL(firebaseRefreshToken)}";
        
        // Use UnityWebRequest directly for refresh token endpoint
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(formData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"Token refresh failed: {request.downloadHandler.text}");
                return null;
            }

            // Parse response - Firebase returns id_token, refresh_token, etc.
            var responseJson = request.downloadHandler.text;
            try
            {
                var response = Utils.JsonToObject<FirebaseRefreshResponse>(responseJson);
                
                if (string.IsNullOrEmpty(response.id_token))
                {
                    Debug.Log("Token refresh response missing id_token");
                    return null;
                }
                
                firebaseIdToken = response.id_token;
                if (!string.IsNullOrEmpty(response.refresh_token))
                {
                    firebaseRefreshToken = response.refresh_token;
                }

                Debug.Log("✅ WebGL token refreshed successfully");
                return firebaseIdToken;
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to parse token refresh response: {ex.Message}. Response: {responseJson}");
                return null;
            }
        }
#else
        if (User == null) return null;

        try
        {
            // Force refresh by passing true
            // But we store it for consistency and potential debugging
            firebaseIdToken = await User.TokenAsync(true);
            return firebaseIdToken;
        }
        catch (Exception ex)
        {
            Debug.Log($"Token refresh failed: {ex.Message}");
            return null;
        }
#endif
    }

    /// <summary>
    /// Retrieves profile information from the server.
    /// </summary>
    /// <returns>True if profile was loaded successfully, false otherwise.</returns>
    public async Task<bool> GetProfileInfo()
    {
        // Validate email is set
        if (string.IsNullOrEmpty(profileInfo.email))
        {
            Debug.LogError("GetProfileInfo failed: email is not set in profileInfo");
            return false;
        }

        string email = profileInfo.email;

        // Get token
        string token = await GetIdToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("GetProfileInfo failed: Could not retrieve authentication token");
            return false;
        }

        string url = $"auth/profile?email={email}";
        var result = await requestManager.GetRequestAsync(url, token);

        // Check if request failed
        if (string.IsNullOrEmpty(result))
        {
            Debug.LogError("GetProfileInfo failed: Server request returned null or empty response");
            return false;
        }

        try
        {
            // Check if server returned null (player not found in Firestore)
            if (result.Trim() == "null" || result.Trim() == "{}")
            {
                Debug.LogWarning($"GetProfileInfo: No profile found for email {email}. Player may need to be created in Firestore.");
                return false;
            }

            profileInfo = Utils.JsonToObject<ProfileInfo>(result);
            
            // Validate that profile was parsed correctly
            if (profileInfo == null || string.IsNullOrEmpty(profileInfo.playerId))
            {
                Debug.LogError($"GetProfileInfo failed: Invalid profile data received. Response: {result}");
                return false;
            }

            Debug.Log($"✅ Profile loaded successfully for {email}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"GetProfileInfo failed: JSON parsing error - {ex.Message}. Response: {result}");
            return false;
        }
    }

    public async Task<string> SendPasswordResetRequest(string email)
    {
#if UNITY_WEBGL
        return await WebGL_SendPasswordReset(email);
#else
        return await SDK_SendPasswordReset(email);
#endif
    }

#if !UNITY_WEBGL
    private async Task<string> SDK_SendPasswordReset(string email)
    {
        try
        {
            await auth.SendPasswordResetEmailAsync(email);
            return "Check your inbox or spam folder.";
        }
        catch (FirebaseException fe)
        {
            var errorCode = fe.ErrorCode;

            switch (errorCode)
            {
                case (int)AuthError.InvalidEmail:
                    return "Invalid email address.";
                case (int)AuthError.UserNotFound:
                    return "No user found with this email.";
                default:
                    return "Reset failed. Try again later.";
            }
        }
        catch (Exception)
        {
            return "Unexpected error. Try again.";
        }
    }
#endif

    public async void GetRewardToken()
    {
        ParamInfo paramInfo = new ParamInfo
        {
            param = profileInfo.playerId
        };

        string jsonData = Utils.ObjectToJson(paramInfo);
        await requestManager.PostRequestAsync("auth/reward-token", jsonData, firebaseIdToken);
    }

    //public void SignOut()
    //{
    //    auth.SignOut();
    //    Debug.Log("👋 User signed out");
    //}
}
