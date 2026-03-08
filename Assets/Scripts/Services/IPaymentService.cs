using System;

// Interface for payment system
// iOS/Android use IAP, WebGL/Desktop use Stripe
public interface IPaymentService
{
    // Start up the payment service
    void Initialize(Action<bool> onInitialized);

    // Buy tokens
    void PurchaseTokens(int tokenAmount, int price, Action onSuccess, Action<string> onFailed);

    // Check if ready to use
    bool IsInitialized { get; }

    // Check if available on this device
    bool IsAvailable { get; }
}

