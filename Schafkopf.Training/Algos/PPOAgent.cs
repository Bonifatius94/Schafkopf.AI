namespace Schafkopf.Training;

public class PPOTrainingSettings
{
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
    public int NumEnvs = 64;
    public int NumStateDims = 90;
    public int NumActionDims = 32;
    public int StepsPerUpdate = 512;
    public int UpdateEpochs = 10;
    public int NumModelSnapshots = 20;

    public int TrainSteps => TotalSteps / NumEnvs;
    public int ModelSnapshotInterval => TrainSteps / NumModelSnapshots;
    public int NumTrainings => TrainSteps / StepsPerUpdate;
}

public class VecorizedPPOAgent<TState, TAction>
{
    public VecorizedPPOAgent(
        Action<TState, Matrix2D> encodeState,
        Func<Matrix2D, IList<TAction>> sampleActions,
        PPOTrainingSettings config)
    {
        this.encodeState = encodeState;
        this.sampleActions = sampleActions;

        s0 = Matrix2D.Zeros(config.NumEnvs, config.NumStateDims);
        v = Matrix2D.Zeros(config.NumEnvs, 1);
        pi = Matrix2D.Zeros(config.NumEnvs, 1);
        piProbs = Matrix2D.Zeros(config.NumEnvs, 1);
    }

    private readonly Action<TState, Matrix2D> encodeState;
    private readonly Func<Matrix2D, IList<TAction>> sampleActions;

    private readonly Matrix2D s0;
    private readonly Matrix2D pi;
    private readonly Matrix2D piProbs;
    private readonly Matrix2D v;

    public IList<TAction> PickActions(PPOModel model, IList<TState> states)
    {
        for (int i = 0; i < states.Count; i++)
            encodeState(states[i], s0.SliceRows(i, 1));

        model.Predict(s0, pi, piProbs, v);
        return sampleActions(pi);
    }

    public (IList<TAction>, Matrix2D, Matrix2D) PickActionsWithMeta(
        PPOModel model, IList<TState> states)
    {
        for (int i = 0; i < states.Count; i++)
            encodeState(states[i], s0.SliceRows(i, 1));

        model.Predict(s0, pi, piProbs, v);
        return (sampleActions(pi), piProbs, v);
    }
}

public interface IPPOAdapter<TState, TAction>
{
    void EncodeState(TState s0, Matrix2D buf);
    void EncodeAction(TAction a0, Matrix2D buf);
    IList<TAction> SampleActions(Matrix2D pi);
}

public class SingleAgentExpCollector<TState, TAction>
    where TState : IEquatable<TState>, new()
    where TAction : IEquatable<TAction>, new()
{
    public SingleAgentExpCollector(
        PPOTrainingSettings config,
        IPPOAdapter<TState, TAction> adapter,
        Func<MDPEnv<TState, TAction>> envFactory)
    {
        this.config = config;

        var envs = Enumerable.Range(0, config.NumEnvs)
            .Select(i => envFactory()).ToList();
        vecEnv = new VectorizedEnv<TState, TAction>(envs);
        exps = Enumerable.Range(0, config.NumEnvs)
            .Select(i => new PPOExp<TState, TAction>()).ToArray();
        s0 = vecEnv.Reset().ToArray();
        agent = new VecorizedPPOAgent<TState, TAction>(
            adapter.EncodeState, adapter.SampleActions, config
        );
    }

    private readonly PPOTrainingSettings config;
    private readonly VecorizedPPOAgent<TState, TAction> agent;
    private readonly VectorizedEnv<TState, TAction> vecEnv;

    private TState[] s0;
    private PPOExp<TState, TAction>[] exps;

    public void Collect(PPORolloutBuffer<TState, TAction> buffer, PPOModel model)
    {
        for (int t = 0; t < buffer.Steps; t++)
        {
            (var a0, var piProbs, var v) = agent.PickActionsWithMeta(model, s0);
            (var s1, var r1, var t1) = vecEnv.Step(a0);

            for (int i = 0; i < config.NumEnvs; i++)
            {
                exps[i].StateBefore = s0[i];
                exps[i].Action = a0[i];
                exps[i].Reward = r1[i];
                exps[i].IsTerminal = t1[i];
                exps[i].OldProb = piProbs.At(i, 0);
                exps[i].OldBaseline = v.At(i, 0);
            }

            for (int i = 0; i < config.NumEnvs; i++)
                s0[i] = s1[i];

            buffer.AppendStep(exps, t);
        }
    }
}

public class VectorizedEnv<TState, TAction>
{
    public VectorizedEnv(IList<MDPEnv<TState, TAction>> envs)
    {
        this.envs = envs;
        states = new TState[envs.Count];
        rewards = new double[envs.Count];
        terminals = new bool[envs.Count];
    }

    private readonly IList<MDPEnv<TState, TAction>> envs;
    private IList<TState> states;
    private IList<double> rewards;
    private IList<bool> terminals;

    public int NumEnvs => envs.Count;

    public IList<TState> Reset()
    {
        for (int i = 0; i < envs.Count; i++)
            states[i] = envs[i].Reset();
        return states;
    }

