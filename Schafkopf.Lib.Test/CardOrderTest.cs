using FluentAssertions;

namespace Schafkopf.Lib.Test;

public class WenzTrumpfCardOrderTest
{
    private static readonly Random rng = new Random();

    private GameCall newWenz()
    {
        int playerId = rng.Next(0, 4);
        return GameCall.Wenz(playerId);
    }

    [Fact]
    public void Test_OberAndHerzAreNotTrumpf_WhenPlayingWenz()
    {
        var call = newWenz();
        var allTrumpf = CardsDeck.AllCards.Where(x => x.Type == CardType.Unter);
        var allNonTrumpf = CardsDeck.AllCards.Except(allTrumpf);
        allTrumpf.Should().Match(x => x.All(card => call.IsTrumpf(card)));
        allNonTrumpf.Should().Match(x => x.All(card => !call.IsTrumpf(card)));
    }

    [Fact]
    public void Test_AnyTrumpfWinsAgainstAnyOtherCard_WhenPlayingWenz()
    {
        var call = newWenz();
        var comp = new CardComparer(call);

        var allUnter = CardsDeck.AllCards.Where(x => x.Type == CardType.Unter).ToList();
        var allOtherCards = CardsDeck.AllCards.Except(allUnter).ToList();
        allUnter.Should().Match(trumpf => trumpf.All(t =>
            allOtherCards.All(o => comp.Compare(t, o) > 0)));
    }

    [Fact]
    public void Test_TrumpfAreInCorrectOrder_WhenPlayingWenz()
    {
        var call = newWenz();
        var comp = new CardComparer(call);

        IEnumerable<Card> orderedTrumpfCards = new List<Card>() {
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Unter, CardColor.Gras),
            new Card(CardType.Unter, CardColor.Eichel),
        };

        orderedTrumpfCards.Should().BeInAscendingOrder(comp);
        orderedTrumpfCards.Reverse().Should().BeInDescendingOrder(comp);
    }
}

public class SauspielTrumpfCardOrderTest
{
    private static readonly Random rng = new Random();

    private IEnumerable<Card> allTrumpfInAscendingOrder(CardColor trumpf)
        => new List<Card>() {
            new Card(CardType.Sieben, trumpf),
            new Card(CardType.Acht, trumpf),
            new Card(CardType.Neun, trumpf),
            new Card(CardType.Koenig, trumpf),
            new Card(CardType.Zehn, trumpf),
            new Card(CardType.Sau, trumpf),
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Unter, CardColor.Gras),
            new Card(CardType.Unter, CardColor.Eichel),
            new Card(CardType.Ober, CardColor.Schell),
            new Card(CardType.Ober, CardColor.Herz),
            new Card(CardType.Ober, CardColor.Gras),
            new Card(CardType.Ober, CardColor.Eichel),
        };

    private GameCall newSauspiel()
    {
        int playerId = rng.Next(0, 4);
        var gsuchteSau = (CardColor)rng.Next(0, 4);
        var deck = new CardsDeck();
        return GameCall.Sauspiel(playerId, deck, gsuchteSau);
    }

    [Fact]
    public void Test_HerzAndOberAndUnterAreTrumpf_WhenPlayingSauspiel()
    {
        var call = newSauspiel();
        var allTrumpf = allTrumpfInAscendingOrder(CardColor.Herz);
        var notTrumpf = CardsDeck.AllCards.Except(allTrumpf);
        allTrumpf.Should().Match(x => x.All(card => call.IsTrumpf(card)));
        notTrumpf.Should().Match(x => x.All(card => !call.IsTrumpf(card)));
    }

    [Fact]
    public void Test_AnyTrumpfWinsAgainstAnyOtherCard_WhenPlayingSauspiel()
    {
        var call = newSauspiel();
        var comp = new CardComparer(call);

        var allTrumpf = allTrumpfInAscendingOrder(CardColor.Herz);
        var allOtherCards = CardsDeck.AllCards.Except(allTrumpf).ToList();
        allTrumpf.Should().Match(trumpf => trumpf.All(t =>
            allOtherCards.All(o => comp.Compare(t, o) > 0)));
    }

    [Fact]
    public void Test_TrumpfAreInCorrectOrder_WhenPlayingSauspiel()
    {
        var call = newSauspiel();
        var comp = new CardComparer(call);

        var orderedTrumpfCards = allTrumpfInAscendingOrder(CardColor.Herz);
        orderedTrumpfCards.Should().BeInAscendingOrder(comp);
        orderedTrumpfCards.Reverse().Should().BeInDescendingOrder(comp);
    }
}

