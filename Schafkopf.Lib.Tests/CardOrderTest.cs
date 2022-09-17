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
    public void Test_OnlyUnterAreTrumpf_WhenPlayingWenz()
    {
        var call = newWenz();
        var deck = new CardsDeck();
        var allCardsWithMeta = deck.SelectMany(h => h.CacheTrumpf(call.IsTrumpf)).ToArray();
        var allTrumpf = allCardsWithMeta.Where(x => x.Type == CardType.Unter);
        var allNonTrumpf = allCardsWithMeta.Except(allTrumpf);

        allTrumpf.Should().Match(x => x.All(card => card.IsTrumpf));
        allNonTrumpf.Should().Match(x => x.All(card => !card.IsTrumpf));
    }

    [Fact]
    public void Test_AnyTrumpfWinsAgainstAnyOtherCard_WhenPlayingWenz()
    {
        var call = newWenz();
        var comp = new CardComparer(call.Mode);
        var deck = new CardsDeck();
        var allCardsWithMeta = deck.SelectMany(h => h.CacheTrumpf(call.IsTrumpf)).ToArray();
        var allTrumpf = allCardsWithMeta.Where(x => x.Type == CardType.Unter).ToList();
        var allOtherCards = allCardsWithMeta.Except(allTrumpf).ToList();

        allTrumpf.Should().Match(trumpf => trumpf.All(t =>
            allOtherCards.All(o => comp.Compare(t, o) > 0)));
    }

    [Fact]
    public void Test_TrumpfAreInCorrectOrder_WhenPlayingWenz()
    {
        var call = newWenz();
        var comp = new CardComparer(call.Mode);
        var deck = new CardsDeck();
        var allCardsWithMeta = deck.SelectMany(h => h.CacheTrumpf(call.IsTrumpf)).ToArray();

        IEnumerable<Card> orderedTrumpf = allCardsWithMeta
            .Where(x => x.Type == CardType.Unter)
            .OrderBy(x => x, comp)
            .ToList();

        IEnumerable<Card> expOrder = new List<Card>() {
            new Card(CardType.Unter, CardColor.Schell),
            new Card(CardType.Unter, CardColor.Herz),
            new Card(CardType.Unter, CardColor.Gras),
            new Card(CardType.Unter, CardColor.Eichel),
        };

        orderedTrumpf.Should().BeEquivalentTo(
            expOrder, options => options.WithStrictOrdering());
    }
}

public class SauspielTrumpfCardOrderTest
{
    private static readonly Random rng = new Random();

    #region Init

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

