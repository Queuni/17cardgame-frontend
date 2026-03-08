using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

// This file is for card selection.
public class CardPrefab : MonoBehaviour, IPointerClickHandler
{
    private bool isSelected = false;
    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private float riseAmount;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (originalPosition == Vector3.zero)
        {
            rectTransform = GetComponent<RectTransform>();
            originalPosition = rectTransform.localPosition;

            // Rise by 1/4 of card height
            riseAmount = rectTransform.rect.height * 0.25f;
        }


        isSelected = !isSelected;

        float targetY = isSelected
            ? originalPosition.y + riseAmount
            : originalPosition.y;

        rectTransform.DOKill();
        //rectTransform.DOLocalMoveY(targetY, 0.25f)
        //    .SetEase(Ease.OutQuad);
    }
}
