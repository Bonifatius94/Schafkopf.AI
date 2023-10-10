namespace Schafkopf.Lib;

public enum GameMode
{
    Weiter,
    Sauspiel,
    Wenz,
    Solo
}

public readonly struct GameCall
{
    #region Init

    private GameCall(
        GameMode mode,
        int callingPlayerId,
        int partnerPlayerId,
        CardColor trumpf,
        CardColor gsuchteFarbe,
        bool isTout)
    {
        Id = (ushort)(
            (isTout ? 1 : 0) |
            ((int)mode << 1) |
            (callingPlayerId << 3) |
            (partnerPlayerId << 5) |
            ((int)trumpf << 7) |
            ((int)gsuchteFarbe << 9)
        );
    }

    private static readonly GameCall WEITER = new GameCall(
        GameMode.Weiter, 0, 0,
        CardColor.Schell, CardColor.Schell, false);

    public static GameCall Weiter() => WEITER;

    public static GameCall Sauspiel(
            int callingPlayerId,
            int partnerPlayerId,
            CardColor gsuchteFarbe)
        => new GameCall(
            GameMode.Sauspiel, callingPlayerId, partnerPlayerId,
            CardColor.Herz, gsuchteFarbe, false);

    public static GameCall Wenz(
            int callingPlayerId,
            bool isTout = false)
        => new GameCall(
            GameMode.Wenz, callingPlayerId, 0,
            CardColor.Schell, CardColor.Schell, isTout);

    public static GameCall Solo(
            int callingPlayerId,
            CardColor trumpf,
            bool isTout = false)
        => new GameCall(
            GameMode.Solo, callingPlayerId, 0,
            trumpf, CardColor.Schell, isTout);

    #endregion Init

    public readonly ushort Id;

    public bool IsTout => (Id  & 0x01) > 0;
    public GameMode Mode => (GameMode)((Id & 0x06) >> 1);
    public byte CallingPlayerId => (byte)((Id & 0x18) >> 3);
    public byte PartnerPlayerId => (byte)((Id & 0x60) >> 5);
    public CardColor Trumpf => (CardColor)((Id & 0x180) >> 7);
    public CardColor GsuchteFarbe => (CardColor)((Id & 0x600) >> 9);

    public Card GsuchteSau => new Card(CardType.Sau, GsuchteFarbe);

    #region Trumpf

    private static readonly TrumpfEvaluator[] evaluators =
        new TrumpfEvaluator[] {
            new TrumpfEvaluator(GameMode.Weiter),
            new TrumpfEvaluator(GameMode.Weiter),
            new TrumpfEvaluator(GameMode.Weiter),
            new TrumpfEvaluator(GameMode.Weiter),
            new TrumpfEvaluator(GameMode.Sauspiel),
            new TrumpfEvaluator(GameMode.Sauspiel),
            new TrumpfEvaluator(GameMode.Sauspiel),
            new TrumpfEvaluator(GameMode.Sauspiel),
            new TrumpfEvaluator(GameMode.Wenz),
            new TrumpfEvaluator(GameMode.Wenz),
            new TrumpfEvaluator(GameMode.Wenz),
            new TrumpfEvaluator(GameMode.Wenz),
            new TrumpfEvaluator(GameMode.Solo, CardColor.Schell),
            new TrumpfEvaluator(GameMode.Solo, CardColor.Herz),
            new TrumpfEvaluator(GameMode.Solo, CardColor.Gras),
            new TrumpfEvaluator(GameMode.Solo, CardColor.Eichel),
        };

    public Func<Card, bool> IsTrumpf
        => evaluators[(int)Mode * 4 + (int)Trumpf].IsTrumpf;

    #endregion Trumpf

    public override string ToString()
    {
        if (Mode == GameMode.Weiter)
            return "Weiter";
        else if (Mode == GameMode.Sauspiel)
            return $"Sauspiel mit der {GsuchteFarbe} Sau";
        else if (Mode == GameMode.Wenz)
            return $"Wenz{(IsTout ? " Tout" : "")}";
        else if (Mode == GameMode.Solo)
            return $"{Trumpf} Solo{(IsTout ? " Tout" : "")}";
        return $"unknown game mode {Mode}";
    }

    #region Equality

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is GameCall call && call.Id == Id;

    public override int GetHashCode() => Id;

    #endregion Equality
}

