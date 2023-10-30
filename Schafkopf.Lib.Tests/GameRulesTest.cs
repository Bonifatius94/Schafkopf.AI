namespace Schafkopf.Lib.Test;

public class GameRulesTest
{
    #region Init

    private Turn turnOfCards(GameCall call, IEnumerable<Card> cards, int kommtRaus = 0)
    {
        var turn = Turn.InitFirstTurn(kommtRaus, call);
        foreach (var card in cards)
            turn = turn.NextCard(card);
        return turn;
    }

    private Hand handWithCards(GameCall call, IEnumerable<Card> cards)
    {
        var discardedCards = CardsDeck.AllCards
            .Except(cards).RandomSubset(8 - cards.Count()).ToArray();
        var initialHand = discardedCards.Union(cards).ToArray();
        var hand = new Hand(initialHand).CacheTrumpf(call.IsTrumpf);
        foreach (var c in discardedCards)
            hand = hand.Discard(c);
        return hand;
    }

    #endregion Init

    private GameRules drawEval = new GameRules();

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void Test_CanPlayAnyCard_WhenKommtRausAndNoSauspiel(
        int indexOfCardToPlay, bool canPlayCard)
    {
        var call = GameCall.Solo(0, CardColor.Schell);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Ober, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Eichel),
            new Card(CardType.Ober, CardColor.Gras),
        });
        var turn = Turn.InitFirstTurn(0, call);

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void Test_TrumpfZugeben_WhenTrumpfPlayedAndTrumpfInHand(
        int indexOfCardToPlay, bool canPlayCard)
    {
        var call = GameCall.Wenz(0);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Eichel),
            new Card(CardType.Ober, CardColor.Gras),
        });
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Unter, CardColor.Eichel, true, true)
        });

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void Test_TrumpfZugeben_WhenTrumpfPlayedAndTrumpfInHand_GivenSauspiel(
        int indexOfCardToPlay, bool canPlayCard)
    {
        var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Eichel),
            new Card(CardType.Sau, CardColor.Gras),
        });
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Unter, CardColor.Eichel, true, true)
        });

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void Test_FarbeZugeben_WhenFarbePlayedAndFarbeInHand_GivenSauspiel(
        int indexOfCardToPlay, bool canPlayCard)
    {
        var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Eichel),
            new Card(CardType.Sau, CardColor.Gras),
        });
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Neun, CardColor.Eichel, true, false)
        });

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void Test_NotTrumpfZugeben_WhenTrumpfPlayedButNoTrumpfInHand(
        int indexOfCardToPlay, bool canPlayCard)
    {
        var call = GameCall.Wenz(0);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Ober, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Eichel),
            new Card(CardType.Ober, CardColor.Gras),
        });
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Unter, CardColor.Eichel, true, true)
        });

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void Test_FarbeZugeben_WhenFarbePlayedAndFarbeInHand(
        int indexOfCardToPlay, bool canPlayCard)
    {
        // TODO: parameterize for testing all colors
        var call = GameCall.Wenz(0);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Neun, CardColor.Eichel),
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Herz),
            new Card(CardType.Ober, CardColor.Gras),
        });
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Ober, CardColor.Eichel, true, false)
        });

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void Test_NotFarbeZugeben_WhenFarbePlayedAndIsFrei(
        int indexOfCardToPlay, bool canPlayCard)
    {
        // TODO: parameterize for testing all colors
        var call = GameCall.Wenz(0);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Unter, CardColor.Eichel),
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Herz),
            new Card(CardType.Ober, CardColor.Gras),
        });
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Ober, CardColor.Eichel, true, false)
        });

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void Test_MustPlayGsuchteSau_WhenGsuchtIsAndGsuchteInHandAndNotKannUntenDurch(
        int indexOfCardToPlay, bool canPlayCard)
    {
        // TODO: parameterize gsuchte sau for testing all colors
        var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Eichel),
            new Card(CardType.Sau, CardColor.Gras),
        });
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Sieben, CardColor.Schell, true, false)
        });

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void Test_MustPlayGsuchteSau_WhenGsuchtIsAndKannUntenDurch(
        int indexOfCardToPlay, bool canPlayCard)
    {
        // TODO: parameterize gsuchte sau for testing all colors
        var call = GameCall.Sauspiel(0, 1, CardColor.Schell);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Neun, CardColor.Schell),
            new Card(CardType.Ober, CardColor.Gras),
        });
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Koenig, CardColor.Schell, true, false)
        });

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void Test_MustPlayGsuchteSau_WhenPartnerKommtRausAndNotKannUntenDurch(
        int indexOfCardToPlay, bool canPlayCard)
    {
        var call = GameCall.Sauspiel(1, 0, CardColor.Schell);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Herz),
            new Card(CardType.Koenig, CardColor.Herz),
            new Card(CardType.Zehn, CardColor.Herz),
            new Card(CardType.Ober, CardColor.Gras),
            new Card(CardType.Sau, CardColor.Eichel),
            new Card(CardType.Unter, CardColor.Schell),
        });
        var turn = Turn.InitFirstTurn(0, call);

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void Test_CanPlayAnyCardOfFarbe_WhenPartnerKommtRausAndKannUntenDurch(
        int indexOfCardToPlay, bool canPlayCard)
    {
        var call = GameCall.Sauspiel(1, 0, CardColor.Schell);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Neun, CardColor.Schell),
        });
        var turn = Turn.InitFirstTurn(0, call);

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void Test_MustNotSchmierenGsuchteSau_WhenNotAlreadyGsucht(
        int indexOfCardToPlay, bool canPlayCard)
    {
        var call = GameCall.Sauspiel(2, 1, CardColor.Schell);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Neun, CardColor.Schell),
        });
        var turn = turnOfCards(call, new Card[] {
                new Card(CardType.Sieben, CardColor.Eichel, true, false),
            }, 0);

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void Test_CanSchmierenGsuchteSau_WhenAlreadyGsucht(
        int indexOfCardToPlay, bool canPlayCard)
    {
        var call = GameCall.Sauspiel(1, 0, CardColor.Schell);
        var hand = handWithCards(call, new Card[] {
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Neun, CardColor.Schell),
        });
        var gsuchtTurn = turnOfCards(call, new Card[] {
            new Card(CardType.Koenig, CardColor.Schell, true, false),
            new Card(CardType.Zehn, CardColor.Schell, true, false),
            new Card(CardType.Zehn, CardColor.Eichel, true, false),
            new Card(CardType.Sau, CardColor.Herz, true, true),
        });
        var turn = Turn.InitNextTurn(gsuchtTurn);
        turn = turn.NextCard(new Card(CardType.Sieben, CardColor.Eichel, true, false));

        var cardToPlay = hand.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }
}

