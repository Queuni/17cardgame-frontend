#if UNITY_IOS || UNITY_ANDROID
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System.Threading.Tasks;

// Handles in-app purchases for iOS and Android
// Note: Uses the old IAP API which still works fine, just shows warnings
#pragma warning disable CS0618
public class IAPService : MonoBehaviour, IDetailedStoreListener, IPaymentService
{
    public static IAPService Instance { get; private set; }

    private IStoreController storeController;
    private IExtensionProvider extensionProvider;
    private bool isInitialized = false;

    // Map token amounts to product IDs
    // Change "com.yourcompany.cardgame" to match your actual bundle ID
    private readonly Dictionary<int, string> productIdMap = new Dictionary<int, string>
    {
        { 20, "com.belleviewbestllc.seventeencard.tokens_20" },
        { 50, "com.belleviewbestllc.seventeencard.tokens_50" },
        { 100, "com.belleviewbestllc.seventeencard.tokens_100" }
    };

    // Reverse map to get token amount from product ID
    private readonly Dictionary<string, int> tokenAmountMap = new Dictionary<string, int>();

    // Callbacks for current purchase attempt
    private Action currentPurchaseSuccess;
    private Action<string> currentPurchaseFailed;

    public bool IsInitialized => isInitialized;
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

        // Create reverse lookup for product IDs
        foreach (var kvp in productIdMap)
        {
            tokenAmountMap[kvp.Value] = kvp.Key;
        }
    }

    public void Initialize(Action<bool> onInitialized)
    {
        if (isInitialized)
        {
            onInitialized?.Invoke(true);
            return;
        }

        StartCoroutine(InitializeCoroutine(onInitialized));
    }

    private System.Collections.IEnumerator InitializeCoroutine(Action<bool> onInitialized)
    {
        // Start Unity services first
        var initTask = UnityServices.InitializeAsync();
        yield return new WaitUntil(() => initTask.IsCompleted);

        if (initTask.Exception != null)
        {
            Debug.LogError($"Failed to initialize Unity Services: {initTask.Exception}");
            onInitialized?.Invoke(false);
            yield break;
        }

        // Set up IAP products
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        foreach (var kvp in productIdMap)
        {
            builder.AddProduct(kvp.Value, ProductType.Consumable);
        }

        UnityPurchasing.Initialize(this, builder);
        yield return null;

        // Will finish in OnInitialized callback
        this.onInitializedCallback = onInitialized;
    }

    private Action<bool> onInitializedCallback;

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("IAP Service: Initialized successfully");
        storeController = controller;
        extensionProvider = extensions;
        isInitialized = true;
        onInitializedCallback?.Invoke(true);
        onInitializedCallback = null;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"IAP Service: Initialization failed - {error}");
        isInitialized = false;
        // Don't show error to user during initialization - it might be temporary
        // The purchase attempt will handle showing errors if needed
        onInitializedCallback?.Invoke(false);
        onInitializedCallback = null;
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"IAP Service: Initialization failed - {error}: {message}");
        isInitialized = false;
        // Don't show error to user during initialization - it might be temporary
        // The purchase attempt will handle showing errors if needed
        onInitializedCallback?.Invoke(false);
        onInitializedCallback = null;
    }

    public void PurchaseTokens(int tokenAmount, int price, Action onSuccess, Action<string> onFailed)
    {
        if (!isInitialized)
        {
            onFailed?.Invoke("IAP service not initialized. Please wait and try again.");
            return;
        }

        if (!productIdMap.TryGetValue(tokenAmount, out string productId))
        {
            onFailed?.Invoke($"No product configured for {tokenAmount} tokens.");
            return;
        }

        // Save callbacks to call when purchase finishes
        currentPurchaseSuccess = onSuccess;
        currentPurchaseFailed = onFailed;

        // Start the purchase flow
        storeController.InitiatePurchase(productId);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        Debug.Log($"Purchase successful: {args.purchasedProduct.definition.id}");

        if (!tokenAmountMap.TryGetValue(args.purchasedProduct.definition.id, out int tokenAmount))
        {
            Debug.LogError($"Unknown product ID: {args.purchasedProduct.definition.id}");
            currentPurchaseFailed?.Invoke("Unknown product. Please contact support.");
            currentPurchaseSuccess = null;
            currentPurchaseFailed = null;
            return PurchaseProcessingResult.Complete;
        }

        // Check receipt with server to make sure it's valid
        VerifyReceiptWithBackend(args.purchasedProduct.receipt, tokenAmount);

        return PurchaseProcessingResult.Complete;
    }

    private async void VerifyReceiptWithBackend(string receipt, int tokenAmount)
    {
        var authManager = AuthManager.Instance;
        if (authManager == null)
        {
            Debug.LogError("AuthManager not found");
            currentPurchaseFailed?.Invoke("Authentication failed. Please try again.");
            currentPurchaseSuccess = null;
            currentPurchaseFailed = null;
            return;
        }

        Spinner.Instance?.Show();

        try
        {
            // Get user's auth token
            string idToken = await authManager.GetIdToken();

            var requestManager = RequestManager.Instance;
            if (requestManager == null)
            {
                Debug.LogError("RequestManager not found");
                Spinner.Instance?.Hide();
                currentPurchaseFailed?.Invoke("Request manager not available.");
                currentPurchaseSuccess = null;
                currentPurchaseFailed = null;
                return;
            }

            // Make sure we have player info
            if (authManager.profileInfo == null || string.IsNullOrEmpty(authManager.profileInfo.playerId))
            {
                Debug.LogError("Player ID not available");
                Spinner.Instance?.Hide();
                currentPurchaseFailed?.Invoke("Player information not available.");
                currentPurchaseSuccess = null;
                currentPurchaseFailed = null;
                return;
            }

            // Send receipt to server for validation
            var verifyInfo = new IAPReceiptVerifyInfo
            {
                receipt = receipt,
                tokenAmount = tokenAmount,
                platform = Application.platform == RuntimePlatform.IPhonePlayer ? "ios" : "android",
                playerId = authManager.profileInfo.playerId
            };

            string infoJson = Utils.ObjectToJson(verifyInfo);
            string url = "auth/verify-iap-receipt";
            string responseJson = await requestManager.PostRequestAsync(url, infoJson, idToken);

            Spinner.Instance?.Hide();

            if (string.IsNullOrEmpty(responseJson))
            {
                Debug.LogError("Receipt verification failed: Empty response");
                currentPurchaseFailed?.Invoke("Unable to verify purchase. Please try again or contact support if the issue persists.");
                currentPurchaseSuccess = null;
                currentPurchaseFailed = null;
                return;
            }

            ResultInfo resultInfo = Utils.JsonToObject<ResultInfo>(responseJson);

            if (resultInfo != null && resultInfo.status == "success")
            {
                Debug.Log($"Tokens successfully added: {tokenAmount}");
                currentPurchaseSuccess?.Invoke();
            }
            else
            {
                // Use user-friendly error message
                string errorMsg = resultInfo?.msg ?? "Purchase verification failed.";
                
                // Make error message more user-friendly
                if (errorMsg.Contains("Invalid receipt") || errorMsg.Contains("receipt"))
                {
                    errorMsg = "Unable to verify purchase. Please try again or contact support.";
                }
                else if (errorMsg.Contains("Transaction already processed"))
                {
                    errorMsg = "This purchase has already been processed. Your tokens should be available.";
                }
                else if (errorMsg.Contains("Player not found"))
                {
                    errorMsg = "Account error. Please log out and log back in, then try again.";
                }
                
                Debug.LogError($"Receipt verification failed: {errorMsg}");
                currentPurchaseFailed?.Invoke(errorMsg);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Receipt verification exception: {e.Message}");
            Spinner.Instance?.Hide();
            currentPurchaseFailed?.Invoke("Unable to complete purchase verification. Please check your internet connection and try again.");
        }
        finally
        {
            currentPurchaseSuccess = null;
            currentPurchaseFailed = null;
        }
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogWarning($"IAP Service: Purchase failed - Product: {product.definition.id}, Reason: {failureReason}");

        string errorMessage = failureReason switch
        {
            PurchaseFailureReason.PurchasingUnavailable => "In-app purchases are currently unavailable. Please check your internet connection and try again.",
            PurchaseFailureReason.ExistingPurchasePending => "A purchase is already in progress. Please wait for it to complete.",
            PurchaseFailureReason.ProductUnavailable => "This product is temporarily unavailable. Please try again later.",
            PurchaseFailureReason.SignatureInvalid => "There was an issue verifying your purchase. Please try again or contact support if the problem persists.",
            PurchaseFailureReason.UserCancelled => "Purchase cancelled.",
            PurchaseFailureReason.PaymentDeclined => "Payment was declined. Please check your payment method in Settings.",
            PurchaseFailureReason.DuplicateTransaction => "This purchase has already been processed. Your tokens should be available.",
            _ => "Purchase could not be completed. Please try again."
        };

        currentPurchaseFailed?.Invoke(errorMessage);
        currentPurchaseSuccess = null;
        currentPurchaseFailed = null;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.LogWarning($"IAP Service: Purchase failed - Product: {product.definition.id}, Reason: {failureDescription.reason}, Message: {failureDescription.message}");

        // Use detailed message if available and user-friendly, otherwise use generic message
        string errorMessage = failureDescription.reason switch
        {
            PurchaseFailureReason.PurchasingUnavailable => "In-app purchases are currently unavailable. Please check your internet connection and try again.",
            PurchaseFailureReason.ExistingPurchasePending => "A purchase is already in progress. Please wait for it to complete.",
            PurchaseFailureReason.ProductUnavailable => "This product is temporarily unavailable. Please try again later.",
            PurchaseFailureReason.SignatureInvalid => "There was an issue verifying your purchase. Please try again or contact support if the problem persists.",
            PurchaseFailureReason.UserCancelled => "Purchase cancelled.",
            PurchaseFailureReason.PaymentDeclined => "Payment was declined. Please check your payment method in Settings.",
            PurchaseFailureReason.DuplicateTransaction => "This purchase has already been processed. Your tokens should be available.",
            _ => !string.IsNullOrEmpty(failureDescription.message) && 
                 !failureDescription.message.Contains("Error") && 
                 !failureDescription.message.Contains("Exception") 
                 ? failureDescription.message 
                 : "Purchase could not be completed. Please try again."
        };

        currentPurchaseFailed?.Invoke(errorMessage);
        currentPurchaseSuccess = null;
        currentPurchaseFailed = null;
    }
}
#pragma warning restore CS0618

// Data sent to server when verifying a purchase receipt
[Serializable]
public class IAPReceiptVerifyInfo
{
    public string receipt;
    public int tokenAmount;
    public string platform; // "ios" or "android"
    public string playerId;
}
#endif