[Obsolete("New AllPossibleCalls() function implements this by other convention.")]
public class GameCallComparer : IComparer<GameCall>
{
    public int Compare(GameCall x, GameCall y)
    {
        int modeDiff = (int)x.Mode - (int)y.Mode;
        if (modeDiff != 0)
            return modeDiff;

        if (x.Mode == GameMode.Sauspiel)
            return 0;

        int xTout = x.IsTout ? 1 : 0;
        int yTout = y.IsTout ? 1 : 0;
        return xTout - yTout;
    }
}

public class GameCallGenerator
{
    #region Init

    private static readonly GameCallComparer callComp = new GameCallComparer();

    private static readonly List<CardColor> soloTrumpf =
        new List<CardColor>() {
            CardColor.Schell,
            CardColor.Herz,
            CardColor.Gras,
            CardColor.Eichel
        };

    private static readonly List<bool> touts =
        new List<bool>() { false, true };

    private static readonly GameCall[][] wenzenByPlayer =
        Enumerable.Range(0, 4)
            .Select(playerId => touts.Select(
                tout => GameCall.Wenz(playerId, tout)).ToArray())
            .ToArray();
    private static readonly GameCall[][] soliByPlayer =
        Enumerable.Range(0, 4)
            .Select(playerId => touts.SelectMany(
                tout => soloTrumpf.Select(
                    trumpf => GameCall.Solo(playerId, trumpf, tout)))
                .ToArray())
            .ToArray();
    private static readonly TrumpfEvaluator sauspielEval =
        new TrumpfEvaluator(GameMode.Sauspiel);

    private static readonly CardColor[] allRufbareFarben =
        new CardColor[] { CardColor.Schell, CardColor.Gras, CardColor.Eichel };

    private GameCall[] sauspiele = new GameCall[3];
    private GameCall[] allCalls = new GameCall[14];
    // info: 3x sauspiel + wenz + wenz tout + 4x solo + 4x solo tout
    private static readonly int[] lastCallOffsets = new int[] { 0, 1, 2, 6, 10 };
    private static readonly GameCall weiter = GameCall.Weiter();

    private static readonly Vector128<byte> gsuchteQueryMask =
        Vector128.Create((byte)0x1F);
    private static readonly Vector128<byte>[] gsuchteQueries =
        new Vector128<byte>[] {
            Vector128.Create((byte)((byte)CardColor.Schell | ((byte)CardType.Sau << 2))),
            Vector128.Create((byte)((byte)CardColor.Herz   | ((byte)CardType.Sau << 2))),
            Vector128.Create((byte)((byte)CardColor.Gras   | ((byte)CardType.Sau << 2))),
            Vector128.Create((byte)((byte)CardColor.Eichel | ((byte)CardType.Sau << 2))),
        };

    #endregion Init

    public ReadOnlySpan<GameCall> AllPossibleCalls(
        int playerId, Hand[] initialHandsWithoutMeta, GameCall last)
    {
        int p = 3;
        if (last.Mode == GameMode.Weiter)
        {
            var handSauspiel = initialHandsWithoutMeta[playerId]
                .CacheTrumpf(sauspielEval.IsTrumpf);
            for (int i = 0; i < 3; i++)
            {
                var farbe = allRufbareFarben[i];
                if (handSauspiel.IsSauRufbar(farbe))
                {
                    int partnerId = findSauspielPartner(
                        initialHandsWithoutMeta, farbe);
                    sauspiele[--p] = GameCall.Sauspiel(
                        playerId, partnerId, farbe);
                }
            }
        }

        sauspiele.CopyTo(allCalls, 0);
        wenzenByPlayer[playerId].CopyTo(allCalls, 3);
        soliByPlayer[playerId].CopyTo(allCalls, 5);
        allCalls[13] = weiter;

        // info: weiter=0, sauspiel=0, wenz=1, wenz_tout=2, solo=3, solo_tout=4
        int mode = (int)last.Mode < 2 ? 0 :
            ((((int)last.Mode - 2) * 2) + (last.IsTout ? 1 : 0) + 1);
        int offset = lastCallOffsets[mode] + p;
        return allCalls[offset..14];
    }

