namespace Schafkopf.Training;

public class RandomPlayBenchmark
{
    public double Benchmark(ISchafkopfAIAgent agentToEval, int epochs = 10_000)
    {
        var gameCaller = new HeuristicGameCaller(
            new GameMode[] { GameMode.Sauspiel, GameMode.Wenz, GameMode.Solo });
        var players = new Player[] {
            new Player(0, agentToEval),
            new Player(1, new RandomAgent(gameCaller)),
            new Player(2, new RandomAgent(gameCaller)),
            new Player(3, new RandomAgent(gameCaller))
        };
        var table = new Table(
            players[0], players[1],
            players[2], players[3]);
        var deck = new CardsDeck();
        var session = new GameSession(table, deck);

        int wins = 0;
        for (int i = 0; i < epochs; i++)
        {
            var log = session.ProcessGame();

            // info: only evaluate games where cards were played
            if (log.Call.Mode == GameMode.Weiter) { i--; continue; }

            var eval = new GameScoreEvaluation(log);
            bool isCaller = log.CallerIds.Contains(0);
            bool isWin = !eval.DidCallerWin ^ isCaller;
            wins += isWin ? 1 : 0;
        }

        return (double)wins / epochs; // win rate
    }
}
