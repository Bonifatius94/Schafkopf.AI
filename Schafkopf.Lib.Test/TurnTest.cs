using FluentAssertions;

namespace Schafkopf.Lib.Test;

public class TurnAddCardTest
{
    public static IEnumerable<object[]> PlayerIds
        => Enumerable.Range(0, 4).Select(i => new object[] { i });

    public static IEnumerable<object[]> AllCards
        => CardsDeck.AllCards
            .Select(c => new Card((byte)(c.Id | Card.EXISTING_FLAG)))
            .Select(c => new object[] { c });

    public static IEnumerable<object[]> PlayerIdsXAllCards
        => PlayerIds.SelectMany(x => AllCards.Select(y => new object[] { x[0], y[0] }));

    [Theory]
    [MemberData(nameof(PlayerIds))]
    public void Test_TurnIsEmpty_AfterInitialization(int playerId)
    {
        var turn = Turn.NewTurn((byte)playerId);
        turn.FirstDrawingPlayerId.Should().Be(playerId);
        turn.CardsCount.Should().Be(0);
        turn.AllCards.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(PlayerIdsXAllCards))]
    public void Test_ShouldYieldTurnWithCard_WhenApplyingNextCard(
        int playerId, Card cardToApply)
    {
        var turn = Turn.NewTurn((byte)playerId);
        var turnWithCardApplied = turn.NextCard(cardToApply);

        turnWithCardApplied.AllCards.Should().Contain(cardToApply);
        turnWithCardApplied.CardsCount.Should().Be(turn.CardsCount + 1);
        turnWithCardApplied.FirstDrawingPlayerId.Should().Be(playerId);
    }

    private static readonly Random rng = new Random();

    [Theory]
    [MemberData(nameof(PlayerIds))]
    public void Test_ShouldYieldFinishedTurn_WhenApplying4Cards(int playerId)
    {
        var allCards = AllCards;
        var permGen = new EqualDistPermutator(32);
        var cardsToApply = permGen.NextPermutation().Take(4)
            .Select(i => (Card)allCards.ElementAt(i)[0]).ToList();

        var turn = Turn.NewTurn((byte)playerId);

        foreach (var cardToApply in cardsToApply)
            turn = turn.NextCard(cardToApply);

        turn.AllCards.Should().Contain(cardsToApply);
        turn.CardsCount.Should().Be(4);
        turn.FirstDrawingPlayerId.Should().Be(playerId);
    }
}

public class TurnAugenCountTest
{
    [Fact]
    public void Test_HasCorrectAugen_WhenInInitialState()
    {
        var turn = Turn.NewTurn(0);
        turn.Augen.Should().Be(0);
    }

    #region TestData

    public static IEnumerable<object[]> CardsWithExpAugen =
        new List<object[]> {
            new object[] {
                new List<Card> {
                    new Card(CardType.Sieben, CardColor.Schell, true, false),
                    new Card(CardType.Acht, CardColor.Eichel, true, false),
                    new Card(CardType.Neun, CardColor.Herz, true, false),
                    new Card(CardType.Sieben, CardColor.Gras, true, false)
                },
                0
            },
            new object[] {
                new List<Card> {
                    new Card(CardType.Unter, CardColor.Schell, true, false),
                },
                2
            },
            new object[] {
                new List<Card> {
                    new Card(CardType.Ober, CardColor.Eichel, true, false),
                },
                3
            },
            new object[] {
                new List<Card> {
                    new Card(CardType.Koenig, CardColor.Eichel, true, false),
                },
                4
            },
            new object[] {
                new List<Card> {
                    new Card(CardType.Zehn, CardColor.Herz, true, false),
                },
                10
            },
            new object[] {
                new List<Card> {
                    new Card(CardType.Sau, CardColor.Eichel, true, false),
                },
                11
            },
            new object[] {
                new List<Card> {
                    new Card(CardType.Unter, CardColor.Schell, true, false),
                    new Card(CardType.Ober, CardColor.Eichel, true, false),
                    new Card(CardType.Koenig, CardColor.Herz, true, false),
                    new Card(CardType.Zehn, CardColor.Gras, true, false)
                },
                19
            },
            new object[] {
                new List<Card> {
                    new Card(CardType.Sau, CardColor.Schell, true, false),
                    new Card(CardType.Sieben, CardColor.Eichel, true, false),
                    new Card(CardType.Koenig, CardColor.Herz, true, false),
                    new Card(CardType.Zehn, CardColor.Gras, true, false)
                },
                25
            },
            new object[] {
                new List<Card> {
                    new Card(CardType.Sau, CardColor.Schell, true, false),
                    new Card(CardType.Sau, CardColor.Eichel, true, false),
                    new Card(CardType.Sau, CardColor.Herz, true, false),
                    new Card(CardType.Sau, CardColor.Gras, true, false)
                },
                44
            },
        };

    #endregion TestData

    [Theory]
    [MemberData(nameof(CardsWithExpAugen))]
    public void Test_HasCorrectAugen_WhenCardsInserted(
        List<Card> cardsToApply, int expAugen)
    {
        var turn = Turn.NewTurn(0);
        foreach (var card in cardsToApply)
            turn = turn.NextCard(card);

        turn.Augen.Should().Be(expAugen);
    }
}
