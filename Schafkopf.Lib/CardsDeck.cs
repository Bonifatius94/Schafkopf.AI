namespace Schafkopf.Lib;

public class CardsDeck : IEnumerable<Hand>
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

    #region Enumerate

    public Card this[int i] => hands[i / 8][i % 8];

    public IEnumerator<Hand> GetEnumerator()
    {
        for (int i = 0; i < 4; i++)
            yield return hands[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    #endregion Enumerate

    public Hand[] InitialHands()
    {
        var hands = new Hand[4];
        hands[0] = this.hands[0];
        hands[1] = this.hands[1];
        hands[2] = this.hands[2];
        hands[3] = this.hands[3];
        return hands;
    }

    public Hand[] InitialHands(GameCall call)
    {
        var hands = new Hand[4];
        hands[0] = this.hands[0].CacheTrumpf(call.IsTrumpf);
        hands[1] = this.hands[1].CacheTrumpf(call.IsTrumpf);
        hands[2] = this.hands[2].CacheTrumpf(call.IsTrumpf);
        hands[3] = this.hands[3].CacheTrumpf(call.IsTrumpf);
        return hands;
    }

    #region Simple

    public Hand[] InitialHandsSimple()
        => Enumerable.Range(0, 4)
            .Select(i => hands[i])
            .ToArray();

    public Hand[] InitialHandsSimple(GameCall call)
        => Enumerable.Range(0, 4)
            .Select(i => hands[i].CacheTrumpf(call.IsTrumpf))
            .ToArray();

    #endregion Simple

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
        // init perms with 0, 1, 2, ..., n
        var inputPerm = new byte[4];
        var shufPerms = new byte[32];
        initPerms(inputPerm, shufPerms);

        // draw perms for shuffling
        permGenHighLow.NextPermutation(shufPerms.AsSpan(0, 16));
        permGenHighLow.NextPermutation(shufPerms.AsSpan(16, 16));
        permGenInput.NextPermutation(inputPerm);

        // load cards and permutation vector for SIMD shuffle
        Vector256<byte> cardsVec; Vector256<byte> permVec;
        unsafe
        {
            fixed (byte* permBytes = &shufPerms[0])
                permVec = Vector256.Load<byte>(permBytes);

            // permutate input bytes in 8-byte chunks
            // -> all cards can reach every deck position eventually
            // -> takes 2 shuffles for cards to be equal dist.
            fixed (Hand* hp = &hands[0])
            {
                ulong* cards_u64 = (ulong*)hp;
                var vecAsUlong = Vector256.Create(
                    cards_u64[inputPerm[0]], cards_u64[inputPerm[1]],
                    cards_u64[inputPerm[2]], cards_u64[inputPerm[3]]);
                cardsVec = vecAsUlong.AsByte();
            }
        }

        // shuffle bytes within upper and lower 128 bit vector
        var shufVec = Avx2.Shuffle(cardsVec, permVec);
        unsafe { fixed (Hand* hp = &hands[0]) shufVec.Store((byte*)hp); }
    }

    private void initPerms(byte[] inputPerms, byte[] shufPerms)
    {
        const ulong defaultShufPerm_0 = 0x0706050403020100;
        const ulong defaultShufPerm_1 = 0x0F0E0D0C0B0A0908;
        const uint defaultInputPerm = 0x00010203;

        unsafe
        {
            fixed (byte* permBytes = &inputPerms[0])
            {
                uint* permBytes_u32 = (uint*)permBytes;
                permBytes_u32[0] = defaultInputPerm;
            }

            fixed (byte* permBytes = &shufPerms[0])
            {
                ulong* permBytes_u64 = (ulong*)permBytes;
                permBytes_u64[0] = defaultShufPerm_0;
                permBytes_u64[1] = defaultShufPerm_1;
                permBytes_u64[2] = defaultShufPerm_0;
                permBytes_u64[3] = defaultShufPerm_1;
            }
        }
    }

    #endregion VectorizedShuffle
}

public class EqualDistPermutator_256
{
    private readonly byte[] cache_ids;

    public EqualDistPermutator_256(int numItems)
    {
        this.numItems = numItems;
        cache_ids = Enumerable.Range(0, numItems)
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
        NextPermutation(cache_ids);
        return cache_ids;
    }
}
