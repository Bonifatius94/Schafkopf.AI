using Schafkopf.Lib;

var session = new GameSession(
    new RandomAgent(),
    new RandomAgent(),
    new RandomAgent(),
    new RandomAgent());

var deck = new CardsDeck();
var call = new GameCall(GameMode.Sauspiel, 0, CardColor.Schell, deck);
while (!call.CanCallSauspiel(deck))
{
    deck.Shuffle();
    call = new GameCall(GameMode.Sauspiel, 0, CardColor.Schell, deck);
}

var history = session.PlayGameUntilEnd(call);

foreach ((var turn, int id) in history.Turns.Zip(Enumerable.Range(0, 8)))
    Console.WriteLine($"turn {id}: {turn}");
