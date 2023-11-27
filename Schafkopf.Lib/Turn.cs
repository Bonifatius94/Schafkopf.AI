namespace Schafkopf.Lib;

public readonly struct TurnMetaData
{
    private const byte FIRST_PLAYER_MASK = 0x03;
    private const byte ALREADY_GSUCHT_FLAG = 0x04;

    #region Init

    public TurnMetaData(
        GameCall call,
        int firstDrawingPlayerId,
        bool alreadyGsucht = false)
    {
        Id = (ushort)((firstDrawingPlayerId & 0x03)
            | (alreadyGsucht ? ALREADY_GSUCHT_FLAG : 0x00));
        Call = call;
    }

    #endregion Init

    public readonly ushort Id;
    public readonly GameCall Call;

    public int FirstDrawingPlayerId
        => (int)(Id & FIRST_PLAYER_MASK);
    public bool AlreadyGsucht
        => (Id & ALREADY_GSUCHT_FLAG) > 0;
}

public readonly struct Turn
{
    private const byte CARD_OFFSET = 8;
    private static readonly uint EXISTING_BITMASK;
    private static readonly Vector128<byte> ZERO = Vector128.Create((byte)0);
    private static readonly Vector256<short> MINUS_ONE_16 = Vector256.Create((short)-1);
    private static readonly Vector256<ulong> MAXVALUE = Vector256.Create(0xFFFFFFFFFFFFFFFF);
    private static readonly Card[] EMPTY_CARDS = new Card[4];

    #region Init

    static Turn()
    {
        uint cardCountMask = 0;
        for (byte i = 0; i < 8; i++)
            cardCountMask |= (uint)Card.EXISTING_FLAG << (i * CARD_OFFSET);
        EXISTING_BITMASK = cardCountMask;
    }

    private Turn(TurnMetaData meta, ReadOnlySpan<Card> cards)
    {
        Meta = meta;
        unsafe { fixed (Card* cp = &cards[0]) Cards = *((uint*)cp); }
    }

    private Turn(uint cards_u32, TurnMetaData meta)
    {
        Cards = cards_u32;
        Meta = meta;
    }

    #endregion Init

    public static Turn InitFirstTurn(int firstDrawingPlayerId, GameCall call)
    {
        var meta = new TurnMetaData(call, firstDrawingPlayerId);
        return new Turn(meta, EMPTY_CARDS);
    }

    public static Turn InitNextTurn(Turn last)
    {
        var meta = new TurnMetaData(last.Meta.Call, last.WinnerId, last.AlreadyGsucht);
        return new Turn(meta, EMPTY_CARDS);
    }

    public Turn NextCard(Card card)
    {
        int playerId = (Meta.FirstDrawingPlayerId + CardsCount) & 0x03;
        uint newId = Cards | ((uint)card.Id << (playerId * CARD_OFFSET));
        bool alreadyGsucht = Meta.AlreadyGsucht
            || (FirstCard.Exists && FirstCard.Color == Meta.Call.GsuchteFarbe);
        var newMeta = new TurnMetaData(
            Meta.Call, FirstDrawingPlayerId, alreadyGsucht);
        return new Turn(newId, newMeta);
    }

    private readonly uint Cards;
    private readonly TurnMetaData Meta;

    private Card c1 => new Card((byte)(Cards & Card.CARD_MASK_WITH_META));
    private Card c2 => new Card((byte)((Cards >> 8) & Card.CARD_MASK_WITH_META));
    private Card c3 => new Card((byte)((Cards >> 16) & Card.CARD_MASK_WITH_META));
    private Card c4 => new Card((byte)((Cards >> 24) & Card.CARD_MASK_WITH_META));

    public bool IsTrumpfPlayed => FirstCard.IsTrumpf;
    public bool IsFarbePlayed => !FirstCard.IsTrumpf;
    public CardColor FarbePlayed => FirstCard.Color;

    public int FirstDrawingPlayerId => Meta.FirstDrawingPlayerId;
    public bool AlreadyGsucht => Meta.AlreadyGsucht;
    public int DrawingPlayerId => (FirstDrawingPlayerId + CardsCount) % 4;

    public Card FirstCard => new Card(
        (byte)((Cards >> (Meta.FirstDrawingPlayerId * CARD_OFFSET))
            & Card.CARD_MASK_WITH_META));

    public int CardsCount => BitOperations.PopCount(Cards & EXISTING_BITMASK);

    public Card[] AllCards
    {
        get
        {
            // info: don't optimize, it's only used for printing
            var allCards = new Card[] { c1, c2, c3, c4 };
            return Enumerable.Range(Meta.FirstDrawingPlayerId, 4)
                .Select(i => allCards[i % 4])
                .ToArray()[0..CardsCount];
        }
    }

    public void CopyCards(Card[] cache)
    {
        unsafe { fixed (Card* cp = &cache[0]) *((uint*)cp) = Cards; }
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
        => cardCountByType(Cards, CardType.Unter) * 2
            + cardCountByType(Cards, CardType.Ober) * 3
            + cardCountByType(Cards, CardType.Koenig) * 4
            + cardCountByType(Cards, CardType.Zehn) * 10
            + cardCountByType(Cards, CardType.Sau) * 11;

    #endregion Augen

    #region Winner

    // note: this only yields valid results when the turn is over
    public int WinnerId => winnerId();

    private static readonly CardComparer[] compCache =
        new CardComparer[] {
            new CardComparer(GameMode.Weiter),
            new CardComparer(GameMode.Weiter),
            new CardComparer(GameMode.Weiter),
            new CardComparer(GameMode.Weiter),
            new CardComparer(GameMode.Sauspiel),
            new CardComparer(GameMode.Sauspiel),
            new CardComparer(GameMode.Sauspiel),
            new CardComparer(GameMode.Sauspiel),
            new CardComparer(GameMode.Wenz),
            new CardComparer(GameMode.Wenz),
            new CardComparer(GameMode.Wenz),
            new CardComparer(GameMode.Wenz),
            new CardComparer(GameMode.Solo, CardColor.Schell),
            new CardComparer(GameMode.Solo, CardColor.Herz),
            new CardComparer(GameMode.Solo, CardColor.Gras),
            new CardComparer(GameMode.Solo, CardColor.Eichel),
        };

    private int winnerId()
    {
        // ignore farbe when other farbe played as first card
        // -> make ignored cards "schell sieben" -> lose comparison
        int firstPlayerId = Meta.FirstDrawingPlayerId;
        uint cards_u32 = Cards;
        byte sameFarbeQuery = (byte)((Cards >> (firstPlayerId * CARD_OFFSET)) & 0x03);
        const byte sameFarbeMask = 0x03;
        uint sameFarbeMatches = matchMask(cards_u32, sameFarbeQuery, sameFarbeMask);
        uint trumpfMatches = matchMask(cards_u32, Card.TRUMPF_FLAG, Card.TRUMPF_FLAG);
        cards_u32 = (cards_u32 & sameFarbeMatches) | (cards_u32 & trumpfMatches);
        var comparer = compCache[(int)Meta.Call.Mode * 4 + (int)Meta.Call.Trumpf];

        short cmp_01; short cmp_02; short cmp_03;
        short cmp_12; short cmp_13; short cmp_23;
        unsafe
        {
            Card* cp = (Card*)&cards_u32;
            cmp_01 = (short)comparer.Compare(cp[0], cp[1]);
            cmp_02 = (short)comparer.Compare(cp[0], cp[2]);
            cmp_03 = (short)comparer.Compare(cp[0], cp[3]);
            cmp_12 = (short)comparer.Compare(cp[1], cp[2]);
            cmp_13 = (short)comparer.Compare(cp[1], cp[3]);
            cmp_23 = (short)comparer.Compare(cp[2], cp[3]);
        }

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
        var firstPlayerVec = Vector128.Create((byte)(1 << firstPlayerId));
        ulong firstPlayerHatRechtMask = Sse2.CompareEqual(
                Sse2.Xor(Sse2.And(countsVec, firstPlayerVec), firstPlayerVec), ZERO)
            .AsUInt64().GetElement(0);

        return (int)((firstPlayerHatRechtMask & (ulong)firstPlayerId)
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

    public override string ToString()
        => $"{string.Join(", ", AllCards.Select(x => x.ToString()))}";

    #region Simple

    private static readonly Dictionary<CardType, int> augenByType =
        new Dictionary<CardType, int>() {
            { CardType.Sieben, 0 },
            { CardType.Acht, 0 },
            { CardType.Neun, 0 },
            { CardType.Unter, 2 },
            { CardType.Ober, 3 },
            { CardType.Koenig, 4 },
            { CardType.Zehn, 10 },
            { CardType.Sau, 11 },
        };

    [Obsolete("Optimized version is faster!")]
    public int AugenSimple()
        => AllCards.Select(c => augenByType[c.Type]).Sum();

    [Obsolete("Optimized version is faster!")]
    public int WinnerIdSimple()
    {
        var cards = new Card[4];
        unsafe { fixed (Card* cp = &cards[0]) *((uint*)cp) = Cards; }

        var cardsByPlayer = Enumerable.Range(FirstDrawingPlayerId, CardsCount)
            .ToDictionary(i => i % 4, i => cards[i % 4]);

        var comparer = new CardComparer(Meta.Call.Mode, Meta.Call.Trumpf);
        if (IsTrumpfPlayed)
            return cardsByPlayer.MaxBy(x => x.Value, comparer).Key;

        var farbePlayed = FarbePlayed;
        return cardsByPlayer
            .Where(x => x.Value.Color == farbePlayed || x.Value.IsTrumpf)
            .MaxBy(x => x.Value, comparer).Key;
    }

    #endregion Simple
}