public class GameRules_RegressionTests
{
    private Hand handWithCards(GameCall call, IEnumerable<Card> cards)
    {
        var discardedCards = CardsDeck.AllCards
            .Except(cards).RandomSubset(8 - cards.Count()).ToArray();
        var initialHand = discardedCards.Union(cards).ToArray();
        var hand = new Hand(initialHand).CacheTrumpf(call.IsTrumpf);
        foreach (var c in discardedCards)
            hand = hand.Discard(c);
        return hand;
    }

    [Fact]
    public void Test_MussFarbeZugeben()
    {
        // player 3 called Sauspiel mit der Gras Sau
        // player 0 holding Schell Sau, Eichel Koenig, Herz Ober (trumpf), Schell Sieben, Eichel Acht, Herz Sau (trumpf), Herz Sieben (trumpf), Herz Koenig (trumpf)
        // player 1 holding Schell Ober (trumpf), Herz Acht (trumpf), Gras Sau, Gras Ober (trumpf), Herz Zehn (trumpf), Gras Unter (trumpf), Schell Zehn, Eichel Ober (trumpf)
        // player 2 holding Gras Acht, Schell Koenig, Gras Koenig, Herz Neun (trumpf), Gras Zehn, Eichel Zehn, Eichel Sau, Gras Sieben
        // player 3 holding Eichel Unter (trumpf), Eichel Sieben, Schell Unter (trumpf), Gras Neun, Schell Neun, Herz Unter (trumpf), Schell Acht, Eichel Neun
        // -----------------------------
        // started turn 1, 0 kommt raus
        // player 0 played Schell Sau
        // player 1 played Schell Ober (trumpf)
        // player 2 played Schell Koenig
        // player 3 played Schell Neun

        var call = GameCall.Sauspiel(3, 1, CardColor.Gras);
        var hand = handWithCards(call, new Card[] {
                new Card(CardType.Ober, CardColor.Schell),
                new Card(CardType.Acht, CardColor.Herz),
                new Card(CardType.Sau, CardColor.Gras),
                new Card(CardType.Ober, CardColor.Gras),
                new Card(CardType.Zehn, CardColor.Herz),
                new Card(CardType.Unter, CardColor.Gras),
                new Card(CardType.Zehn, CardColor.Schell),
                new Card(CardType.Ober, CardColor.Eichel),
            });

        var turn = Turn.InitFirstTurn(0, call);
        turn = turn.NextCard(new Card(CardType.Sau, CardColor.Schell, true, false));

        var drawEval = new GameRules();
        drawEval.CanPlayCard(call, hand[0], turn, hand)
            .Should().BeFalse();
        drawEval.CanPlayCard(call, hand[6], turn, hand)
            .Should().BeTrue();
    }
}
