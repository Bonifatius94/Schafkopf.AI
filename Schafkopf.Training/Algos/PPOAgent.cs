namespace Schafkopf.Training;

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
    public PPOAgent(PPOTrainingSettings config)
    {
        this.config = config;

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
        featureCache = Matrix2D.Zeros(config.BatchSize, 90);
        strategyOpt = new AdamOpt(config.LearnRate);
        valueFuncOpt = new AdamOpt(config.LearnRate);
    }

    private PPOTrainingSettings config;
    private FFModel valueFunc;
    private FFModel strategy;
    private IOptimizer strategyOpt;
    private IOptimizer valueFuncOpt;

    private Matrix2D featureCache;
    private GameStateSerializer stateSerializer = new GameStateSerializer();
    private UniformDistribution uniform = new UniformDistribution();

    public Card ChooseCard(GameLog log, ReadOnlySpan<Card> possibleCards)
    {
        var x = featureCache;
        var s0 = stateSerializer.SerializeState(log);
        unsafe { s0.ExportFeatures(x.Data);}

        var predPi = strategy.PredictBatch(x);
        var probDist = normProbDist(predPi, possibleCards);
        int i = uniform.Sample(probDist);
        var action = possibleCards[i];
        return action;
    }

    public (Card, double, double) Predict(
        GameLog log, ReadOnlySpan<Card> possibleCards)
    {
        var x = featureCache;
        var s0 = stateSerializer.SerializeState(log);
        unsafe { s0.ExportFeatures(x.Data);}

        var predPi = strategy.PredictBatch(x);
        var predV = valueFunc.PredictBatch(x);
        var probDist = normProbDist(predPi, possibleCards);
        int i = uniform.Sample(probDist);

        var action = possibleCards[i];
        var pi = predPi.At(0, action.Id);
        var v = predV.At(0, action.Id);
        return (action, pi, v);
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

    public void Train(PPORolloutBuffer memory)
    {
        var batches = memory.SampleDataset(
            config.BatchSize, config.UpdateEpochs);

        foreach (var batch in batches)
            updateModels(batch);
    }

    private void updateModels(PPOTrainBatch batch)
    {
        var predPi = strategy.PredictBatch(batch.StatesBefore);
        var policyDeltas = strategy.Layers.Last().Cache.DeltasIn;
        computePolicyDeltas(batch, predPi, policyDeltas);
        strategy.FitBatch(policyDeltas, strategyOpt);

        var predV = valueFunc.PredictBatch(batch.StatesBefore);
        var valueDeltas = valueFunc.Layers.Last().Cache.DeltasIn;
        computeValueDeltas(batch, predV, valueDeltas);
        valueFunc.FitBatch(valueDeltas, valueFuncOpt);
    }

    private void computePolicyDeltas(
        PPOTrainBatch batch, Matrix2D predPi, Matrix2D policyDeltas)
    {
        var normAdvantages = Matrix2D.Zeros(batch.Size, 1);
        var policyRatios = Matrix2D.Zeros(batch.Size, 1);
        var derPolicyRatios = Matrix2D.Zeros(batch.Size, 1);
        var newProbs = Matrix2D.Zeros(batch.Size, 1);
        var derNewProbs = Matrix2D.Zeros(batch.Size, 1);
        var clipMask = Matrix2D.Zeros(batch.Size, 1);
        var policyDeltasSparse = Matrix2D.Zeros(batch.Size, 1);

        var advantages = batch.Advantages;
        if (config.NormAdvantages)
        {
            double mean = Matrix2D.Mean(batch.Advantages);
            double stdDev = Matrix2D.StdDev(batch.Advantages);
            Matrix2D.BatchSub(batch.Advantages, mean, normAdvantages);
            Matrix2D.BatchDiv(batch.Advantages, stdDev, normAdvantages);
            advantages = normAdvantages;
        }

        var onehots = onehotIndices(batch.Actions, 32).Zip(Enumerable.Range(0, 32));
        foreach ((int p, int i) in onehots)
            unsafe { newProbs.Data[i] = predPi.Data[p]; }
        Matrix2D.BatchAdd(batch.OldProbs, 1e-8, policyRatios);
        Matrix2D.ElemDiv(policyRatios, newProbs, policyRatios);
        Matrix2D.BatchOneOver(derNewProbs, derPolicyRatios);
        Matrix2D.ElemMul(policyRatios, derPolicyRatios, derPolicyRatios);

        Matrix2D.ElemGeq(policyRatios, 1 + config.ProbClip, clipMask);
        Matrix2D.ElemLeq(policyRatios, 1 - config.ProbClip, clipMask);
        Matrix2D.ElemNeq(clipMask, 1, clipMask);

        Matrix2D.ElemMul(clipMask, derPolicyRatios, policyDeltasSparse);
        Matrix2D.ElemMul(policyDeltasSparse, advantages, policyDeltasSparse);
        Matrix2D.BatchMul(policyDeltasSparse, -1, policyDeltasSparse);

        Matrix2D.BatchMul(policyDeltas, 0, policyDeltas);
        foreach ((int p, int i) in onehots)
            unsafe { policyDeltas.Data[p] = policyDeltasSparse.Data[i]; }
    }

    private void computeValueDeltas(
        PPOTrainBatch batch, Matrix2D predV, Matrix2D valueDeltas)
    {
        var mse = new MeanSquaredError();
        var valueDeltasSparse = Matrix2D.Zeros(batch.Size, 1);
        mse.LossDeltas(predV, batch.Returns, valueDeltasSparse);

        // TODO: add value clipping

        Matrix2D.BatchMul(valueDeltas, 0, valueDeltas);
        var onehots = onehotIndices(batch.Actions, 32).Zip(Enumerable.Range(0, 32));
        foreach ((int p, int i) in onehots)
            unsafe { valueDeltas.Data[p] = valueDeltasSparse.Data[i]; }
    }

    private IEnumerable<int> onehotIndices(Matrix2D sparseClassIds, int numClasses)
    {
        for (int i = 0; i < sparseClassIds.NumRows; i++)
            yield return i * numClasses + (int)sparseClassIds.At(0, i);
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
            trainBuf.StatesBefore = StatesBefore.SliceRows(p, batchSize);
            trainBuf.Actions = Actions.SliceRows(p, batchSize);
            trainBuf.Rewards = Rewards.SliceRows(p, batchSize);
            trainBuf.Terminals = Terminals.SliceRows(p, batchSize);
            trainBuf.Returns = Returns.SliceRows(p, batchSize);
            trainBuf.Advantages = Advantages.SliceRows(p, batchSize);
            trainBuf.OldProbs = OldProbs.SliceRows(p, batchSize);
            trainBuf.OldBaselines = OldBaselines.SliceRows(p, batchSize);
            yield return trainBuf;
            p += batchSize;
        }
    }

    public PPOTrainBatch SliceRows(int rowid, int length)
        => new PPOTrainBatch {
            Size = length,
            StatesBefore = StatesBefore.SliceRows(rowid, length),
            Actions = Actions.SliceRows(rowid, length),
            Rewards = Rewards.SliceRows(rowid, length),
            Terminals = Terminals.SliceRows(rowid, length),
            Returns = Returns.SliceRows(rowid, length),
            Advantages = Advantages.SliceRows(rowid, length),
            OldProbs = OldProbs.SliceRows(rowid, length),
            OldBaselines = OldBaselines.SliceRows(rowid, length)
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

    public IEnumerable<PPOTrainBatch> SampleDataset(int batchSize, int epochs = 1)
    {
        cacheGAE(cache);

        for (int i = 0; i < epochs; i++)
        {
            shuffleDataset();
            foreach(var batch in cacheWithoutLastStep.SampleBatched(batchSize))
                yield return batch;
        }

        copyOverlappingStep();
    }

    private void shuffleDataset()
    {
        Perm.Permutate(permCache);
        cacheWithoutLastStep.Shuffle(permCache);
    }

    private void cacheGAE(PPOTrainBatch cache)
    {
        var nonterm_t1 = Matrix2D.Zeros(1, numEnvs);
        var lambda = Matrix2D.Zeros(1, numEnvs);
        var delta = Matrix2D.Zeros(1, numEnvs);

        for (int t = steps - 1; t >= 0; t--)
        {
            var r_t0 = cache.Rewards.SliceRows(t, 1);
            var term_t1 = cache.Terminals.SliceRows(t+1, 1);
            var v_t0 = cache.OldBaselines.SliceRows(t, 1);
            var v_t1 = cache.OldBaselines.SliceRows(t+1, 1);
            var A_t0 = cache.Advantages.SliceRows(t, 1);
            var G_t0 = cache.Returns.SliceRows(t, 1);

            Matrix2D.BatchMul(term_t1, -1, nonterm_t1);
            Matrix2D.BatchAdd(nonterm_t1, 1, nonterm_t1);

            Matrix2D.ElemMul(v_t1, nonterm_t1, delta);
            Matrix2D.BatchMul(delta, gamma, delta);
            Matrix2D.ElemAdd(delta, r_t0, delta);
            Matrix2D.ElemSub(delta, v_t0, delta);

            Matrix2D.ElemMul(nonterm_t1, lambda, lambda);
            Matrix2D.BatchMul(lambda, gamma * gaeGamma, lambda);
            Matrix2D.ElemAdd(lambda, delta, lambda);

            Matrix2D.CopyData(lambda, A_t0);
            Matrix2D.ElemAdd(v_t0, A_t0, G_t0);
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
