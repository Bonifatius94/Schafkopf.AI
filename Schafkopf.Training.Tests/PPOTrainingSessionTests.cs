namespace Schafkopf.Training.Tests;

public class PPOTraining_CartPole_Tests
{
    [Fact]
    public void Inference_WithRandomAgent_DoesNotThrowException()
    {
        var rng = new Random(42);

        var env = new CartPoleEnv();
        env.Reset();
        for (int i = 0; i < 10_000; i++)
            env.Step(new CartPoleAction() { Direction = (CartPoleDirection)rng.Next(0, 2) });

        Assert.True(true); // just ensure no exception occurs
    }

    [Fact(Skip="test requires further debugging")]
    public void Training_WithPPOAgent_CanLearnCartPole()
    {
        var config = new PPOTrainingSettings() {
            NumStateDims = 4, NumActionDims = 2
        };
        var model = new PPOModel(config);
        var envFactory = () => new CartPoleEnv();

        var encodeState = (CartPoleState s0, Matrix2D buf) => {
            var cache = buf.SliceRowsRaw(0, 1);
            cache[0] = s0.x;
            cache[1] = s0.x_dot;
            cache[2] = s0.theta;
            cache[3] = s0.theta_dot;
        };
        var encodeAction = (CartPoleAction a0, Matrix2D buf) => {
            buf.SliceRowsRaw(0, 1)[0] = (int)a0.Direction;
        };

        var actionsCache = Enumerable.Range(0, config.NumEnvs)
            .Select(x => new CartPoleAction()).ToArray();
        var sampleActions = (Matrix2D pi) => {
            for (int i = 0; i < pi.NumRows; i++)
                actionsCache[i].Direction = (CartPoleDirection)(int)pi.At(i, 0);
            return (IList<CartPoleAction>)actionsCache;
        };

        var rollout = new PPORolloutBuffer<CartPoleState, CartPoleAction>(
            config, encodeState, encodeAction
        );
        var exps = new SingleAgentExpCollector<CartPoleState, CartPoleAction>(
            config, encodeState, sampleActions, envFactory
        );

        for (int ep = 0; ep < config.NumTrainings; ep++)
        {
            exps.Collect(rollout, model);
            model.Train(rollout);
        }

        // TODO: make assertion, e.g. avg_steps < x
    }
}

public class SingleAgentExpCollector<TState, TAction>
    where TState : IEquatable<TState>, new()
    where TAction : IEquatable<TAction>, new()
{
    public SingleAgentExpCollector(
        PPOTrainingSettings config,
        Action<TState, Matrix2D> encodeState,
        Func<Matrix2D, IList<TAction>> sampleActions,
        Func<MDPEnv<TState, TAction>> envFactory)
    {
        this.config = config;
        this.encodeState = encodeState;
        this.sampleActions = sampleActions;

        var envs = Enumerable.Range(0, config.NumEnvs)
            .Select(i => envFactory()).ToList();
        vecEnv = new VectorizedEnv<TState, TAction>(envs);
        exps = Enumerable.Range(0, config.NumEnvs)
            .Select(i => new PPOExp<TState, TAction>()).ToArray();
        s0 = vecEnv.Reset().ToArray();

        s0_enc = Matrix2D.Zeros(config.NumEnvs, config.NumStateDims);
        v = Matrix2D.Zeros(config.NumEnvs, 1);
        pi = Matrix2D.Zeros(config.NumEnvs, 1);
        piProbs = Matrix2D.Zeros(config.NumEnvs, 1);
    }

    private readonly PPOTrainingSettings config;
    private readonly VectorizedEnv<TState, TAction> vecEnv;
    private readonly Action<TState, Matrix2D> encodeState;
    private readonly Func<Matrix2D, IList<TAction>> sampleActions;

    private TState[] s0;
    private PPOExp<TState, TAction>[] exps;
    private Matrix2D s0_enc;
    private Matrix2D v;
    private Matrix2D pi;
    private Matrix2D piProbs;

    public void Collect(PPORolloutBuffer<TState, TAction> buffer, PPOModel model)
    {
        for (int t = 0; t < buffer.Steps; t++)
        {
            for (int i = 0; i < config.NumEnvs; i++)
                encodeState(s0[i], s0_enc.SliceRows(i, 1));

            model.Predict(s0_enc, pi, piProbs, v);
            var a0 = sampleActions(pi);

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

public record struct CartPoleState(
    double x,
    double x_dot,
    double theta,
    double theta_dot
);

public enum CartPoleDirection { Right = 1, Left = 0 }

public record struct CartPoleAction(
    CartPoleDirection Direction
);

public class VectorizedEnv<TState, TAction>
{
    private readonly IList<MDPEnv<TState, TAction>> envs;
    private IList<TState> states;
    private IList<double> rewards;
    private IList<bool> terminals;

    public VectorizedEnv(IList<MDPEnv<TState, TAction>> envs)
    {
        this.envs = envs;
        states = new TState[envs.Count];
        rewards = new double[envs.Count];
        terminals = new bool[envs.Count];
    }

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

public class CartPoleEnv : MDPEnv<CartPoleState, CartPoleAction>
{
    private const double gravity = 9.8;
    private const double masscart = 1.0;
    private const double masspole = 0.1;
    private const double total_mass = masspole + masscart;
    private const double length = 0.5;
    private const double polemass_length = masspole * length;
    private const double force_mag = 10.0;
    private const double tau = 0.02;
    private const double theta_threshold_radians = 12 * 2 * Math.PI / 360;
    private const double x_threshold = 2.4;

    private CartPoleState? state = null;
    private Random rng = new Random(0);

    public (CartPoleState, double, bool) Step(CartPoleAction action)
    {
        if (state == null)
            throw new InvalidOperationException("Environment needs to be initialized with Reset()");

        (var x, var x_dot, var theta, var theta_dot) = state.Value;

        var force = action.Direction == CartPoleDirection.Right ? force_mag : -force_mag;
        var costheta = Math.Cos(theta);
        var sintheta = Math.Sin(theta);

        var temp = (force + polemass_length * Math.Pow(theta_dot, 2) * sintheta) / total_mass;
        var thetaacc = (gravity * sintheta - costheta * temp) / (
            length * (4.0 / 3.0 - masspole * Math.Pow(costheta, 2) / total_mass)
        );
        var xacc = temp - polemass_length * thetaacc * costheta / total_mass;

        // Euler interpolation
        state = new CartPoleState(
            x + tau * x_dot,
            x_dot + tau * xacc,
            theta + tau * theta_dot,
            theta_dot + tau * thetaacc
        );

        // TODO: check if this condition is correct
        var terminated =
            x > -x_threshold
            && x < x_threshold
            && theta > -theta_threshold_radians
            && theta < theta_threshold_radians;

        var reward = 1.0;
        return (state.Value, reward, terminated);
    }

    public CartPoleState Reset()
    {
        state = new CartPoleState(
            sample(x_threshold * -2, x_threshold * 2),
            sample(-10.0, 10.0),
            sample(theta_threshold_radians * -2, theta_threshold_radians * 2),
            sample(-Math.PI, Math.PI)
        );
        return state.Value;
    }

    private double sample(double low, double high)
        => rng.NextDouble() * (high - low) + low;
}
