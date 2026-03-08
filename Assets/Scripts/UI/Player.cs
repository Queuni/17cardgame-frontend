using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public Image waitingImage;
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;
    public Button avatarButton;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI tokenText;
    public Image tokenImage;

    private Tween activeTween;
    private float fadeDuration = 0.3f;
    private float displayDuration = 0.7f;

    private Tween waitingTween;
    private RectTransform waitingRect;

    void Start()
    {
        waitingImage.enabled = false;
        messagePanel.SetActive(false);

        waitingRect = waitingImage.GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetAvatarSprite(Sprite sprite)
    {
        Image avatarImage = avatarButton.GetComponent<Image>();
        avatarImage.sprite = sprite;
    }

    public void SetPlayerName(string name)
    {
        nameText.text = name;
    }

    public void SetToken(int token)
    {
        tokenText.text = token.ToString();
    }

    public void SetTokenColor(bool hasCPU)
    {
        if (hasCPU)
        {
            tokenImage.sprite = Resources.Load<Sprite>("images/coin_40_silver");
        }
        else
        {
            tokenImage.sprite = Resources.Load<Sprite>("images/coin_40");
        }
    }

    public void ShowMessage(string message, string textColor = "")
    {
        messageText.text = message;
        if (textColor == "")
        {
            textColor = "#FFFFFF"; // default white
        }
        if (ColorUtility.TryParseHtmlString(textColor, out Color newColor))
        {
            messageText.color = newColor;
        }
        else
        {
            Debug.Log("Invalid hex color code!");
        }

        messagePanel.SetActive(true);

        Image panelImage = messagePanel.GetComponent<Image>();

        // Kill any running tween
        activeTween?.Kill();

        // Create sequence: fade in → hold → fade out
        Sequence seq = DOTween.Sequence();
        seq.Append(panelImage.DOFade(1, fadeDuration)); // fade in
        seq.AppendInterval(displayDuration);             // stay visible
        seq.Append(panelImage.DOFade(0, fadeDuration)); // fade out
        seq.OnComplete(() =>
        {
            messagePanel.SetActive(false);
        });

        activeTween = seq;
    }

    public void ShowPass()
    {
        ShowMessage("Pass");
    }

    public void ShowPlayType(PlayType playType)
    {
        ShowMessage(Rules.PlayTypeToString(playType), "#E7E119");
    }

    public void ShowWaiting()
    {
        // Stop any previous tween to avoid duplicates
        waitingTween?.Kill();
        waitingImage.enabled = true;

        // Rotate infinitely with linear motion
        waitingTween = waitingRect.DORotate(new Vector3(0, 0, -360f), 0.8f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    public void HideWaiting()
    {
        waitingImage.enabled = false;
        waitingTween?.Kill();
        waitingRect.rotation = Quaternion.identity;
    }

    public void HideToken()
    {
        tokenText.transform.parent.gameObject.SetActive(false);
    }
}
