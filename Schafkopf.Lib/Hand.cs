using System.Collections;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Schafkopf.Lib;

public readonly struct Hand : IEnumerable<Card>
{
    #region Init

    private const byte CARD_OFFSET = 8;
    private static readonly ulong EXISTING_BITMASK;
    private static readonly ulong TRUMPF_BITMASK;
    private static readonly Vector128<byte> ZERO = Vector128.Create((byte)0);
    private static readonly Vector128<ulong> ZERO_U64 = Vector128.Create(0ul);

    static Hand()
    {
        ulong cardCountMask = 0;
        for (byte i = 0; i < 8; i++)
            cardCountMask |= (ulong)Card.EXISTING_FLAG << (i * CARD_OFFSET);
        EXISTING_BITMASK = cardCountMask;

        ulong trumofBitmask = 0;
        for (byte i = 0; i < 8; i++)
            trumofBitmask |= (ulong)Card.TRUMPF_FLAG << (i * CARD_OFFSET);
        TRUMPF_BITMASK = trumofBitmask;
    }

    public Hand(ReadOnlySpan<Card> initialHandOfUniqueCards)
    {
        ulong cards = 0;

        for (byte i = 0; i < initialHandOfUniqueCards.Length; i++)
        {
            var card = initialHandOfUniqueCards[i];
            cards |= ((ulong)card.Id | Card.EXISTING_FLAG) << (i * CARD_OFFSET);
        }

        this.cards = cards;
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
                newCards |= (ulong)Card.TRUMPF_FLAG << (i * CARD_OFFSET);
        return new Hand(newCards);
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

    private int indexOf(Card card)
        => indexOf((byte)(Card.EXISTING_FLAG | card.Id), 0x3F);

    private Card cardAt(int index)
        => new Card((byte)((cards >> (index * CARD_OFFSET)) & Card.CARD_MASK_WITH_META));

    private bool hasCardAt(int index)
        => (cards & ((ulong)Card.EXISTING_FLAG << (index * CARD_OFFSET))) > 0;

    private bool isTrumpfAt(int index)
        => (cards & ((ulong)Card.TRUMPF_FLAG << (index * CARD_OFFSET))) > 0;

    #endregion Accessors

    public bool HasCard(Card card)
        => indexOf(card) >= 0;

    public Hand Discard(Card card)
    {
        int index = indexOf(card);
        if (index == -1)
            throw new ArgumentException(
                $"Player does not have {card} in hand!");

        ulong newCards = cards ^ ((ulong)Card.EXISTING_FLAG << (index * CARD_OFFSET));
        return new Hand(newCards);
    }

    public IEnumerator<Card> GetEnumerator()
    {
        for (int i = 0; i < 8; i++)
            if (hasCardAt(i))
                yield return cardAt(i);
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public bool HasTrumpf()
        => (cards & TRUMPF_BITMASK) > 0;

    public bool HasFarbe(CardColor farbe)
        => indexOf((byte)farbe, 0x43) >= 0;
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
}