    private IEnumerable<Card> allTrumpfOfDeck(
            IEnumerable<Card> origCards,
            CardColor trumpf)
    {
        var trumpfCards = new List<Card>() {
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

        return origCards.Where(c => trumpfCards.Contains(c)).ToList();
    }

    private GameCall newSauspiel()
    {
        int playerId = rng.Next(0, 4);
        var gsuchteSau = (CardColor)rng.Next(0, 4);
        var deck = new CardsDeck();
        return GameCall.Sauspiel(playerId, playerId + 1, gsuchteSau);
    }

    #endregion Init

    [Fact]
    public void Test_HerzAndOberAndUnterAreTrumpf_WhenPlayingSauspiel()
    {
        var call = newSauspiel();
        var deck = new CardsDeck();
        var allCardsWithMeta = deck.SelectMany(h => h.CacheTrumpf(call.IsTrumpf)).ToArray();
        var allTrumpf = allTrumpfOfDeck(allCardsWithMeta, CardColor.Herz);
        var notTrumpf = allCardsWithMeta.Except(allTrumpf);
        allTrumpf.Should().Match(x => x.All(card => card.IsTrumpf));
        notTrumpf.Should().Match(x => x.All(card => !card.IsTrumpf));
    }

    [Fact]
    public void Test_AnyTrumpfWinsAgainstAnyOtherCard_WhenPlayingSauspiel()
    {
        var call = newSauspiel();
        var comp = new CardComparer(call.Mode);
        var deck = new CardsDeck();
        var allCardsWithMeta = deck.SelectMany(h => h.CacheTrumpf(call.IsTrumpf)).ToArray();
        var allTrumpf = allTrumpfOfDeck(allCardsWithMeta, CardColor.Herz);
        var notTrumpf = allCardsWithMeta.Except(allTrumpf);

        allTrumpf.Should().Match(trumpf => trumpf.All(t =>
            notTrumpf.All(o => comp.Compare(t, o) > 0)));
    }

    [Fact]
    public void Test_TrumpfAreInCorrectOrder_WhenPlayingSauspiel()
    {
        var call = newSauspiel();
        var comp = new CardComparer(call.Mode);
        var deck = new CardsDeck();
        var allCardsWithMeta = deck.SelectMany(h => h.CacheTrumpf(call.IsTrumpf)).ToArray();
        var allTrumpf = allTrumpfOfDeck(allCardsWithMeta, CardColor.Herz);

        var orderedTrumpf = allTrumpf
            .OrderBy(x => x, comp)
            .ToList();

        var expOrder = allTrumpfInAscendingOrder(CardColor.Herz);
        orderedTrumpf.Should().BeEquivalentTo(
            expOrder, options => options.WithStrictOrdering());
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

    private IEnumerable<Card> allTrumpfOfDeck(
            IEnumerable<Card> origCards,
            CardColor trumpf)
    {
        var trumpfCards = new List<Card>() {
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

        return origCards.Where(c => trumpfCards.Contains(c)).ToList();
    }

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
        var deck = new CardsDeck();
        var allCardsWithMeta = deck.SelectMany(h => h.CacheTrumpf(call.IsTrumpf)).ToArray();
        var allTrumpf = allTrumpfOfDeck(allCardsWithMeta, trumpf);
        var notTrumpf = allCardsWithMeta.Except(allTrumpf);
        allTrumpf.Should().Match(x => x.All(card => card.IsTrumpf));
        notTrumpf.Should().Match(x => x.All(card => !card.IsTrumpf));
    }

    [Theory]
    [InlineData(CardColor.Schell)]
    [InlineData(CardColor.Herz)]
    [InlineData(CardColor.Gras)]
    [InlineData(CardColor.Eichel)]
    public void Test_AnyTrumpfWinsAgainstAnyOtherCard_WhenPlayingSolo(CardColor trumpf)
    {
        var call = newSolo(trumpf);
        var comp = new CardComparer(call.Mode);
        var deck = new CardsDeck();
        var allCardsWithMeta = deck.SelectMany(h => h.CacheTrumpf(call.IsTrumpf)).ToArray();
        var allTrumpf = allTrumpfOfDeck(allCardsWithMeta, trumpf);
        var notTrumpf = allCardsWithMeta.Except(allTrumpf);

        allTrumpf.Should().Match(trumpf => trumpf.All(t =>
            notTrumpf.All(o => comp.Compare(t, o) > 0)));
    }

    [Theory]
    [InlineData(CardColor.Schell)]
    [InlineData(CardColor.Herz)]
    [InlineData(CardColor.Gras)]
    [InlineData(CardColor.Eichel)]
    public void Test_TrumpfAreInCorrectOrder_WhenPlayingSolo(CardColor trumpf)
    {
        var call = newSolo(trumpf);
        var comp = new CardComparer(call.Mode);
        var deck = new CardsDeck();
        var allCardsWithMeta = deck.SelectMany(h => h.CacheTrumpf(call.IsTrumpf)).ToArray();
        var allTrumpf = allTrumpfOfDeck(allCardsWithMeta, trumpf);

        var orderedTrumpf = allTrumpf
            .OrderBy(x => x, comp)
            .ToList();

        var expOrder = allTrumpfInAscendingOrder(trumpf);
        orderedTrumpf.Should().BeEquivalentTo(
            expOrder, options => options.WithStrictOrdering());
    }
}

// TODO: add test for Farbe order in all game modes
