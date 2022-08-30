namespace Schafkopf.Lib;

public class GameTable
{
    public GameTable(
        ISchafkopfPlayer p1,
        ISchafkopfPlayer p2,
        ISchafkopfPlayer p3,
        ISchafkopfPlayer p4)
    {
        players = new ISchafkopfPlayer[] { p1, p2, p3, p4 };
        FirstDrawingPlayerId = 0;
    }

    private ISchafkopfPlayer[] players;
    public int FirstDrawingPlayerId { get; private set; }

    public IEnumerable<ISchafkopfPlayer> PlayersInDrawingOrder()
        => PlayersInDrawingOrder(FirstDrawingPlayerId);

    public IEnumerable<ISchafkopfPlayer> PlayersInDrawingOrder(
        int beginningPlayerId)
    {
        yield return players[beginningPlayerId];
        yield return players[(++beginningPlayerId % 4)];
        yield return players[(++beginningPlayerId % 4)];
        yield return players[(++beginningPlayerId % 4)];
    }

    public void SupplyHands(CardsDeck deck)
    {
        // TODO: implement Klopfen here ...
        foreach (int id in Enumerable.Range(0, 4))
            players[id].NewGame(deck.HandOfPlayer(id));
    }

    public void Shift()
        => FirstDrawingPlayerId = (FirstDrawingPlayerId + 1) % 4;
}
