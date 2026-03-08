using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameState
{
    public List<Card>[] Hands = { new(), new(), new() };
    public Play CurrentTopPlay;
    public int currentPlayerIndex;
    public int PassesInRow;
    public bool FirstTrick;

    // --- Tokens ---
    public int[] PlayerTokens = { 20, 20, 20 };  // each starts with 20 tokens in local game
    public int CurrentBet = 1;                      // 1, 5, or 10 per round
    public int Pot = 0;

    bool hasCPU;

    public int myTurnIndex;

    public GameState()
    {
        PassesInRow = 0;
        FirstTrick = false;
        CurrentTopPlay = null;
        CurrentBet = 1;
    }

    public void ChooseStarter()
    {
        // Use Length instead of Count() for WebGL compatibility (avoid LINQ)
        for (int i = 0; i < Hands.Length; i++)
            if (HasCard(i, Suit.Spades, Rank.Three))
            {
                currentPlayerIndex = i;
                FirstTrick = true;

                return;
            }
    }

    public void CaculatePot()
    {
        Pot = 0;
        for (int i = 0; i < PlayerTokens.Length; i++)
        {
            Pot += CurrentBet;
            PlayerTokens[i] -= CurrentBet;
        }
    }

    public void giveTokensToWinner(int winnerIndex)
    {
        PlayerTokens[winnerIndex] += Pot;
    }

    public void ClearState()
    {
        for (int i = 0; i < 3; i++)
            Hands[i].Clear();

        CurrentTopPlay = null;
    }

    public int GetPlayerLocalIndex(int globalIndex)
    {
        // global means server side
        // local means client side
        int offset = 3 - myTurnIndex;
        int localIndex = (globalIndex + offset) % 3;
        return localIndex;
    }

    public void RemoveCardsFromHand(int playerIndex, List<Card> cards)
    {
        if (playerIndex < 0 || playerIndex >= Hands.Length || cards == null || Hands[playerIndex] == null)
            return;

        foreach (var cardToRemove in cards)
        {
            if (cardToRemove == null) continue;
            
            // First try reference equality (fastest and most accurate)
            bool removed = Hands[playerIndex].Remove(cardToRemove);
            
            // If reference equality failed, try matching by suit and rank
            // This handles cases where card objects might be different instances
            // (e.g., after multiple rounds or when cards are recreated)
            // Using manual loop instead of LINQ for WebGL compatibility
            if (!removed)
            {
                Card found = null;
                var hand = Hands[playerIndex];
                for (int i = 0; i < hand.Count; i++)
                {
                    var c = hand[i];
                    if (c != null && c.suit == cardToRemove.suit && c.rank == cardToRemove.rank)
                    {
                        found = c;
                        break;
                    }
                }
                
                if (found != null)
                {
                    Hands[playerIndex].Remove(found);
                }
                else
                {
                    Debug.LogWarning($"Could not remove card {cardToRemove.name} (suit: {cardToRemove.suit}, rank: {cardToRemove.rank}) from player {playerIndex}'s hand. Card not found.");
                }
            }
        }
    }

    public bool HasCard(int player, Suit s, Rank r)
    {
        if (player < 0 || player >= Hands.Length || Hands[player] == null)
            return false;
        return Hands[player].Exists(c => c != null && c.suit == s && c.rank == Rules.RV(r));
    }
}
