namespace Schafkopf.Lib;

public class Table
{
    public Table(Player p1, Player p2, Player p3, Player p4)
    {
        playersCache = new Player[] { p1, p2, p3, p4, p1, p2, p3 };
        Players = new Player[] { p1, p2, p3, p4 };
        FirstDrawingPlayerId = 0;
    }

    private Player[] playersCache;
    public Player[] Players;
    public int FirstDrawingPlayerId { get; private set; }

    public ReadOnlySpan<Player> PlayersInDrawingOrder()
        => PlayersInDrawingOrder(FirstDrawingPlayerId);

    public ReadOnlySpan<Player> PlayersInDrawingOrder(int kommtRaus)
        => playersCache.AsSpan(kommtRaus, 4);

    public IEnumerable<Player> PlayersById(IEnumerable<int> ids)
    {
        foreach (int id in ids)
            yield return Players[id];
    }

    public void Shift()
        => FirstDrawingPlayerId = (FirstDrawingPlayerId + 1) & 0x03;
}
