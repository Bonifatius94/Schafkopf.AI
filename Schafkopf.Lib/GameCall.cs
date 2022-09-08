namespace Schafkopf.Lib;

public enum GameMode
{
    Sauspiel,
    Wenz,
    Solo,
    Weiter
}

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
        CardColor gsuchteSau)
    {
        Mode = GameMode.Sauspiel;
        Trumpf = CardColor.Herz;
        CallingPlayerId = callingPlayerId;
        GsuchteSau = new Card(CardType.Sau, gsuchteSau);
        PartnerPlayerId = partnerPlayerId;
    }

    public static GameCall Weiter(int callingPlayerId)
        => new GameCall();

    public static GameCall Sauspiel(
            int callingPlayerId,
            int partnerPlayerId,
            CardColor gsuchteSau)
        => new GameCall((byte)callingPlayerId, (byte)partnerPlayerId, gsuchteSau);

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

    public GameMode Mode { get; private set; }
    public byte CallingPlayerId { get; private set; }
    public byte PartnerPlayerId { get; private set; }
    public Card GsuchteSau { get; private set; }
    public CardColor Trumpf { get; private set; }

    public Func<Card, bool> IsTrumpf =>
        (card) => new TrumpfEval(Mode, Trumpf).IsTrumpf(card);
}

public class TrumpfEval
{
    public TrumpfEval(GameMode mode, CardColor trumpf = CardColor.Herz)
    {
        this.mode = mode;
        this.trumpf = trumpf;
    }

    private readonly GameMode mode;
    private readonly CardColor trumpf;

    public bool IsTrumpf(Card card)
    {
        if (mode == GameMode.Wenz)
            return card.Type == CardType.Unter;
        else if (mode == GameMode.Sauspiel || mode == GameMode.Solo)
            return card.Type == CardType.Unter
                || card.Type == CardType.Ober
                || card.Color == trumpf;
        else
            throw new NotSupportedException(
                $"game mode {mode} is currently not supported!");
    }
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
        if (!callingPlayerHand.HasFarbe(call.GsuchteSau.Color))
            return false;

        return true;
    }
}
