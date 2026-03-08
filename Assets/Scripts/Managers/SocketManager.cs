using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if !UNITY_WEBGL || UNITY_EDITOR
using SocketIOClient;
#endif

public class SocketManager : MonoBehaviour
{
    public static SocketManager Instance { get; private set; }

    private string serverUrl = Constants.SERVER_WS_URL;

    private AuthManager authManager;
    private string currentSocketId;

    // Token refresh management
    private Coroutine tokenRefreshCoroutine = null;
    private const float TOKEN_REFRESH_INTERVAL = 50f * 60f; // Refresh every 50 minutes (tokens expire after 1 hour)

    // Reconnection management with exponential backoff (available for all platforms)
    private int reconnectAttempts = 0;
    private const int MAX_RECONNECT_ATTEMPTS = 10;
    private const float INITIAL_RECONNECT_DELAY = 2f; // Start with 2 seconds
    private const float MAX_RECONNECT_DELAY = 60f; // Max 60 seconds
    private bool shouldReconnect = true; // Flag to prevent reconnection on app quit (available for all platforms)

    // =============================
    //    Native SocketIOClient
    // =============================
#if !UNITY_WEBGL || UNITY_EDITOR
    public SocketIO socket { get; private set; }
    private bool isConnecting = false;
    private bool isConnected => socket != null && socket.Connected;
#endif

    // Store token for reconnection (available for all platforms)
    private string pendingAuthToken = null;

    // ======================================================
    //   HELPER METHODS (Reduce Duplication)
    // ======================================================

    /// <summary>
    /// Get authentication token - checks pending token first, then refreshes, then falls back to GetIdToken
    /// This pattern is used in multiple places, so extracted to reduce duplication
    /// </summary>
    private async Task<string> GetAuthTokenAsync()
    {
        // Use pending token if available (from background refresh)
        if (!string.IsNullOrEmpty(pendingAuthToken))
        {
            string token = pendingAuthToken;
            pendingAuthToken = null; // Clear after using
            return token;
        }

        // Try to refresh token first (preferred method)
        if (authManager != null)
        {
            string refreshedToken = await authManager.RefreshIdToken();
            if (!string.IsNullOrEmpty(refreshedToken))
            {
                return refreshedToken;
            }

            // Fallback to GetIdToken if refresh token is not available
            return await authManager.GetIdToken();
        }

        return null;
    }

    /// <summary>
    /// Refresh token and send to server via refresh_token event (without disconnecting)
    /// Returns true if successful, false otherwise
    /// </summary>
    private async Task<bool> RefreshTokenAndEmitAsync()
    {
        try
        {
            if (authManager == null) return false;

            string newToken = await authManager.RefreshIdToken();
            if (string.IsNullOrEmpty(newToken))
            {
                return false;
            }

            // Send new token to server without disconnecting
            if (IsConnected())
            {
                Emit("refresh_token", new { token = newToken });
                Debug.Log("🔄 Token refresh request sent to server (no disconnect)");
                return true;
            }
            else
            {
                // Store token for next connection if not connected
                pendingAuthToken = newToken;
                Debug.Log("✅ Token refreshed (will use on next connection)");
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"⚠️ Token refresh failed: {ex.Message}");
            return false;
        }
    }

    // =============================
    //    WebGL Flags
    // =============================
#if UNITY_WEBGL && !UNITY_EDITOR
    private bool webglConnected = false;
    private bool webglConnecting = false;

    [DllImport("__Internal")] private static extern void SocketIO_WebGLConnect(string url, string goName, string token);
    [DllImport("__Internal")] private static extern void SocketIO_WebGLDisconnect();
    [DllImport("__Internal")] private static extern void SocketIO_WebGLEmit(string eventName, string payloadJson);
#endif

    // =============================
    //   Event Routing Table
    // =============================
    private Dictionary<string, Action<string>> handlers =
        new Dictionary<string, Action<string>>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        serverUrl = Debug.isDebugBuild ? Constants.LOCAL_WS_URL : Constants.SERVER_WS_URL;

#if !UNITY_WEBGL || UNITY_EDITOR
        // Socket will be created with auth token in ConnectAsync
        socket = null;
#endif

        authManager = AuthManager.Instance;
    }



    // ======================================================
    //   ENTRY POINT TO START SOCKET CONNECTION
    // ======================================================
    
    // Public method to check if socket is connected or connecting
    public bool IsConnected()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return webglConnected || webglConnecting;
