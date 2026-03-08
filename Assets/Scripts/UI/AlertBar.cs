using UnityEngine;
using TMPro;
using DG.Tweening;

public class AlertBar : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI messageText;

    public static AlertBar Instance;

    [Header("Animation Settings")]
    public float fadeDuration = 0.5f;
    public float displayDuration = 2f;

    private Tween activeTween;

    private void Awake()
    {
        // Singleton logic
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        gameObject.SetActive(true);
        messageText.text = message;

        // Kill any running tween
        activeTween?.Kill();

        // Reset alpha
        canvasGroup.alpha = 0;

        // Create sequence: fade in → hold → fade out
        Sequence seq = DOTween.Sequence();
        seq.Append(canvasGroup.DOFade(1, fadeDuration)); // fade in
        seq.AppendInterval(displayDuration);             // stay visible
        seq.Append(canvasGroup.DOFade(0, fadeDuration)); // fade out
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });

        activeTween = seq;
    }
}
