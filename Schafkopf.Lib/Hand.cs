namespace Schafkopf.Lib;

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