    public (IList<TState>, IList<double>, IList<bool>) Step(IList<TAction> actions)
    {
        for (int i = 0; i < envs.Count; i++)
        {
            (var s1, var r1, var t1) = envs[i].Step(actions[i]);
            s1 = t1 ? envs[i].Reset() : s1;
            states[i] = s1;
            rewards[i] = r1;
            terminals[i] = t1;
        }

        return (states, rewards, terminals);
    }
}

public class PPOModel
{
    public PPOModel(PPOTrainingSettings config)
    {
        this.config = config;

        var sharedLayers = new ILayer[] {
            new DenseLayer(64),
            new ReLULayer(),
            new DenseLayer(64),
            new ReLULayer(),
        };
        var valueFuncHead = new ILayer[] {
            new DenseLayer(1)
        };
        piSampler = new UniformSamplingLayer();
        var strategyHead = new ILayer[] {
            new DenseLayer(config.NumActionDims),
            new SoftmaxLayer(),
            piSampler
        };

        valueFunc = new FFModel(sharedLayers.Concat(valueFuncHead).ToArray());
        strategy = new FFModel(sharedLayers.Concat(strategyHead).ToArray());

        valueFunc.Compile(config.BatchSize, config.NumStateDims);
        strategy.Compile(config.BatchSize, config.NumStateDims);
        strategyOpt = new AdamOpt(config.LearnRate);
        valueFuncOpt = new AdamOpt(config.LearnRate);
        strategyOpt.Compile(strategy.GradsTape);
        valueFuncOpt.Compile(valueFunc.GradsTape);

        normAdvantages = Matrix2D.Zeros(config.BatchSize, 1);
        policyRatios = Matrix2D.Zeros(config.BatchSize, 1);
        derPolicyRatios = Matrix2D.Zeros(config.BatchSize, 1);
        derNewProbs = Matrix2D.Zeros(config.BatchSize, 1);
        clipMask = Matrix2D.Zeros(config.BatchSize, 1);
    }

    private PPOTrainingSettings config;
    private FFModel valueFunc;
    private FFModel strategy;
    private ISampler piSampler;
    private IOptimizer strategyOpt;
    private IOptimizer valueFuncOpt;
    private ILoss mse = new MeanSquaredError();

    private Matrix2D normAdvantages;
    private Matrix2D policyRatios;
    private Matrix2D derPolicyRatios;
    private Matrix2D derNewProbs;
    private Matrix2D clipMask;

    public int BatchSize => config.BatchSize;

    public void Predict(Matrix2D s0, Matrix2D outPi, Matrix2D outProbs, Matrix2D outV)
    {
        var predPi = strategy.PredictBatch(s0);
        var predV = valueFunc.PredictBatch(s0);
        var predProbs = piSampler.FetchSelectionProbs();
        Matrix2D.CopyData(predPi, outPi);
        Matrix2D.CopyData(predProbs, outProbs);
        Matrix2D.CopyData(predV, outV);
    }

    public void Train<TState, TAction>(
            PPORolloutBuffer<TState, TAction> memory,
            bool showProgress = true
        )
        where TState : IEquatable<TState>, new()
        where TAction : IEquatable<TAction>, new()
    {
        int numBatches = memory.NumBatches(
            config.BatchSize, config.UpdateEpochs);
        var batches = memory.SampleDataset(
            config.BatchSize, config.UpdateEpochs);

        int i = 1;
        foreach (var batch in batches)
        {
            if (showProgress)
                Console.Write($"\rtraining {i++} / {numBatches}           ");
            updateModels(batch);
        }
        if (showProgress)
            Console.WriteLine();
    }

    private void updateModels(PPOTrainBatch batch)
    {
        // update strategy pi(s)
        piSampler.Seed(batch.Actions.SliceRows(0, batch.Size));
        var predPi = strategy.PredictBatch(batch.StatesBefore);
        piSampler.Unseed();
        var strategyDeltas = strategy.Layers.Last().Cache.DeltasIn;
        computePolicyDeltas(batch, predPi, strategyDeltas);
        strategy.FitBatch(strategyDeltas, strategyOpt);

        // update baseline V(s)
        var predV = valueFunc.PredictBatch(batch.StatesBefore);
        var valueDeltas = valueFunc.Layers.Last().Cache.DeltasIn;
        mse.LossDeltas(predV, batch.Returns, valueDeltas);
        // TODO: add value clipping
        valueFunc.FitBatch(valueDeltas, valueFuncOpt);
    }

