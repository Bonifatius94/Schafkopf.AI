
// TODO: train a policy to predict the likelihood
//       of selecting an action in a given state

public class UniformDistribution
{
    private static readonly Random rng = new Random();

    public static int Sample(ReadOnlySpan<double> probs)
    {
        double p = rng.NextDouble();
        double sum = 0;
        for (int i = 0; i < probs.Length - 1; i++)
        {
            sum += probs[i];
            if (p < sum)
                return i;
        }
        return probs.Length - 1;
    }
}

public class PPOAgent : ISchafkopfAIAgent
{
    private FFModel valueModel = new FFModel(new ILayer[] {
        new DenseLayer(64),
        new ReLULayer(),
        new DenseLayer(64),
        new ReLULayer(),
        new DenseLayer(1),
        new FlattenLayer()
    });

    private FFModel strategyModel =
        new FFModel(
            new ILayer[] {
                new DenseLayer(64),
                new ReLULayer(),
                new DenseLayer(64),
                new ReLULayer(),
                new DenseLayer(1),
                new FlattenLayer(),
                new SoftmaxLayer()
            });

    private GameStateSerializer stateSerializer = new GameStateSerializer();
    private Matrix2D featureCache = Matrix2D.Zeros(8, 92);
    public Card ChooseCard(GameLog log, ReadOnlySpan<Card> possibleCards)
    {
        var x = featureCache;
        var s0 = stateSerializer.SerializeState(log);

        int p = 0;
        for (int i = 0; i < possibleCards.Length; i++)
        {
            unsafe
            {
                var card = possibleCards[i];
                x.Data[p++] = GameEncoding.Encode(card.Type);
                x.Data[p++] = GameEncoding.Encode(card.Color);
                s0.ExportFeatures(x.Data + p);
                p += GameState.NUM_FEATURES;
            }
        }

        var probDist = strategyModel.PredictBatch(featureCache);
        ReadOnlySpan<double> probDistSlice;
        unsafe { probDistSlice = new Span<double>(probDist.Data, possibleCards.Length); }
        int id = UniformDistribution.Sample(probDistSlice);
        return possibleCards[id];
    }

    public void OnGameFinished(GameLog final)
    {
        throw new NotImplementedException();
    }

    #region Misc

    public bool CallKontra(GameLog log) => false;

    public bool CallRe(GameLog log) => false;

    public bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards) => false;

    private HeuristicGameCaller caller =
        new HeuristicGameCaller(new GameMode[] { GameMode.Sauspiel });
    public GameCall MakeCall(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => caller.MakeCall(possibleCalls, position, hand, klopfer);

    #endregion Misc
}

public class PPOTrainingSettings
{
    public int NumObsFeatures { get; set; }
    public int TotalSteps = 10_000_000;
    public double LearnRate = 3e-4;
    public double RewardDiscount = 0.99;
    public double GAEDiscount = 0.95;
    public double ProbClip = 0.2;
    public double ValueClip = 0.2;
    public double VFCoef = 0.5;
    public double EntCoef = 0.01;
    public bool NormAdvantages = true;
    public bool ClipValues = true;
    public int BatchSize = 64;
    public int NumEnvs = 32;
    public int StepsPerUpdate = 512;
    public int UpdateEpochs = 4;
    public int NumModelSnapshots = 20;

    public int TrainSteps => TotalSteps / NumEnvs;
    public int ModelSnapshotInterval => TrainSteps / NumModelSnapshots;
}

public class PPOTrainingSession
{
    public void Train()
    {

    }
}

public class PPORolloutBuffer
{
    // 
}
