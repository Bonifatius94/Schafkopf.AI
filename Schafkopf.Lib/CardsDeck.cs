namespace Schafkopf.Lib;

public class CardsDeck
{
    public static readonly IReadOnlySet<Card> AllCards =
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
