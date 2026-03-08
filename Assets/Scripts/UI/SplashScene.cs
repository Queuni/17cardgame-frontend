using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class SplashScreen : MonoBehaviour
{
    [Header("Logo")]
    public Image logoImage;             // Logo image component (required)
    
    [Header("Background")]
    public CanvasGroup bgCanvas;        // Background canvas group
    public Image bgImage;               // Background image (alternative fade method)
    
    [Header("Animation Settings")]
    public float displayDuration = 1.7f;
    public float fadeInDuration = 0.5f;

    private void Awake()
    {
        // Set orientation to portrait (Unity splash was landscape)
        Screen.orientation = ScreenOrientation.Portrait;
    }

    private void Start()
    {
        // Start with splash content hidden to avoid visual glitch during orientation change
        HideSplashContent();
        
        StartCoroutine(SplashSequence());
    }

    private void HideSplashContent()
    {
        // Hide logo initially
        if (logoImage != null)
        {
            Color logoColor = logoImage.color;
            logoColor.a = 0f;
            logoImage.color = logoColor;
        }
        
        // Hide background initially
        if (bgCanvas != null)
        {
            bgCanvas.alpha = 0f;
        }
        
        if (bgImage != null)
        {
            Color bgColor = bgImage.color;
            bgColor.a = 0f;
            bgImage.color = bgColor;
        }
    }

    // Wait for screen orientation to stabilize after Unity splash (landscape) -> custom splash (portrait)
    // Enhanced for real iPhone devices which may have slower orientation changes
    private IEnumerator WaitForOrientationStabilization()
    {
        ScreenOrientation targetOrientation = ScreenOrientation.Portrait;
        int stableFrames = 0;
        const int requiredStableFrames = 5; // Need 5 consecutive frames with same orientation
        
        // Wait until orientation is stable (not just changed once, but stable)
        float timeout = 3f; // Increased timeout for real iPhone devices
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
            
            if (Screen.orientation == targetOrientation)
            {
                stableFrames++;
                if (stableFrames >= requiredStableFrames)
                {
                    break; // Orientation is stable
                }
            }
            else
            {
                stableFrames = 0; // Reset counter if orientation changes
            }
        }
        
        // Wait additional frames for iOS to complete orientation change (real devices need more)
        for (int i = 0; i < 5; i++)
        {
            yield return null;
        }
        
        // Force Canvas to recalculate all layouts after orientation change
        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases(); // Double update for real iPhone
        yield return null;
        
        // Wait for positions to stabilize (iOS real devices may need extra time)
        yield return new WaitForSeconds(0.2f);
        
        // Force another Canvas update to ensure positions are final
        Canvas.ForceUpdateCanvases();
        yield return null;
        
        // Final verification: ensure screen dimensions are stable
        int width = Screen.width;
        int height = Screen.height;
        yield return null;
        
        // If screen size changed, wait more (indicates orientation still changing)
        if (Screen.width != width || Screen.height != height)
        {
            yield return new WaitForSeconds(0.1f);
            Canvas.ForceUpdateCanvases();
        }
    }

    private IEnumerator SplashSequence()
    {
        // CRITICAL: Wait for orientation to stabilize before showing splash content
        // This prevents visual glitch when transitioning from Unity splash (landscape) to custom splash (portrait)
        yield return StartCoroutine(WaitForOrientationStabilization());
        
        // Now fade in the splash content smoothly
        if (bgCanvas != null)
        {
            bgCanvas.DOFade(1f, fadeInDuration).SetEase(DG.Tweening.Ease.OutQuad);
        }
        
        if (bgImage != null)
        {
            Color targetColor = bgImage.color;
            targetColor.a = 1f;
            bgImage.DOColor(targetColor, fadeInDuration).SetEase(DG.Tweening.Ease.OutQuad);
        }
        
        if (logoImage != null)
        {
            Color targetColor = logoImage.color;
            targetColor.a = 1f;
            logoImage.DOColor(targetColor, fadeInDuration).SetEase(DG.Tweening.Ease.OutQuad);
        }
        
        // Wait for fade-in to complete
        yield return new WaitForSeconds(fadeInDuration);
        
        // --- Hold splash screen visible ---
        yield return new WaitForSeconds(displayDuration);

        // Load next scene
        SceneManager.LoadScene("Login");
    }

}
