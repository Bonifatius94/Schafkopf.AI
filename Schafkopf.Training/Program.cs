using Schafkopf.Lib;

var deck = new CardsDeck();
var table = new GameTable(
    new RandomAgent(),
    new RandomAgent(),
    new RandomAgent(),
    new RandomAgent());
var session = new GameSession(table, deck);

var call = new GameCall(GameMode.Sauspiel, 0, deck, CardColor.Schell);
while (!call.CanCallSauspiel(deck))
{
    deck.Shuffle();
    call = new GameCall(GameMode.Sauspiel, 0, deck, CardColor.Schell);
}

var history = session.PlayGameUntilEnd(call);

foreach ((var turn, int id) in history.Turns.Zip(Enumerable.Range(0, 8)))
    Console.WriteLine($"turn {id}: {turn}");
