using FluentAssertions;

namespace Schafkopf.Lib.Test;

public class HandPropertiesTest
{
    [Fact]
    public void Test_CanRetrieveCardOfAnyTypeAndColor()
    {
        var cards = new Card[] {
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Neun, CardColor.Herz),
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Ober, CardColor.Gras),
            new Card(CardType.Koenig, CardColor.Gras),
            new Card(CardType.Zehn, CardColor.Eichel),
            new Card(CardType.Sau, CardColor.Eichel),
        };
        var hand = new Hand(cards);

        hand.Cards.Should().BeEquivalentTo(cards);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Test_HasCardIsTrue_WhenCardIsInHand(int lookupIndex)
    {
        var cards = new Card[] {
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Neun, CardColor.Herz),
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Ober, CardColor.Gras),
            new Card(CardType.Koenig, CardColor.Gras),
            new Card(CardType.Zehn, CardColor.Eichel),
            new Card(CardType.Sau, CardColor.Eichel),
        };
        var hand = new Hand(cards);

        var cardToLookup = cards[lookupIndex];
        var hasCard = hand.HasCard(cardToLookup);

        hasCard.Should().BeTrue();
    }

    public static IEnumerable<object[]> OtherCardIds =
        Enumerable.Range(0, 24).Select(i => new object[] { i }).ToList();
    [Theory]
    [MemberData(nameof(OtherCardIds))]
    public void Test_HasCardIsFalse_WhenCardIsNotInHand(int lookupIndex)
    {
        var cards = new Card[] {
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Neun, CardColor.Herz),
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Ober, CardColor.Gras),
            new Card(CardType.Koenig, CardColor.Gras),
            new Card(CardType.Zehn, CardColor.Eichel),
            new Card(CardType.Sau, CardColor.Eichel),
        };
        var otherCards = CardsDeck.AllCards.Except(cards).ToList();
        var hand = new Hand(cards);

        var cardToLookup = otherCards[lookupIndex];
        var hasCard = hand.HasCard(cardToLookup);
        hasCard.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Test_CardIsNotInHand_AfterItWasDiscarded(int delIndex)
    {
        var cards = new Card[] {
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Neun, CardColor.Herz),
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Ober, CardColor.Gras),
            new Card(CardType.Koenig, CardColor.Gras),
            new Card(CardType.Zehn, CardColor.Eichel),
            new Card(CardType.Sau, CardColor.Eichel),
        };
        var hand = new Hand(cards);

        var cardToDiscard = cards[delIndex];
        var newHand = hand.Discard(cardToDiscard);

        newHand.Cards.Should().BeEquivalentTo(
            cards.Except(new Card[] { cardToDiscard }));
    }
}

// TODO: add tests for TrumpfCount, HasCard(), Discard()
// TODO: add tests for trumpf/farbe related properties HasTrumpf(), HasFarbe(), FarbeCount()
