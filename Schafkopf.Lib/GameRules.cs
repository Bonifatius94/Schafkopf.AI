namespace Schafkopf.Lib;

public interface IDrawValidator
{
    bool CanPlayCard(GameCall call, Card cardPlayed, Turn currentTurn, Hand playerHand);
}

public class DrawValidator : IDrawValidator
{
    public bool CanPlayCard(GameCall call, Card cardPlayed, Turn currentTurn, Hand playerHand)
    {
        if (call.Mode == GameMode.Solo || call.Mode == GameMode.Wenz)
            return validateSoloOrWenz(call, cardPlayed, currentTurn, playerHand);
        else if (call.Mode == GameMode.Sauspiel)
            return validateSauspiel(call, cardPlayed, currentTurn, playerHand);
        throw new NotSupportedException($"Game mode {call.Mode} is currently not supported!");
    }

    public bool validateSauspiel(GameCall call, Card cardPlayed, Turn currentTurn, Hand playerHand)
    {
        bool kommtRaus = currentTurn.CardsCount == 0;
        bool darfNichtUntenDurch = kommtRaus && playerHand.HasCard(call.GsuchteSau)
            && playerHand.FarbeCount(call.GsuchteSau.Color) < 4;
        if (darfNichtUntenDurch && cardPlayed.Color == call.GsuchteSau.Color
                && cardPlayed.Type != CardType.Sau)
            return false;
        else if (kommtRaus)
            return true;

        bool isTrumpfTurn = currentTurn.FirstCard.IsTrumpf;
        if (isTrumpfTurn && !cardPlayed.IsTrumpf && playerHand.HasTrumpf())
            return false;

        bool mussAngeben = !isTrumpfTurn &&
            playerHand.HasFarbe(currentTurn.FirstCard.Color);
        if (mussAngeben && cardPlayed.Color != currentTurn.FirstCard.Color)
            return false;

        bool gsuchtIs = mussAngeben && call.GsuchteSau.Color == currentTurn.FirstCard.Color;
        if (gsuchtIs && cardPlayed.Type != CardType.Sau)
            return false;

        return true;
    }

    private bool validateSoloOrWenz(GameCall call, Card cardPlayed, Turn currentTurn, Hand playerHand)
    {
        bool kommtRaus = currentTurn.CardsCount == 0;
        if (kommtRaus)
            return true;

        bool isTrumpfTurn = currentTurn.FirstCard.IsTrumpf;
        if (isTrumpfTurn && !cardPlayed.IsTrumpf && playerHand.HasTrumpf())
            return false;

        bool mussAngeben = !isTrumpfTurn &&
            playerHand.HasFarbe(currentTurn.FirstCard.Color);
        if (mussAngeben && cardPlayed.Color != currentTurn.FirstCard.Color)
            return false;

        return true;
    }
}

public class CardComparer : IComparer<Card>
{
    public CardComparer(GameMode mode)
        => this.mode = mode;

    private readonly GameMode mode;

    public int Compare(Card x, Card y)
    {
        bool isXTrumpf = x.IsTrumpf;
        bool isYTrumpf = y.IsTrumpf;

        // TODO: remove branching, e.g. using SIMD instructions
        //       or making use of some score func like trumpfScore()
        if (isXTrumpf && !isYTrumpf)
            return 1;
        if (!isXTrumpf && isYTrumpf)
            return -1;

        if (!isXTrumpf && !isYTrumpf)
            return x.Type - y.Type;

        if (mode == GameMode.Wenz)
            return x.Color - y.Color;

        // case: both Trumpf in Solo or Sauspiel
        int scoreX = trumpfScore(x);
        int scoreY = trumpfScore(y);
        return scoreX - scoreY;
    }

    private int trumpfScore(Card x)
        => x.Type == CardType.Ober || x.Type == CardType.Unter
            ? (int)x.Type * 4 + (int)x.Color + 8
            : (int)x.Type;
}
