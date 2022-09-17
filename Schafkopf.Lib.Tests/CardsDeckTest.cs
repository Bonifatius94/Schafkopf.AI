namespace Schafkopf.Lib.Test;

public class DeckAttributesTest
{
    public static IEnumerable<object[]> CardIndices
        => Enumerable.Range(0, 32).Select(i => new object[] { i });
    [Theory]
    [MemberData(nameof(CardIndices))]
    public void TestName(int i)
    {
        var deck = new CardsDeck();
        var card = deck[i];
    }
}
