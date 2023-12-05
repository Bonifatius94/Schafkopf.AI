namespace Schafkopf.Training;

public struct SarsExp : IEquatable<SarsExp>
{
    public SarsExp() { }

    public GameState StateBefore = new GameState();
    public GameState StateAfter = new GameState();
    public Card Action = new Card();
    public double Reward = 0.0;
    public bool IsTerminal = false;

    public bool Equals(SarsExp other)
        => StateBefore.Equals(other.StateBefore)
            && StateAfter.Equals(other.StateAfter)
            && Action == other.Action
            && Reward == other.Reward
            && IsTerminal == other.IsTerminal;

    public override int GetHashCode() => 0;
}

public class SupervisedSchafkopfDataset
{
    public static FlatFeatureDataset GenerateDataset(
        int trainSize, int testSize)
    {
        (var trainX, var trainY) = generateDataset(trainSize);
        (var testX, var testY) = generateDataset(testSize);
        return new FlatFeatureDataset(trainX, trainY, testX, testY);
    }

    private static (Matrix2D, Matrix2D) generateDataset(int size)
    {
        var x = Matrix2D.Zeros(size, GameState.NUM_FEATURES + 2);
        var y = Matrix2D.Zeros(size, 1);

        int i = 0; int p = 0;
        foreach (var exp in generateExperiences(size))
        {
            Console.Write($"\rdataset {i+1} / {size} complete               ");
            unsafe
            {
                var card = exp.Action;
                x.Data[p++] = GameEncoding.Encode(card.Type);
                x.Data[p++] = GameEncoding.Encode(card.Color);
                var stateDest = new Span<double>(x.Data + p, GameState.NUM_FEATURES);
                exp.StateBefore.ExportFeatures(stateDest);
                p += stateDest.Length;
                y.Data[i++] = exp.Reward;
            }
        }
        Console.WriteLine();

        return (x, y);
    }

    private static IEnumerable<SarsExp> generateExperiences(
        int? numExamples = null)
    {
        var gameCaller = new HeuristicGameCaller(
            new GameMode[] { GameMode.Sauspiel });

        // TODO: supervised transfer learning requires pre-trained agent / heuristic
        var agent = new RandomAgent(gameCaller);
        var table = new Table(
            new Player(0, agent), new Player(1, agent),
            new Player(2, agent), new Player(3, agent));
        var deck = new CardsDeck();
        var session = new GameSession(table, deck);

        var serializer = new GameStateSerializer();
        var expBuffer = new SarsExp[32];
        for (int i = 0; i < 32; i++)
            expBuffer[i] = new SarsExp() {
                StateBefore = new GameState(),
                StateAfter = new GameState()
            };

        int p = 0;
        while (true)
        {
            var log = session.ProcessGame();
            if (log.Call.Mode == GameMode.Weiter)
                continue;

            serializer.SerializeSarsExps(log, expBuffer);

            for (int i = 0; i < 32; i++)
                if (numExamples == null || p++ < numExamples)
                    yield return expBuffer[i];

            if (numExamples != null && p >= numExamples)
                break;
        }
    }
}