#else
        return isConnected || isConnecting;
#endif
    }
    
    public void installSocket()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (webglConnected || webglConnecting)
            return;

        webglConnecting = true;

        // Get Firebase ID token for authentication asynchronously
        _ = GetTokenAndConnectWebGL();

#else
        if (isConnecting || isConnected) return;

        // Re-enable reconnection when explicitly installing socket
        shouldReconnect = true;

        // Get token before connecting
        _ = ConnectAsync();
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private async Task GetTokenAndConnectWebGL()
    {
        try
        {
            string token = await GetAuthTokenAsync();
            
            if (string.IsNullOrEmpty(token))
            {
                Debug.Log("[WebGL] Cannot connect: No Firebase token available");
                webglConnecting = false;
                return;
            }

            Debug.Log($"[WebGL] Connecting to: {serverUrl}");
            SocketIO_WebGLConnect(serverUrl, gameObject.name, token);
        }
        catch (Exception ex)
        {
            Debug.Log($"[WebGL] Error getting token: {ex.Message}");
            webglConnecting = false;
        }
    }
#endif


    // ======================================================
    //    NATIVE (Windows, Android, iOS)
    // ======================================================
#if !UNITY_WEBGL || UNITY_EDITOR

    private async Task ConnectAsync()
    {
        if (isConnected || isConnecting) return;

        // Don't connect if reconnection is disabled
        if (!shouldReconnect) return;

        isConnecting = true;

        try
        {
            // Get Firebase ID token for authentication
            string token = await GetAuthTokenAsync();
            
            if (string.IsNullOrEmpty(token))
            {
                Debug.Log("[Native] Cannot connect: No Firebase token available");
                isConnecting = false;
                return;
            }

            // Dispose old socket if exists
            if (socket != null)
            {
                socket.OnConnected -= OnConnected;
                socket.OnDisconnected -= OnDisconnected;
                // Note: OnAny is a method, not an event, so we can't unsubscribe
                // Disposing the socket will clean up all handlers
                try { socket.Dispose(); } catch { }
            }

            // Create new socket with authentication token
            // SocketIOUnity supports auth via SocketIOOptions
            var options = new SocketIOOptions
            {
                Auth = new Dictionary<string, string>
                {
                    { "token", token }
                }
            };

            socket = new SocketIO(serverUrl, options);
            socket.OnConnected += OnConnected;
            socket.OnDisconnected += OnDisconnected;
            socket.OnAny(OnAnyNative);
            
            // Register error handler
            On("error", HandleServerError);

            Debug.Log("[Native] Connecting with authentication…");
            await socket.ConnectAsync();
            // Note: isConnecting will be reset in OnConnected() when connection succeeds
            // If connection fails, it will throw and go to catch block
        }
        catch (Exception ex)
        {
            Debug.Log("Native connect failed: " + ex.Message);
            // Reset connecting flag on failure
            isConnecting = false;

            // Only attempt reconnect if allowed and GameObject is still active
            if (shouldReconnect && gameObject != null && gameObject.activeInHierarchy)
            {
                _ = ReconnectDelay(5f);
            }
        }
        // Don't reset isConnecting here - let OnConnected() handle it for successful connections
    }

    private async Task ReconnectDelay(float seconds)
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds));

        // Check if we should still reconnect (app might have quit, GameObject destroyed, etc.)
        if (!shouldReconnect || gameObject == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        // Only reconnect if not already connected or connecting
        // ConnectAsync() will check this too, but we check here to avoid unnecessary token refresh
        if (!isConnected && !isConnecting)
        {
            Debug.Log("🔄 Attempting reconnect…");
            // Get fresh token before reconnecting (uses GetAuthTokenAsync which handles refresh/fallback)
            // Store token for ConnectAsync to use (GetAuthTokenAsync consumes pendingAuthToken if it exists)
            try
            {
                string token = await GetAuthTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    pendingAuthToken = token; // Store for ConnectAsync
                }
                else
                {
                    Debug.LogWarning("⚠️ Cannot refresh token: No token available");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to refresh token for reconnect: {ex.Message}");
            }
            await ConnectAsync();
        }
    }

    private void OnAnyNative(string eventName, SocketIOResponse response)
    {
        string json = response.GetValue<string>();
        DispatchEvent(eventName, json);
    }

    private async void OnConnected(object sender, EventArgs e)
    {
        Debug.Log("✅ Native Socket connected.");

        // Reset connection state flags
        isConnecting = false;
        
        // Reset reconnect attempts on successful connection
        reconnectAttempts = 0;

        // Wait for socket.Id with timeout and safety checks
        int attempts = 0;
        const int maxAttempts = 100; // 5 seconds max wait (100 * 50ms)

        while (string.IsNullOrEmpty(socket.Id) && attempts < maxAttempts)
        {
            if (!shouldReconnect || gameObject == null || !gameObject.activeInHierarchy)
            {
                return;
            }
            await Task.Delay(50);
            attempts++;
        }

        if (!string.IsNullOrEmpty(socket.Id))
        {
            currentSocketId = socket.Id;
            // Socket registration is now handled automatically by the backend on connection

            // Start periodic token refresh
            StartTokenRefresh();
            
            // Setup heartbeat
            SetupHeartbeat();
        }
        else
        {
            Debug.LogWarning("Socket connected but ID not received");
        }
    }

    private void OnDisconnected(object sender, string reason)
    {
        Debug.Log("⚠️ Native Socket disconnected: " + reason);

        // Stop token refresh when disconnected
        StopTokenRefresh();
        
        // Reset reconnect attempts on disconnect (consistent with WebGL)
        reconnectAttempts = 0;

        // Check if disconnection was due to authentication error
        if (reason.Contains("auth") || reason.Contains("token") || reason.Contains("unauthorized"))
        {
            Debug.Log("🔄 Authentication error detected on disconnect. Reconnecting with new token...");
            // Just reconnect with new token - don't call RefreshTokenAndReconnect() which would disconnect again
            // The server's disconnect handler will detect quick reconnect and skip game removal
            _ = ReconnectWithNewToken();
            return;
        }

        // Only attempt reconnect if app is still running and reconnection is allowed
        if (shouldReconnect && gameObject != null && gameObject.activeInHierarchy)
        {
            _ = ReconnectWithBackoff();
        }
    }
