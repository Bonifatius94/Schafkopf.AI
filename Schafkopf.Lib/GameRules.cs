namespace Schafkopf.Lib;

public class DrawValidator
{
    public bool CanPlayCard(GameCall call, Card cardPlayed, Turn currentTurn, Hand playerHand)
    {
        if (call.Mode == GameMode.Solo || call.Mode == GameMode.Wenz)
            return validateSoloOrWenz(call, cardPlayed, currentTurn, playerHand);
        else if (call.Mode == GameMode.Sauspiel)
            return validateSauspiel(call, cardPlayed, currentTurn, playerHand);
        throw new NotSupportedException($"Game mode {call.Mode} is currently not supported!");
    }

    public bool validateSauspiel(GameCall call, Card cardPlayed, Turn turn, Hand playerHand)
    {
        bool gsuchtIs = turn.FarbePlayed == call.GsuchteFarbe;
        bool kommtRaus = turn.CardsCount == 0;
        if (kommtRaus)
        {
            bool darfNichtUntenDurch = playerHand.HasCard(call.GsuchteSau)
                && playerHand.FarbeCount(call.GsuchteFarbe) < 4;
            bool mussGsuchteAnspielen = gsuchtIs && darfNichtUntenDurch;
            return !mussGsuchteAnspielen || cardPlayed == call.GsuchteSau;
        }
        else // kommt nicht raus
        {
            if (turn.IsTrumpfPlayed && playerHand.HasTrumpf())
            {
                return cardPlayed.IsTrumpf;
            }
            else if (turn.IsFarbePlayed && playerHand.HasFarbe(turn.FarbePlayed))
            {
                bool gsuchteZugeben = gsuchtIs && playerHand.HasCard(call.GsuchteSau);
                return !cardPlayed.IsTrumpf && cardPlayed.Color == turn.FarbePlayed
                    && (!gsuchteZugeben || cardPlayed == call.GsuchteSau);
            }
            else // schmieren / einstechen
            {
                return turn.AlreadyGsucht
                    || !playerHand.HasCard(call.GsuchteSau)
                    || cardPlayed != call.GsuchteSau;
            }
        }
    }

    private bool validateSoloOrWenz(GameCall call, Card cardPlayed, Turn turn, Hand playerHand)
    {
        bool kommtRaus = turn.CardsCount == 0;
        if (!kommtRaus)
        {
            if (turn.IsTrumpfPlayed && playerHand.HasTrumpf())
            {
                return cardPlayed.IsTrumpf;
            }
            else if (turn.IsFarbePlayed && playerHand.HasFarbe(turn.FarbePlayed))
            {
                return !cardPlayed.IsTrumpf && cardPlayed.Color == turn.FarbePlayed;
            }
            else // schmieren / einstechen
            {
                return true;
            }
        }
        else // kommt raus
        {
            return true;
        }
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
