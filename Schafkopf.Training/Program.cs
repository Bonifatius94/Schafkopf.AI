
// var qlAgent = new QLAgent();
// var players = new Player[] {
//     new Player(0, qlAgent),
//     new Player(1, qlAgent),
//     new Player(2, qlAgent),
//     new Player(3, qlAgent)
// };

// var table = new Table(players[0], players[1], players[2], players[3]);
// var deck = new CardsDeck();
// var session = new GameSession(table, deck);

Console.WriteLine("Launching QL Training");

// foreach (int i in Enumerable.Range(0, 1000000))
// {
//     if ((i+1) % 10000 == 0)
//         Console.WriteLine($"episode {(i+1)}");

//     var log = session.ProcessGame();
//     players[0].OnGameFinished(log);
//     // info: only train for the first player as all players use the same model;
//     //       all game states are normalized to the first player's view
//     //       -> AI doesn't train 4 model, instead fuses all 4 player's experiences
// }
