namespace Schafkopf.Lib.Test;

public class TrumpfFlagAndCardOrderTest
{
    #region Init

    private static readonly Random rng = new Random();

    private static IEnumerable<Card> allTrumpfOfDeckAsc(GameCall call)
        => (call.Mode == GameMode.Wenz)
            ? new List<Card>() {
                new Card(CardType.Unter, CardColor.Schell),
                new Card(CardType.Unter, CardColor.Herz),
                new Card(CardType.Unter, CardColor.Gras),
                new Card(CardType.Unter, CardColor.Eichel),
            }
            : new List<Card>() {
                new Card(CardType.Sieben, call.Trumpf),
                new Card(CardType.Acht, call.Trumpf),
                new Card(CardType.Neun, call.Trumpf),
                new Card(CardType.Koenig, call.Trumpf),
                new Card(CardType.Zehn, call.Trumpf),
                new Card(CardType.Sau, call.Trumpf),
                new Card(CardType.Unter, CardColor.Schell),
                new Card(CardType.Unter, CardColor.Herz),
                new Card(CardType.Unter, CardColor.Gras),
                new Card(CardType.Unter, CardColor.Eichel),
                new Card(CardType.Ober, CardColor.Schell),
                new Card(CardType.Ober, CardColor.Herz),
                new Card(CardType.Ober, CardColor.Gras),
                new Card(CardType.Ober, CardColor.Eichel),
            };

    private static IEnumerable<Card> allFarbeOfDeckAsc(GameCall call, CardColor farbe)
        => (call.Mode == GameMode.Wenz)
            ? new List<Card>() {
                new Card(CardType.Sieben, farbe),
                new Card(CardType.Acht, farbe),
                new Card(CardType.Neun, farbe),
                new Card(CardType.Ober, farbe),
                new Card(CardType.Koenig, farbe),
                new Card(CardType.Zehn, farbe),
                new Card(CardType.Sau, farbe),
            }
            : new List<Card>() {
                new Card(CardType.Sieben, farbe),
                new Card(CardType.Acht, farbe),
                new Card(CardType.Neun, farbe),
                new Card(CardType.Koenig, farbe),
                new Card(CardType.Zehn, farbe),
                new Card(CardType.Sau, farbe),
            };

    private static CardsDeck deck = new CardsDeck();
    private static IEnumerable<Card> allCardsWithMeta(GameCall call)
        => deck.SelectMany(h => h.CacheTrumpf(call.IsTrumpf)).ToArray();

    private static GameCall newSauspiel()
    {
        int playerId = rng.Next(0, 4);
        var gsuchteSau = (CardColor)rng.Next(0, 4);
        return GameCall.Sauspiel(playerId, (playerId + 1) % 4, gsuchteSau);
    }

    private static GameCall newWenz()
    {
        int playerId = rng.Next(0, 4);
        return GameCall.Wenz(playerId);
    }

    private static GameCall newSolo(CardColor trumpf)
    {
        byte playerId = (byte)rng.Next(0, 4);
        return GameCall.Solo(playerId, trumpf);
    }

    private static IEnumerable<CardColor> allFarben
        => new List<CardColor>() {
            CardColor.Schell, CardColor.Herz,
            CardColor.Gras, CardColor.Eichel
        };

    private static IEnumerable<GameCall> callsToTest
        => new List<GameCall>() { newSauspiel(), newWenz() }
            .Union(allFarben.Select(t => newSolo(t)));

    private static IEnumerable<CardColor> farbenForCall(GameCall call)
        => call.Mode == GameMode.Wenz ? allFarben
            : allFarben.Except(new List<CardColor>() { call.Trumpf });

    public static IEnumerable<object[]> callsXExpTrumpfXNotExpTrumpf
        => callsToTest.Select(call => new object[] {
            call,
            allTrumpfOfDeckAsc(call)
                .Select(c => allCardsWithMeta(call).First(m => m == c)).ToArray(),
            CardsDeck.AllCards.Except(allTrumpfOfDeckAsc(call))
                .Select(c => allCardsWithMeta(call).First(m => m == c)).ToArray()
        });

    public static IEnumerable<object[]> callsXAllFarbeAsc
        => callsToTest.SelectMany(call => 
            farbenForCall(call).Select(farbe => new object[] {
                call,
                allFarbeOfDeckAsc(call, farbe)
                    .Select(c => allCardsWithMeta(call).First(m => m == c)).ToArray(),
            })
        );

    #endregion Init

    [Theory]
    [MemberData(nameof(callsXExpTrumpfXNotExpTrumpf))]
    public void Test_IsTrumpfOnlyTrueForCardsThatAreTrumpf_GivenTheGameMode(
        GameCall call, IEnumerable<Card> allTrumpf, IEnumerable<Card> allNonTrumpf)
    {
        allTrumpf.Should().Match(x => x.All(card => card.IsTrumpf));
        allNonTrumpf.Should().Match(x => x.All(card => !card.IsTrumpf));
    }

    [Theory]
    [MemberData(nameof(callsXExpTrumpfXNotExpTrumpf))]
    public void Test_AnyTrumpfWinsAgainstAnyOtherCard_GivenTheGameMode(
        GameCall call, IEnumerable<Card> allTrumpf, IEnumerable<Card> allNonTrumpf)
    {
        var comp = new CardComparer(call.Mode, call.Trumpf);
        allTrumpf.Should().Match(trumpf => trumpf.All(t =>
            allNonTrumpf.All(o => comp.Compare(t, o) > 0)));
    }

    [Theory]
    [MemberData(nameof(callsXExpTrumpfXNotExpTrumpf))]
    public void Test_TrumpfAreInCorrectOrder_GivenTheGameMode(
        GameCall call, IEnumerable<Card> allTrumpfAsc, IEnumerable<Card> allNonTrumpf)
    {
        var comp = new CardComparer(call.Mode, call.Trumpf);

        var orderedTrumpf = allTrumpfAsc
            .OrderBy(x => x, comp)
            .ToList();

        orderedTrumpf.Should().BeEquivalentTo(
            allTrumpfAsc, options => options.WithStrictOrdering());
    }

    [Theory]
    [MemberData(nameof(callsXAllFarbeAsc))]
    public void Test_FarbenAreInCorrectOrder_GivenTheGameMode(
        GameCall call, IEnumerable<Card> callsXAllFarbeAsc)
    {
        var comp = new CardComparer(call.Mode, call.Trumpf);

        var orderedTrumpf = callsXAllFarbeAsc
            .OrderBy(x => x, comp)
            .ToList();

        orderedTrumpf.Should().BeEquivalentTo(
            callsXAllFarbeAsc, options => options.WithStrictOrdering());
    }
}
