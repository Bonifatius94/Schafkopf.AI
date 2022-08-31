using Schafkopf.Lib;

var deck = new CardsDeck();
var table = new GameTable(
    new RandomAgent(),
    new RandomAgent(),
    new RandomAgent(),
    new RandomAgent());
var session = new GameSession(table, deck);

var call = GameCall.Sauspiel(0, deck, CardColor.Schell);
var callValidator = new GameCallValidator();
while (!callValidator.IsValidCall(call, deck))
{
    deck.Shuffle();
    call = GameCall.Sauspiel(0, deck, CardColor.Schell);
}

var history = session.PlayGameUntilEnd(call);

foreach ((var turn, int id) in history.Turns.Zip(Enumerable.Range(0, 8)))
    Console.WriteLine($"turn {id}: {turn}");
