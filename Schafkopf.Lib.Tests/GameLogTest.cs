namespace Schafkopf.Lib.Test;

public class TestGameHistory_Initialization
{
    private static IEnumerable<int> kommtRaus
        => Enumerable.Range(0, 4);
    private static IEnumerable<GameCall> gameCalls
        => new List<GameCall>() {
            GameCall.Sauspiel(0, 1, CardColor.Schell),
            GameCall.Wenz(0),
            GameCall.Solo(0, CardColor.Schell),
        };
    public static IEnumerable<object[]> KommtRausXCalls
        => kommtRaus.SelectMany(x =>
            gameCalls.Select(y => new object[] { x, y }));

    [Theory]
    [MemberData(nameof(KommtRausXCalls))]
    public void Test_GameHistoryIsEmpty_AfterInitialization(
        int kommtRaus, GameCall call)
    {
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = new Hand[4];
        deck.InitialHands(call, initialHands);

        var history = new GameLog(call, initialHands, kommtRaus);

        history.Call.Should().Be(call);
        history.KommtRaus.Should().Be(kommtRaus);
        history.Turns.Should().HaveCount(1);
        history.CurrentTurn.CardsCount.Should().Be(0);
    }
}

public class GameHistoryTest_IteratingOverTurnsWhileApplyingCards
{
    [Fact]
    public void Test_ApplyCardToHistory_WhenTurnIsEmpty()
    {
        var call = GameCall.Wenz(0);
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = new Hand[4];
        deck.InitialHands(call, initialHands);
        var history = new GameLog(call, initialHands, 0);

        var cardToPlay = initialHands[0].PickRandom();
        var turnBefore = history.CurrentTurn;
        var turnAfter = history.NextCard(cardToPlay);

        turnAfter.Should().Be(history.CurrentTurn);
        turnAfter.CardsCount.Should().Be(turnBefore.CardsCount + 1);
        turnAfter.AllCards.ElementAt(0).Should().Be(cardToPlay);
    }

    public static IEnumerable<object[]> cardsAlreadyPlayed
        => Enumerable.Range(1, 3).Select(x => new object[] { x });
    [Theory]
    [MemberData(nameof(cardsAlreadyPlayed))]
    public void Test_ApplyCardToHistory_WhenTurnIsNotEmpty(int cardsAlreadyPlayed)
    {
        var call = GameCall.Wenz(0);
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = new Hand[4];
        deck.InitialHands(call, initialHands);
        var history = new GameLog(call, initialHands, 0);

        foreach (int i in Enumerable.Range(0, cardsAlreadyPlayed))
            history.NextCard(initialHands[i].PickRandom());
        var turnBefore = history.CurrentTurn;
        int cardPos = cardsAlreadyPlayed;
        var cardToPlay = initialHands[cardPos].PickRandom();
        var turnAfter = history.NextCard(cardToPlay);

        turnAfter.Should().Be(history.CurrentTurn);
        turnAfter.CardsCount.Should().Be(turnBefore.CardsCount + 1);
        turnAfter.AllCards.ElementAt(cardPos).Should().Be(cardToPlay);
    }

    [Fact]
    public void Test_TurnFinalizesAndStartsNewTurn_AfterApplyingLastCardOfTurn()
    {
        var call = GameCall.Wenz(0);
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = new Hand[4];
        deck.InitialHands(call, initialHands);
        var history = new GameLog(call, initialHands, 0);

        var turnEnumerator = history.GetEnumerator();
        turnEnumerator.MoveNext();
        foreach (int i in Enumerable.Range(0, 4))
            history.NextCard(initialHands[i].PickRandom());
        turnEnumerator.MoveNext();
        var nextTurn = turnEnumerator.Current;

        nextTurn.Should().Be(history.CurrentTurn);
        nextTurn.CardsCount.Should().Be(0);
        history.TurnCount.Should().Be(2);
    }

    [Fact]
    public void Test_HistoryFinalizes_AfterApplyingLastCardOfLastTurn()
    {
        var call = GameCall.Wenz(0);
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = new Hand[4];
        deck.InitialHands(call, initialHands);
        var history = new GameLog(call, initialHands, 0);

        foreach (var turn in history)
            foreach (int i in Enumerable.Range(0, 4))
                history.NextCard(initialHands[i].PickRandom());

        history.Turns.Should().Match(turns => turns.All(t => t.CardsCount == 4));
        history.TurnCount.Should().Be(8);
        history.Turns.Should().HaveCount(8);
    }
}
