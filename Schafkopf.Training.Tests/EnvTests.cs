using Schafkopf.Lib;

namespace Schafkopf.Training.Tests;

public class CardPickerEnvTests
{
    [Fact]
    public void Test_CanPlayGame()
    {
        var rules = new GameRules();
        var cardCache = new Card[8];
        var rng = new Random();

        var env = new CardPickerEnv();
        var state = env.Reset();
        foreach (int i in Enumerable.Range(0, 32))
        {
            var possActions = rules.PossibleCards(state, cardCache);
            var action = possActions[rng.Next(possActions.Length)];
            (state, var __, var ___) = env.Step(action);
            Assert.Equal(i+1, state.CardCount);
        }

        Assert.Equal(32, state.CardCount); // assert that no exception occurred
    }

    [Fact]
    public void Test_CanPlayConsequtiveGames()
    {
        var rules = new GameRules();
        var cardCache = new Card[8];
        var rng = new Random();
        var env = new CardPickerEnv();

        foreach (int _ in Enumerable.Range(0, 1000))
        {
            var state = env.Reset();
            foreach (int i in Enumerable.Range(0, 32))
            {
                var possActions = rules.PossibleCards(state, cardCache);
                var action = possActions[rng.Next(possActions.Length)];
                (state, var __, var ___) = env.Step(action);
                Assert.Equal(i+1, state.CardCount);
            }

            Assert.Equal(32, state.CardCount); // assert that no exception occurred
        }
    }
}
