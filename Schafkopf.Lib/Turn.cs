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
    private static readonly Vector256<short> ZERO_16 = Vector256.Create((short)0);
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

        ulong id = (uint)firstDrawingPlayerId << 32;
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
    // public Card C2 => new Card((byte)((Id >> 8) & Card.CARD_MASK_WITH_META));
    // public Card C3 => new Card((byte)((Id >> 16) & Card.CARD_MASK_WITH_META));
    // public Card C4 => new Card((byte)((Id >> 24) & Card.CARD_MASK_WITH_META));
    public int FirstDrawingPlayerId => (int)((Id & FIRST_PLAYER_MASK) >> 32);

    public int CardsCount => BitOperations.PopCount(Id & EXISTING_BITMASK);
    public bool IsDone => CardsCount == 4;

    #region Augen

    private int cardCountByType(uint cards, CardType type)
    {
        const byte mask = 0x1C;
        byte query = (byte)((byte)type << 2);
        var cardsVec = Vector128.Create(cards).AsByte();
        var queryVec = Vector128.Create(query);
        var maskVec = Vector128.Create(mask);

        // match all cards of given card type
        var result = Sse2.Xor(Sse2.And(cardsVec, maskVec), queryVec);
        var eqSimd = Sse2.CompareEqual(result, ZERO);
        ulong equalBytes = eqSimd.AsUInt32().GetElement(0);

        // count the amount of matching bytes (divide bit count by 8)
        int count = BitOperations.PopCount(equalBytes) >> 3;
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

    public int WinnerId(GameCall call)
    {
        if (CardsCount < 4)
            throw new InvalidOperationException(
                "Can only evaluate winner when turn is over!");

        var c1 = C1;
        bool isTrumpfTurn = c1.IsTrumpf;

        var cards = new Card[4];
        unsafe
        {
            fixed (Card* cp = &cards[0])
            {
                var cp_u32 = (uint*)cp;
                *cp_u32 = (uint)(Id & CARDS_MASK);
            }
        }

        var cardsByPlayer = Enumerable.Range(FirstDrawingPlayerId, CardsCount)
            .ToDictionary(i => i % 4, i => cards[i]);

        var comparer = new CardComparer(call);
        if (isTrumpfTurn)
            return cardsByPlayer.MaxBy(x => x.Value, comparer).Key;

        return cardsByPlayer
            .Where(x => x.Value.Color == c1.Color || call.IsTrumpf(x.Value))
            .MaxBy(x => x.Value, comparer).Key;
    }

    public int WinnerIdSimd(GameCall call)
    {
        // TODO: test if this works now

        var cards = new Card[4];
        unsafe
        {
            fixed (Card* cp = &cards[0])
            {
                var cp_u32 = (uint*)cp;
                *cp_u32 = (uint)(Id & CARDS_MASK);
            }
        }

        var comparer = new CardComparer(call);
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
        var cmpResult = Avx2.CompareGreaterThan(cmpVec, ZERO_16).AsUInt64();
        cmpResult = Avx2.CompareEqual(cmpResult, MAXVALUE);

        // determine the filtered row's id -> winner id
        uint bitcnt_0 = (byte)BitOperations.PopCount(cmpResult.GetElement(0));
        uint bitcnt_1 = (byte)BitOperations.PopCount(cmpResult.GetElement(1));
        uint bitcnt_2 = (byte)BitOperations.PopCount(cmpResult.GetElement(2));
        uint bitcnt_3 = (byte)BitOperations.PopCount(cmpResult.GetElement(3));
        // info: winner has popcnt() = 64, all others have popcnt() = 0
        //       -> arrange popcnt() results such that tzcnt() yields the index
        uint counts = (bitcnt_0 >> 5) | (bitcnt_1 >> 4) | (bitcnt_2 >> 3) | (bitcnt_3 >> 2);
        int winnerId = BitOperations.TrailingZeroCount(counts);
        return winnerId;
    }

    #endregion Winner
}
