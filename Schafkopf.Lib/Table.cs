namespace Schafkopf.Lib;

public class Table
{
    public Table(Player p1, Player p2, Player p3, Player p4)
    {
        playersIdOffsetCache = new Player[] { p1, p2, p3, p4, p1, p2, p3 };
        Players = new Player[] { p1, p2, p3, p4 };
        FirstDrawingPlayerId = 0;
    }

    private Player[] playersIdOffsetCache;
    public Player[] Players;
    public int FirstDrawingPlayerId { get; private set; }

    public ReadOnlySpan<Player> PlayersInDrawingOrder()
        => PlayersInDrawingOrder(FirstDrawingPlayerId);

    public ReadOnlySpan<Player> PlayersInDrawingOrder(int kommtRaus)
        => playersIdOffsetCache.AsSpan(kommtRaus, 4);

    private Player[] playersByIdCache = new Player[4];
    public ReadOnlySpan<Player> PlayersById(ReadOnlySpan<int> ids)
    {
        for (int i = 0; i < ids.Length; i++)
            playersByIdCache[i] = Players[i];
        return playersByIdCache.AsSpan(0, ids.Length);
    }

    public void Shift()
        => FirstDrawingPlayerId = (FirstDrawingPlayerId + 1) & 0x03;
}
