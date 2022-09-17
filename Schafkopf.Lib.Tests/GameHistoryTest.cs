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
        var initialHands = deck.InitialHands(call);

        var history = new GameHistory(call, initialHands, kommtRaus);

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
        var initialHands = deck.InitialHands(call);
        var history = new GameHistory(call, initialHands, 0);

        var cardToPlay = initialHands[0].PickRandom();
        var turnBefore = history.CurrentTurn;
        var turnAfter = history.NextCard(cardToPlay);

        turnAfter.Should().Be(history.CurrentTurn);
        turnAfter.CardsCount.Should().Be(turnBefore.CardsCount + 1);
        turnAfter.AllCards.ElementAt(0).Should().Be(cardToPlay);
    }

    [Fact]
    public void Test_ApplyCardToHistory_WhenTurnIsNotEmpty()
    {
        // var call = GameCall.Wenz(0);
        // var deck = new CardsDeck();
        // deck.Shuffle();
        // var initialHands = deck.InitialHands(call);
        // var history = new GameHistory(call, initialHands, 0);

        // var cardToPlay = initialHands[0].PickRandom();
        // var turnBefore = history.CurrentTurn;
        // var turnAfter = history.NextCard(cardToPlay);

        // turnAfter.Should().Be(history.CurrentTurn);
        // turnAfter.CardsCount.Should().Be(turnBefore.CardsCount + 1);
        // turnAfter.AllCards.ElementAt(0).Should().Be(cardToPlay);
    }

    [Fact]
    public void Test_TurnFinalizesAndStartsNewTurn_AfterApplyingLastCardOfTurn()
    {
        // TODO: implement test
    }

    [Fact]
    public void Test_HistoryFinalizes_AfterApplyingLastCardOfLastTurn()
    {
        // TODO: implement test
    }
}
