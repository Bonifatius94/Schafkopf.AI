using FluentAssertions;

namespace Schafkopf.Lib.Test;

public class GameRulesTest
{
    [Fact]
    public void Test_TrumpfZugeben_WhenTrumpfPlayedAndTrumpfInHand()
    {
        // var deck = new CardsDeck();

        // var drawEval = new WenzOrSoloDrawValidator();
        // var cardToPlay = new Card(CardType.Acht, CardColor.Herz);
        // drawEval.IsValid(call, cardToPlay, turn, hand)
        //     .Should().BeTrue();
    }

    [Fact]
    public void Test_NotTrumpfZugeben_WhenTrumpfPlayedButNoTrumpfInHand()
    {
        
    }

    [Fact]
    public void Test_MussFarbeZugeben_WhenFarbePlayedAndFarbeInHand()
    {
        
    }

    [Fact]
    public void Test_NotMussFarbeZugeben_WhenFarbePlayedButNoFarbeInHand()
    {
        
    }

    [Fact]
    public void Test_MustPlayGsuchteSau_WhenGsuchtIsAndNotKannUntenDurch()
    {
        
    }

    [Fact]
    public void Test_CanOrCannotPlayGsuchteSau_WhenGsuchtIsAndKannUntenDurch()
    {
        
    }

    [Fact]
    public void Test_MustToPlayGsuchteSau_WhenGsuchtByPartnerHimself()
    {
        
    }
}
