
// TODO: train a policy to predict the likelihood
//       of selecting an action in a given state

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
        Size = size;
        StatesBefore = Matrix2D.Zeros(size, 90);
        Actions = Matrix2D.Zeros(size, 1);
        Rewards = Matrix2D.Zeros(size, 1);
        Terminals = Matrix2D.Zeros(size, 1);
        Returns = Matrix2D.Zeros(size, 1);
        Advantages = Matrix2D.Zeros(size, 1);
        OldProbs = Matrix2D.Zeros(size, 1);
        OldBaselines = Matrix2D.Zeros(size, 1);
    }

    public int Size;
    public Matrix2D StatesBefore;
    public Matrix2D Actions;
    public Matrix2D Rewards;
    public Matrix2D Terminals;
    public Matrix2D Returns;
    public Matrix2D Advantages;
    public Matrix2D OldProbs;
    public Matrix2D OldBaselines;

    public void WriteRow(ACSarsExp exp, int rowid)
    {
        unsafe
        {
            exp.StateBefore.ExportFeatures(StatesBefore.Data + rowid * 90);
            Actions.Data[rowid] = exp.Action.Id & Card.ORIG_CARD_MASK;
            Rewards.Data[rowid] = exp.Reward;
            Terminals.Data[rowid] = exp.IsTerminal ? 1 : 0;
            OldProbs.Data[rowid] = exp.OldProb;
            OldBaselines.Data[rowid] = exp.OldBaseline;
        }
    }

    public void Shuffle(int[] permCache)
    {
        var perm = permCache ?? Perm.Identity(Size);
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
        int p = 0;
        int numBatches = Size / batchSize;
        var trainBuf = new PPOTrainBatch() { Size = batchSize };
        for (int i = 0; i < numBatches; i++)
        {
            trainBuf.StatesBefore = Matrix2D.SliceRows(StatesBefore, p, batchSize);
            trainBuf.Actions = Matrix2D.SliceRows(Actions, p, batchSize);
            trainBuf.Rewards = Matrix2D.SliceRows(Rewards, p, batchSize);
            trainBuf.Terminals = Matrix2D.SliceRows(Terminals, p, batchSize);
            trainBuf.Returns = Matrix2D.SliceRows(Returns, p, batchSize);
            trainBuf.Advantages = Matrix2D.SliceRows(Advantages, p, batchSize);
            trainBuf.OldProbs = Matrix2D.SliceRows(OldProbs, p, batchSize);
            trainBuf.OldBaselines = Matrix2D.SliceRows(OldBaselines, p, batchSize);
            yield return trainBuf;
            p += batchSize;
        }
    }

    public PPOTrainBatch SliceRows(int rowid, int length)
        => new PPOTrainBatch {
            Size = length,
            StatesBefore = Matrix2D.SliceRows(StatesBefore, rowid, length),
            Actions = Matrix2D.SliceRows(Actions, rowid, length),
            Rewards = Matrix2D.SliceRows(Rewards, rowid, length),
            Terminals = Matrix2D.SliceRows(Terminals, rowid, length),
            Returns = Matrix2D.SliceRows(Returns, rowid, length),
            Advantages = Matrix2D.SliceRows(Advantages, rowid, length),
            OldProbs = Matrix2D.SliceRows(OldProbs, rowid, length),
            OldBaselines = Matrix2D.SliceRows(OldBaselines, rowid, length)
        };
}

public class PPORolloutBuffer
{
    public PPORolloutBuffer(int numEnvs, int steps, double gamma, double gaeGamma)
    {
        this.numEnvs = numEnvs;
        this.steps = steps;
        this.gamma = gamma;
        this.gaeGamma = gaeGamma;

        // info: the cache stores an extra timestep at the end
        //       which facilitates proper GAE computation

        int size = steps * numEnvs;
        int sizeWithExtraStep = (steps + 1) * numEnvs;
        cache = new PPOTrainBatch(sizeWithExtraStep);
        cacheWithoutLastStep = cache.SliceRows(0, size);
        cacheOnlyFirstStep = cache.SliceRows(0, numEnvs);
        cacheOnlyLastStep = cache.SliceRows(size, numEnvs);
        permCache = Perm.Identity(size);
    }

    private int numEnvs;
    private int steps;
    private double gamma;
    private double gaeGamma;
    private PPOTrainBatch cache;
    private PPOTrainBatch cacheWithoutLastStep;
    private PPOTrainBatch cacheOnlyFirstStep;
    private PPOTrainBatch cacheOnlyLastStep;
    private int[] permCache;

    public bool IsReadyForModelUpdate(int t) => t > 0 && t % steps == 0;

    public void AppendStep(ACSarsExp[] expsOfStep, int t)
    {
        int offset = IsReadyForModelUpdate(t)
            ? steps * numEnvs : (t % steps) * numEnvs;
        for (int i = 0; i < expsOfStep.Length; i++)
            cache.WriteRow(expsOfStep[i], offset + i);
    }

    public IEnumerable<PPOTrainBatch> SampleDataset(int batchSize)
    {
        cacheGAE();

        Perm.Permutate(permCache);
        cacheWithoutLastStep.Shuffle(permCache);

        foreach(var batch in cacheWithoutLastStep.SampleBatched(batchSize))
            yield return batch;

        copyOverlappingStep();
    }

    private void cacheGAE()
    {
        for (int t = steps - 1; t >= 0; t--)
        {
            // TODO: do the GAE magic
        }
    }

    private void copyOverlappingStep()
    {
        Matrix2D.CopyData(cacheOnlyLastStep.StatesBefore, cacheOnlyFirstStep.StatesBefore);
        Matrix2D.CopyData(cacheOnlyLastStep.Actions, cacheOnlyFirstStep.Actions);
        Matrix2D.CopyData(cacheOnlyLastStep.Rewards, cacheOnlyFirstStep.Rewards);
        Matrix2D.CopyData(cacheOnlyLastStep.Terminals, cacheOnlyFirstStep.Terminals);
        Matrix2D.CopyData(cacheOnlyLastStep.Returns, cacheOnlyFirstStep.Returns);
        Matrix2D.CopyData(cacheOnlyLastStep.Advantages, cacheOnlyFirstStep.Advantages);
        Matrix2D.CopyData(cacheOnlyLastStep.OldProbs, cacheOnlyFirstStep.OldProbs);
        Matrix2D.CopyData(cacheOnlyLastStep.OldBaselines, cacheOnlyFirstStep.OldBaselines);
    }
}
