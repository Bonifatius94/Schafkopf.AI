
// TODO: train a policy to predict the likelihood
//       of selecting an action in a given state

using System.Runtime.CompilerServices;

public class UniformDistribution
{
    public UniformDistribution(int? seed = null)
        => rng = seed != null ? new Random(seed.Value) : new Random();

    private Random rng;

    public int Sample(ReadOnlySpan<double> probs)
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
    public PPOAgent(PPOTrainingSettings config, Action<ACSarsExp> expConsumer)
    {
        this.config = config;
        this.expConsumer = expConsumer;

        valueFunc = new FFModel(new ILayer[] {
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(32)
        });

        strategy = new FFModel(new ILayer[] {
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(32),
            new SoftmaxLayer()
        });

        valueFunc.Compile(config.BatchSize, 90);
        strategy.Compile(config.BatchSize, 90);

        trainExpCache = new SarsExp[config.BatchSize];
        for (int i = 0; i < config.BatchSize; i++)
            trainExpCache[i] = new SarsExp() {
                StateBefore = new GameState(),
                StateAfter = new GameState() };
    }

    private PPOTrainingSettings config;
    private Action<ACSarsExp> expConsumer;
    private FFModel valueFunc;
    private FFModel strategy;

    private GameStateSerializer stateSerializer = new GameStateSerializer();
    private Matrix2D featureCache = Matrix2D.Zeros(1, 90);
    private SarsExp[] trainExpCache;
    private UniformDistribution uniform = new UniformDistribution();

    public Card ChooseCard(GameLog log, ReadOnlySpan<Card> possibleCards)
    {
        var x = featureCache;
        var s0 = stateSerializer.SerializeState(log);
        unsafe { s0.ExportFeatures(x.Data);}

        var predPi = strategy.PredictBatch(featureCache);
        var predQ = valueFunc.PredictBatch(featureCache);
        var probDist = normProbDist(predPi, possibleCards);
        int cardId = uniform.Sample(probDist);

        expConsumer(new ACSarsExp() {

        });

        return possibleCards[cardId];
    }

    private double[] probDistCache = new double[8];
    private ReadOnlySpan<double> normProbDist(
        Matrix2D pred, ReadOnlySpan<Card> possibleCards)
    {
        Span<double> probDistAll;
        unsafe { probDistAll = new Span<double>(pred.Data, 32); }

        double probSum = 0;
        for (int i = 0; i < possibleCards.Length; i++)
            probDistCache[i] = probDistAll[possibleCards[i].Id & Card.ORIG_CARD_MASK];
        for (int i = 0; i < possibleCards.Length; i++)
            probSum += probDistCache[i];
        double scale = 1 / probSum;
        for (int i = 0; i < possibleCards.Length; i++)
            probDistCache[i] *= scale;

        return probDistCache.AsSpan().Slice(0, possibleCards.Length);
    }

    public void Train(ReplayMemory memory)
    {
        int numBatches = memory.Size / trainExpCache.Length;

        for (int i = 0; i < numBatches; i++)
        {
            memory.SampleBatched(trainExpCache);
            updateModels(trainExpCache);
        }
    }

    private void updateModels(ReadOnlySpan<SarsExp> expsBatch)
    {

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
        // TODO: implement training loop here ...
    }
}

public struct PPOTrainBatch
{
    public PPOTrainBatch(int size)
    {
        this.size = size;
        StatesBefore = Matrix2D.Zeros(size, 90);
        Actions = Matrix2D.Zeros(size, 1);
        Rewards = Matrix2D.Zeros(size, 1);
        Terminals = Matrix2D.Zeros(size, 1);
        Returns = Matrix2D.Zeros(size, 1);
        Advantages = Matrix2D.Zeros(size, 1);
        OldProbs = Matrix2D.Zeros(size, 1);
        OldBaselines = Matrix2D.Zeros(size, 1);
    }

    private int size;
    public Matrix2D StatesBefore;
    public Matrix2D Actions;
    public Matrix2D Rewards;
    public Matrix2D Terminals;
    public Matrix2D Returns;
    public Matrix2D Advantages;
    public Matrix2D OldProbs;
    public Matrix2D OldBaselines;

    public void Shuffle(int[] permCache)
    {
        var perm = permCache ?? Perm.Identity(size);
        Perm.Permutate(perm);

        Matrix2D.ShuffleRows(StatesBefore, perm);
        Matrix2D.ShuffleRows(Actions, perm);
        Matrix2D.ShuffleRows(Rewards, perm);
        Matrix2D.ShuffleRows(Terminals, perm);
        Matrix2D.ShuffleRows(Returns, perm);
        Matrix2D.ShuffleRows(Advantages, perm);
        Matrix2D.ShuffleRows(OldProbs, perm);
        Matrix2D.ShuffleRows(OldBaselines, perm);
    }

    public IEnumerable<PPOTrainBatch> SampleBatched(int batchSize)
    {
        var trainBuf = new PPOTrainBatch(batchSize);

        int p = 0;
        int numBatches = size / batchSize;
        for (int i = 0; i < numBatches; i++)
        {
            Matrix2D.CopyData(trainBuf.StatesBefore, Matrix2D.SliceRows(StatesBefore, p, batchSize));
            Matrix2D.CopyData(trainBuf.Actions, Matrix2D.SliceRows(Actions, p, batchSize));
            Matrix2D.CopyData(trainBuf.Rewards, Matrix2D.SliceRows(Rewards, p, batchSize));
            Matrix2D.CopyData(trainBuf.Terminals, Matrix2D.SliceRows(Terminals, p, batchSize));
            Matrix2D.CopyData(trainBuf.Returns, Matrix2D.SliceRows(Returns, p, batchSize));
            Matrix2D.CopyData(trainBuf.Advantages, Matrix2D.SliceRows(Advantages, p, batchSize));
            Matrix2D.CopyData(trainBuf.OldProbs, Matrix2D.SliceRows(OldProbs, p, batchSize));
            Matrix2D.CopyData(trainBuf.OldBaselines, Matrix2D.SliceRows(OldBaselines, p, batchSize));
            yield return trainBuf;
            p += batchSize;
        }
    }
}

public class PPORolloutBuffer
{
    public PPORolloutBuffer(int numEnvs, int steps)
    {
        this.numEnvs = numEnvs;
        this.steps = steps;
        totalSize = (steps + 1) * numEnvs;
        cache = new PPOTrainBatch(totalSize);
        cache = new PPOTrainBatch(totalSize);
    }

    private Random rng = new Random();

    private int numEnvs;
    private int steps;
    private int totalSize;
    private PPOTrainBatch cache;

    public void AppendStep(ACSarsExp[] expsOfStep, int t)
    {
        int p = t * numEnvs;
        for (int i = 0; i < expsOfStep.Length; i++)
        {
            unsafe
            {
                expsOfStep[i].StateBefore.ExportFeatures(cache.StatesBefore.Data + p * 90);
                cache.Actions.Data[p] = expsOfStep[i].Action.Id & Card.ORIG_CARD_MASK;
                cache.Rewards.Data[p] = expsOfStep[i].Reward;
                cache.Terminals.Data[p] = expsOfStep[i].IsTerminal ? 1 : 0;
                cache.OldProbs.Data[p] = expsOfStep[i].OldProb;
                cache.OldBaselines.Data[p] = expsOfStep[i].OldBaseline;
                p++;
            }
        }
    }

    public void CacheGAE(double gamma, double gaeGamma)
    {
        for (int t = steps - 1; t >= 0; t--)
        {
            // TODO: do the GAE magic
        }
    }
}
