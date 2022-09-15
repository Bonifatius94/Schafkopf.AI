using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

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

    public static GameCall Weiter()
        => new GameCall(
            GameMode.Weiter, 0, 0,
            CardColor.Schell, CardColor.Schell, false);

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

    private static readonly Dictionary<(GameMode, CardColor), TrumpfEval> evalCache =
        new Dictionary<(GameMode, CardColor), TrumpfEval>() {
            { (GameMode.Sauspiel, CardColor.Herz),
                new TrumpfEval(GameMode.Sauspiel, CardColor.Herz) },
            { (GameMode.Wenz, CardColor.Schell),
                new TrumpfEval(GameMode.Wenz) },
            { (GameMode.Solo, CardColor.Schell),
                new TrumpfEval(GameMode.Solo, CardColor.Schell) },
            { (GameMode.Solo, CardColor.Herz),
                new TrumpfEval(GameMode.Solo, CardColor.Herz) },
            { (GameMode.Solo, CardColor.Gras),
                new TrumpfEval(GameMode.Solo, CardColor.Gras) },
            { (GameMode.Solo, CardColor.Eichel),
                new TrumpfEval(GameMode.Solo, CardColor.Eichel) },
        };

    public bool IsTrumpf(Card card)
        => evalCache[(Mode, Trumpf)].IsTrumpf(card);

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
}

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

    private static readonly List<CardColor> sauspielColors =
        new List<CardColor>() {
            CardColor.Schell,
            CardColor.Gras,
            CardColor.Eichel
        };

    private static readonly List<CardColor> soloColors =
        new List<CardColor>() {
            CardColor.Schell,
            CardColor.Herz,
            CardColor.Gras,
            CardColor.Eichel
        };

    private static readonly List<bool> touts =
        new List<bool>() { true, false };

    #endregion Init

    public IEnumerable<GameCall> AllPossibleCalls(
        int playerId, Hand[] initialHandsWithoutMeta, GameCall last)
    {
        var hand = initialHandsWithoutMeta[playerId];
        var handSauspiel = hand.CacheTrumpf(
            new TrumpfEval(GameMode.Sauspiel).IsTrumpf);
        var possibleSauspielColors = sauspielColors
            .Where(c => !handSauspiel.HasCard(new Card(CardType.Sau, c))
                        && handSauspiel.HasFarbe(c));

        var sauspiele = possibleSauspielColors
            .Select(color => GameCall.Sauspiel(
                playerId,
                findSauspielPartner(initialHandsWithoutMeta, color),
                color));

        var wenzen = touts.Select(tout => GameCall.Wenz(playerId, tout));

        var soli = touts.SelectMany(tout =>
            soloColors.Select(c => GameCall.Solo(playerId, c, tout)));

        var allGameCalls = sauspiele.Union(wenzen).Union(soli);
        var possibleCalls = last.Mode == GameMode.Weiter ? allGameCalls
            : allGameCalls.Where(call => callComp.Compare(call, last) > 0);
        return possibleCalls.Append(GameCall.Weiter()).ToList();
    }

    private int findSauspielPartner(Hand[] initialHands, CardColor gsuchte)
    {
        var gsuchteSau = new Card(CardType.Sau, gsuchte);
        return Enumerable.Range(0, 4)
            .First(i => initialHands[i].HasCard(gsuchteSau));
    }
}

public class TrumpfEval
{
    #region Masks

    private const byte SIEBEN = (byte)((byte)CardType.Sieben << 2);
    private const byte ACHT = (byte)((byte)CardType.Acht << 2);
    private const byte NEUN = (byte)((byte)CardType.Neun << 2);
    private const byte UNTER = (byte)((byte)CardType.Unter << 2);
    private const byte OBER = (byte)((byte)CardType.Ober << 2);
    private const byte KOENIG = (byte)((byte)CardType.Koenig << 2);
    private const byte ZEHN = (byte)((byte)CardType.Zehn << 2);
    private const byte SAU = (byte)((byte)CardType.Sau << 2);
    private const byte SCHELL = (byte)CardColor.Schell;
    private const byte HERZ = (byte)CardColor.Herz;
    private const byte GRAS = (byte)CardColor.Gras;
    private const byte EICHEL = (byte)CardColor.Eichel;

