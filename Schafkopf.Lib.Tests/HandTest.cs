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

        hand.Should().BeEquivalentTo(cards);
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

    public static IEnumerable<object[]> AllCardsAtAllPositions =
        Enumerable.Range(0, 8)
            .SelectMany(i => CardsDeck.AllCards
                .Select(c => new object[] { i, c }))
            .ToList();

    [Theory]
    [MemberData(nameof(AllCardsAtAllPositions))]
    public void Test_CardIsNotInInitialHand_AfterItWasDiscarded(
        int delIndex, Card cardToDiscard)
    {
        var cards = CardsDeck.AllCards
            .Except(new Card[] { cardToDiscard })
            .RandomSubset(7).ToList();
        cards.Insert(delIndex, cardToDiscard);
        var hand = new Hand(cards.ToArray());

        var newHand = hand.Discard(cardToDiscard);

        newHand.Should().BeEquivalentTo(
            cards.Except(new Card[] { cardToDiscard }));
        newHand.Should().HaveCount(7);
    }

    public static IEnumerable<object[]> LotsOfRandomHandsToDiscard
        => Enumerable.Range(0, 500)
            .Select(_ => new object[] {
                CardsDeck.AllCards.RandomSubset(8).ToList(),
                permGen.NextPermutation().ToList()
            });

    [Theory]
    [MemberData(nameof(LotsOfRandomHandsToDiscard))]
    public void Test_CanDiscardEntireHand_GivenLotsOfPerms(
        IEnumerable<Card> cards, IEnumerable<byte> cardsDelOrder)
    {
        // info: exhaustive testing would involve ~ 10^7 unique hands
        //       -> this only tests a representative random subset

        var hand = new Hand(cards.ToArray());
        var cardsToDiscard = cardsDelOrder
            .Select(x => hand.ElementAt(x)).ToArray();
        var expCards = cards.ToList();

        foreach (var cardToDiscard in cardsToDiscard)
        {
            expCards.Remove(cardToDiscard);
            var handAfter = hand.Discard(cardToDiscard);
            handAfter.Should().BeEquivalentTo(expCards);
            handAfter.Should().HaveCount(hand.CardsCount - 1);
            hand = handAfter;
        }

        hand.Should().BeEmpty();
    }

    private static readonly EqualDistPermutator_256 permGen =
        new EqualDistPermutator_256(8);

    public static IEnumerable<object[]> cardsCount =
        Enumerable.Range(0, 9).Select(i => new object[] { i }).ToList();

    [Theory]
    [MemberData(nameof(cardsCount))]
    public void Test_CardsCountIsCorrect_AfterDiscardingAGivenAmountOfCards(int cardsCount)
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
        var perm = permGen.NextPermutation()
            .Take(8 - cardsCount).ToList();
        var cardsToDiscard = perm.Select(i => cards[i]);

        var hand = new Hand(cards);
        foreach (var card in cardsToDiscard)
            hand = hand.Discard(card);

        hand.CardsCount.Should().Be(cardsCount);
    }
}

public class HandTrumpfPropertiesTest
{
    [Fact]
    public void Test_HasTrumpfIsTrue_WhenTrumpfInHand()
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
        var call = GameCall.Solo(0, CardColor.Eichel);

        var hand = new Hand(cards);
        hand = hand.CacheTrumpf(call.IsTrumpf);

        hand.HasTrumpf().Should().BeTrue();
    }

    [Fact]
    public void Test_HasTrumpfIsFalse_WhenNoTrumpfInHand()
    {
        var cards = new Card[] {
            new Card(CardType.Sieben, CardColor.Schell),
            new Card(CardType.Acht, CardColor.Schell),
            new Card(CardType.Neun, CardColor.Schell),
            new Card(CardType.Koenig, CardColor.Schell),
            new Card(CardType.Sau, CardColor.Gras),
            new Card(CardType.Koenig, CardColor.Gras),
            new Card(CardType.Zehn, CardColor.Herz),
            new Card(CardType.Sau, CardColor.Herz),
        };
        var call = GameCall.Solo(0, CardColor.Eichel);

        var hand = new Hand(cards);
        hand = hand.CacheTrumpf(call.IsTrumpf);

        hand.HasTrumpf().Should().BeFalse();
    }
}


