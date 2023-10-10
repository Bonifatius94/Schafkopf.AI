namespace Schafkopf.Lib;

public class GameSession
{
    public GameSession(Table table, CardsDeck deck)
    {
        this.table = table;
        this.deck = deck;
    }

    private CardsDeck deck;
    private Table table;

    private static readonly DrawValidator validator = new DrawValidator();
    private static readonly GameCallGenerator callGen = new GameCallGenerator();

    private Hand[] initialHandsCache = new Hand[4];

    public GameLog ProcessGame() // TODO: pass history, reduce alloc
    {
        deck.Shuffle();

        deck.InitialHands(initialHandsCache);
        int klopfer = askForKlopfer(initialHandsCache);
        var call = makeCalls(klopfer, initialHandsCache);

        GameLog history;
        if (call.Mode == GameMode.Weiter)
            history = new GameLog(call, initialHandsCache, table.FirstDrawingPlayerId);
        else
            history = playGame(call, initialHandsCache);

        table.Shift();
        return history;
    }

    private GameLog playGame(GameCall call, Hand[] initialHands)
    {
        deck.InitialHands(call, initialHands);
        int kommtRaus = table.FirstDrawingPlayerId;
        var history = new GameLog(call, initialHands, kommtRaus);

        foreach (var player in table.PlayersInDrawingOrder())
            player.NewGame(history);
        history = playGameUntilEnd(history);
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

    private Card[] firstFourCache = new Card[4];

    private int askForKlopfer(Hand[] initialHands)
    {
        int pos = 0;
        int klopfer = 0;
        foreach (var player in table.PlayersInDrawingOrder())
        {
            initialHands[player.Id].FirstFour(firstFourCache);
            if (player.IsKlopfer(pos++, firstFourCache))
                klopfer++;
        }
        return klopfer;
    }

    private void askForKontraRe(GameLog history)
    {
        if (!history.IsKontraCalled)
        {
            var opponents = table.PlayersById(history.OpponentIds);
            bool kontraCalled = false;
            foreach (var player in opponents)
                if (player.CallKontra(history)) {
                    kontraCalled = true;
                    break;
                }
            if (kontraCalled)
                history.CallKontra();
        }

        if (history.IsKontraCalled)
        {
            var callers = table.PlayersById(history.CallerIds);
            bool reCalled = false;
            foreach (var player in callers)
                if (player.CallRe(history)) {
                    reCalled = true;
                    break;
                }
            if (reCalled)
                history.CallRe();
        }
    }

    #endregion Call

    private Card[] possibleCards = new Card[8];

    private GameLog playGameUntilEnd(GameLog history)
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
                    foreach (var card in player.Hand)
                        if (validator.CanPlayCard(history.Call, card, turn, player.Hand))
                            possibleCards[p++] = card;

                    var cardToPlay = player.ChooseCard(history, possibleCards[0..p]);
                    player.Discard(cardToPlay);
                    turn = history.NextCard(cardToPlay);
                }
            }
        }

        return history;
    }
}
