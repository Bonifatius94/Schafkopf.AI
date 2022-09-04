namespace Schafkopf.Lib;

public class CardsDeck
{
    public static readonly IReadOnlySet<Card> AllCards =
        Enumerable.Range(0, 32)
            .Select(id => new Card((byte)id))
            .ToHashSet();

    // info: use this array to store the cards in-place
    //       8 cards in a row belong to each player
    private readonly Hand[] hands =
        AllCards.Chunk(8)
            .Select(cards => new Hand(cards.ToArray()))
            .ToArray();

    public Hand HandOfPlayer(int playerId)
        => hands[playerId];

    public Hand HandOfPlayerWithMeta(int playerId, GameCall call)
        => hands[playerId].CacheTrumpf(call.IsTrumpf);

    #region Shuffle

    private static readonly EqualDistPermutator permGen =
        new EqualDistPermutator(32);

    public void Shuffle()
    {
        var cards = AllCards.ToList();
        var perm = permGen.NextPermutation();
        var deckCopy = perm.Select(i => cards[i]).ToArray();

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

    // TODO: make this vectorization work for better performance

    //// experimental shuffle with AVX2 256-bit vector byte shuffle
    // public void Shuffle()
    // {
    //     Vector256<byte> cardsVec;
    //     unsafe
    //     {
    //         fixed (Hand* hp = &hands[0])
    //             cardsVec = cardsToVec(hp);
    //     }

    //     var perm = permGen.NextPermutation();
    //     var permVec = permToVec(perm);
    //     var shuffledCardsVec = Avx2.Shuffle(cardsVec, permVec);

    //     unsafe
    //     {
    //         fixed (Hand* hp = &hands[0])
    //         {
    //             Card* cards = (Card*)hp;
    //             vecToCards(shuffledCardsVec, cards);
    //         }
    //     }
    // }

    // private unsafe Vector256<byte> cardsToVec(Hand* hands)
    // {
    //     Vector256<byte> vec;

    //     unsafe
    //     {
    //         ulong* cardBytes = (ulong*)hands;
    //         var vecAsUlong = Vector256.Create(
    //             cardBytes[0],
    //             cardBytes[1],
    //             cardBytes[2],
    //             cardBytes[3]);
    //         vec = vecAsUlong.AsByte();
    //     }

    //     return vec;
    // }

    // private readonly byte[] output = new byte[256];

    // private unsafe void vecToCards(Vector256<byte> vec, Card* output)
    // {
    //     var vecAsUlong = vec.AsUInt64();
    //     ulong v1 = vecAsUlong.GetElement(0);
    //     ulong v2 = vecAsUlong.GetElement(1);
    //     ulong v3 = vecAsUlong.GetElement(2);
    //     ulong v4 = vecAsUlong.GetElement(3);

    //     unsafe
    //     {
    //         ulong* pul = (ulong*)output;
    //         *(pul++) = v1;
    //         *(pul++) = v2;
    //         *(pul++) = v3;
    //         *(pul++) = v4;
    //     }
    // }

    // private Vector256<byte> permToVec(byte[] perm)
    // {
    //     Vector256<byte> vec;

    //     unsafe
    //     {
    //         fixed (byte* p = &perm[0])
    //         {
    //             ulong* pul = (ulong*)p;
    //             var vecAsUlong = Vector256.Create(
    //                 pul[0], pul[1], pul[2], pul[3]);
    //             vec = vecAsUlong.AsByte();
    //         }
    //     }

    //     return vec;
    // }

    #endregion VectorizedShuffle
}

public class EqualDistPermutator
{
    private readonly byte[] ids;

    public EqualDistPermutator(int numItems)
    {
        this.numItems = numItems;
        ids = Enumerable.Range(0, numItems)
            .Select(i => (byte)i).ToArray();
    }

    private int numItems;

    private static readonly Random rng = new Random();

    public byte[] NextPermutation()
    {
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
