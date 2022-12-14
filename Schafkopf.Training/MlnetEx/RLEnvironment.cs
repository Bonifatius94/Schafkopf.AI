
// public class RLContext<TState, TAction>
// {
//     public RLContext(Func<TState, (TAction, TState)> stateTransition)
//     {
//         this.stateTransition = stateTransition;
//     }

//     private Func<TState, (TAction, TState)> stateTransition;

//     private List<TState> states = new List<TState>();
//     private List<TAction> actions = new List<TAction>();

//     public TState CurrentState => states.Last();

//     public void Simulate(CancellationToken token)
//     {
//         while (!token.IsCancellationRequested)
//         {
//             (var nextState, var action) = stateTransition(CurrentState);
            
//         }
//     }
// }

public interface ISarsExperience<TState, TAction>
{
    TState S0 { get; }
    TState? S1 { get; }
    TAction Action { get; }
    double Reward { get; }
    bool IsTerminalState { get; }
}

public class ExperienceReplay<ISarsExperience>
{
    public ExperienceReplay(int bufferSize, int batchSize, double alpha)
    {
        nextId = 0;
        recordCount = 0;
        this.batchSize = batchSize;
        ringBuffer = new ISarsExperience[bufferSize];
        sampleCache = new ISarsExperience[batchSize];
    }

    private int nextId;
    private int recordCount;
    private int batchSize;
    private ISarsExperience[] ringBuffer;

    private int bufferSize => ringBuffer.Length;

    public void Add(ISarsExperience exp)
    {
        recordCount = recordCount < bufferSize
            ? recordCount + 1 : recordCount;
        ringBuffer[nextId] = exp;
        nextId = ++nextId % bufferSize;
    }

    public void Reset()
    {
        nextId = 0;
        recordCount = 0;
    }

    private ISarsExperience[] sampleCache;
    public IReadOnlyList<ISarsExperience> Sample()
    {
        throw new NotImplementedException();
    }
}

/* ideas:
 ===============================
  - represent experience input stream as IEnumerable, e.g. mlContext.Data.LoadFromEnumerable
  - add a caching layer for experience collection / caching
*/

/*
class PrioritizedReplayMemory(ReplayMemory):
    def __init__(self, size, alpha):
        """Create Prioritized Replay buffer.
        Parameters
        ----------
        size: int
            Max number of transitions to store in the buffer. When the buffer
            overflows the old memories are dropped.
        alpha: float
            how much prioritization is used
            (0 - no prioritization, 1 - full prioritization)
        See Also
        --------
        ReplayBuffer.__init__
        """
        super(PrioritizedReplayMemory, self).__init__(size)
        assert alpha >= 0
        self._alpha = alpha

        it_capacity = 1
        while it_capacity < size:
            it_capacity *= 2

        self._it_sum = SumSegmentTree(it_capacity)
        self._it_min = MinSegmentTree(it_capacity)
        self._max_priority = 1.0

    def add(self, *args, **kwargs):
        """See ReplayBuffer.store_effect"""
        idx = self._next_id
        super().add(*args, **kwargs)
        self._it_sum[idx] = self._max_priority ** self._alpha
        self._it_min[idx] = self._max_priority ** self._alpha

    def _sample_proportional(self, batch_size):
        res = []
        p_total = self._it_sum.sum(0, len(self._storage) - 1)
        every_range_len = p_total / batch_size
        for i in range(batch_size):
            mass = random.random() * every_range_len + i * every_range_len
            idx = self._it_sum.find_prefixsum_idx(mass)
            res.append(idx)
        return res

    def sample(self, batch_size, beta):
        """Sample a batch of experiences.
        compared to ReplayBuffer.sample
        it also returns importance weights and idxes
        of sampled experiences.
        Parameters
        ----------
        batch_size: int
            How many transitions to sample.
        beta: float
            To what degree to use importance weights
            (0 - no corrections, 1 - full correction)
        Returns
        -------
        obs_batch: np.array
            batch of observations
        act_batch: np.array
            batch of actions executed given obs_batch
        rew_batch: np.array
            rewards received as results of executing act_batch
        next_obs_batch: np.array
            next set of observations seen after executing act_batch
        done_mask: np.array
            done_mask[i] = 1 if executing act_batch[i] resulted in
            the end of an episode and 0 otherwise.
        weights: np.array
            Array of shape (batch_size,) and dtype np.float32
            denoting importance weight of each sampled transition
        idxes: np.array
            Array of shape (batch_size,) and dtype np.int32
            idexes in buffer of sampled experiences
        """
        assert beta > 0

        idxes = self._sample_proportional(batch_size)

        weights = []
        p_min = self._it_min.min() / self._it_sum.sum()
        max_weight = (p_min * len(self._storage)) ** (-beta)

        for idx in idxes:
            p_sample = self._it_sum[idx] / self._it_sum.sum()
            weight = (p_sample * len(self._storage)) ** (-beta)
            weights.append(weight / max_weight)
        weights = np.array(weights)
        encoded_sample = self._format_fetch_sample(idxes)
        return tuple(list(encoded_sample) + [weights, idxes])

    def update_priorities(self, idxes, priorities):
        """Update priorities of sampled transitions.
        sets priority of transition at index idxes[i] in buffer
        to priorities[i].
        Parameters
        ----------
        idxes: [int]
            List of idxes of sampled transitions
        priorities: [float]
            List of updated priorities corresponding to
            transitions at the sampled idxes denoted by
            variable `idxes`.
        """
        assert len(idxes) == len(priorities)
        for idx, priority in zip(idxes, priorities):
            assert priority > 0
            assert 0 <= idx < len(self._storage)
            self._it_sum[idx] = priority ** self._alpha
            self._it_min[idx] = priority ** self._alpha

            self._max_priority = max(self._max_priority, priority)

*/
