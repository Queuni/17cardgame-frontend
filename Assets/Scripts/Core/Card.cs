using UnityEngine;

[System.Serializable]
public class Card
{
    public string name;
    public Suit suit;
    public int rank; // 3=3 ... 13=K, 14=A, 15=2
    public Sprite frontSprite;
    public Sprite backSprite;

    public Card(string name, Suit suit, int rank, Sprite frontSprite, Sprite backSprite)
    {
        this.name = name;
        this.suit = suit;
        this.rank = rank;
        this.frontSprite = frontSprite;
        this.backSprite = backSprite;
    }

    public void UpdateCardSprite(string cardName)
    {
        string[] parts = cardName.Split('_');
        if (parts.Length != 3)
        {
            Debug.Log("⚠ Invalid card name: " + cardName);
            return;
        }

        Sprite sprite = Resources.Load<Sprite>($"images/card_front/{cardName}");
        int rank = Rules.ParseRank(parts[0]);
        Suit suit = Rules.ParseSuit(parts[2]);

        this.suit = suit;
        this.rank = rank;
        this.frontSprite = sprite;
    }

    public override string ToString()
    {
        return this.name;
    }
}