    private void computePolicyDeltas(
        PPOTrainBatch batch, Matrix2D predPi, Matrix2D policyDeltas)
    {
        var advantages = batch.Advantages;
        if (config.NormAdvantages)
        {
            double mean = Matrix2D.Mean(batch.Advantages);
            double stdDev = Matrix2D.StdDev(batch.Advantages);
            Matrix2D.BatchSub(batch.Advantages, mean, normAdvantages);
            Matrix2D.BatchDiv(batch.Advantages, stdDev, normAdvantages);
            advantages = normAdvantages;
        }

        Matrix2D.BatchAdd(batch.OldProbs, 1e-8, policyRatios);
        Matrix2D.ElemDiv(policyRatios, predPi, policyRatios);
        Matrix2D.BatchOneOver(derNewProbs, derPolicyRatios);
        Matrix2D.ElemMul(policyRatios, derPolicyRatios, derPolicyRatios);

        Matrix2D.ElemGeq(policyRatios, 1 + config.ProbClip, clipMask);
        Matrix2D.ElemLeq(policyRatios, 1 - config.ProbClip, clipMask);
        Matrix2D.ElemNeq(clipMask, 1, clipMask);

        Matrix2D.ElemMul(clipMask, derPolicyRatios, policyDeltas);
        Matrix2D.ElemMul(policyDeltas, advantages, policyDeltas);
        Matrix2D.BatchMul(policyDeltas, -1, policyDeltas);
    }

    public void RecompileCache(int batchSize)
    {
        strategy.RecompileCache(batchSize);
        valueFunc.RecompileCache(batchSize);
    }
}

public struct PPOTrainBatch
{
    public PPOTrainBatch(int size, int numStateDims, int numActionDims)
    {
        Size = size;
        StatesBefore = Matrix2D.Zeros(size, numStateDims);
        Actions = Matrix2D.Zeros(size, numActionDims);
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

public class PPORolloutBuffer<TState, TAction>
    where TState : IEquatable<TState>, new()
    where TAction : IEquatable<TAction>, new()
{
    public PPORolloutBuffer(
        PPOTrainingSettings config,
        IPPOAdapter<TState, TAction> adapter)
    {
        Adapter = adapter;
        NumEnvs = config.NumEnvs;
        Steps = config.StepsPerUpdate;
        gamma = config.RewardDiscount;
        gaeGamma = config.GAEDiscount;

        // info: the cache stores an extra timestep at the end
        //       which facilitates proper GAE computation

        int size = Steps * NumEnvs;
        int sizeWithExtraStep = (Steps + 1) * NumEnvs;
        cache = new PPOTrainBatch(
            sizeWithExtraStep,
            config.NumStateDims,
            config.NumActionDims
        );
        cacheWithoutLastStep = cache.SliceRows(0, size);
        cacheOnlyFirstStep = cache.SliceRows(0, NumEnvs);
        cacheOnlyLastStep = cache.SliceRows(size, NumEnvs);
        permCache = Perm.Identity(size);
    }

    private IPPOAdapter<TState, TAction> Adapter;
    public int NumEnvs;
    public int Steps;
    private double gamma;
    private double gaeGamma;
    private PPOTrainBatch cache;
    private PPOTrainBatch cacheWithoutLastStep;
    private PPOTrainBatch cacheOnlyFirstStep;
    private PPOTrainBatch cacheOnlyLastStep;
    private int[] permCache;

    public bool IsReadyForModelUpdate(int t) => t > 0 && t % Steps == 0;

    public int NumBatches(int batchSize, int epochs = 1)
        => cacheWithoutLastStep.Size / batchSize * epochs;

    public void AppendStep(PPOExp<TState, TAction>[] exps, int t)
    {
        if (exps.Length != NumEnvs)
            throw new ArgumentException("Invalid amount of experiences!");

        int offset = IsReadyForModelUpdate(t)
            ? Steps * NumEnvs : (t % Steps) * NumEnvs;
        var buffer = cache.SliceRows(offset, NumEnvs);

        for (int i = 0; i < exps.Length; i++)
        {
            var exp = exps[i];
            unsafe
            {
                var s0Dest = buffer.StatesBefore.SliceRows(i, 1);
                Adapter.EncodeState(exp.StateBefore, s0Dest);
                var a0Dest = buffer.Actions.SliceRows(i, 1);
                Adapter.EncodeAction(exp.Action, a0Dest);
                buffer.Rewards.Data[i] = exp.Reward;
                buffer.Terminals.Data[i] = exp.IsTerminal ? 1 : 0;
                buffer.OldProbs.Data[i] = exp.OldProb;
                buffer.OldBaselines.Data[i] = exp.OldBaseline;
            }
        }
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

        // copyOverlappingStep();
    }

    private void shuffleDataset()
    {
        Perm.Permutate(permCache);
        cacheWithoutLastStep.Shuffle(permCache);
    }

    private void cacheGAE(PPOTrainBatch cache)
    {
        var nonterm_t1 = Matrix2D.Zeros(NumEnvs, 1);
        var lambda = Matrix2D.Zeros(NumEnvs, 1);
        var delta = Matrix2D.Zeros(NumEnvs, 1);

        for (int t = Steps - 1; t >= 0; t--)
        {
            var r_t0 = cache.Rewards.SliceRows(t, NumEnvs);
            var term_t1 = cache.Terminals.SliceRows(t+1, NumEnvs);
            var v_t0 = cache.OldBaselines.SliceRows(t, NumEnvs);
            var v_t1 = cache.OldBaselines.SliceRows(t+1, NumEnvs);
            var A_t0 = cache.Advantages.SliceRows(t, NumEnvs);
            var G_t0 = cache.Returns.SliceRows(t, NumEnvs);

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