    private int findSauspielPartner(Hand[] initialHands, CardColor gsuchte)
    {
        Vector128<byte> handsVec_01; Vector128<byte> handsVec_23;
        unsafe
        {
            fixed (Hand* hp = &initialHands[0])
            {
                handsVec_01 = Vector128.Load<ulong>((ulong*)hp).AsByte();
                handsVec_23 = Vector128.Load<ulong>((ulong*)(hp + 2)).AsByte();
            }
        }

        var query = gsuchteQueries[(int)gsuchte];
        var res_01 = Sse2.Xor(Sse2.And(handsVec_01, gsuchteQueryMask), query);
        var res_23 = Sse2.Xor(Sse2.And(handsVec_23, gsuchteQueryMask), query);
        var eq_01 = Sse2.CompareEqual(res_01, Vector128<byte>.Zero).AsUInt64();
        var eq_23 = Sse2.CompareEqual(res_23, Vector128<byte>.Zero).AsUInt64();

        uint tzcnt_0 = (uint)BitOperations.TrailingZeroCount(eq_01.GetElement(0));
        uint tzcnt_1 = (uint)BitOperations.TrailingZeroCount(eq_01.GetElement(1));
        uint tzcnt_2 = (uint)BitOperations.TrailingZeroCount(eq_23.GetElement(0));
        uint tzcnt_3 = (uint)BitOperations.TrailingZeroCount(eq_23.GetElement(1));

        uint res_id = ((64 - tzcnt_0)) | ((64 - tzcnt_1) << 8)
            | ((64 - tzcnt_2) << 16) | ((64 - tzcnt_3) << 24);
        return BitOperations.TrailingZeroCount(res_id) >> 3;
    }

    #region Simple

    [Obsolete("Optimized version is faster!")]
    public ReadOnlySpan<GameCall> AllPossibleCallsSimple(
        int playerId, Hand[] initialHandsWithoutMeta, GameCall last)
    {
        var hand = initialHandsWithoutMeta[playerId];
        var handSauspiel = hand.CacheTrumpf(
            new TrumpfEvaluator(GameMode.Sauspiel).IsTrumpf);
        var possibleSauspielColors = allRufbareFarben
            .Where(c => !handSauspiel.HasCard(new Card(CardType.Sau, c))
                        && handSauspiel.HasFarbe(c));

        var sauspiele = possibleSauspielColors
            .Select(color => GameCall.Sauspiel(
                playerId,
                findSauspielPartnerSimple(initialHandsWithoutMeta, color),
                color));

        var wenzen = touts.Select(tout => GameCall.Wenz(playerId, tout));
        var soli = touts.SelectMany(tout =>
            soloTrumpf.Select(trumpf => GameCall.Solo(playerId, trumpf, tout)));

        var allGameCalls = sauspiele.Union(wenzen).Union(soli);
        var possibleCalls = last.Mode == GameMode.Weiter ? allGameCalls
            : allGameCalls.Where(call => callComp.Compare(call, last) > 0);
        return possibleCalls.Append(GameCall.Weiter()).ToArray();
    }

    private int findSauspielPartnerSimple(Hand[] initialHands, CardColor gsuchte)
    {
        var gsuchteSau = new Card(CardType.Sau, gsuchte);
        return Enumerable.Range(0, 4)
            .First(i => initialHands[i].HasCard(gsuchteSau));
    }

    #endregion Simple
}