public class SoloTrumpfCardOrderTest
{
    private static readonly Random rng = new Random();

    private IEnumerable<Card> allTrumpfInAscendingOrder(CardColor trumpf)
        => new List<Card>() {
            new Card(CardType.Sieben, trumpf),
            new Card(CardType.Acht, trumpf),
            new Card(CardType.Neun, trumpf),
            new Card(CardType.Koenig, trumpf),
            new Card(CardType.Zehn, trumpf),
            new Card(CardType.Sau, trumpf),
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Unter, CardColor.Gras),
            new Card(CardType.Unter, CardColor.Eichel),
            new Card(CardType.Ober, CardColor.Schell),
            new Card(CardType.Ober, CardColor.Herz),
            new Card(CardType.Ober, CardColor.Gras),
            new Card(CardType.Ober, CardColor.Eichel),
        };

    private GameCall newSolo(CardColor trumpf)
    {
        byte playerId = (byte)rng.Next(0, 4);
        return GameCall.Solo(playerId, trumpf);
    }

    [Theory]
    [InlineData(CardColor.Schell)]
    [InlineData(CardColor.Herz)]
    [InlineData(CardColor.Gras)]
    [InlineData(CardColor.Eichel)]
    public void Test_TrumpfColorAndOberAndUnterAreTrumpf_WhenPlayingSolo(CardColor trumpf)
    {
        var call = newSolo(trumpf);
        var allTrumpf = allTrumpfInAscendingOrder(trumpf);
        var allNonTrumpf = CardsDeck.AllCards.Except(allTrumpf);
        allTrumpf.Should().Match(x => x.All(card => call.IsTrumpf(card)));
        allNonTrumpf.Should().Match(x => x.All(card => !call.IsTrumpf(card)));
    }

    [Theory]
    [InlineData(CardColor.Schell)]
    [InlineData(CardColor.Herz)]
    [InlineData(CardColor.Gras)]
    [InlineData(CardColor.Eichel)]
    public void Test_AnyTrumpfWinsAgainstAnyOtherCard_WhenPlayingSolo(CardColor trumpf)
    {
        var call = newSolo(trumpf);
        var comp = new CardComparer(call);

        var allTrumpf = allTrumpfInAscendingOrder(trumpf);
        var allOtherCards = CardsDeck.AllCards.Except(allTrumpf).ToList();
        allTrumpf.Should().Match(trumpf => trumpf.All(t =>
            allOtherCards.All(o => comp.Compare(t, o) > 0)));
    }

    [Theory]
    [InlineData(CardColor.Schell)]
    [InlineData(CardColor.Herz)]
    [InlineData(CardColor.Gras)]
    [InlineData(CardColor.Eichel)]
    public void Test_TrumpfAreInCorrectOrder_WhenPlayingSolo(CardColor trumpf)
    {
        var call = newSolo(trumpf);
        var comp = new CardComparer(call);

        var orderedTrumpfCards = allTrumpfInAscendingOrder(trumpf);
        orderedTrumpfCards.Should().BeInAscendingOrder(comp);
        orderedTrumpfCards.Reverse().Should().BeInDescendingOrder(comp);
    }
}
