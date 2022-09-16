using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Schafkopf.Lib;

public class CardsDeck
{
    public static readonly IReadOnlySet<Card> AllCards =
        Enumerable.Range(0, 32)
            .Select(id => new Card((byte)id))
            .ToHashSet();

    // info: use this array to store the cards in-place,
    //       8 cards in a row belong to each player
    private readonly Hand[] hands =
        AllCards.Chunk(8)
            .Select(cards => new Hand(cards.ToArray()))
            .ToArray();

    public Hand HandOfPlayer(int playerId)
        => hands[playerId];

    public Hand HandOfPlayerWithMeta(int playerId, GameCall call)
        => hands[playerId].CacheTrumpf(call.IsTrumpf);

    public Hand[] InitialHands()
        => Enumerable.Range(0, 4)
            .Select(i => HandOfPlayer(i))
            .ToArray();

    public Hand[] InitialHands(GameCall call)
        => Enumerable.Range(0, 4)
            .Select(i => HandOfPlayerWithMeta(i, call))
            .ToArray();

    // TODO: find a better performing implementation
    public IEnumerable<Card> AllCardsWithMeta(GameCall call)
        => Enumerable.Range(0, 4)
            .SelectMany(i => HandOfPlayerWithMeta(i, call))
            .ToList();

    #region Shuffle

    private static readonly EqualDistPermutator_256 permGen =
        new EqualDistPermutator_256(32);

    public void ShuffleSimple()
    {
        var cards = AllCards.ToList();
        var perm = permGen.NextPermutation();

        var deckCopy = new Card[32];
        for (int i = 0; i < 32; i++)
            deckCopy[i] = cards[perm[i]];

        unsafe
        {
            fixed (Card* deckp = &deckCopy[0])
            {
                ulong* dpul = (ulong*)deckp;
                hands[0] = new Hand(dpul[0], setExisting: true);
                hands[1] = new Hand(dpul[1], setExisting: true);
                hands[2] = new Hand(dpul[2], setExisting: true);
                hands[3] = new Hand(dpul[3], setExisting: true);
            }
        }
    }

    #endregion Shuffle

    #region VectorizedShuffle

    private static readonly EqualDistPermutator_256 permGenHighLow =
        new EqualDistPermutator_256(16);
    private static readonly EqualDistPermutator_256 permGenInput =
        new EqualDistPermutator_256(4);

    public void Shuffle()
    {
        // shuffle bytes within higher and lower 16 bytes
        var shufPerms = new byte[32];
        permGenHighLow.NextPermutation(shufPerms[0..16]);
        permGenHighLow.NextPermutation(shufPerms[16..32]);
        var intermPerm = permGenInput.NextPermutation();

        Vector256<byte> cardsVec; Vector256<byte> permVec;
        unsafe
        {
            fixed (byte* permBytes = &shufPerms[0])
                permVec = Vector256.Load<byte>(permBytes);

            fixed (Hand* hp = &hands[0])
                cardsVec = cardsToVec(hp, intermPerm);
        }

        // shuffle bytes within upper and lower 128 bit vector
        var shufVec = Avx2.Shuffle(cardsVec, permVec);
        unsafe { fixed (Hand* hp = &hands[0]) shufVec.Store((byte*)hp); }
    }

    private unsafe Vector256<byte> cardsToVec(Hand* hands, byte[] perm)
    {
        // TODO: replace this with Gather() in .NET 7
        Vector256<byte> vec;

        ulong* cardBytes = (ulong*)hands;
        var vecAsUlong = Vector256.Create(
            cardBytes[perm[0]], cardBytes[perm[1]],
            cardBytes[perm[2]], cardBytes[perm[3]]);
        vec = vecAsUlong.AsByte();

        return vec;
    }

    #endregion VectorizedShuffle
}

public class EqualDistPermutator_256
{
    private readonly byte[] ids;

    public EqualDistPermutator_256(int numItems)
    {
        this.numItems = numItems;
        ids = Enumerable.Range(0, numItems)
            .Select(i => (byte)i).ToArray();
    }

    private int numItems;

    private static readonly Random rng = new Random();

    public void NextPermutation(Span<byte> ids)
    {
        // TODO: find an intrinsic for doing this

        for (int i = 0; i < numItems; i++)
        {
            int j = rng.Next(i, numItems);

            if (i != j)
            {
                byte temp = ids[i];
                ids[i] = ids[j];
                ids[j] = temp;
            }
        }
    }

    public byte[] NextPermutation()
    {
        // TODO: find an intrinsic for doing this

        for (int i = 0; i < numItems; i++)
        {
            int j = rng.Next(i, numItems);

            if (i != j)
            {
                byte temp = ids[i];
                ids[i] = ids[j];
                ids[j] = temp;
            }
        }

        return ids;
    }
}
