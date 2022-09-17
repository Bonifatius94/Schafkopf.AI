var deck = new CardsDeck();
var table = new GameTable(
    new Player(0, new RandomAgent()),
    new Player(1, new RandomAgent()),
    new Player(2, new RandomAgent()),
    new Player(3, new RandomAgent()));
var session = new GameSession(table, deck);
var history = session.ProcessGame();

foreach ((var turn, int id) in history.Turns.Zip(Enumerable.Range(0, 8)))
    Console.WriteLine($"turn {id}: {turn}");
