namespace Schafkopf.Lib;

public class Table
{
    public Table(
        Player p1,
        Player p2,
        Player p3,
        Player p4)
    {
        Players = new Player[] { p1, p2, p3, p4 };
        FirstDrawingPlayerId = 0;
    }

    public Player[] Players;
    public int FirstDrawingPlayerId { get; private set; }

    public IEnumerable<Player> PlayersInDrawingOrder()
        => PlayersInDrawingOrder(FirstDrawingPlayerId);

    public IEnumerable<Player> PlayersInDrawingOrder(int kommtRaus)
    {
        int id = kommtRaus;
        yield return Players[id];
        id = ++id & 0x03;
        yield return Players[id];
        id = ++id & 0x03;
        yield return Players[id];
        id = ++id & 0x03;
        yield return Players[id];
    }

    public IEnumerable<Player> PlayersById(IEnumerable<int> ids)
    {
        foreach (int id in ids)
            yield return Players[id];
    }

    public void Shift()
        => FirstDrawingPlayerId = (FirstDrawingPlayerId + 1) & 0x03;
}
