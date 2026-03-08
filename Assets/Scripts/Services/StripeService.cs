#if UNITY_WEBGL || (!UNITY_IOS && !UNITY_ANDROID)
using System;
using UnityEngine;

// Handles Stripe payments for WebGL and desktop platforms
public class StripeService : MonoBehaviour, IPaymentService
{
    public static StripeService Instance { get; private set; }

    private RequestManager requestManager;
    private AuthManager authManager;

    public bool IsInitialized => true;
    public bool IsAvailable => true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize(Action<bool> onInitialized)
    {
        requestManager = RequestManager.Instance;
        authManager = AuthManager.Instance;

        if (requestManager == null || authManager == null)
        {
            Debug.LogError("StripeService: RequestManager or AuthManager not found");
            onInitialized?.Invoke(false);
            return;
        }

        onInitialized?.Invoke(true);
    }

    public async void PurchaseTokens(int tokenAmount, int price, Action onSuccess, Action<string> onFailed)
    {
        if (requestManager == null)
        {
            requestManager = RequestManager.Instance;
            if (requestManager == null)
            {
                onFailed?.Invoke("Request manager not available.");
                return;
            }
        }

        if (authManager == null || authManager.profileInfo == null)
        {
            onFailed?.Invoke("Authentication required. Please log in.");
            return;
        }

        if (string.IsNullOrEmpty(authManager.profileInfo.email) || string.IsNullOrEmpty(authManager.profileInfo.playerId))
        {
            onFailed?.Invoke("Profile information incomplete.");
            return;
        }

        try
        {
            string url = "auth/buy-token";
            string token = await authManager.GetIdToken();
            string email = authManager.profileInfo.email;
            string playerId = authManager.profileInfo.playerId;

            BuyTokenInfo buyInfo = new BuyTokenInfo
            {
                email = email,
                playerId = playerId,
                tokenAmount = tokenAmount,
                price = price
            };

            string infoJson = Utils.ObjectToJson(buyInfo);
            string resultJson = await requestManager.PostRequestAsync(url, infoJson, token);
            ResultInfo resultInfo = Utils.JsonToObject<ResultInfo>(resultJson);

            if (resultInfo == null)
            {
                onFailed?.Invoke("Payment processing failed. Please try again.");
                return;
            }

            string sessionUrl = resultInfo.result;
            if (sessionUrl != null)
            {
                // Open Stripe checkout page
                Application.OpenURL(sessionUrl);
                // Success is handled by webhook when payment completes
            }
            else
            {
                onFailed?.Invoke("Failed to create payment session. Please try again.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"StripeService: Purchase failed - {e.Message}");
            onFailed?.Invoke($"Payment failed: {e.Message}");
        }
    }
}
#endif

