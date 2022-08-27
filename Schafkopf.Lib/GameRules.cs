namespace Schafkopf.Lib;

public enum GameMode
{
    Sauspiel,
    Wenz,
    Solo
}

public class GameCall
{
    public GameCall(
        GameMode mode,
        byte callingPlayerId,
        CardColor trumpfOrGsuchteSau,
        CardsDeck deck)
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

public interface IDrawValidator
{
    bool IsValid(GameCall call, Card cardPlayed, Turn currentTurn, Hand playerHand);
}

public class DrawValidatorFactory
{
    private static readonly Dictionary<GameMode, IDrawValidator> validators =
        new Dictionary<GameMode, IDrawValidator>() {
            { GameMode.Sauspiel, new SauspielDrawValidator() },
            { GameMode.Wenz, new WenzOrSoloDrawValidator() },
            { GameMode.Solo, new WenzOrSoloDrawValidator() },
        };

    public IDrawValidator Create(GameMode mode)
        => validators[mode];
}

public class SauspielDrawValidator : IDrawValidator
{
    public bool IsValid(GameCall call, Card cardPlayed, Turn currentTurn, Hand playerHand)
    {
        if (call.Mode != GameMode.Sauspiel)
            throw new InvalidOperationException("Can only handle Sauspiel game mode!");

        bool kommtRaus = currentTurn.CardsCount == 0;
        bool darfNichtUntenDurch = kommtRaus && playerHand.HasCard(call.GsuchteSau)
            && playerHand.FarbeCount(call.GsuchteSau.Color, call.IsTrumpf) < 4;
        if (darfNichtUntenDurch && cardPlayed.Color == call.GsuchteSau.Color
                && cardPlayed.Type != CardType.Sau)
            return false;
        else if (kommtRaus)
            return true;

        bool isTrumpfTurn = call.IsTrumpf(currentTurn.C1);
        if (isTrumpfTurn && !call.IsTrumpf(cardPlayed) && playerHand.HasTrumpf(call.IsTrumpf))
            return false;

        bool mussAngeben = !isTrumpfTurn &&
            playerHand.Cards.Any(c => c.Color == currentTurn.C1.Color);
        if (mussAngeben && cardPlayed.Color != currentTurn.C1.Color)
            return false;

        bool gsuchtIs = mussAngeben && call.GsuchteSau.Color == currentTurn.C1.Color;
        if (gsuchtIs && cardPlayed.Type != CardType.Sau)
            return false;

        return true;
    }
}

public class WenzOrSoloDrawValidator : IDrawValidator
{
    public bool IsValid(GameCall call, Card cardPlayed, Turn currentTurn, Hand playerHand)
    {
        if (call.Mode != GameMode.Wenz)
            throw new InvalidOperationException("Can only handle Wenz game mode!");

        bool kommtRaus = currentTurn.CardsCount == 0;
        if (kommtRaus)
            return true;

        bool isTrumpfTurn = call.IsTrumpf(currentTurn.C1);
        if (isTrumpfTurn && !call.IsTrumpf(cardPlayed) && playerHand.HasTrumpf(call.IsTrumpf))
            return false;

        bool mussAngeben = !isTrumpfTurn &&
            playerHand.Cards.Any(c => c.Color == currentTurn.C1.Color);
        if (mussAngeben && cardPlayed.Color != currentTurn.C1.Color)
            return false;

        return true;
    }
}

public class TurnEvaluator
{
    public TurnEvaluator(GameCall call)
    {
        this.call = call;
        comparer = new CardComparer(call);
    }

    private readonly GameCall call;
    private readonly CardComparer comparer;

    public int WinnerId(Turn turn)
    {
        bool isTrumpfTurn = call.IsTrumpf(turn.C1);
        var cardsByPlayer = turn.CardsByPlayer();

        if (isTrumpfTurn)
            return cardsByPlayer.MaxBy(x => x.Value, comparer).Key;

        return cardsByPlayer
            .Where(x => x.Value.Color == turn.C1.Color)
            .MaxBy(x => x.Value, comparer).Key;
    }
}

public class CardComparer : IComparer<Card>
{
    public CardComparer(GameCall call)
        => this.call = call;

    private readonly GameCall call;

    public int Compare(Card x, Card y)
    {
        bool isXTrumpf = call.IsTrumpf(x);
        bool isYTrumpf = call.IsTrumpf(y);

        if (isXTrumpf && !isYTrumpf)
            return -1;
        if (!isXTrumpf && isYTrumpf)
            return 1;

        if (!isXTrumpf && !isYTrumpf)
            return x.Type - y.Type;

        if (call.Mode == GameMode.Wenz)
            return x.Color - y.Color;

        // case: both Trumpf in Solo or Sauspiel
        return trumpfScore(x) - trumpfScore(y);
    }

    private int trumpfScore(Card x)
        => x.Type == CardType.Ober || x.Type == CardType.Unter
            ? (int)x.Type * (int)x.Color
            : (int)x.Type;
}
