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
        CardsDeck deck,
        CardColor gsuchteSau)
    {
        Mode = GameMode.Sauspiel;
        Trumpf = CardColor.Herz;
        CallingPlayerId = callingPlayerId;
        GsuchteSau = new Card(CardType.Sau, gsuchteSau);
        PartnerPlayerId = (byte)Enumerable.Range(0, 4)
            .First(i => deck.HandOfPlayer(i).HasCard(GsuchteSau));
    }

    public static GameCall Weiter(int callingPlayerId)
        => new GameCall();

    public static GameCall Sauspiel(
            int callingPlayerId,
            CardsDeck deck,
            CardColor gsuchteSau)
        => new GameCall((byte)callingPlayerId, deck, gsuchteSau);

    public static GameCall Wenz(int callingPlayerId)
        => new GameCall((byte)callingPlayerId);

    public static GameCall Solo(int callingPlayerId, CardColor trumpf)
        => new GameCall((byte)callingPlayerId, trumpf);

    public GameMode Mode { get; private set; }
    public byte CallingPlayerId { get; private set; }
    public byte PartnerPlayerId { get; private set; }
    public Card GsuchteSau { get; private set; }
    public CardColor Trumpf { get; private set; }

    public bool IsTrumpf(Card card)
    {
        if (Mode == GameMode.Wenz)
            return card.Type == CardType.Unter;
        else if (Mode == GameMode.Sauspiel || Mode == GameMode.Solo)
            return card.Type == CardType.Unter
                || card.Type == CardType.Ober
                || card.Color == Trumpf;
        else
            throw new NotSupportedException(
                $"game mode {Mode} is currently not supported!");
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
        if (!callingPlayerHand.HasFarbe(call.GsuchteSau.Color, call.IsTrumpf))
            return false;

        return true;
    }
}
