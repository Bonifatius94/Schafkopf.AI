using FluentAssertions;

namespace Schafkopf.Lib.Test;

public class CardPropertiesTest
{
    private List<CardColor> colors =
        new List<CardColor>() {
            CardColor.Schell,
            CardColor.Herz,
            CardColor.Gras,
            CardColor.Eichel
        };

    private List<CardType> types =
        new List<CardType>() {
            CardType.Sieben,
            CardType.Acht,
            CardType.Neun,
            CardType.Unter,
            CardType.Ober,
            CardType.Koenig,
            CardType.Zehn,
            CardType.Sau,
        };

    [Fact]
    public void Test_CanRetrieveColorAndTypeOfAnyGivenCard()
    {
        foreach (var type in types)
            foreach (var color in colors)
                new Card(type, color).Should()
                    .Match<Card>(c => c.Color == color && c.Type == type);
    }
}
