using FluentAssertions;

namespace Schafkopf.Lib.Test;

public class WenzCardOrderTest
{
    private static readonly Random rng = new Random();

    [Fact]
    public void Test_OberAndHerzAreNotTrumpf_WhenPlayingWenz()
    {
        byte playerId = (byte)rng.Next(0, 4);
        var deck = new CardsDeck();
        var call = new GameCall(GameMode.Wenz, playerId, deck);

        var allUsualTrumpfWithoutUnter = CardsDeck.AllCards
            .Where(x => (x.Color == CardColor.Herz && x.Type != CardType.Unter)
                        || x.Type == CardType.Ober);
        allUsualTrumpfWithoutUnter.Should().Match(x => x.All(card => !call.IsTrumpf(card)));
    }

    [Fact]
    public void Test_UnterAreTrumpf_WhenPlayingWenz()
    {
        byte playerId = (byte)rng.Next(0, 4);
        var deck = new CardsDeck();
        var call = new GameCall(GameMode.Wenz, playerId, deck);

        var allUnter = CardsDeck.AllCards
            .Where(x => x.Type == CardType.Unter);
        allUnter.Should().Match(x => x.All(card => call.IsTrumpf(card)));
    }

    [Fact]
    public void Test_TrumpfAreInCorrectOrder_WhenPlayingWenz()
    {
        byte playerId = (byte)rng.Next(0, 4);
        var deck = new CardsDeck();
        var call = new GameCall(GameMode.Wenz, playerId, deck);
        var comp = new CardComparer(call);

        // any Unter 'sticht' non-Trumpf
        var allUnter = CardsDeck.AllCards.Where(x => x.Type == CardType.Unter).ToList();
        var allOtherCards = CardsDeck.AllCards.Except(allUnter).ToList();
        allUnter.Should().Match(unter => unter.All(u =>
            allOtherCards.All(o => comp.Compare(u, o) > 0)));

        // Unter are in correct order
        var schellUnter = new Card(CardType.Unter, CardColor.Schell);
        var herzUnter = new Card(CardType.Unter, CardColor.Herz);
        var grasUnter = new Card(CardType.Unter, CardColor.Gras);
        var eichelUnter = new Card(CardType.Unter, CardColor.Eichel);
        schellUnter.Should().Match<Card>(u => comp.Compare(u, herzUnter) < 0);
        schellUnter.Should().Match<Card>(u => comp.Compare(u, grasUnter) < 0);
        schellUnter.Should().Match<Card>(u => comp.Compare(u, eichelUnter) < 0);
        herzUnter.Should().Match<Card>(u => comp.Compare(u, schellUnter) > 0);
        herzUnter.Should().Match<Card>(u => comp.Compare(u, grasUnter) < 0);
        herzUnter.Should().Match<Card>(u => comp.Compare(u, eichelUnter) < 0);
        grasUnter.Should().Match<Card>(u => comp.Compare(u, schellUnter) > 0);
        grasUnter.Should().Match<Card>(u => comp.Compare(u, herzUnter) > 0);
        grasUnter.Should().Match<Card>(u => comp.Compare(u, eichelUnter) < 0);
        eichelUnter.Should().Match<Card>(u => comp.Compare(u, schellUnter) > 0);
        eichelUnter.Should().Match<Card>(u => comp.Compare(u, herzUnter) > 0);
        eichelUnter.Should().Match<Card>(u => comp.Compare(u, grasUnter) > 0);
    }
}
