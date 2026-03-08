using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardAnimator : MonoBehaviour
{
    public Transform deckPos;
    public Transform dealerPos;       // Center of the table

    public GameObject cardPrefab;
    public SelectionManager selectionManager;
    public Transform cardParent;
    public Transform userCardSpreadPos;
    public Transform[] playerCardPosList;
    public Transform tableCenter;

    private List<GameObject> tableCards = new();
    private Dictionary<int, GameObject> opponentTempCardHash = new();

    // Helper method to safely get position (handles RectTransform for UI elements)
    private Vector3 GetSafePosition(Transform transform)
    {
        if (transform == null) return Vector3.zero;
        
        // If it's a UI element (RectTransform), use RectTransform.position (more reliable on iOS)
        if (transform is RectTransform rectTransform)
        {
            // Force layout update if needed (iOS-specific: ensures position is calculated)
            Canvas.ForceUpdateCanvases();
            return rectTransform.position;
        }
        
        return transform.position;
    }

    // Optimized version that doesn't force canvas updates (for use during animations)
    private Vector3 GetPositionNoUpdate(Transform transform)
    {
        if (transform == null) return Vector3.zero;
        
        // If it's a UI element (RectTransform), use RectTransform.position without forcing update
        if (transform is RectTransform rectTransform)
        {
            return rectTransform.position;
        }
        
        return transform.position;
    }

    // Wait for screen orientation to stabilize and Canvas to recalculate positions
    // This is critical when switching from portrait to landscape (or vice versa)
    // Enhanced for real iPhone devices which may have slower orientation changes
    private IEnumerator WaitForOrientationStabilization()
    {
        // Get current orientation (should be LandscapeLeft for gameplay scene)
        ScreenOrientation targetOrientation = Screen.orientation;
        ScreenOrientation previousOrientation = Screen.orientation;
        int stableFrames = 0;
        const int requiredStableFrames = 5; // Need 5 consecutive frames with same orientation
        
        // Initial wait to allow orientation to start changing (if needed)
        yield return null;
        yield return null;
        
        // Wait until orientation is stable (not just changed once, but stable)
        float timeout = 3f; // Increased timeout for real iPhone devices
        float elapsed = 0f;
        bool orientationChanged = false;
        
        while (elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
            
            ScreenOrientation currentOrientation = Screen.orientation;
            
            // Check if orientation has changed from initial
            if (currentOrientation != previousOrientation)
            {
                orientationChanged = true;
                previousOrientation = currentOrientation;
                stableFrames = 0; // Reset counter when orientation changes
            }
            
            // If orientation matches target and is stable
            if (currentOrientation == targetOrientation)
            {
                stableFrames++;
                if (stableFrames >= requiredStableFrames)
                {
                    break; // Orientation is stable
                }
            }
            else
            {
                stableFrames = 0; // Reset counter if orientation doesn't match target
            }
        }
        
        // If orientation never changed, it might already be correct, but verify stability
        if (!orientationChanged)
        {
            // Still verify it's stable by checking a few more frames
            for (int i = 0; i < requiredStableFrames; i++)
            {
                yield return null;
                if (Screen.orientation != targetOrientation)
                {
                    orientationChanged = true;
                    break;
                }
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
        yield return new WaitForSeconds(0.15f); // Slightly increased for real devices
        
        // Force another Canvas update to ensure positions are final
        Canvas.ForceUpdateCanvases();
        yield return null;
        
        // Final verification: ensure screen dimensions are stable (critical for real iPhone)
        int width = Screen.width;
        int height = Screen.height;
        yield return null;
        yield return null; // Extra frame for real iPhone
        
        // If screen size changed, wait more (indicates orientation still changing)
        if (Screen.width != width || Screen.height != height)
        {
            yield return new WaitForSeconds(0.15f);
            Canvas.ForceUpdateCanvases();
            yield return null;
        }
    }

    public IEnumerator DealCardsAnimated(GameState state)
    {
        if (cardPrefab == null) { Debug.Log("❌ DealCardsAnimated stopped: cardPrefab is null"); yield break; }
        if (cardParent == null) { Debug.Log("❌ DealCardsAnimated stopped: cardParent is null"); yield break; }
        if (state.Hands == null) { Debug.Log("❌ DealCardsAnimated stopped: hands is null"); yield break; }
        
        // CRITICAL: Wait for screen orientation to stabilize before reading positions
        // This fixes the issue where CPU hand positions are incorrect after portrait->landscape switch
        yield return StartCoroutine(WaitForOrientationStabilization());
        
        // Ensure Canvas layout is complete before reading positions (fixes iOS timing issues)
        Canvas.ForceUpdateCanvases();
        yield return null; // Wait one frame for layout to settle
        
        // Validate and cache player positions at start (prevents timing issues during dealing)
        if (playerCardPosList == null || playerCardPosList.Length < 3)
        {
            Debug.LogError("❌ DealCardsAnimated: playerCardPosList is null or incomplete");
            yield break;
        }
        
        Vector3[] cachedPositions = new Vector3[3];
        for (int i = 0; i < 3; i++)
        {
            if (playerCardPosList[i] == null)
            {
                Debug.LogError($"❌ DealCardsAnimated: playerCardPosList[{i}] is null");
                yield break;
            }
            
            // Ensure Transform is active (iOS-specific: inactive transforms may have wrong positions)
            if (!playerCardPosList[i].gameObject.activeInHierarchy)
            {
                playerCardPosList[i].gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                yield return null; // Wait for activation
            }
            
            // Read position multiple times to ensure it's stable after orientation change (critical for real iPhone)
            Vector3 pos1 = GetSafePosition(playerCardPosList[i]);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Vector3 pos2 = GetSafePosition(playerCardPosList[i]);
            yield return null;
            Vector3 pos3 = GetSafePosition(playerCardPosList[i]);
            
            // Use the last reading (most likely to be correct after orientation change)
            cachedPositions[i] = pos3;
            
            // Validate position is not zero (indicates uninitialized Transform on iOS)
            if (cachedPositions[i] == Vector3.zero && i > 0)
            {
                Debug.LogWarning($"⚠️ playerCardPosList[{i}] position is zero after orientation change, may cause cards to appear at center");
            }
            
            // Additional check: If position changed significantly between readings, wait more (real iPhone may need this)
            float positionDelta1 = Vector3.Distance(pos1, pos2);
            float positionDelta2 = Vector3.Distance(pos2, pos3);
            
            if (positionDelta1 > 1f || positionDelta2 > 1f) // Position is still changing
            {
                yield return new WaitForSeconds(0.15f); // Increased wait for real iPhone
                Canvas.ForceUpdateCanvases();
                yield return null;
                cachedPositions[i] = GetSafePosition(playerCardPosList[i]);
            }
        }

        List<GameObject> userCardObjList = new List<GameObject>();
        List<GameObject> tempAnimationCards = new List<GameObject>();
        Dictionary<GameObject, Card> cardObjToCardMap = new Dictionary<GameObject, Card>(); // Map GameObject to Card before sorting

        // CRITICAL iOS OPTIMIZATION: Cache dealer position once to avoid repeated property access
        Vector3 cachedDealerPos = (dealerPos != null) ? dealerPos.position : Vector3.zero;

        // Create temporary cards for all players to animate dealing
        for (int round = 0; round < 17; round++)
        {
            yield return new WaitForSeconds(0.1f);

            for (int p = 0; p < 3; p++)
            {
                // In online mode, only Hands[0] is populated, so use p=0
                int handIndex = (PrepareManager.Instance.gameMode == GameMode.Online) ? 0 : p;
                
                // Skip if this hand doesn't have cards
                if (state.Hands[handIndex].Count <= round) continue;

                GameObject cardObj = Instantiate(cardPrefab, cardParent);
                cardObj.name = state.Hands[handIndex][round].name;

                // CRITICAL: Cache transform.Find() results - these are VERY expensive on iOS when called in nested loops
                // 17 rounds × 3 players = 51 cards × 3 Find() calls = 153 expensive hierarchy traversals!
                Transform frontTransform = cardObj.transform.Find("Front");
                Transform backTransform = cardObj.transform.Find("Back");
                Transform remainedTransform = cardObj.transform.Find("RemainedText");

                if (frontTransform == null || backTransform == null || remainedTransform == null)
                {
                    Debug.Log($"❌ Card prefab missing Front or Back image — {cardObj.name}");
                    Destroy(cardObj);
                    yield break;
                }

                Image front = frontTransform.GetComponent<Image>();
                Image back = backTransform.GetComponent<Image>();
                TextMeshProUGUI remainedText = remainedTransform.GetComponent<TextMeshProUGUI>();

                if (front == null || back == null || remainedText == null)
                {
                    Debug.Log($"❌ Card prefab missing Front or Back image — {cardObj.name}");
                    Destroy(cardObj);
                    yield break;
                }

                front.sprite = state.Hands[handIndex][round].frontSprite;
                back.sprite = state.Hands[handIndex][round].backSprite;

                front.enabled = false;
                back.enabled = true;
                remainedText.enabled = false;

                // Use cached dealer position
                cardObj.transform.position = cachedDealerPos;
                cardObj.transform.localScale = Vector3.one * 1.1f;

                // Use cached position (prevents iOS timing issues where position changes during dealing)
                Vector3 target = cachedPositions[p];

                // CRITICAL: Add SetUpdate(true) for smoother animations on iOS
                cardObj.transform.DOMove(target, 0.5f).SetEase(Ease.OutQuad).SetUpdate(true);
                cardObj.transform.DOScale(Vector3.one * 1.25f, 0.1f).SetUpdate(true);

                // Keep only my cards, mark others for deletion
                if (p == 0)
                {
                    userCardObjList.Add(cardObj);
                    cardObjToCardMap[cardObj] = state.Hands[handIndex][round]; // Store mapping before sorting
                }
                else
                {
                    if (round == 16)
                    {
                        opponentTempCardHash.Add(p, cardObj);
                        remainedText.enabled = true;
                    }// keep one card for each opponent as placeholder
                    else
                    {
                        tempAnimationCards.Add(cardObj);
                    }
                }

                // Reduced wait time for smoother animation on iPhone
                yield return new WaitForSeconds(0.03f);
            }
        }

        yield return new WaitForSeconds(0.8f); // Wait for animations to complete

        // Delete temporary animation cards (in online mode, none; in local mode, 34 cards deleted!)
        int deletedCount = tempAnimationCards.Count;
        foreach (GameObject tempCard in tempAnimationCards)
        {
            if (tempCard != null)
            {
                Destroy(tempCard);
            }
        }
        tempAnimationCards.Clear();

        // Move Player's cards to show area
        OrderCardsByRank(userCardObjList);
        SpreadStraight(userCardObjList);
        yield return new WaitForSeconds(0.6f); // Wait for SpreadStraight animation to complete (0.6s)

        // CRITICAL iOS OPTIMIZATION: Pre-cache all component references before flip loop
        // This avoids expensive transform.Find() calls during animation
        Dictionary<GameObject, (Image front, Image back)> flipComponentsCache = new Dictionary<GameObject, (Image, Image)>();
        foreach (var cardObj in userCardObjList)
        {
            if (cardObj != null)
            {
                Transform frontTransform = cardObj.transform.Find("Front");
                Transform backTransform = cardObj.transform.Find("Back");
                if (frontTransform != null && backTransform != null)
                {
                    Image front = frontTransform.GetComponent<Image>();
                    Image back = backTransform.GetComponent<Image>();
                    if (front != null && back != null)
                    {
                        flipComponentsCache[cardObj] = (front, back);
                    }
                }
            }
        }

        // Flip Player's cards face-up
        for (int k = 0; k < userCardObjList.Count; k++)
        {
            GameObject cardObj = userCardObjList[k];
            
            // Use cached components if available
            Image front, back;
            if (flipComponentsCache.TryGetValue(cardObj, out var cached))
            {
                front = cached.front;
                back = cached.back;
            }
            else
            {
                // Fallback if not cached (shouldn't happen)
                Transform frontTransform = cardObj.transform.Find("Front");
                Transform backTransform = cardObj.transform.Find("Back");
                front = frontTransform?.GetComponent<Image>();
                back = backTransform?.GetComponent<Image>();
            }

            if (front == null || back == null)
            {
                yield return new WaitForSeconds(0.03f);
                continue;
            }

            // WebGL-safe: Wrap DOTween operations in try-catch
            Sequence seq = null;
            try
            {
                seq = DOTween.Sequence();
                if (cardObj != null && cardObj.transform != null)
                {
                    // CRITICAL: Add SetUpdate(true) for smoother rotation on iOS
                    seq.Append(cardObj.transform.DORotate(new Vector3(0, 90, 0), 0.15f).SetUpdate(true));
                    
                    // WebGL-safe callback
                    Image safeBack = back;
                    Image safeFront = front;
                    seq.AppendCallback(() =>
                    {
                        try
                        {
                            if (safeBack != null) safeBack.enabled = false;
                            if (safeFront != null) safeFront.enabled = true;
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"WebGL: Error in card flip callback: {e.Message}");
                        }
                    });
                    seq.Append(cardObj.transform.DORotate(Vector3.zero, 0.15f).SetUpdate(true));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"WebGL: Error creating card flip animation: {e.Message}");
                // Fallback: just enable front image
                try
                {
                    if (back != null) back.enabled = false;
                    if (front != null) front.enabled = true;
                }
                catch { }
            }
            
            // Reduced wait time for smoother animation on iPhone
            yield return new WaitForSeconds(0.02f);

            // Get the correct Card object from the pre-sorted mapping
            Card card = cardObjToCardMap[cardObj];
            selectionManager.RegisterUserCard(cardObj, card);
        }

        yield return new WaitForSeconds(0.6f);
    }

    void OrderCardsByRank(List<GameObject> cards)
    {
        // CRITICAL iOS OPTIMIZATION: Pre-cache sprite names to avoid expensive Find() calls during sort
        // Sort operations call the comparison function many times, so caching is essential
        Dictionary<GameObject, string> spriteNameCache = new Dictionary<GameObject, string>();
        foreach (var card in cards)
        {
            if (card != null)
            {
                Transform frontTransform = card.transform.Find("Front");
                if (frontTransform != null)
                {
                    Image frontImg = frontTransform.GetComponent<Image>();
                    if (frontImg != null && frontImg.sprite != null)
                    {
                        spriteNameCache[card] = frontImg.sprite.name;
                    }
                }
            }
        }

        cards.Sort((a, b) =>
        {
            // Use cached sprite names instead of Find() calls
            if (!spriteNameCache.TryGetValue(a, out string nameA) || !spriteNameCache.TryGetValue(b, out string nameB))
            {
                // Fallback if not cached (shouldn't happen)
                return 0;
            }

            string[] partsA = nameA.Split('_');
            string[] partsB = nameB.Split('_');

            int rankA = Rules.ParseRank(partsA[0]);
            int rankB = Rules.ParseRank(partsB[0]);
            Suit suitA = Rules.ParseSuit(partsA[2]);
            Suit suitB = Rules.ParseSuit(partsB[2]);

            // First by rank (3 < ... < K < A < 2)
            int rankCompare = rankA.CompareTo(rankB);
            if (rankCompare != 0)
                return rankCompare;

            // Then by suit (♠ > ♣ > ♦ > ♥)
            return suitA.CompareTo(suitB);
        });
    }


    // Spread cards evenly in a straight row
    public void SpreadStraight(List<GameObject> cards)
    {
        if (cards == null || cards.Count == 0) return;

        // CRITICAL iOS OPTIMIZATION: Cache position once to avoid repeated property access
        // Force canvas update only once before calculating positions
        if (userCardSpreadPos != null)
        {
            Canvas.ForceUpdateCanvases();
        }

        float spacing = Screen.width / 33;
        int count = cards.Count;
        float totalWidth = (count - 1) * spacing;
        
        // Use cached position method
        Vector3 startPos = (userCardSpreadPos != null) 
            ? GetPositionNoUpdate(userCardSpreadPos) 
            : Vector3.zero;
        Vector3 start = startPos - new Vector3(totalWidth / 2f, 0, 0);

        for (int i = 0; i < count; i++)
        {
            if (cards[i] == null) continue;
            
            cards[i].transform.SetSiblingIndex(i);
            Vector3 pos = start + new Vector3(i * spacing, 0, 0);
            
            // CRITICAL: Add SetUpdate(true) for smoother animation on iOS
            cards[i].transform.DOMove(pos, 0.6f).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }

    public IEnumerator AnimatePlay(int playerIndex, Play play, int actualPlayerIndex = -1)
    {
        // Cache managers (inject later if possible)
        var selectionManager = FindAnyObjectByType<SelectionManager>();
        var gameManager = FindAnyObjectByType<GameManager>();
        var tableAnimator = FindAnyObjectByType<TableAnimator>();

        if (!selectionManager || !tableAnimator)
        {
            Debug.LogError("SelectionManager or TableAnimator not found");
            yield break;
        }

        // Determine local player
        bool isLocal = false;
        if (gameManager?.state != null)
        {
            if (PrepareManager.Instance.gameMode == GameMode.Online)
                isLocal = actualPlayerIndex >= 0
                    ? actualPlayerIndex == gameManager.state.myTurnIndex
                    : playerIndex == gameManager.state.myTurnIndex;
            else
                isLocal = playerIndex == 0;
        }

        List<GameObject> cardsFromHand = null;
        if (isLocal)
            cardsFromHand = selectionManager.GetCardGameObjects(play.Cards);

        // FADE & DESTROY OLD TABLE CARDS (SAFE)
        if (tableCards.Count > 0)
        {
            int fadeDone = 0;
            int fadeTotal = tableCards.Count;

            // CRITICAL iOS OPTIMIZATION: Batch CanvasGroup operations before animations
            // GetComponent/AddComponent in loops causes frame drops on iPhone
            List<CanvasGroup> fadeCanvasGroups = new List<CanvasGroup>();
            foreach (var old in tableCards)
            {
                if (!old) continue;
                
                CanvasGroup cg = old.GetComponent<CanvasGroup>();
                if (!cg) cg = old.AddComponent<CanvasGroup>();
                cg.alpha = 1f;
                fadeCanvasGroups.Add(cg);
            }

            // Now animate with cached CanvasGroups
            int cgIndex = 0;
            foreach (var old in tableCards)
            {
                if (!old)
                {
                    fadeDone++;
                    continue;
                }

                var oldCard = old;
                DOTween.Kill(oldCard, true);

                CanvasGroup cg = (cgIndex < fadeCanvasGroups.Count) ? fadeCanvasGroups[cgIndex] : oldCard.GetComponent<CanvasGroup>();
                cgIndex++;

                // CRITICAL: Add SetUpdate(true) for smoother fade animation on iOS
                DOTween.Sequence()
                    .SetLink(oldCard, LinkBehaviour.KillOnDestroy)
                    .Append(cg.DOFade(0f, 0.12f).SetUpdate(true))
                    .OnComplete(() =>
                    {
                        if (oldCard)
                            Destroy(oldCard);

                        fadeDone++;
                    });
            }

            yield return new WaitUntil(() => fadeDone >= fadeTotal);
            tableCards.Clear();
        }

        // ------------------------------------------------
        // Announce play type AFTER clearing
        // ------------------------------------------------
        tableAnimator.SayPlayType(playerIndex, play.Type);

        // ------------------------------------------------
        // Animate NEW cards
        // ------------------------------------------------
        const float spread = 30f;
        var newCards = new List<GameObject>(play.Cards.Count);

        // CRITICAL: Cache positions ONCE before loop to avoid expensive Canvas.ForceUpdateCanvases() calls
        // This prevents the "bumpiness" on iPhone by avoiding canvas updates during animation
        Vector3 cachedTableCenterPos;
        Vector3 cachedPlayerPos = Vector3.zero;
        
        // Cache table center position once (with canvas update only once)
        if (tableCenter != null)
        {
            Canvas.ForceUpdateCanvases();
            cachedTableCenterPos = GetPositionNoUpdate(tableCenter);
        }
        else
        {
            cachedTableCenterPos = Vector3.zero;
        }
        
        // Cache player position once (only needed for non-local players)
        if (!isLocal && playerCardPosList != null && playerIndex >= 0 && playerIndex < playerCardPosList.Length && playerCardPosList[playerIndex] != null)
        {
            Canvas.ForceUpdateCanvases();
            cachedPlayerPos = GetPositionNoUpdate(playerCardPosList[playerIndex]);
        }

        // CRITICAL iOS OPTIMIZATION: Pre-cache all card component references to avoid expensive Find() calls in loop
        // transform.Find() is VERY expensive on iOS and causes stuttering
        Dictionary<GameObject, (Image front, Image back, TextMeshProUGUI remained)> cardComponentsCache = new Dictionary<GameObject, (Image, Image, TextMeshProUGUI)>();
        
        // Pre-cache components for local cards (if any)
        if (isLocal && cardsFromHand != null)
        {
            foreach (var cardObj in cardsFromHand)
            {
                if (cardObj != null)
                {
                    var front = cardObj.transform.Find("Front")?.GetComponent<Image>();
                    var back = cardObj.transform.Find("Back")?.GetComponent<Image>();
                    if (front != null && back != null)
                    {
                        cardComponentsCache[cardObj] = (front, back, null);
                    }
                }
            }
        }

        int completed = 0;

        for (int i = 0; i < play.Cards.Count; i++)
        {
            var cardData = play.Cards[i];
            GameObject cardObj;
            Image frontImg = null;
            Image backImg = null;

            if (isLocal && cardsFromHand != null && i < cardsFromHand.Count && cardsFromHand[i])
            {
                cardObj = cardsFromHand[i];
                
                // Use cached components if available, otherwise find them (should be cached)
                if (cardComponentsCache.TryGetValue(cardObj, out var cached))
                {
                    frontImg = cached.front;
                    backImg = cached.back;
                }
                else
                {
                    frontImg = cardObj.transform.Find("Front")?.GetComponent<Image>();
                    backImg = cardObj.transform.Find("Back")?.GetComponent<Image>();
                }
                
                // CRITICAL: SetParent with worldPositionStays=true for local cards to preserve their current position
                // This avoids layout recalculation while maintaining correct visual position
                cardObj.transform.SetParent(cardParent, true);
            }
            else
            {
                cardObj = Instantiate(cardPrefab);
                cardObj.name = cardData.name;
                
                // Set parent first, then position to minimize layout recalculations
                cardObj.transform.SetParent(cardParent, false);
                
                // Use cached position (no canvas update during animation)
                // For UI elements, setting position after SetParent will work correctly
                cardObj.transform.position = cachedPlayerPos;
                cardObj.transform.localScale = Vector3.one * 1.25f;

                // Cache these components immediately to avoid repeated Find() calls
                frontImg = cardObj.transform.Find("Front")?.GetComponent<Image>();
                backImg = cardObj.transform.Find("Back")?.GetComponent<Image>();
                var remained = cardObj.transform.Find("RemainedText")?.GetComponent<TextMeshProUGUI>();

                if (!frontImg || !backImg || !remained)
                {
                    Destroy(cardObj);
                    completed++;
                    continue;
                }

                frontImg.sprite = cardData.frontSprite;
                backImg.sprite = cardData.backSprite;
                frontImg.enabled = false;
                backImg.enabled = true;
                remained.enabled = false;
            }

            // Use cached position (no canvas update during animation - prevents iPhone stuttering)
            Vector3 target = cachedTableCenterPos + new Vector3((i - play.Cards.Count / 2f) * spread, 0f, 0f);

            bool counted = false;
            void MarkComplete()
            {
                if (counted) return;
                counted = true;
                completed++;
            }

            // CRITICAL: Use smoother easing (OutQuad instead of Linear) and slightly longer duration for smoother animation on iOS
            // Also use SetUpdate(true) to ensure smooth updates
            DOTween.Sequence()
                .SetLink(cardObj, LinkBehaviour.KillOnDestroy)
                .Append(cardObj.transform.DOMove(target, 0.35f).SetEase(Ease.OutQuad).SetUpdate(true))
                .AppendCallback(() =>
                {
                    if (backImg) backImg.enabled = false;
                    if (frontImg) frontImg.enabled = true;
                })
                .OnComplete(MarkComplete);

            newCards.Add(cardObj);

            // CRITICAL: Further reduce stagger - only wait every 3rd card to minimize frame drops on iPhone
            // This significantly reduces micro-stutters while maintaining visual stagger
            if (i % 3 == 0 && i > 0)
            {
                yield return null;
            }
        }

        // ------------------------------------------------
        // Wait for ALL animations deterministically
        // ------------------------------------------------
        yield return new WaitUntil(() => completed >= newCards.Count);
        
        // CRITICAL: Small delay before reordering to avoid animation overlap stutter on iPhone
        // This prevents ReorderUserHand from starting while play animation is still finishing
        yield return new WaitForSeconds(0.1f);

        // ------------------------------------------------
        // Apply game state AFTER animation
        // ------------------------------------------------
        if (isLocal)
        {
            selectionManager.RemoveCardsFromHand(play.Cards);
            // Delay ReorderUserHand slightly to avoid simultaneous animations causing stutter
            yield return new WaitForSeconds(0.05f);
            ReorderUserHand(selectionManager.GetAllUserCardObjects());
        }
        else if (opponentTempCardHash.TryGetValue(playerIndex, out var signObj))
        {
            var text = signObj.transform.Find("RemainedText")?.GetComponent<TextMeshProUGUI>();
            if (text && int.TryParse(text.text, out int count))
            {
                count -= play.Cards.Count;
                text.text = count.ToString();
                if (count <= 0)
                    Destroy(signObj);
            }
        }

        // ------------------------------------------------
        // Final deterministic ordering
        // ------------------------------------------------
        newCards.Sort((a, b) =>
            a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));

        for (int i = 0; i < newCards.Count; i++)
            newCards[i].transform.SetSiblingIndex(i);

        tableCards = newCards;
    }


    public IEnumerator ClearTable()
    {
        // Nothing to clear
        if (tableCards == null || tableCards.Count == 0)
            yield break;

        int completed = 0;
        int total = tableCards.Count;

        // CRITICAL iOS OPTIMIZATION: Batch CanvasGroup operations to reduce GetComponent/AddComponent calls
        // Pre-check and add CanvasGroups before starting animations
        List<CanvasGroup> canvasGroups = new List<CanvasGroup>();
        foreach (var c in tableCards)
        {
            if (!c) continue;
            
            CanvasGroup cg = c.GetComponent<CanvasGroup>();
            if (!cg)
                cg = c.AddComponent<CanvasGroup>();
            
            cg.alpha = 1f;
            canvasGroups.Add(cg);
        }

        // Animate with slight stagger to reduce frame drops on iPhone
        int cardIndex = 0;
        foreach (var c in tableCards)
        {
            if (!c)
            {
                completed++;
                continue;
            }

            var card = c;
            CanvasGroup cg = (cardIndex < canvasGroups.Count) ? canvasGroups[cardIndex] : card.GetComponent<CanvasGroup>();
            cardIndex++;

            // Kill any running tweens on this card (important for multiplayer)
            DOTween.Kill(card, true);

            // CRITICAL: Add SetUpdate(true) for smoother animation on iOS
            DOTween.Sequence()
                .SetLink(card, LinkBehaviour.KillOnDestroy)
                .Append(cg.DOFade(0f, 0.25f).SetUpdate(true))
                .Join(card.transform.DOScale(Vector3.zero, 0.25f)
                    .SetEase(Ease.InBack).SetUpdate(true))
                .OnComplete(() =>
                {
                    if (card)
                        Destroy(card);

                    completed++;
                });

            // Small stagger every 3rd card to reduce simultaneous animations on iPhone
            if (cardIndex % 3 == 0)
            {
                yield return null;
            }
        }

        // Deterministic wait — safe for lag, low FPS, and multiplayer timing
        yield return new WaitUntil(() => completed >= total);

        tableCards.Clear();
    }


    public void ReorderUserHand(List<GameObject> cards)
    {
        if (cards == null || cards.Count == 0) return;

        // Cache position once to avoid repeated property access (iOS optimization)
        // Force canvas update only once before calculating positions
        if (userCardSpreadPos != null)
        {
            Canvas.ForceUpdateCanvases();
        }

        // Keep consistent spacing
        float spacing = Screen.width / 33f;

        // Calculate total width of the current hand
        int count = cards.Count;
        float totalWidth = (count - 1) * spacing;

        // Calculate centered start position (use cached position if available)
        Vector3 startPos = (userCardSpreadPos != null) 
            ? GetPositionNoUpdate(userCardSpreadPos) 
            : Vector3.zero;
        Vector3 start = startPos - new Vector3(totalWidth / 2f, 0, 0);

        // Animate each card smoothly into its new centered position
        for (int i = 0; i < count; i++)
        {
            GameObject card = cards[i];
            if (card == null) continue;

            Vector3 targetPos = start + new Vector3(i * spacing, 0, 0);
            card.transform.SetSiblingIndex(i);
            
            // CRITICAL: Add SetUpdate(true) for smoother animation on iOS
            card.transform.DOMove(targetPos, 0.35f).SetEase(Ease.OutCubic).SetUpdate(true);
        }
    }

    public void ClearAllCardsImmediate()
    {
        // Destroy every instantiated card under the card parent
        foreach (Transform child in cardParent)
            if (child != null)
                Destroy(child.gameObject);

        // Also clear SelectionManager lookups
        if (selectionManager != null)
            selectionManager.ClearManager();

        foreach (var kvp in opponentTempCardHash)
        {
            if (kvp.Value != null)
                GameObject.Destroy(kvp.Value);
        }

        opponentTempCardHash.Clear();

        // IMPORTANT: clear internal table state so we don't hold stale references
        tableCards.Clear();
    }
}
