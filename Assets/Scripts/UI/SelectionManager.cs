using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    [HideInInspector] public List<Card> Selected = new();

    public Dictionary<GameObject, Card> userCardLookup = new();
    private Dictionary<GameObject, float> originalYHash = new();
    private GameState state;

    public void RegisterUserCard(GameObject obj, Card card)
    {
        userCardLookup[obj] = card;
        var handler = obj.AddComponent<CardClickHandler>();
        handler.manager = this;
        handler.card = card;

        RectTransform originRect = obj.GetComponent<RectTransform>();
        // Ensure any ongoing animations are complete before capturing original position
        originRect.DOKill();
        originalYHash[obj] = originRect.localPosition.y;
    }

    public void UpdateGameState(GameState newState)
    {
        state = newState;
    }

    public void OnCardClicked(GameObject obj, Card card)
    {
        if (PrepareManager.Instance.gameMode == GameMode.Local)
        {
            if (state.currentPlayerIndex != 0)
                return; // Not user's turn
        }
        else
        {
            if (state.myTurnIndex != state.currentPlayerIndex) return;
        }

        RectTransform rect = obj.GetComponent<RectTransform>();
        float originY = originalYHash[obj];
        float riseAmount = rect.rect.height * 0.25f;

        if (Selected.Contains(card))
        {
            Selected.Remove(card);
            rect.DOLocalMoveY(originY, 0.2f).SetEase(Ease.OutQuad);
        }
        else
        {
            Selected.Add(card);
            rect.DOLocalMoveY(originY + riseAmount, 0.2f).SetEase(Ease.OutQuad);
        }

        // Disable or enable play button by selected cards
        var play = Rules.BuildPlay(Selected);
        
        GameObject playButtonObj = GameObject.Find("PlayButton");
        if (playButtonObj != null)
        {
            Button playButton = playButtonObj.GetComponent<Button>();
            playButton.interactable = Rules.CanMatchAndBeat(play, state.CurrentTopPlay);
            if (state.FirstTrick && play != null)
            {
                // On first trick, only allow plays that contain the spade - 3
                bool hasThreeOfSpades = play.Cards.Any(c => c.rank == 3 && c.suit == Suit.Spades);
                playButton.interactable &= hasThreeOfSpades;
            }
        }
}

    public GameObject GetCardGameObject(Card card)
    {
        foreach (var kvp in userCardLookup)
        {
            if (kvp.Value == card || (kvp.Value != null && kvp.Value.name == card.name))
            {
                return kvp.Key;
            }
        }
        return null;
    }

    public List<GameObject> GetAllUserCardObjects()
    {
        List<GameObject> objects = new List<GameObject>();
        foreach (var kvp in userCardLookup)
        {
            objects.Add(kvp.Key);
        }

        return objects;
    }

    public List<GameObject> GetCardGameObjects(List<Card> cards)
    {
        List<GameObject> cardObjs = new List<GameObject>();
        foreach (var card in cards)
        {
            GameObject obj = GetCardGameObject(card);
            if (obj != null)
            {
                cardObjs.Add(obj);
            }
        }
        return cardObjs;
    }

    public void RemoveCardFromHand(Card card)
    {
        GameObject cardObj = GetCardGameObject(card);
        if (cardObj != null)
        {
            // Remove from lookup dictionaries (but don't destroy - it's moving to table)
            userCardLookup.Remove(cardObj);
            originalYHash.Remove(cardObj);
            Selected.Remove(card);
            
            // Remove CardClickHandler since card is no longer in hand
            CardClickHandler handler = cardObj.GetComponent<CardClickHandler>();
            if (handler != null)
            {
                Destroy(handler);
            }
        }
    }

    public void RemoveCardsFromHand(List<Card> cards)
    {
        foreach (var card in cards)
        {
            RemoveCardFromHand(card);
        }
    }

    public void ClearManager()
    {
        Selected.Clear();
        userCardLookup.Clear();
        originalYHash.Clear();
    }

    public void HighlightSuggestedCards(Play suggestedPlay)
    {
        // First clear all highlights
        foreach (var kvp in userCardLookup)
        {
            RectTransform rect = kvp.Key.GetComponent<RectTransform>();
            float originY = originalYHash[kvp.Key];
            rect.DOLocalMoveY(originY, 0.2f).SetEase(Ease.OutQuad);
        }
        Selected.Clear();
        
        if (suggestedPlay == null || suggestedPlay.Cards == null) return;
        // Highlight suggested cards
        foreach (var card in suggestedPlay.Cards)
        {
            GameObject cardObj = GetCardGameObject(card);
            if (cardObj != null)
            {
                OnCardClicked(cardObj, card);
            }
        }
    }

    public void ClearSelection()
    {
        foreach (var kvp in userCardLookup)
        {
            RectTransform rect = kvp.Key.GetComponent<RectTransform>();
            float originY = originalYHash[kvp.Key];
            rect.DOLocalMoveY(originY, 0.2f).SetEase(Ease.OutQuad);
        }
        Selected.Clear();
    }

    /// <summary>
    /// Removes cards from selection by matching suit and rank (for WebGL compatibility)
    /// </summary>
    public void RemoveCardsFromSelection(List<Card> cardsToRemove)
    {
        if (cardsToRemove == null || Selected == null) return;

        foreach (var cardToRemove in cardsToRemove)
        {
            if (cardToRemove == null) continue;

            // First try reference equality (fastest)
            bool removed = Selected.Remove(cardToRemove);
            
            // If reference equality failed, find matching card by suit/rank
            // Using manual loop instead of LINQ for WebGL compatibility
            if (!removed)
            {
                Card found = null;
                for (int i = 0; i < Selected.Count; i++)
                {
                    var sel = Selected[i];
                    if (sel != null && sel.suit == cardToRemove.suit && sel.rank == cardToRemove.rank)
                    {
                        found = sel;
                        break;
                    }
                }
                if (found != null)
                {
                    Selected.Remove(found);
                }
            }
        }
    }
}


public class CardClickHandler : MonoBehaviour, IPointerClickHandler
{
    public SelectionManager manager;
    public Card card;
    public void OnPointerClick(PointerEventData e) => manager.OnCardClicked(gameObject, card);
}
