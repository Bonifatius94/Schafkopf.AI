namespace Schafkopf.Lib.Test;

public class TestGameResult_Laufende
{
    public static IEnumerable<object[]> CallersWithLaufende
        => allCalls.SelectMany(call => 
            Enumerable.Range(1, maxLaufendePerGameMode[call.Mode])
                .Select(laufende => new object[] {
                    call,
                    distributeLaufendeAccrossInitialHands(
                        call, laufende, callerIds(call)),
                    laufende
                })
            );

    public static IEnumerable<object[]> OpponentsWithLaufende
        => allCalls.SelectMany(call => 
            Enumerable.Range(1, maxLaufendePerGameMode[call.Mode])
                .Select(laufende => new object[] {
                    call,
                    distributeLaufendeAccrossInitialHands(
                        call, laufende, opponentIds(call)),
                    laufende
                })
            );

    [Theory]
    [MemberData(nameof(CallersWithLaufende))]
    public void Test_LaufendeCount_WhenCallersHaveLaufende(
        GameCall call, Hand[] initialHands, int expLaufende)
    {
        var log = playRandomValidGame(call, initialHands);
        var eval = new GameScoreEvaluation(log);
        eval.Laufende.Should().Be(expLaufende);
    }

    [Theory]
    [MemberData(nameof(OpponentsWithLaufende))]
    public void Test_LaufendeCount_WhenOpponentsHaveLaufende(
        GameCall call, Hand[] initialHands, int expLaufende)
    {
        var log = playRandomValidGame(call, initialHands);
        var eval = new GameScoreEvaluation(log);
        eval.Laufende.Should().Be(expLaufende);
    }

    #region Init

    private static IEnumerable<Card> trumpfDesc(GameCall call)
        => CardsDeck.AllCards
            .Where(c => call.IsTrumpf(c))
            .Select(c => new Card(c.Type, c.Color, true, true))
            .OrderByDescending(x => x, new CardComparer(call.Mode, call.Trumpf))
            .ToList();

    private static Hand[] distributeLaufendeAccrossInitialHands(
        GameCall call, int laufende, IEnumerable<int> laufendeOwners)
    {
        var allTrumpfDesc = trumpfDesc(call);
        var laufendeDesc = allTrumpfDesc.Take(laufende);
        var allPlayers = Enumerable.Range(0, 4);
        var opponents = allPlayers.Except(laufendeOwners);

        var initialHands = new List<Card>[] {
            new List<Card>(), new List<Card>(),
            new List<Card>(), new List<Card>(),
        };

        var randomUnoccupiedPlayer = (IEnumerable<int> pids)
            => pids.Where(id => initialHands[id].Count < 8)
                .PickRandom();

        // ensure that the gsuchte sau is distributed correctly
        // and that the caller has a card of the same farbe
        if (call.Mode == GameMode.Sauspiel)
        {
            var cardCaller = CardsDeck.AllCards
                .Where(c => c.Color == call.GsuchteFarbe
                    && !call.IsTrumpf(c) && c.Type != CardType.Sau)
                .PickRandom();
            initialHands[call.CallingPlayerId].Add(cardCaller);
            initialHands[call.PartnerPlayerId].Add(call.GsuchteSau);
        }

        // distribute laufende among owners
        foreach (var card in laufendeDesc)
        {
            int pid = randomUnoccupiedPlayer(laufendeOwners);
            initialHands[pid].Add(card);
        }

        // distribute the next highest two trumpf such that
        // it's a correct example for laufende count
        if (allTrumpfDesc.Count() > laufendeDesc.Count())
        {
            var card = allTrumpfDesc.ElementAt(laufendeDesc.Count());
            int pid = randomUnoccupiedPlayer(opponents);
            initialHands[pid].Add(card);
        }
        if (allTrumpfDesc.Count() > laufendeDesc.Count() + 1
            && laufendeOwners.Any(id => initialHands[id].Count < 8))
        {
            var card = allTrumpfDesc.ElementAt(laufendeDesc.Count() + 1);
            int pid = randomUnoccupiedPlayer(laufendeOwners);
            initialHands[pid].Add(card);
        }

        // distribute the remaining cards randomly among all players
        var remainingCards = CardsDeck.AllCards
            .Except(initialHands.SelectMany(h => h)).ToList();
        foreach (var card in remainingCards)
        {
            int pid = randomUnoccupiedPlayer(allPlayers);
            initialHands[pid].Add(card);
        }

        return initialHands
            .Select(h => new Hand(h.ToArray())
                .CacheTrumpf(call.IsTrumpf))
            .ToArray();
    }

    private static GameLog playRandomValidGame(GameCall call, Hand[] hands)
    {
        var possCardEval = new DrawValidator();
        int kommtRaus = Enumerable.Range(0, 4).PickRandom();
        var log = new GameLog(call, hands, kommtRaus);

        foreach (var turn in log)
        {
            var playersInOrder = Enumerable.Range(0, 4)
                .Select(i => (kommtRaus + i) % 4);

            foreach (int pid in playersInOrder)
            {
                var possCards = hands[pid].Where(c =>
                    possCardEval.CanPlayCard(call, c, turn, hands[pid]));
                possCards = log.TurnCount < 8 ? possCards : hands[pid];
                log.NextCard(possCards.PickRandom());
            }
        }

        return log;
    }

    private static IEnumerable<CardColor> rufbareFarben
        => new List<CardColor>() { CardColor.Schell, CardColor.Gras, CardColor.Eichel };
    private static IEnumerable<CardColor> soloTrumpf
        => new List<CardColor>() { CardColor.Schell, CardColor.Herz, CardColor.Gras, CardColor.Eichel };
    private static IEnumerable<(int, int)> sauspielPartnerPerms
        => Enumerable.Range(0, 4)
            .SelectMany(i => Enumerable.Range(0, 4)
                .Except(new int[] { i }).Select(j => (i, j)));
    private static IEnumerable<GameCall> sauspiele
        => sauspielPartnerPerms.SelectMany(x =>
            rufbareFarben.Select(gsuchteFarbe => (
            GameCall.Sauspiel(x.Item1, x.Item2, gsuchteFarbe)
        )));

    private static IEnumerable<GameCall> wenzen
        => Enumerable.Range(0, 4).Select(i => GameCall.Wenz(i));

    private static IEnumerable<GameCall> soli
        => Enumerable.Range(0, 4)
            .SelectMany(i => soloTrumpf.Select(t => GameCall.Solo(i, t)));

    private static IEnumerable<GameCall> allCalls
        => sauspiele.Union(wenzen).Union(soli);

    private static Dictionary<GameMode, int> maxLaufendePerGameMode
        => new Dictionary<GameMode, int>() {
            { GameMode.Sauspiel, 14 },
            { GameMode.Wenz, 4 },
            { GameMode.Solo, 8 },
        };

    private static IEnumerable<int> callerIds(GameCall call)
        => call.Mode == GameMode.Sauspiel
            ? new List<int>() { call.CallingPlayerId, call.PartnerPlayerId }
            : new List<int>() { call.CallingPlayerId };

    private static IEnumerable<int> opponentIds(GameCall call)
        => Enumerable.Range(0, 4).Except(callerIds(call));

    #endregion Init
}
