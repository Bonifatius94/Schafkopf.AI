using FluentAssertions;

namespace Schafkopf.Lib.Test;

public class GameRulesTest
{
    #region Init

    private Turn turnOfCards(GameCall call, IEnumerable<Card> cards)
    {
        var turn = Turn.InitFirstTurn(0, call);
        foreach (var card in cards)
            turn = turn.NextCard(card);
        return turn;
    }

    #endregion Init

    private DrawValidator drawEval = new DrawValidator();

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
        var hand = new Hand(new Card[] {
            new Card(CardType.Ober, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Eichel),
            new Card(CardType.Ober, CardColor.Gras),
        });
        hand = hand.CacheTrumpf(call.IsTrumpf);
        var turn = Turn.InitFirstTurn(0, call);

        var cardToPlay = hand.Cards.ElementAt(indexOfCardToPlay);
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
        var hand = new Hand(new Card[] {
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Eichel),
            new Card(CardType.Ober, CardColor.Gras),
        });
        hand = hand.CacheTrumpf(call.IsTrumpf);
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Unter, CardColor.Eichel, true, true)
        });

        var cardToPlay = hand.Cards.ElementAt(indexOfCardToPlay);
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
        var hand = new Hand(new Card[] {
            new Card(CardType.Ober, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Eichel),
            new Card(CardType.Ober, CardColor.Gras),
        });
        hand = hand.CacheTrumpf(call.IsTrumpf);
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Unter, CardColor.Eichel, true, true)
        });

        var cardToPlay = hand.Cards.ElementAt(indexOfCardToPlay);
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
        var hand = new Hand(new Card[] {
            new Card(CardType.Neun, CardColor.Eichel),
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Herz),
            new Card(CardType.Ober, CardColor.Gras),
        });
        hand = hand.CacheTrumpf(call.IsTrumpf);
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Ober, CardColor.Eichel, true, false)
        });

        var cardToPlay = hand.Cards.ElementAt(indexOfCardToPlay);
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
        var hand = new Hand(new Card[] {
            new Card(CardType.Unter, CardColor.Eichel),
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Herz),
            new Card(CardType.Ober, CardColor.Gras),
        });
        hand = hand.CacheTrumpf(call.IsTrumpf);
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Ober, CardColor.Eichel, true, false)
        });

        var cardToPlay = hand.Cards.ElementAt(indexOfCardToPlay);
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
        var hand = new Hand(new Card[] {
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Eichel),
            new Card(CardType.Sau, CardColor.Gras),
        });
        hand = hand.CacheTrumpf(call.IsTrumpf);
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Sieben, CardColor.Schell, true, false)
        });

        var cardToPlay = hand.Cards.ElementAt(indexOfCardToPlay);
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
        var hand = new Hand(new Card[] {
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Neun, CardColor.Schell),
            new Card(CardType.Ober, CardColor.Gras),
        });
        hand = hand.CacheTrumpf(call.IsTrumpf);
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Koenig, CardColor.Schell, true, false)
        });

        var cardToPlay = hand.Cards.ElementAt(indexOfCardToPlay);
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
        var hand = new Hand(new Card[] {
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
        });
        hand = hand.CacheTrumpf(call.IsTrumpf);
        var turn = Turn.InitFirstTurn(0, call);

        var cardToPlay = hand.Cards.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    // TODO: there needs to be some meta-data flag in Turn whether already gsucht
    //       Sauspiel partner is allowed to play cards of gsuchte farbe except
    //       for gsuchte sau and can still use 'kann unten durch' rule

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void Test_CanPlayAnyCardOfFarbe_WhenPartnerKommtRausAndKannUntenDurch(
        int indexOfCardToPlay, bool canPlayCard)
    {
        var call = GameCall.Sauspiel(1, 0, CardColor.Schell);
        var hand = new Hand(new Card[] {
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Neun, CardColor.Schell),
        });
        hand = hand.CacheTrumpf(call.IsTrumpf);
        var turn = Turn.InitFirstTurn(0, call);

        var cardToPlay = hand.Cards.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }

    [Theory(Skip="logic is not ready for this test")]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void Test_MustNotSchmierenGsuchteSau_WhenNotAlreadyGsucht(
        int indexOfCardToPlay, bool canPlayCard)
    {
        var call = GameCall.Sauspiel(1, 0, CardColor.Schell);
        var hand = new Hand(new Card[] {
            new Card(CardType.Sau, CardColor.Schell),
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Neun, CardColor.Schell),
        });
        hand = hand.CacheTrumpf(call.IsTrumpf);
        var turn = turnOfCards(call, new Card[] {
            new Card(CardType.Sieben, CardColor.Eichel, true, false)
        });
        // TODO: initialize turn meta-data such that it's already gsucht

        var cardToPlay = hand.Cards.ElementAt(indexOfCardToPlay);
        drawEval.CanPlayCard(call, cardToPlay, turn, hand)
            .Should().Be(canPlayCard);
    }
}
