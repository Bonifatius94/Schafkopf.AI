using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Schafkopf.Lib;

public class CardsDeck
{
    // TODO: re-design this to ensure that the meta-data of Hand is always initialized

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

    public void Shuffle()
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

    public void ShuffleSimd()
    {
        // info: this implementation works, but it's unclear
        //       whether it's actually faster than the other one

        // TODO: init the permutators to generate different outputs
        var permGenUpper = new EqualDistPermutator_256(16);
        var permGenLower = new EqualDistPermutator_256(16);
        var permGenInterm = new EqualDistPermutator_256(4);
        var permGenOut = new EqualDistPermutator_256(4);

        // shuffle bytes within higher and lower 16 bytes
        var upperPerm = permGenUpper.NextPermutation();
        var lowerPerm = permGenLower.NextPermutation();
        var intermPerm = permGenInterm.NextPermutation();
        var outPerm = permGenOut.NextPermutation();

        Vector256<byte> cardsVec;
        unsafe
        {
            fixed (Hand* hp = &hands[0])
                cardsVec = cardsToVec(hp);
        }

        // shuffle bytes within upper and lower 128 bit vector
        var permVec = permToVec(upperPerm, lowerPerm);
        var intermVec = Avx2.Shuffle(cardsVec, permVec).AsUInt64();
        // pull shuffled bytes out by random order as 64-bit blocks
        cardsVec = Vector256.Create(
                intermVec.GetElement(intermPerm[0]),
                intermVec.GetElement(intermPerm[1]),
                intermVec.GetElement(intermPerm[2]),
                intermVec.GetElement(intermPerm[3])
            ).AsByte();
        upperPerm = permGenUpper.NextPermutation();
        lowerPerm = permGenLower.NextPermutation();
        permVec = permToVec(upperPerm, lowerPerm);
        // shuffle bytes again within upper and lower 128-bit vector
        var shuffledCardsVec = Avx2.Shuffle(cardsVec, permVec);

        unsafe
        {
            fixed (Hand* hp = &hands[0])
            {
                Card* cards = (Card*)hp;
                var vecAsUlong = shuffledCardsVec.AsUInt64();
                ulong* pul = (ulong*)cards;
                *(pul++) = vecAsUlong.GetElement(outPerm[0]);
                *(pul++) = vecAsUlong.GetElement(outPerm[1]);
                *(pul++) = vecAsUlong.GetElement(outPerm[2]);
                *(pul++) = vecAsUlong.GetElement(outPerm[3]);
            }
        }
    }

    private unsafe Vector256<byte> cardsToVec(Hand* hands)
    {
        // TODO: replace this with Gather() in .NET 7
        Vector256<byte> vec;

        unsafe
        {
            ulong* cardBytes = (ulong*)hands;
            var vecAsUlong = Vector256.Create(
                cardBytes[0],
                cardBytes[1],
                cardBytes[2],
                cardBytes[3]);
            vec = vecAsUlong.AsByte();
        }

        return vec;
    }

    private Vector256<byte> permToVec(byte[] upperPerm, byte[] lowerPerm)
    {
        // TODO: replace this with Gather() in .NET 7
        Vector256<byte> vec;

        unsafe
        {
            fixed (byte* up = &upperPerm[0])
            {
                fixed (byte* lp = &lowerPerm[0])
                {
                    ulong* up_u64 = (ulong*)up;
                    ulong* lp_u64 = (ulong*)lp;
                    var vecAsUlong = Vector256.Create(
                        up_u64[0], up_u64[1], lp_u64[0], lp_u64[1]);
                    vec = vecAsUlong.AsByte();
                }
            }
        }

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
