using UnityEngine;
using DG.Tweening;

public class Spinner : MonoBehaviour
{
    private RectTransform rect;

    private Tween spinTween;

    public static Spinner Instance;

    void Awake()
    {
        GameObject spinnerObject = GameObject.Find("SpinnerImage");
        rect = spinnerObject.GetComponent<RectTransform>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Show()
    {
        gameObject.SetActive(true);

        DOTween.Kill("HideSpinner");

        DOVirtual.DelayedCall(20f, () => Hide()).SetId("HideSpinner");
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        StartSpinning();
    }

    void OnDisable()
    {
        StopSpinning();
    }

    public void StartSpinning()
    {
        // Stop any previous tween to avoid duplicates
        spinTween?.Kill();

        // Rotate infinitely with linear motion
        spinTween = rect.DORotate(new Vector3(0, 0, -360f), 0.8f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    public void StopSpinning()
    {
        spinTween?.Kill();
        rect.rotation = Quaternion.identity;
    }
}
