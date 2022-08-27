using System.Diagnostics.CodeAnalysis;

namespace Schafkopf.Lib;

public enum CardType
{
    Sieben,
    Acht,
    Neun,
    Unter,
    Ober,
    Koenig,
    Zehn,
    Sau
}

public enum CardColor
{
    Schell,
    Herz,
    Gras,
    Eichel
}

public readonly struct Card
{
    public Card(CardType type, CardColor color)
    {
        Id = (byte)(((byte)type << 2) | (byte)color);
    }

    public Card(byte id)
    {
        Id = id;
    }

    public byte Id { get; init; }

    public CardType Type => (CardType)(Id >> 2);
    public CardColor Color => (CardColor)(Id & 3);

    #region Equality

    public override bool Equals([NotNullWhen(true)] object obj)
        => obj != null && obj is Card c && c.Id == this.Id;

    public override int GetHashCode() => Id;

    #endregion Equality

    public override string ToString() => $"{Color} {Type}";
    // TODO: add an emoji format
}

public class CardsDeck
{
    public CardsDeck() => Shuffle();

    public static IReadOnlySet<Card> AllCards =
        Enumerable.Range(0, 32)
            .Select(id => new Card((byte)id))
            .ToHashSet();

    public readonly Card[] Deck = AllCards.ToArray();

    public Hand HandOfPlayer(int playerId)
        => new Hand(Deck[(playerId*8)..((playerId+1)*8)]);

    #region Shuffle

    private static readonly Random rng = new Random();

    public void Shuffle()
    {
        var cards = AllCards.ToList();
        var perm = randomPermutation(cards.Count);
        var deckCopy = perm.Select(i => cards[i]).ToArray();
        Array.Copy(deckCopy, Deck, Deck.Length);
    }

    private IEnumerable<int> randomPermutation(int count)
    {
        var ids = Enumerable.Range(0, count).ToArray();

        for (int i = 0; i < count; i++)
        {
            int j = rng.Next(i, count);
            yield return ids[j];

            if (i != j)
                ids[j] = ids[i];
        }
    }

    #endregion Shuffle
}

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
    public int FirstDrawingPlayerId => Id >> 20;
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

public class Hand
{
    public Hand(IReadOnlyList<Card> initialHandOfUniqueCards)
        : this(initialHandOfUniqueCards.ToHashSet()) { }

    public Hand(ISet<Card> initialCardsInHand)
    {
        if (initialCardsInHand.Count != 8)
            throw new ArgumentException("Expected 8 cards on initial hand!");
        Cards = initialCardsInHand;
    }

    public ISet<Card> Cards { get; private set; }

    public bool HasCard(Card card)
        => Cards.Contains(card);

    public Card Discard(Card card)
    {
        Cards.Remove(card);
        return card;
    }

    public bool HasTrumpf(Func<Card, bool> isTrumpf)
        => Cards.Any(c => isTrumpf(c));

    private static readonly HashSet<CardType> farbeTypes =
        new HashSet<CardType>() {
            CardType.Sieben,
            CardType.Acht,
            CardType.Neun,
            CardType.Koenig,
            CardType.Zehn,
            CardType.Sau
        };

    public bool HasFarbe(CardColor gsucht, Func<Card, bool> isTrumpf)
        => Cards.Any(c => c.Color == gsucht && !isTrumpf(c));

    public int FarbeCount(CardColor farbe, Func<Card, bool> isTrumpf)
        => Cards.Where(c => c.Color == farbe && !isTrumpf(c)).Count();
}

public interface ISchafkopfPlayer
{
    Hand Hand { get; }

    void NewGame(Hand hand);
    void OnInvalidCardPicked(Card card);
    Card ChooseCard(Turn state);
    void OnGameFinished(GameHistory game);
}
