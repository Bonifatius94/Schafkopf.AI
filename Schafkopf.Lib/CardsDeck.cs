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

    private static readonly EqualDistPermutator permGen =
        new EqualDistPermutator(32);

    public void Shuffle()
    {
        var cards = AllCards.ToList();
        var perm = permGen.NextPermutation();
        var deckCopy = perm.Select(i => cards[i]).ToArray();
        Array.Copy(deckCopy, Deck, Deck.Length);
    }

    #endregion Shuffle
}

public class EqualDistPermutator
{
    public EqualDistPermutator(int numItems)
        => this.numItems = numItems;

    private int numItems;

    private static readonly Random rng = new Random();

    public IEnumerable<int> NextPermutation()
    {
        var ids = Enumerable.Range(0, numItems).ToArray();

        for (int i = 0; i < numItems; i++)
        {
            int j = rng.Next(i, numItems);
            yield return ids[j];

            if (i != j)
                ids[j] = ids[i];
        }
    }
}
