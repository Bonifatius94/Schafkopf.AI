namespace RLNetDemo;

using BackpropNet;
using Schafkopf.Training;

public record struct CartPoleState(
    double x,
    double x_dot,
    double theta,
    double theta_dot
);

public enum CartPoleDirection
{
    Right = 1,
    Left = 0
}

public record struct CartPoleAction(
    CartPoleDirection Direction
);

public class CartPoleEnv : MDPEnv<CartPoleState, CartPoleAction>
{
    // OpenAI reference implementation:
    // https://github.com/openai/gym/blob/master/gym/envs/classic_control/cartpole.py

    private const double gravity = 9.8;
    private const double masscart = 1.0;
    private const double masspole = 0.1;
    private const double total_mass = masspole + masscart;
    private const double length = 0.5;
    private const double polemass_length = masspole * length;
    private const double force_mag = 10.0;
    private const double tau = 0.02;
    private const double theta_threshold_radians = 12.0 * 2.0 * Math.PI / 360.0;
    private const double x_threshold = 2.4;

    private CartPoleState high = new CartPoleState(
        x: 4.8,
        x_dot: 0,
        theta: 0.42,
        theta_dot: 0
    );

    private CartPoleState low = new CartPoleState(
        x: -4.8,
        x_dot: 0,
        theta: -0.42,
        theta_dot: 0
    );

    private CartPoleState? state = null;
    private Random rng = new Random(0);

    public void Seed(int seed)
        => rng = new Random(seed);

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

        var terminated =
               x < -x_threshold
            || x > x_threshold
            || theta < -theta_threshold_radians
            || theta > theta_threshold_radians;

        var reward = 1.0;
        return (state.Value, reward, terminated);
    }

    public CartPoleState Reset()
    {
        state = new CartPoleState(
            sample(low.x, high.x),
            sample(low.x_dot, high.x_dot),
            sample(low.theta, high.theta),
            sample(low.theta_dot, high.theta_dot)
        );
        return state.Value;
    }

    private double sample(double low, double high)
        => rng.NextDouble() * (high - low) + low;
}

public class CartPolePPOAdapter : IPPOAdapter<CartPoleState, CartPoleAction>
{
    public CartPolePPOAdapter(PPOTrainingSettings config)
    {
        actionsCache = Enumerable.Range(0, config.NumEnvs)
            .Select(x => new CartPoleAction()).ToArray();
    }

    private CartPoleAction[] actionsCache;

    public void EncodeState(CartPoleState s0, Matrix2D buf)
    {
        var cache = buf.SliceRowsRaw(0, 1);
        cache[0] = s0.x;
        cache[1] = s0.x_dot;
        cache[2] = s0.theta;
        cache[3] = s0.theta_dot;
    }

    public void EncodeAction(CartPoleAction a0, Matrix2D buf)
    {
        buf.SliceRowsRaw(0, 1)[0] = (int)a0.Direction;
    }

    public IList<CartPoleAction> SampleActions(Matrix2D pi)
    {
        for (int i = 0; i < pi.NumRows; i++)
            actionsCache[i].Direction = (CartPoleDirection)(int)pi.At(i, 0);
        return actionsCache;
    }
}

public record CartPoleBenchmarkStats(
    // TODO: figure out other interesting stats to benchmark
    double AvgEpSteps,
    double AvgEpRewards
);

public class CartPoleBenchmark
{
    public CartPoleBenchmark(
        PPOTrainingSettings config,
        Func<CartPoleEnv> envFactory)
    {
        this.config = config;
        this.envFactory = envFactory;
    }

    private readonly PPOTrainingSettings config;
    private readonly Func<CartPoleEnv> envFactory;

    public CartPoleBenchmarkStats Benchmark(PPOModel model, int totalEpisodes)
    {
        var adapter = new CartPolePPOAdapter(config);
        var agent = new VecorizedPPOAgent<CartPoleState, CartPoleAction>(
            adapter.EncodeState, adapter.SampleActions, config
        );
        var vecEnv = new VectorizedEnv<CartPoleState, CartPoleAction>(
            Enumerable.Range(0, config.NumEnvs).Select(i => envFactory()).ToArray()
        );

        int ep = 0;
        var rewardCaches = new double[vecEnv.NumEnvs];
        var stepCaches = new int[vecEnv.NumEnvs];
        var epSteps = new double[totalEpisodes];
        var epRewards = new double[totalEpisodes];

        var states = vecEnv.Reset();

        while (ep < totalEpisodes)
        {
            var actions = agent.PickActions(model, states);
            (states, var rewards, var terminals) = vecEnv.Step(actions);

            for (int i = 0; i < vecEnv.NumEnvs; i++)
            {
                rewardCaches[i] += rewards[i];
                stepCaches[i] += 1;

                if (terminals[i])
                {
                    epRewards[ep] = rewardCaches[i];
                    epSteps[ep] = stepCaches[i];

                    if (++ep == totalEpisodes)
                        break;
                }
            }
        }

        return new CartPoleBenchmarkStats(
            epSteps.Average(), epRewards.Average()
        );
    }
}
