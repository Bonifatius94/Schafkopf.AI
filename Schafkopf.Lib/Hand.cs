namespace Schafkopf.Lib;

// TODO: remove enumerator, use proper cache mechanism
// TODO: reuse struct memory, immutability is slow
public unsafe struct Hand : IEnumerable<Card>
{
    #region Init

    private static readonly ulong EXISTING_BITMASK;
    private static readonly ulong TRUMPF_BITMASK;
    private static readonly Vector128<byte> ZERO = Vector128.Create((byte)0);
    private static readonly Vector128<ulong> ZERO_U64 = Vector128.Create(0ul);

    private static readonly ulong[] EXISTING_SINGLE;
    private static readonly ulong[] TRUMPF_SINGLE;

    public static readonly Hand EMPTY = new Hand(0);

    static Hand()
    {
        EXISTING_SINGLE = new ulong[8];
        ulong cardCountMask = 0;
        for (byte i = 0; i < 8; i++)
            EXISTING_SINGLE[i] = (ulong)Card.EXISTING_FLAG << (i * 8);
        for (byte i = 0; i < 8; i++)
            cardCountMask |= EXISTING_SINGLE[i];
        EXISTING_BITMASK = cardCountMask;

        TRUMPF_SINGLE = new ulong[8];
        ulong trumpfBitmask = 0;
        for (byte i = 0; i < 8; i++)
            TRUMPF_SINGLE[i] = (ulong)Card.TRUMPF_FLAG << (i * 8);
        for (byte i = 0; i < 8; i++)
            trumpfBitmask |= TRUMPF_SINGLE[i];
        TRUMPF_BITMASK = trumpfBitmask;
    }

    public Hand(ReadOnlySpan<Card> initialHandOfUniqueCards)
    {
        if (initialHandOfUniqueCards.Length != 8)
            throw new ArgumentException("invalid amount of cards on start hand");

        unsafe
        {
            fixed (Card* p = &initialHandOfUniqueCards[0])
                this.cards = *((ulong*)p);
        }
        this.cards |= EXISTING_BITMASK;
    }

    public Hand(ulong cards, bool setExisting = false)
    {
        ulong mask = setExisting ? EXISTING_BITMASK : 0;
        this.cards = cards | mask;
    }

    public Hand CacheTrumpf(Func<Card, bool> isTrumpf)
    {
        ulong newCards = cards;
        for (byte i = 0; i < 8; i++)
            if (hasCardAt(i) && isTrumpf(cardAt(i)))
                newCards |= TRUMPF_SINGLE[i];
        return new Hand(newCards); // TODO: remove allocation
    }

    #endregion Init

    private readonly ulong cards;

    public int CardsCount => BitOperations.PopCount(cards & EXISTING_BITMASK);

    #region Accessors

    private int indexOf(byte cardQuery, byte cardMask)
    {
        // vectorize the card query
        var broadQuery = Vector128.Create(cardQuery);
        var broadMask = Vector128.Create(cardMask);
        var cardsBytes = Vector128.Create(cards).AsByte();

        // carry out the query using XOR + bitwise AND
        // match -> zero byte, so CompareEqual filters those 0x00 bytes
        var broadResult = Sse2.Xor(Sse2.And(cardsBytes, broadMask), broadQuery);
        var eqSimd = Sse2.CompareEqual(broadResult, ZERO);
        ulong equalBytes = eqSimd.AsUInt64().GetElement(0);

        // now, find the lowest matching byte and retrieve its index
        ulong id = (ulong)(BitOperations.TrailingZeroCount(equalBytes) >> 3);

        // handle case when there's no match -> return -1
        var noMatchSimd = Sse42.CompareEqual(eqSimd.AsUInt64(), ZERO_U64);
        ulong noMatch = noMatchSimd.GetElement(0);
        return (int)(id | noMatch);
    }

    public Card this[int i] => cardAt(i);

    // info: cache has to be of size 4 !!!
    public void FirstFour(Card[] cache)
    {
        unsafe
        {
            fixed (Card* cp = &cache[0])
                *((uint*)cp) = (uint)cards;
        }
    }

