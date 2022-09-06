using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Schafkopf.Lib;

public readonly struct Turn
{
    private const byte CARD_OFFSET = 8;
    private const ulong CARDS_MASK = 0xFFFFFFFF;
    private const ulong FIRST_PLAYER_MASK = 0x300000000;
    private static readonly ulong EXISTING_BITMASK;
    private static readonly Vector128<byte> ZERO = Vector128.Create((byte)0);
    private static readonly Vector256<short> MINUS_ONE_16 = Vector256.Create((short)-1);
    private static readonly Vector256<ulong> MAXVALUE = Vector256.Create(0xFFFFFFFFFFFFFFFF);
    private static readonly Card[] EMPTY_CARDS = new Card[4];

    #region Init
    
    static Turn()
    {
        ulong cardCountMask = 0;
        for (byte i = 0; i < 8; i++)
            cardCountMask |= (ulong)Card.EXISTING_FLAG << (i * CARD_OFFSET);
        EXISTING_BITMASK = cardCountMask;
    }

    private Turn(byte firstDrawingPlayerId)
       : this(firstDrawingPlayerId, EMPTY_CARDS) { }

    private Turn(byte firstDrawingPlayerId, ReadOnlySpan<Card> cards)
    {
        if (firstDrawingPlayerId < 0 || firstDrawingPlayerId > 3)
            throw new ArgumentException(
                $"Invalid player id {firstDrawingPlayerId}, needs to be within [0, 3]");
        if (cards.Length > 4)
            throw new ArgumentException(
                "Too many cards! A turn only consists of max. 4 cards.");

        ulong id = (ulong)firstDrawingPlayerId << 32;
        unsafe
        {
            fixed (Card* cp = &cards[0])
            {
                uint* cp_u32 = (uint*)cp;
                id |= *cp_u32;
            }
        }

        Id = id;
    }

    private Turn(ulong id) => Id = id;

    #endregion Init

    // TODO: think of putting the game mode as argument and store it in Id
    public static Turn NewTurn(byte firstDrawingPlayerId)
        => new Turn(firstDrawingPlayerId);

    public Turn NextCard(Card card)
    {
        if (CardsCount >= 4)
            throw new InvalidOperationException(
                "Cannot add another card. The turn is already over!");

        int playerId = (FirstDrawingPlayerId + CardsCount) % 4;
        ulong newId = (Id & CARDS_MASK)
            | ((ulong)card.Id << (playerId * CARD_OFFSET))
            | (Id & FIRST_PLAYER_MASK);
        return new Turn(newId);
    }

    private readonly ulong Id;

    public Card C1 => new Card((byte)(Id & Card.CARD_MASK_WITH_META));
    public Card C2 => new Card((byte)((Id >> 8) & Card.CARD_MASK_WITH_META));
    public Card C3 => new Card((byte)((Id >> 16) & Card.CARD_MASK_WITH_META));
    public Card C4 => new Card((byte)((Id >> 24) & Card.CARD_MASK_WITH_META));
    public int FirstDrawingPlayerId => (int)((Id & FIRST_PLAYER_MASK) >> 32);

    public int CardsCount => BitOperations.PopCount(Id & EXISTING_BITMASK);
    public bool IsDone => CardsCount == 4;

    public Card[] AllCards
    {
        get
        {
            var allCards = new Card[] { C1, C2, C3, C4 };
            return Enumerable.Range(FirstDrawingPlayerId, 4)
                .Select(i => allCards[i % 4])
                .ToArray()[0..CardsCount];
        }
    }

    #region Augen

    private int cardCountByType(uint cards, CardType type)
    {
        const byte CARD_TYPE_MASK = 0x1C;
        byte query = (byte)((byte)type << 2);
        uint matches = matchMask(cards, query, CARD_TYPE_MASK);
        int count = BitOperations.PopCount(matches) >> 3;
        return count;
    }

    public int Augen
    {
        get
        {
            // TODO: implement this with AVX2 256-bit vector ops
            //         -> should be even more efficient

            uint cards = (uint)(Id & CARDS_MASK);
            return cardCountByType(cards, CardType.Unter) * 2
                + cardCountByType(cards, CardType.Ober) * 3
                + cardCountByType(cards, CardType.Koenig) * 4
                + cardCountByType(cards, CardType.Zehn) * 10
                + cardCountByType(cards, CardType.Sau) * 11;
        }
    }

    #endregion Augen

    #region Winner

    // TODO: remove the call parameter by caching the game mode in Id
    public int WinnerId(GameCall call)
    {
        if (CardsCount < 4)
            throw new InvalidOperationException(
                "Can only evaluate winner when turn is over!");

        // ignore farbe when other farbe played as first card
        // -> make ignored cards "schell sieben" -> lose comparison
        uint cards_u32 = (uint)(Id & CARDS_MASK);
        byte sameFarbeQuery = (byte)((Id >> (FirstDrawingPlayerId * CARD_OFFSET)) & 0x03);
        const byte sameFarbeMask = 0x03;
        uint sameFarbeMatches = matchMask(cards_u32, sameFarbeQuery, sameFarbeMask);
        uint trumpfMatches = matchMask(cards_u32, Card.TRUMPF_FLAG, Card.TRUMPF_FLAG);
        cards_u32 = (cards_u32 & sameFarbeMatches) | (cards_u32 & trumpfMatches);

        var cards = new Card[4];
        unsafe
        {
            fixed (Card* cp = &cards[0])
            {
                var cp_u32 = (uint*)cp;
                *cp_u32 = cards_u32;
            }
        }

        var comparer = new CardComparer(call.Mode);
        short cmp_01 = (short)comparer.Compare(cards[0], cards[1]);
        short cmp_02 = (short)comparer.Compare(cards[0], cards[2]);
        short cmp_03 = (short)comparer.Compare(cards[0], cards[3]);
        short cmp_12 = (short)comparer.Compare(cards[1], cards[2]);
        short cmp_13 = (short)comparer.Compare(cards[1], cards[3]);
        short cmp_23 = (short)comparer.Compare(cards[2], cards[3]);

        // build a 4x4 matrix with comparisons
        var cmpVec = Vector256.Create(
            cmp_01,           cmp_02,           cmp_03,           (short)1,
            (short)(-cmp_01), cmp_12,           cmp_13,           (short)1,
            (short)(-cmp_02), (short)(-cmp_12), cmp_23,           (short)1,
            (short)(-cmp_03), (short)(-cmp_13), (short)(-cmp_23), (short)1
        );

        // filter the row that wins all comparisons
        var cmpResult = Avx2.CompareGreaterThan(cmpVec, MINUS_ONE_16).AsUInt64();
        cmpResult = Avx2.CompareEqual(cmpResult, MAXVALUE);

        // determine the filtered row's id -> winner id
        ulong bitcnt_0 = (byte)BitOperations.PopCount(cmpResult.GetElement(0));
        ulong bitcnt_1 = (byte)BitOperations.PopCount(cmpResult.GetElement(1));
        ulong bitcnt_2 = (byte)BitOperations.PopCount(cmpResult.GetElement(2));
        ulong bitcnt_3 = (byte)BitOperations.PopCount(cmpResult.GetElement(3));
        // info: winner has popcnt() = 64, all others have popcnt() = 0
        //       -> arrange popcnt() results such that tzcnt() yields the index
        ulong counts = (bitcnt_0 >> 6) | (bitcnt_1 >> 5) | (bitcnt_2 >> 4) | (bitcnt_3 >> 3);
        ulong winnerId = (ulong)BitOperations.TrailingZeroCount(counts);

        // when first player's bit is set, always pick first player
        // -> this implements first player 'hat Recht' rule
        var countsVec = Vector128.Create((byte)counts);
        var firstPlayerVec = Vector128.Create((byte)(1 << FirstDrawingPlayerId));
        ulong firstPlayerHatRechtMask = Sse2.CompareEqual(
                Sse2.Xor(Sse2.And(countsVec, firstPlayerVec), firstPlayerVec), ZERO)
            .AsUInt64().GetElement(0);

        return (int)((firstPlayerHatRechtMask & (ulong)FirstDrawingPlayerId)
            | (~firstPlayerHatRechtMask & winnerId));
    }

    #endregion Winner

    #region Matching

    private uint matchMask(uint cards, byte query, byte mask)
    {
        var cardsVec = Vector128.Create(cards).AsByte();
        var queryVec = Vector128.Create(query);
        var maskVec = Vector128.Create(mask);
        var result = Sse2.Xor(Sse2.And(cardsVec, maskVec), queryVec);
        var eqSimd = Sse2.CompareEqual(result, ZERO);
        uint resultMask = eqSimd.AsUInt32().GetElement(0);
        return resultMask;
    }

    #endregion Matching
}
