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

    public IEnumerable<ISchafkopfPlayer> PlayersInDrawingOrder(int kommtRaus)
    {
        int id = kommtRaus;
        yield return players[id];
        id = ++id & 0x03;
        yield return players[id];
        id = ++id & 0x03;
        yield return players[id];
        id = ++id & 0x03;
        yield return players[id];
    }

    public IEnumerable<ISchafkopfPlayer> PlayersById(IEnumerable<int> ids)
        => ids.Select(id => players[id]);

    public void Shift()
        => FirstDrawingPlayerId = (FirstDrawingPlayerId + 1) & 0x03;
}