    private static readonly Vector128<byte> WENZ_TRUMPF_CARDS =
        Vector128.Create(
            EICHEL | UNTER, GRAS | UNTER, HERZ | UNTER, SCHELL | UNTER,
                      0xFF,         0xFF,         0xFF,           0xFF,
                      0xFF,         0xFF,         0xFF,           0xFF,
                      0xFF,         0xFF,         0xFF,           0xFF);

    private static readonly Vector128<byte>[] SOLO_TRUMPF_CARDS =
        new Vector128<byte>[] {
            Vector128.Create(
                EICHEL |  OBER,   GRAS |   OBER,   HERZ |   OBER, SCHELL |  OBER,
                EICHEL | UNTER,   GRAS |  UNTER,   HERZ |  UNTER, SCHELL | UNTER,
                SCHELL |   SAU, SCHELL |   ZEHN, SCHELL | KOENIG, SCHELL |  NEUN,
                SCHELL |  ACHT, SCHELL | SIEBEN,            0xFF,           0xFF),
            Vector128.Create(
                EICHEL |  OBER,   GRAS |   OBER,   HERZ |   OBER, SCHELL |  OBER,
                EICHEL | UNTER,   GRAS |  UNTER,   HERZ |  UNTER, SCHELL | UNTER,
                  HERZ |   SAU,   HERZ |   ZEHN,   HERZ | KOENIG,   HERZ |  NEUN,
                  HERZ |  ACHT,   HERZ | SIEBEN,            0xFF,           0xFF),
            Vector128.Create(
                EICHEL |  OBER,   GRAS |   OBER,   HERZ |   OBER, SCHELL |  OBER,
                EICHEL | UNTER,   GRAS |  UNTER,   HERZ |  UNTER, SCHELL | UNTER,
                  GRAS |   SAU,   GRAS |   ZEHN,   GRAS | KOENIG,   GRAS |  NEUN,
                  GRAS |  ACHT,   GRAS | SIEBEN,            0xFF,           0xFF),
            Vector128.Create(
                EICHEL |  OBER,   GRAS |   OBER,   HERZ |   OBER, SCHELL |  OBER,
                EICHEL | UNTER,   GRAS |  UNTER,   HERZ |  UNTER, SCHELL | UNTER,
                EICHEL |   SAU, EICHEL |   ZEHN, EICHEL | KOENIG, EICHEL |  NEUN,
                EICHEL |  ACHT, EICHEL | SIEBEN,            0xFF,           0xFF),
        };

    private static readonly Vector128<byte> ZERO =
        Vector128.Create((byte)0);
    private static readonly Vector128<byte> WENZ_MODE =
        Vector128.Create((byte)GameMode.Wenz);

    private static readonly Vector128<byte>[] MODE_MASKS =
        new Vector128<byte>[] {
            Vector128.Create((byte)GameMode.Weiter),
            Vector128.Create((byte)GameMode.Sauspiel),
            Vector128.Create((byte)GameMode.Wenz),
            Vector128.Create((byte)GameMode.Solo),
        };

    #endregion Masks

    public TrumpfEval(GameMode mode, CardColor trumpf = CardColor.Herz)
    {
        this.mode = mode;
        this.trumpf = trumpf;
    }

    private readonly GameMode mode;
    private readonly CardColor trumpf;

    public bool IsTrumpf(Card card)
    {
        var cardVec = Vector128.Create((byte)(card.Id & 0x1F));
        var modeMask = MODE_MASKS[(byte)mode];
        var wenzModeMask = Sse2.CompareEqual(modeMask, WENZ_MODE);
        var soloModeMask = Sse2.CompareEqual(wenzModeMask, ZERO);
        var soloOrSauspielTrumpf = SOLO_TRUMPF_CARDS[(byte)trumpf];

        var wenzCmp = Sse2.Xor(cardVec, WENZ_TRUMPF_CARDS);
        var soloCmp = Sse2.Xor(cardVec, soloOrSauspielTrumpf);
        var wenzMatches = Sse2.And(Sse2.CompareEqual(wenzCmp, ZERO), wenzModeMask);
        var soloMatches = Sse2.And(Sse2.CompareEqual(soloCmp, ZERO), soloModeMask);
        var allMatches = Sse2.Or(wenzMatches, soloMatches).AsUInt64();
        bool isTrumpf = (allMatches.GetElement(0) | allMatches.GetElement(1)) > 0;
        return isTrumpf;
    }

    // TODO: use this matching table to compute the rank for card comparison
}
