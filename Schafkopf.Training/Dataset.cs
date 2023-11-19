namespace Schafkopf.Training;

public class SupervisedSchafkopfDataset
{
    public FlatFeatureDataset GenerateDataset(
        int trainSize, int testSize)
    {
        (var trainX, var trainY) = generateDataset(trainSize);
        (var testX, var testY) = generateDataset(testSize);
        return new FlatFeatureDataset(trainX, trainY, testX, testY);
    }

    private (Matrix2D, Matrix2D) generateDataset(int size)
    {
        var x = Matrix2D.Zeros(size, GameState.NUM_FEATURES + 2);
        var y = Matrix2D.Zeros(size, 1);

        int i = 0; int p = 0;
        foreach (var exp in generateExperiences(size))
        {
            unsafe
            {
                var card = exp.Action.CardPlayed;
                x.Data[p++] = GameEncoding.Encode(card.Type);
                x.Data[p++] = GameEncoding.Encode(card.Color);
                exp.StateBefore.ExportFeatures(x.Data + p);
                p += GameState.NUM_FEATURES;
                y.Data[i++] = exp.Reward;
            }
        }

        return (x, y);
    }

    private static IEnumerable<SarsExp> generateExperiences(
        int? numExamples = null)
    {
        var agent = new RandomAgent();
        var table = new Table(
            new Player(0, agent), new Player(1, agent),
            new Player(2, agent), new Player(3, agent));
        var deck = new CardsDeck();
        var session = new GameSession(table, deck);

        var serializer = new GameStateSerializer();
        var expBuffer = new SarsExp[32];
        int p = 0;

        while (true)
        {
            var log = session.ProcessGame();
            serializer.SerializeSarsExps(
                log, expBuffer, GameReward.Reward);

            for (int i = 0; i < 32; i++)
                if (numExamples == null || p++ < numExamples)
                    yield return expBuffer[i];
        }
    }
}
