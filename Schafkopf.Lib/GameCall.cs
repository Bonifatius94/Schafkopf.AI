namespace Schafkopf.Lib;

public enum GameMode
{
    Sauspiel,
    Wenz,
    Solo
}

public class GameCall
{
    // TODO: make this constructor private and add
    //       static methods to create sauspiel / solo / wenz
    public GameCall(
        GameMode mode,
        byte callingPlayerId,
        CardsDeck deck,
        CardColor trumpfOrGsuchteSau = CardColor.Herz)
    {
        Mode = mode;
        Trumpf = CardColor.Herz;
        CallingPlayerId = callingPlayerId;

        if (mode == GameMode.Sauspiel)
        {
            GsuchteSau = new Card(CardType.Sau, trumpfOrGsuchteSau);
            PartnerPlayerId = (byte)Enumerable.Range(0, 4)
                .First(i => deck.HandOfPlayer(i).HasCard(GsuchteSau));
        }
        else if (mode == GameMode.Solo)
            Trumpf = trumpfOrGsuchteSau;
    }

    public bool CanCallSauspiel(CardsDeck deck)
    {
        if (CallingPlayerId == PartnerPlayerId)
            return false;

        var callingPlayerHand = deck.HandOfPlayer(CallingPlayerId);
        if (!callingPlayerHand.HasFarbe(GsuchteSau.Color, IsTrumpf))
            return false;

        return true;
    }

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