public class HandFarbePropertiesTest
{
    public static IEnumerable<object[]> AllColors =
        Enumerable.Range(0, 4).Select(i =>  new object[] { (CardColor)i }).ToList();

    [Theory]
    [MemberData(nameof(AllColors))]
    public void Test_HasFarbeIsTrue_WhenFarbeInHand(CardColor farbe)
    {
        var trumpf = (CardColor)(((int)farbe + 1) % 4);
        var cards = new Card[] {
            new Card(CardType.Unter, trumpf),
            new Card(CardType.Ober, trumpf),
            new Card(CardType.Sieben, farbe),
            new Card(CardType.Acht, farbe),
            new Card(CardType.Neun, farbe),
            new Card(CardType.Koenig, farbe),
            new Card(CardType.Zehn, farbe),
            new Card(CardType.Sau, farbe),
        };
        var call = GameCall.Solo(0, trumpf);

        var hand = new Hand(cards);
        hand = hand.CacheTrumpf(call.IsTrumpf);

        hand.HasFarbe(farbe).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(AllColors))]
    public void Test_HasFarbeIsFalse_WhenCardWithSameColorInHandThatIsTrumpf(CardColor farbe)
    {
        var trumpf = (CardColor)(((int)farbe + 1) % 4);
        var cards = new Card[] {
            new Card(CardType.Unter, farbe),
            new Card(CardType.Ober, farbe),
            new Card(CardType.Sieben, trumpf),
            new Card(CardType.Acht, trumpf),
            new Card(CardType.Neun, trumpf),
            new Card(CardType.Koenig, trumpf),
            new Card(CardType.Zehn, trumpf),
            new Card(CardType.Sau, trumpf),
        };
        var call = GameCall.Solo(0, trumpf);

        var hand = new Hand(cards);
        hand = hand.CacheTrumpf(call.IsTrumpf);

        hand.HasFarbe(farbe).Should().BeFalse();
    }

    public static IEnumerable<object[]> AllCounts =
        Enumerable.Range(0, 7).Select(i =>  new object[] { i }).ToList();
    public static IEnumerable<object[]> AllColorsXCounts =
        AllColors.SelectMany(x => AllCounts.Select(y => new object[] { x[0], y[0] }));

    [Theory]
    [MemberData(nameof(AllColorsXCounts))]
    public void Test_FarbeCountIsCorrect_When(CardColor farbe, int count)
    {
        var trumpf = (CardColor)(((int)farbe + 1) % 4);
        var allTrumpf = new Card[] {
            new Card(CardType.Unter, farbe),
            new Card(CardType.Ober, farbe),
            new Card(CardType.Sieben, trumpf),
            new Card(CardType.Acht, trumpf),
            new Card(CardType.Neun, trumpf),
            new Card(CardType.Koenig, trumpf),
            new Card(CardType.Zehn, trumpf),
            new Card(CardType.Sau, trumpf),
        };
        var allFarbe = new Card[] {
            new Card(CardType.Sieben, farbe),
            new Card(CardType.Acht, farbe),
            new Card(CardType.Neun, farbe),
            new Card(CardType.Koenig, farbe),
            new Card(CardType.Zehn, farbe),
            new Card(CardType.Sau, farbe),
        };
        var cards = allFarbe.Take(count)
            .Concat(allTrumpf.Take(8 - count))
            .ToArray();
        var call = GameCall.Solo(0, trumpf);

        var hand = new Hand(cards);
        hand = hand.CacheTrumpf(call.IsTrumpf);

        hand.FarbeCount(farbe).Should().Be(count);
    }
}
