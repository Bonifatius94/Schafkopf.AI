namespace Schafkopf.Lib;

public class GameRules
{
    public ReadOnlySpan<Card> PossibleCards(GameLog log, Card[] cache)
        => PossibleCards(log.Call, log.CurrentTurn, log.HandOfDrawingPlayer, cache);

    public ReadOnlySpan<Card> PossibleCards(
        GameCall call, Turn currentTurn, Hand playerHand, Card[] cache)
    {
        int p = 0;
        if (playerHand.CardsCount == 1)
            cache[p++] = playerHand.LastCard;
        else
            foreach (var card in playerHand)
                if (CanPlayCard(call, card, currentTurn, playerHand))
                    cache[p++] = card;

        return cache.AsSpan(0, p);
    }

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
                return cardPlayed.IsTrumpf;
            else if (turn.IsFarbePlayed && playerHand.HasFarbe(turn.FarbePlayed))
                return !cardPlayed.IsTrumpf && cardPlayed.Color == turn.FarbePlayed;
        }

        return true;
    }
}
