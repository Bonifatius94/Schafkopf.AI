using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Schafkopf.Lib;

public enum GameMode
{
    Sauspiel,
    Wenz,
    Solo,
    Weiter
}

// TODO: make this a readonly struct with 16 bit storage
public class GameCall
{
    private GameCall()
    {
        Mode = GameMode.Weiter;
    }

    private GameCall(byte callingPlayerId)
    {
        Mode = GameMode.Wenz;
        CallingPlayerId = callingPlayerId;
    }

    private GameCall(
        byte callingPlayerId,
        CardColor trumpf)
    {
        Mode = GameMode.Solo;
        Trumpf = trumpf;
        CallingPlayerId = callingPlayerId;
    }

    private GameCall(
        byte callingPlayerId,
        byte partnerPlayerId,
        CardColor gsuchteFarbe)
    {
        Mode = GameMode.Sauspiel;
        Trumpf = CardColor.Herz;
        CallingPlayerId = callingPlayerId;
        GsuchteFarbe = gsuchteFarbe;
        PartnerPlayerId = partnerPlayerId;
    }

    public static GameCall Weiter(int callingPlayerId)
        => new GameCall();

    public static GameCall Sauspiel(
            int callingPlayerId,
            int partnerPlayerId,
            CardColor gsuchteFarbe)
        => new GameCall((byte)callingPlayerId, (byte)partnerPlayerId, gsuchteFarbe);

    public static GameCall Wenz(int callingPlayerId)
        => new GameCall((byte)callingPlayerId);

    public static GameCall Solo(int callingPlayerId, CardColor trumpf)
        => new GameCall((byte)callingPlayerId, trumpf);

    public static int FindSauspielPartner(CardsDeck deck, CardColor gsuchte)
    {
        var gsuchteSau = new Card(CardType.Sau, gsuchte);
        return (byte)Enumerable.Range(0, 4)
            .First(i => deck.HandOfPlayer(i).HasCard(gsuchteSau));
    }

    // TODO: represent this data as uint16 readonly struct
    //       -> append it to every turn as meta-data
    public GameMode Mode { get; private set; } // 2 bits
    public byte CallingPlayerId { get; private set; } // 2 bits
    public byte PartnerPlayerId { get; private set; } // 2 bits
    public CardColor Trumpf { get; private set; } // 2 bits
    public CardColor GsuchteFarbe { get; private set; } // 2 bits

    public Card GsuchteSau => new Card(CardType.Sau, GsuchteFarbe);

    public Func<Card, bool> IsTrumpf =>
        (card) => new TrumpfEval(Mode, Trumpf).IsTrumpf(card);
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
    // private static readonly Vector128<byte> CARD_MASK =
    //     Vector128.Create((byte)0x1F);

    private static readonly Vector128<byte>[] MODE_MASKS =
        new Vector128<byte>[] {
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

public class GameCallValidator
{
    public bool IsValidCall(GameCall call, CardsDeck deck)
    {
        if (call.Mode != GameMode.Sauspiel)
            return true;

        if (call.CallingPlayerId == call.PartnerPlayerId)
            return false;

        var callingPlayerHand = deck.HandOfPlayer(call.CallingPlayerId);
        if (!callingPlayerHand.HasFarbe(call.GsuchteFarbe))
            return false;

        return true;
    }
}