    private int indexOf(Card card)
    {
        for (int i = 0; i < 8; i++)
            if (hasCardAt(i) && cardAt(i) == card)
                return i;
        return -1;
    }

    private Card cardAt(int index) // TODO: remove allocation
        => new Card((byte)((cards >> (index * 8)) & Card.CARD_MASK_WITH_META));

    private bool hasCardAt(int index)
        => (cards & EXISTING_SINGLE[index]) > 0;

    private bool isTrumpfAt(int index)
        => (cards & TRUMPF_SINGLE[index]) > 0;

    #endregion Accessors

    public bool HasCard(Card card)
        => indexOf(card) >= 0;

    public Hand Discard(Card card)
    {
        int index = indexOf(card);
        if (index == -1)
            throw new ArgumentException($"Player does not have {card} in hand!");

        int offset = 8 - CardsCount;
        ulong newCards = cards;

        unsafe
        {
            ulong* p = &newCards;
            byte* rawCards = (byte*)p;
            rawCards[index] = rawCards[offset];
            rawCards[offset] = (byte)(card.Id & ~Card.EXISTING_FLAG);
        }

        return new Hand(newCards); // TODO: remove allocation
    }

    public IEnumerator<Card> GetEnumerator()
    {
        int offset = 8 - CardsCount;
        for (int i = offset; i < 8; i++)
            yield return cardAt(i);
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    private static readonly Card[] sauenByFarbe =
        new Card[] {
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Sau, CardColor.Herz),
            new Card(CardType.Sau, CardColor.Gras),
            new Card(CardType.Sau, CardColor.Eichel),
        };

    public bool IsSauRufbar(CardColor farbe)
        => !HasCard(sauenByFarbe[(int)farbe]) && HasFarbe(farbe);

    public bool HasTrumpf()
        => indexOf((byte)0x60, 0x60) >= 0;
        // info: trumpf bit and exists bit set

    public bool HasFarbe(CardColor farbe)
        => indexOf((byte)((byte)farbe | Card.EXISTING_FLAG), 0x63) >= 0;
        // info: trumpf bit not set and farbe same as parameter

    public int FarbeCount(CardColor farbe)
    {
        byte cardQuery = (byte)((byte)farbe | Card.EXISTING_FLAG);
        const byte cardMask = 0x63;

        // vectorize the card query
        var broadQuery = Vector128.Create(cardQuery);
        var boradMask = Vector128.Create(cardMask);
        var cardsBytes = Vector128.Create(cards).AsByte();

        // carry out the query using XOR + bitwise AND
        // match -> zero byte, so CompareEqual filters those 0x00 bytes
        var broadResult = Sse2.Xor(Sse2.And(cardsBytes, boradMask), broadQuery);
        var eqSimd = Sse2.CompareEqual(broadResult, ZERO);
        ulong equalBytes = eqSimd.AsUInt64().GetElement(1);

        // now, find the lowest matching byte and retrieve  its index
        int bitsSet = BitOperations.PopCount(equalBytes);
        int count = bitsSet >> 3; // div by 8
        return count;
    }

    #region SimpleImplForBenchmarks

    [Obsolete("Optimized version is faster!")]
    public bool HasTrumpfSimple()
    {
        for (int i = 0; i < 8; i++)
            if (hasCardAt(i) && cardAt(i).IsTrumpf)
                return true;
        return false;
    }

    [Obsolete("Optimized version is faster!")]
    public bool HasFarbeSimple(CardColor farbe)
    {
        for (int i = 0; i < 8; i++)
            if (hasCardAt(i) && !cardAt(i).IsTrumpf && cardAt(i).Color == farbe)
                return true;
        return false;
    }

    [Obsolete("Optimized version is faster!")]
    public int FarbeCountSimple(CardColor farbe)
    {
        int count = 0;
        for (int i = 0; i < 8; i++)
            if (hasCardAt(i) && !cardAt(i).IsTrumpf && cardAt(i).Color == farbe)
                count++;
        return count;
    }

    #endregion SimpleImplForBenchmarks

    public override string ToString()
        => string.Join(", ", this);
}