#endif

    // Reconnection with exponential backoff (available for all platforms)
    private async Task ReconnectWithBackoff()
    {
        // Calculate exponential backoff delay
        float delay = Mathf.Min(
            INITIAL_RECONNECT_DELAY * Mathf.Pow(2f, reconnectAttempts),
            MAX_RECONNECT_DELAY
        );

        reconnectAttempts++;

        if (reconnectAttempts > MAX_RECONNECT_ATTEMPTS)
        {
            Debug.LogWarning($"❌ Max reconnection attempts ({MAX_RECONNECT_ATTEMPTS}) reached. Stopping reconnection attempts.");
            return;
        }

        Debug.Log($"🔄 Reconnecting in {delay} seconds (attempt {reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS})...");
        await Task.Delay(TimeSpan.FromSeconds(delay));

        // Check if we should still reconnect (app might have quit, GameObject destroyed, etc.)
        if (!shouldReconnect || gameObject == null || !gameObject.activeInHierarchy)
        {
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL reconnection
        if (!webglConnected && !webglConnecting)
        {
            Debug.Log("[WebGL] Attempting reconnect…");
            try
            {
                string token = await GetAuthTokenAsync();
                
                if (!string.IsNullOrEmpty(token))
                {
                    webglConnecting = true;
                    SocketIO_WebGLConnect(serverUrl, gameObject.name, token);
                }
                else
                {
                    Debug.LogWarning("[WebGL] Token refresh failed. Cannot reconnect without valid token.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebGL] Failed to refresh token for reconnect: {ex.Message}");
            }
        }
#else
        // Native reconnection
        // Only reconnect if not already connected or connecting
        // ConnectAsync() will check this too, but we check here to avoid unnecessary token refresh
        if (!isConnected && !isConnecting)
        {
            Debug.Log("🔄 Attempting reconnect…");
            // Get fresh token before reconnecting
            // Store token for ConnectAsync to use (GetAuthTokenAsync consumes pendingAuthToken if it exists)
            try
            {
                string token = await GetAuthTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    pendingAuthToken = token; // Store for ConnectAsync
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to refresh token for reconnect: {ex.Message}");
            }
            await ConnectAsync();
        }
#endif
    }



    // ======================================================
    //    EMIT (Native + WebGL unified)
    // ======================================================
    public void Emit(string eventName, object payload)
    {
        string json = payload != null ? Utils.ObjectToJson(payload) : "";

#if UNITY_WEBGL && !UNITY_EDITOR
        if (!webglConnected)
        {
            Debug.LogWarning($"[WebGL] Cannot emit {eventName}, not connected.");
            return;
        }

        try { SocketIO_WebGLEmit(eventName, json); }
        catch (Exception ex)
        {
            Debug.Log("[WebGL Emit Error] " + ex.Message);
        }

#else
        try { _ = socket.EmitAsync(eventName, json); }
        catch (Exception ex)
        {
            Debug.Log("[Native Emit Error] " + ex.Message);
        }
#endif
    }



    // ======================================================
    // EVENT SUBSCRIPTION (Same API for all platforms)
    // ======================================================
    public void On(string eventName, Action<string> callback)
    {
        handlers[eventName] = callback;
    }

    public void Off(string eventName)
    {
        handlers.Remove(eventName);
    }

    // Setup event handlers (ping-pong removed - using Socket.IO built-in heartbeat)
    private void SetupHeartbeat()
    {
        // Listen for token refresh confirmation from server
        On("token_refreshed", (string json) => {
            try
            {
                TokenRefreshResponse response = Utils.JsonToObject<TokenRefreshResponse>(json);
                if (response.success)
                {
                    Debug.Log("✅ Server confirmed token refresh (no disconnect needed)");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Server rejected token refresh: {response.error}. Will reconnect if needed.");
                    // If server rejects, we'll reconnect on next auth error
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing token refresh response: {ex.Message}");
            }
        });
    }

    private void DispatchEvent(string eventName, string json)
    {
        // Skip Socket.IO internal events (ping/pong are handled automatically by Socket.IO)
        if (eventName == "ping" || eventName == "pong")
        {
            return; // Socket.IO handles these internally
        }

        if (handlers.TryGetValue(eventName, out var callback))
        {
            MainThread.Run(() => callback(json));
        }
    }

    // Handle server error events
    private void HandleServerError(string json)
    {
        try
        {
            var errorData = JsonUtility.FromJson<ServerError>(json);
            Debug.LogError($"❌ Server Error: {errorData.message} (Code: {errorData.code})");

            // Handle specific error codes
            if (errorData.code == "AUTH_FAILED" || errorData.code == "UNAUTHORIZED")
            {
                Debug.Log($"🔄 Authentication error ({errorData.code}). Attempting token refresh without disconnect...");
                // Try to refresh token without disconnecting first
                _ = RefreshTokenOnError();
            }
            // You can add more error handling here (e.g., show UI alerts)
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing server error: {ex.Message}, Raw JSON: {json}");
        }
    }

    // Refresh token on auth error without disconnecting
    private async Task RefreshTokenOnError()
    {
        try
        {
            // Check if still connected
            if (!IsConnected())
            {
                Debug.Log("⚠️ Not connected, will reconnect with new token");
                _ = RefreshTokenAndReconnect();
                return;
            }

            // Try to refresh token and send via refresh_token event
            bool success = await RefreshTokenAndEmitAsync();
            
            if (success)
            {
                // Wait a moment to see if server accepts the token
                await Task.Delay(2000);
                
                // If still connected, token refresh was successful
                if (IsConnected())
                {
                    Debug.Log("✅ Token refreshed successfully without disconnect");
                }
                else
                {
                    // Server rejected or connection lost, need to reconnect
                    Debug.LogWarning("⚠️ Token refresh failed or connection lost, reconnecting...");
                    _ = RefreshTokenAndReconnect();
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Token refresh failed, reconnecting...");
                _ = RefreshTokenAndReconnect();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"⚠️ Token refresh on error failed: {ex.Message}. Reconnecting...");
            _ = RefreshTokenAndReconnect();
        }
    }

    // ======================================================
    //    WEBGL CALLBACKS (JS → C#)
    // ======================================================
#if UNITY_WEBGL && !UNITY_EDITOR

    public void OnWebGLConnected(string socketId)
    {
        Debug.Log("[WebGL] Connected: " + socketId);

        // Reset reconnect attempts on successful connection
        reconnectAttempts = 0;

        currentSocketId = socketId;
        webglConnected = true;
        webglConnecting = false;

        // Register error handler
        On("error", HandleServerError);

        // Socket registration is now handled automatically by the backend on connection
        
        // Start periodic token refresh
        StartTokenRefresh();
        
        // Setup heartbeat
        SetupHeartbeat();
    }

    public void OnWebGLDisconnected(string reason)
    {
        Debug.Log("[WebGL] Disconnected: " + reason);
        webglConnected = false;
        webglConnecting = false;
        
        // Stop token refresh when disconnected
        StopTokenRefresh();
        
        // Reset reconnect attempts on disconnect
        reconnectAttempts = 0;

        // Check if disconnection was due to authentication error
        if (reason.Contains("auth") || reason.Contains("token") || reason.Contains("unauthorized"))
        {
            Debug.Log("[WebGL] Authentication error detected. Reconnecting with new token...");
            // Just reconnect with new token - don't call RefreshTokenAndReconnect() which would disconnect again
            // The server's disconnect handler will detect quick reconnect and skip game removal
            _ = ReconnectWithNewToken();
        }
        else if (shouldReconnect && gameObject != null && gameObject.activeInHierarchy)
        {
            _ = ReconnectWithBackoff();
        }
    }

    /// <summary>
    /// Called from JS onAny forwarding:
    /// payload = { event:"name", data:"json-string" }
    /// </summary>
    public void OnWebGLEvent(string wrapperJson)
    {
        var wrapper = JsonUtility.FromJson<WebGLEventWrapper>(wrapperJson);
        DispatchEvent(wrapper.eventName, wrapper.data);
    }
#endif



    // ======================================================
    //   COMMON: REGISTER SOCKET ID FOR CURRENT USER
    // ======================================================

    // ======================================================
    //   TOKEN REFRESH MANAGEMENT
    // ======================================================
    private void StartTokenRefresh()
    {
        // Don't start if already running (prevents duplicate coroutines)
        if (tokenRefreshCoroutine != null)
        {
            return;
        }

        tokenRefreshCoroutine = StartCoroutine(TokenRefreshCoroutine());
        Debug.Log("🔄 Token refresh started (every 50 minutes)");
    }

    private void StopTokenRefresh()
    {
        if (tokenRefreshCoroutine != null)
        {
            StopCoroutine(tokenRefreshCoroutine);
            tokenRefreshCoroutine = null;
        }
    }

    private IEnumerator TokenRefreshCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(TOKEN_REFRESH_INTERVAL);

            // Check if we should continue
            if (!shouldReconnect || gameObject == null || !gameObject.activeInHierarchy)
            {
                break;
            }

            // Check if still connected
            bool connected = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            connected = webglConnected;
#else
            connected = isConnected;
#endif

            if (connected)
            {
                // Only refresh token without disconnecting - Socket.IO will handle auth on next request
                // We'll only reconnect if we get an authentication error from the server
                Debug.Log("🔄 Refreshing Firebase token (background refresh, no reconnect)...");
                _ = RefreshTokenAndEmitAsync();
            }
        }
    }


    /// <summary>
    /// Reconnect with a new token (used when already disconnected due to auth error)
    /// This does NOT disconnect - it just reconnects with a fresh token
    /// </summary>
    private async Task ReconnectWithNewToken()
    {
        try
        {
            // Get fresh token for reconnection
            string newToken = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(newToken))
            {
                Debug.LogWarning("⚠️ Token refresh failed. Will attempt to reconnect on next error.");
                return;
            }

            Debug.Log("✅ Token refreshed, reconnecting...");

#if UNITY_WEBGL && !UNITY_EDITOR
            // For WebGL, reconnect with new token (already disconnected, so just connect)
            if (!webglConnecting && !webglConnected)
            {
                webglConnecting = true;
                SocketIO_WebGLConnect(serverUrl, gameObject.name, newToken);
            }
#else
            // For native, reconnect with new token (already disconnected, so just connect)
            if (!isConnecting && !isConnected)
            {
                pendingAuthToken = newToken;
                await ConnectAsync();
            }
            else
            {
                // Already connecting or connected, just store token for next connection
                pendingAuthToken = newToken;
            }
#endif
        }
        catch (Exception ex)
        {
            Debug.Log($"❌ Error reconnecting with new token: {ex.Message}");
        }
    }

    /// <summary>
    /// Refresh token and reconnect - tries refresh_token event first, then disconnects/reconnects if needed
    /// This is used when we're proactively refreshing (e.g., from error handler while still connected)
    /// </summary>
    private async Task RefreshTokenAndReconnect()
    {
        try
        {
            // First, try to refresh token without disconnecting (if connected)
            if (IsConnected())
            {
                Debug.Log("🔄 Attempting token refresh without disconnect...");
                bool success = await RefreshTokenAndEmitAsync();
                
                if (success)
                {
                    // Wait a moment to see if server accepts the token
                    await Task.Delay(2000);
                    
                    // Check if still connected (server accepted the token)
                    if (IsConnected())
                    {
                        Debug.Log("✅ Token refreshed successfully without disconnect");
                        return; // Success - no need to reconnect
                    }
                }
                
                // Server rejected or connection lost, need to reconnect
                Debug.LogWarning("⚠️ Token refresh via event failed, reconnecting...");
            }

            // Get fresh token for reconnection
            string newToken = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(newToken))
            {
                Debug.LogWarning("⚠️ Token refresh failed. Will attempt to reconnect on next error.");
                return;
            }

            Debug.Log("✅ Token refreshed successfully");

            // Fallback: Disconnect and reconnect (only if not connected or refresh_token event failed)
#if UNITY_WEBGL && !UNITY_EDITOR
            // For WebGL, we need to reconnect with new token
            if (!webglConnecting)
            {
                webglConnecting = true;
                webglConnected = false;
                SocketIO_WebGLDisconnect();
                
                // Wait a moment before reconnecting
                await Task.Delay(1000);
                
                SocketIO_WebGLConnect(serverUrl, gameObject.name, newToken);
            }
#else
            // For native, disconnect and reconnect with new token
            if (!isConnecting)
            {
                Debug.Log("🔄 Reconnecting with refreshed token...");
                // Disconnect and reconnect with new token
                try
                {
                    if (socket != null && socket.Connected)
                    {
                        socket.OnConnected -= OnConnected;
                        socket.OnDisconnected -= OnDisconnected;
                        try { socket.Dispose(); } catch { }
                        socket = null;
                    }
                    // Reset connection state flags
                    isConnecting = false;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error disconnecting for token refresh: {ex.Message}");
                    isConnecting = false; // Ensure flag is reset even on error
                }
                
                // Wait a moment before reconnecting
                await Task.Delay(500);
                
                // Reconnect with new token
                pendingAuthToken = newToken;
                await ConnectAsync();
            }
            else
            {
                // Already connecting, just store token for next connection
                pendingAuthToken = newToken;
            }
#endif
        }
        catch (Exception ex)
        {
            Debug.Log($"❌ Error refreshing token: {ex.Message}");
        }
    }


    private void OnApplicationQuit()
    {
        // Stop reconnection attempts when app is quitting
        shouldReconnect = false;
        DisconnectSocket();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // When app is paused (e.g., minimized on mobile), stop reconnection
        if (pauseStatus)
        {
            shouldReconnect = false;
            // Disconnect socket when app is paused to save resources
            DisconnectSocket();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // When app regains focus (e.g., brought back to foreground on mobile), reconnect
        if (hasFocus)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!webglConnected && shouldReconnect && gameObject != null && gameObject.activeInHierarchy)
            {
                Debug.Log("[WebGL] App regained focus, reconnecting socket...");
                shouldReconnect = true;
                _ = GetTokenAndConnectWebGL();
            }
#else
            if (!isConnected && shouldReconnect && gameObject != null && gameObject.activeInHierarchy)
            {
                Debug.Log("🔄 App regained focus, reconnecting socket...");
                shouldReconnect = true;
                _ = ConnectAsync();
            }
#endif
        }
        else
        {
            // App lost focus, stop reconnection
            shouldReconnect = false;
        }
    }

    private void OnDestroy()
    {
        // Stop reconnection when GameObject is being destroyed
        shouldReconnect = false;
        DisconnectSocket();
    }

    private void DisconnectSocket()
    {
        // Stop token refresh
        StopTokenRefresh();

#if UNITY_WEBGL && !UNITY_EDITOR
        try 
        { 
            SocketIO_WebGLDisconnect(); 
            webglConnected = false;
            webglConnecting = false;
        } 
        catch { }
#else
        if (socket != null && socket.Connected)
        {
            _ = socket.DisconnectAsync();
        }
#endif
    }
}
