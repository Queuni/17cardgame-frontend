using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public static class Dealer
{
    public static Sprite backSprite;
    public static Dictionary<string, Sprite> frontSpriteMap = new();

    public static void LoadSprites()
    {
        backSprite = Resources.Load<Sprite>("images/card_back/back");
        if (backSprite == null)
        {
            Debug.Log("❌ Missing back.png in Resources/images/card_back/");
            return;
        }

        Sprite[] frontSprites = Resources.LoadAll<Sprite>("images/card_front");
        if (frontSprites.Length != 52)
        {
            Debug.LogWarning($"⚠ Expected 52 front cards, found {frontSprites.Length}");
            if (frontSprites.Length == 0)
            {
                Debug.Log("❌ No front sprites found! Check Resources/images/card_front/");
                return;
            }
        }

        foreach (var sprite in frontSprites)
        {
            string[] parts = sprite.name.Split('_');
            if (parts.Length != 3)
            {
                Debug.LogWarning("⚠ Invalid card name: " + sprite.name);
                continue;
            }
            frontSpriteMap[sprite.name] = sprite;
        }
    }

    public static void Deal(GameState state, List<string> playerHands = null)
    {
        if (PrepareManager.Instance.gameMode == GameMode.Online)
        {
            DealOnline(state, playerHands);
        }
        else
        {
            DealLocal(state);
        }
    }

    private static void DealLocal(GameState state)
    {
        List<Card> deck = new List<Card>();
        deck.Clear();
      
        foreach (var entry in frontSpriteMap)
        {
            // Expect file names like "3_of_spades", "king_of_hearts"
            string[] parts = entry.Key.Split('_');

            int rank = Rules.ParseRank(parts[0]);
            Suit suit = Rules.ParseSuit(parts[2]);

            deck.Add(new Card(entry.Key, suit, rank, entry.Value, backSprite));
        }

        Shuffle(deck);

        // ensure 3♠ in 51 cards
        var s3 = deck.First(c => c.suit == Suit.Spades && c.rank == Rules.RV(Rank.Three));
        if (deck.IndexOf(s3) == 51)
        {
            int rnd = Random.Range(0, 51);
            (deck[51], deck[rnd]) = (deck[rnd], deck[51]);
        }

        var dealt = deck.Take(51).ToList();
        for (int i = 0; i < 51; i++)
            state.Hands[i % 3].Add(dealt[i]);

        deck.Clear();
        dealt.Clear();
    }

    public static void DealOnline(GameState state, List<string> playerHands)
    {
        int totalCount = playerHands.Count;
        // In online mode, we only have the player's cards
        // We still need to populate Hands[0] for the game logic to work
        //Sprite backSprite = Resources.Load<Sprite>("images/card_back/back");
        for (int i = 0; i < totalCount; i++)
        {
            string cardName = playerHands[i];
            //Sprite sprite = Resources.Load<Sprite>($"images/card_front/{cardName}");
            
            if (!frontSpriteMap.ContainsKey(cardName))
            {
                Debug.Log($"Missing sprite for card '{cardName}'");
                continue;
            }

            Sprite sprite = frontSpriteMap[cardName];

            string[] parts = cardName.Split('_');

            int rank = Rules.ParseRank(parts[0]);
            Suit suit = Rules.ParseSuit(parts[2]);

            // Create card and add to player's hand (index 0)
            Card playerCard = new Card(sprite.name, suit, rank, sprite, backSprite);

            state.Hands[0].Add(playerCard);
        }
        
        // Create placeholder cards for opponents (one face-down card each for visual effect)
        Sprite placeholderSprite = backSprite; // Use back sprite as placeholder
        for (int opponentIndex = 1; opponentIndex < 3; opponentIndex++)
        {
            for (int i = 0; i < 17; i++)
            {
                // Create a generic placeholder card (we don't know opponent's cards in online mode)
                Card placeholder = new Card("placeholder", Suit.Spades, 3, placeholderSprite, backSprite);
                state.Hands[opponentIndex].Add(placeholder);
            }
        }
    }

    static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Creates a list of Card objects from card name strings (for online mode card plays)
    /// </summary>
    public static List<Card> CreateCardsFromNames(List<string> cardNames)
    {
        List<Card> playedCards = new List<Card>();

        if (cardNames == null) return playedCards;
        if (frontSpriteMap == null || backSprite == null) return playedCards;

        foreach (string cardName in cardNames)
        {
            // Skip null or empty card names
            if (string.IsNullOrEmpty(cardName)) continue;

            // Check if cardName exists in sprite map
            if (!frontSpriteMap.ContainsKey(cardName))
            {
                Debug.LogWarning("Card sprite not found for: " + cardName);
                continue;
            }

            Sprite sprite = frontSpriteMap[cardName];
            if (sprite == null)
            {
                Debug.LogWarning("Sprite is null for card: " + cardName);
                continue;
            }

            string[] parts = cardName.Split('_');
            
            // Validate card name format (should be "rank_of_suit")
            if (parts == null || parts.Length < 3)
            {
                Debug.LogWarning("Invalid card name format: " + cardName);
                continue;
            }

            // Validate parts are not null/empty before accessing
            if (string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[2]))
            {
                Debug.LogWarning("Invalid card name parts for: " + cardName);
                continue;
            }

            int rank = Rules.ParseRank(parts[0]);
            Suit suit = Rules.ParseSuit(parts[2]);
            
            // Validate rank and suit are valid
            if (rank <= 0 || rank > 15)
            {
                Debug.LogWarning("Invalid rank for card: " + cardName);
                continue;
            }

            Card card = new Card(cardName, suit, rank, sprite, backSprite);
            if (card != null)
            {
                playedCards.Add(card);
            }
        }

        return playedCards;
    }
}