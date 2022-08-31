namespace Schafkopf.Lib;

public readonly struct Turn
{
    private Turn(byte firstDrawingPlayerId)
       : this(firstDrawingPlayerId, Array.Empty<Card>()) { }

    public Turn(byte firstDrawingPlayerId, IReadOnlyList<Card> cards)
    {
        if (firstDrawingPlayerId < 0 || firstDrawingPlayerId > 3)
            throw new ArgumentException(
                $"Invalid player id {firstDrawingPlayerId}, needs to be within [0, 3]");
        if (cards.Count > 4)
            throw new ArgumentException(
                "Too many cards! A turn only consists of max. 4 cards.");

        int id = firstDrawingPlayerId << 20;
        for (int i = 0; i < cards.Count; i++)
            id |= cards[i].Id << (i * 5);
        id |= cards.Count << 22;

        Id = id;
    }

    private Turn(int id) => Id = id;

    public static Turn FromId(int id) => new Turn(id);
    public static Turn NewTurn(byte firstDrawingPlayerId)
        => new Turn(firstDrawingPlayerId);

    public readonly int Id;

    public Card C1 => new Card((byte)(Id & 0x1F));
    public Card C2 => new Card((byte)((Id >> 5) & 0x1F));
    public Card C3 => new Card((byte)((Id >> 10) & 0x1F));
    public Card C4 => new Card((byte)((Id >> 15) & 0x1F));
    public int FirstDrawingPlayerId => (Id >> 20) & 0x3;
    public int CardsCount => Id >> 22;
    public bool IsDone => CardsCount == 4;
    public Card[] AllCards => new Card[] { C1, C2, C3, C4 };

    #region Augen

    private static readonly Dictionary<CardType, int> augenByCardType =
        new Dictionary<CardType, int>() {
            { CardType.Sieben, 0 },
            { CardType.Acht, 0 },
            { CardType.Neun, 0 },
            { CardType.Unter, 2 },
            { CardType.Ober, 3 },
            { CardType.Koenig, 4 },
            { CardType.Zehn, 10 },
            { CardType.Sau, 11 },
        };

    public int Augen
        => new Card[] { C1, C2, C3, C4 }
            .Select(c => augenByCardType[c.Type])
            .Sum();
    // info: Calling this when the turn is not finished yet
    //       yields correct results because Schell 7 with Id=0
    //       is worth 0 Augen, so the sum is the same.

    #endregion Augen

    // TODO: think of transforming this into a list of (player_id, card) tuples
    //       reason: O(1) read access is not required by the implementation
    public IReadOnlyDictionary<int, Card> CardsByPlayer()
    {
        int firstDrawingPlayerId = FirstDrawingPlayerId;
        var cardsAsArray = new Card[] { C1, C2, C3, C4 };
        return Enumerable.Range(0, CardsCount)
            .ToDictionary(
                i => (i + firstDrawingPlayerId) % 4,
                i => cardsAsArray[i]);
    }

    public Turn NextCard(Card card)
    {
        if (CardsCount >= 4)
            throw new InvalidOperationException(
                "Cannot add another card. The turn is already over!");
        int newCardBits = card.Id << (CardsCount * 5);
        int newId = (Id & 0x3FFFFF) | newCardBits | ((CardsCount + 1) << 22);
        return new Turn(newId);
    }
}
