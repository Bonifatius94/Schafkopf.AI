using Schafkopf.Lib;

namespace Schafkopf.Training.Tests;

public class HeuristicAgentTests
{
    public static IEnumerable<object[]> Ober =
        new List<Card>() {
            new Card(CardType.Ober, CardColor.Herz),
            new Card(CardType.Ober, CardColor.Gras),
            new Card(CardType.Ober, CardColor.Eichel)
        }.Select(x => new object[] { x });

    [Theory]
    [MemberData(nameof(Ober))]
    public void Test_PlaySauspiel_WhenFiveTrumpfOrMoreAndNoLaufende(Card ober)
    {
        var cards = new Card[] {
            ober,
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Sau, CardColor.Herz),
            new Card(CardType.Koenig, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Herz),
            new Card(CardType.Koenig, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Zehn, CardColor.Eichel)
        };
        var hand = new Hand(cards);
        var otherHands = CardsDeck.AllCards.Except(cards).Chunk(8).Select(h => new Hand(h));
        var allHands = new Hand[] { hand }.Concat(otherHands).ToArray();
        var possCalls = new GameCallGenerator().AllPossibleCalls(0, allHands, GameCall.Weiter());

        var gameCaller = new HeuristicGameCaller(new GameMode[] { GameMode.Sauspiel });
        var call = gameCaller.MakeCall(possCalls, 0, hand, 0);

        Assert.Equal(GameMode.Sauspiel, call.Mode);
    }

    [Theory]
    [MemberData(nameof(Ober))]
    public void Test_PlaySauspiel_WhenFourTrumpfAndFreiAndNoLaufende(Card ober)
    {
        var cards = new Card[] {
            ober,
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Sau, CardColor.Herz),
            new Card(CardType.Koenig, CardColor.Herz),
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Koenig, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Zehn, CardColor.Eichel)
        };
        var hand = new Hand(cards);
        var otherHands = CardsDeck.AllCards.Except(cards).Chunk(8).Select(h => new Hand(h));
        var allHands = new Hand[] { hand }.Concat(otherHands).ToArray();
        var possCalls = new GameCallGenerator().AllPossibleCalls(0, allHands, GameCall.Weiter());

        var gameCaller = new HeuristicGameCaller(new GameMode[] { GameMode.Sauspiel });
        var call = gameCaller.MakeCall(possCalls, 0, hand, 0);

        Assert.Equal(GameMode.Sauspiel, call.Mode);
    }

    [Fact]
    public void Test_DontPlaySauspiel_WhenFiveTrumpfOrMoreAndLaufende()
    {
        var cards = new Card[] {
            new Card(CardType.Ober, CardColor.Schell),
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Sau, CardColor.Herz),
            new Card(CardType.Koenig, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Herz),
            new Card(CardType.Koenig, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Zehn, CardColor.Eichel)
        };
        var hand = new Hand(cards);
        var otherHands = CardsDeck.AllCards.Except(cards).Chunk(8).Select(h => new Hand(h));
        var allHands = new Hand[] { hand }.Concat(otherHands).ToArray();
        var possCalls = new GameCallGenerator().AllPossibleCalls(0, allHands, GameCall.Weiter());

        var gameCaller = new HeuristicGameCaller(new GameMode[] { GameMode.Sauspiel });
        var call = gameCaller.MakeCall(possCalls, 0, hand, 0);

        Assert.Equal(GameMode.Weiter, call.Mode);
    }

    [Fact]
    public void Test_DontPlaySauspiel_WhenFourTrumpfAndFreiAndLaufende()
    {
        var cards = new Card[] {
            new Card(CardType.Ober, CardColor.Schell),
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Sau, CardColor.Herz),
            new Card(CardType.Koenig, CardColor.Herz),
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Koenig, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Zehn, CardColor.Eichel)
        };
        var hand = new Hand(cards);
        var otherHands = CardsDeck.AllCards.Except(cards).Chunk(8).Select(h => new Hand(h));
        var allHands = new Hand[] { hand }.Concat(otherHands).ToArray();
        var possCalls = new GameCallGenerator().AllPossibleCalls(0, allHands, GameCall.Weiter());

        var gameCaller = new HeuristicGameCaller(new GameMode[] { GameMode.Sauspiel });
        var call = gameCaller.MakeCall(possCalls, 0, hand, 0);

        Assert.Equal(GameMode.Weiter, call.Mode);
    }

    [Fact]
    public void Test_ChooseFarbeWithLeastCards_WhenPlayingSauspiel()
    {
        var cards = new Card[] {
            new Card(CardType.Ober, CardColor.Gras),
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Sau, CardColor.Herz),
            new Card(CardType.Koenig, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Herz),
            new Card(CardType.Koenig, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Zehn, CardColor.Eichel)
        };
        var hand = new Hand(cards);
        var otherHands = CardsDeck.AllCards.Except(cards).Chunk(8).Select(h => new Hand(h));
        var allHands = new Hand[] { hand }.Concat(otherHands).ToArray();
        var possCalls = new GameCallGenerator().AllPossibleCalls(0, allHands, GameCall.Weiter());

        var gameCaller = new HeuristicGameCaller(new GameMode[] { GameMode.Sauspiel });
        var call = gameCaller.MakeCall(possCalls, 0, hand, 0);

        Assert.Equal(GameMode.Sauspiel, call.Mode);
        Assert.Equal(CardColor.Eichel, call.GsuchteFarbe);
    }
}
