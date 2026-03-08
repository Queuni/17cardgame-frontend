using UnityEngine;

// Picks the right payment method based on device
// Mobile uses IAP, WebGL/Desktop uses Stripe
public static class PaymentServiceFactory
{
    private static IPaymentService instance;

    public static IPaymentService GetPaymentService()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject serviceObject = new GameObject("PaymentService");

#if UNITY_IOS || UNITY_ANDROID
        instance = serviceObject.AddComponent<IAPService>();
        Debug.Log("PaymentService: Using IAP for mobile");
#elif UNITY_WEBGL || (!UNITY_IOS && !UNITY_ANDROID)
        instance = serviceObject.AddComponent<StripeService>();
        Debug.Log("PaymentService: Using Stripe for WebGL/Desktop");
#else
        instance = serviceObject.AddComponent<StripeService>();
        Debug.Log("PaymentService: Using Stripe (fallback)");
#endif

        return instance;
    }
}

