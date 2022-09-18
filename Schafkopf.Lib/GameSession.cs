namespace Schafkopf.Lib;

public class GameSession
{
    public GameSession(GameTable table, CardsDeck deck)
    {
        this.table = table;
        this.deck = deck;
    }

    private CardsDeck deck;
    private GameTable table;

    private static readonly DrawValidator validator = new DrawValidator();
    private static readonly GameCallGenerator callGen = new GameCallGenerator();

    public GameHistory ProcessGame()
    {
        deck.Shuffle();

        var initialHandsWithoutMeta = deck.InitialHands();
        int klopfer = askForKlopfer(initialHandsWithoutMeta);
        var call = makeCalls(klopfer, initialHandsWithoutMeta);

        if (call.Mode == GameMode.Weiter)
        {
            table.Shift();
            return new GameHistory(
                call, initialHandsWithoutMeta, table.FirstDrawingPlayerId);
        }

        var initialHands = deck.InitialHands(call);
        int kommtRaus = table.FirstDrawingPlayerId;
        var history = new GameHistory(call, initialHands, kommtRaus);

        foreach (var player in table.PlayersInDrawingOrder())
            player.NewGame(initialHands[player.Id]);
        history = playGameUntilEnd(history);

        table.Shift();
        return history;
    }

    #region Call

    private GameCall makeCalls(int klopfer, Hand[] initialHands)
    {
        int pos = 0;
        var call = GameCall.Weiter();

        foreach (var player in table.PlayersInDrawingOrder())
        {
            var hand = initialHands[player.Id];
            var possibleCalls = callGen.AllPossibleCalls(player.Id, initialHands, call);
            var nextCall = player.MakeCall(possibleCalls, pos++, hand, klopfer);
            if (nextCall.Mode == GameMode.Weiter)
                continue;
            call = nextCall;
        }

        return call;
    }

    private int askForKlopfer(Hand[] initialHands)
    {
        int pos = 0;
        int klopfer = 0;
        foreach (var player in table.PlayersInDrawingOrder())
        {
            if (player.IsKlopfer(pos++, initialHands[player.Id].Take(4)))
                klopfer++;
        }
        return klopfer;
    }

    private void askForKontraRe(GameHistory history)
    {
        if (!history.IsKontraCalled)
        {
            var opponents = table.PlayersById(history.OpponentIds);
            bool kontraCalled = false;
            foreach (var player in opponents)
                if (player.CallKontra(history))
                    kontraCalled = true;
            if (kontraCalled)
                history.CallKontra();
        }

        if (history.IsKontraCalled)
        {
            var callers = table.PlayersById(history.CallerIds);
            bool reCalled = false;
            foreach (var player in callers)
                if (player.CallRe(history))
                    reCalled = true;
            if (reCalled)
                history.CallRe();
        }
    }

    #endregion Call

    private Card[] possibleCards = new Card[8];

    private GameHistory playGameUntilEnd(GameHistory history)
    {
        foreach (var newTurn in history)
        {
            var turn = newTurn;

            if (history.TurnCount == 8)
            {
                foreach (var player in
                    table.PlayersInDrawingOrder(history.KommtRaus))
                {
                    var lastCard = player.Hand.First();
                    turn = history.NextCard(lastCard);
                }
            }
            else
            {
                foreach (var player in
                    table.PlayersInDrawingOrder(history.KommtRaus))
                {
                    if (history.CanKontraRe)
                        askForKontraRe(history);

                    int p = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        var card = player.Hand[i];
                        if (validator.CanPlayCard(
                                history.Call, card, turn, player.Hand))
                            possibleCards[p++] = card;
                    }

                    var possCardsSpan = possibleCards.AsSpan(0, p);
                    var cardToPlay = player.ChooseCard(history, possCardsSpan);
                    player.Discard(cardToPlay);
                    turn = history.NextCard(cardToPlay);
                }
            }
        }

        return history;
    }
}
