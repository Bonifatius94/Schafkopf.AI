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
        var turn = Turn.InitFirstTurn((byte)playerId, GameCall.Wenz(playerId));
        turn.FirstDrawingPlayerId.Should().Be(playerId);
        turn.CardsCount.Should().Be(0);
        turn.AllCards.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(PlayerIdsXAllCards))]
    public void Test_ShouldYieldTurnWithCard_WhenApplyingNextCard(
        int playerId, Card cardToApply)
    {
        var turn = Turn.InitFirstTurn((byte)playerId, GameCall.Wenz(playerId));
        var turnWithCardApplied = turn.NextCard(cardToApply);

        turnWithCardApplied.AllCards.Should().Contain(cardToApply);
        turnWithCardApplied.CardsCount.Should().Be(turn.CardsCount + 1);
        turnWithCardApplied.FirstDrawingPlayerId.Should().Be(playerId);
    }

    [Theory]
    [MemberData(nameof(PlayerIds))]
    public void Test_ShouldYieldFinishedTurn_WhenApplying4Cards(int playerId)
    {
        var allCards = AllCards;
        var permGen = new EqualDistPermutator_256(32);
        var cardsToApply = permGen.NextPermutation().Take(4)
            .Select(i => (Card)allCards.ElementAt(i)[0]).ToList();

        var turn = Turn.InitFirstTurn((byte)playerId, GameCall.Wenz(playerId));

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
        var turn = Turn.InitFirstTurn(0, GameCall.Wenz(0));
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
        var turn = Turn.InitFirstTurn(0, GameCall.Wenz(0));
        foreach (var card in cardsToApply)
            turn = turn.NextCard(card);

        turn.Augen.Should().Be(expAugen);
    }
}

public class TurnWinnerTest
{
    #region Init

    public static IEnumerable<object[]> CardsWithExpWinner =
        new List<object[]> {
            new object[] {
                GameCall.Solo(0, CardColor.Schell),
                new List<Card> {
                    new Card(CardType.Sieben, CardColor.Herz),
                    new Card(CardType.Acht, CardColor.Herz),
                    new Card(CardType.Neun, CardColor.Herz),
                    new Card(CardType.Koenig, CardColor.Herz),
                },
                0, 3
            },
            new object[] {
                GameCall.Solo(0, CardColor.Schell),
                new List<Card> {
                    new Card(CardType.Koenig, CardColor.Herz),
                    new Card(CardType.Zehn, CardColor.Herz),
                    new Card(CardType.Sieben, CardColor.Eichel),
                    new Card(CardType.Sau, CardColor.Herz),
                },
                1, 0
            },
            new object[] {
                GameCall.Solo(0, CardColor.Schell),
                new List<Card> {
                    new Card(CardType.Koenig, CardColor.Herz),
                    new Card(CardType.Zehn, CardColor.Herz),
                    new Card(CardType.Sau, CardColor.Eichel),
                    new Card(CardType.Sieben, CardColor.Eichel),
                },
                1, 2
            },
            new object[] {
                GameCall.Solo(0, CardColor.Schell),
                new List<Card> {
                    new Card(CardType.Ober, CardColor.Eichel),
                    new Card(CardType.Koenig, CardColor.Schell),
                    new Card(CardType.Zehn, CardColor.Herz),
                    new Card(CardType.Sau, CardColor.Herz),
                },
                2, 2
            },
            new object[] {
                GameCall.Solo(0, CardColor.Schell),
                new List<Card> {
                    new Card(CardType.Ober, CardColor.Herz),
                    new Card(CardType.Unter, CardColor.Herz),
                    new Card(CardType.Ober, CardColor.Gras),
                    new Card(CardType.Sau, CardColor.Schell),
                },
                3, 1
            },
            new object[] {
                GameCall.Solo(0, CardColor.Herz),
                new List<Card> {
                    new Card(CardType.Sieben, CardColor.Schell),
                    new Card(CardType.Sieben, CardColor.Eichel),
                    new Card(CardType.Sieben, CardColor.Gras),
                    new Card(CardType.Acht, CardColor.Gras),
                },
                3, 3
            },
        };

    #endregion Init

    [Theory]
    [MemberData(nameof(CardsWithExpWinner))]
    public void Test_YieldsExpectedWinner_AfterApplyingGivenCards(
        GameCall call, List<Card> cardsToApply, int beginningPlayer, int expWinner)
    {
        var deck = new CardsDeck();
        var allCardsWithMeta = deck.SelectMany(h => h.CacheTrumpf(call.IsTrumpf)).ToArray();
        var turn = Turn.InitFirstTurn((byte)beginningPlayer, call);
        var cardsToApplyWithMeta = cardsToApply
            .Select(x => allCardsWithMeta.First(y => y == x));
        foreach (var card in cardsToApplyWithMeta)
            turn = turn.NextCard(card);

        turn.WinnerId.Should().Be(expWinner);
    }

    private static IEnumerable<Card> AllCards
        => CardsDeck.AllCards
            .Select(c => new Card((byte)(c.Id | Card.EXISTING_FLAG)))
            .ToList();

    public static IEnumerable<object[]> TooLessCards =
        Enumerable.Range(0, 4).Select(i => AllCards.Take(i).ToList())
            .Select(x => new object[] { x });

    // TODO: add test coverage for Turn.InitNextTurn(Turn last)
}
