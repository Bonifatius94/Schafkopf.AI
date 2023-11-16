namespace Schafkopf.Training;

public struct GameState : IEquatable<GameState>
{
    public const int NUM_FEATURES = 90;

    public GameState() { }

    public double[] State = new double[NUM_FEATURES];

    public void LoadFeatures(double[] other)
        => Array.Copy(other, State, NUM_FEATURES);

    public bool Equals(GameState other)
    {
        for (int i = 0; i < NUM_FEATURES; i++)
            if (State[i] != other.State[i])
                return false;
        return true;
    }

    public override int GetHashCode() => 0;
}

public class GameStateSerializer
{
    private const int NO_CARD = -1;

    public GameState[] NewBuffer()
        => Enumerable.Range(0, 36).Select(x => new GameState()).ToArray();

    private GameState[] stateBuffer = Enumerable.Range(0, 36)
        .Select(x => new GameState()).ToArray();
    public void SerializeSarsExps(
        GameLog completedGame, SarsExp[] exps, Func<GameState, double> reward)
    {
        Serialize(completedGame, stateBuffer);
        var actions = completedGame.UnrollActions().ToArray();
        var p_ids = completedGame.UnrollActions()
            .Select(x => x.PlayerId).ToArray();

        var order_idx = new byte[4, 8];
        foreach ((byte p_id, int t) in p_ids.Zip(Enumerable.Range(0, 32)))
            order_idx[p_id, t / 4] = (byte)(t % 4);

        for (int t = 0; t < 32; t++)
        {
            int p_id = p_ids[t];
            int t_id = t / 4;
            bool isTerminal = t > 28;
            int p0 = t_id * 4 + order_idx[p_id, t_id];
            int p1 = (t_id + 1) * 4 + order_idx[p_id, t_id + 1];
            p1 = isTerminal ? 32 + p_id : p1;

            exps[t].Action.PlayerId = 0;
            exps[t].Action.CardPlayed = actions[t].CardPlayed;
            exps[t].StateBefore.LoadFeatures(stateBuffer[p0].State);
            exps[t].StateAfter.LoadFeatures(stateBuffer[p1].State);
            exps[t].IsTerminal = isTerminal;
            exps[t].Reward = reward(stateBuffer[p1]);
        }
    }

    public void Serialize(GameLog completedGame, GameState[] states)
    {
        var origCall = completedGame.Call;
        var hands = completedGame.UnrollHands().GetEnumerator();
        var scores = completedGame.UnrollAugen().GetEnumerator();
        var allActions = completedGame.UnrollActions().ToArray();
        var normCalls = new GameCall[] {
            normCallForPlayer(origCall, 0), normCallForPlayer(origCall, 1),
            normCallForPlayer(origCall, 2), normCallForPlayer(origCall, 3)
        };

        int t = 0;
        foreach (var turn in completedGame.Turns)
        {
            scores.MoveNext();

            for (int i = 0; i < 4; i++)
            {
                hands.MoveNext();

                var hand = hands.Current;
                var score = scores.Current;
                var state = states[t].State;
                serializeState(state, normCalls, hand, t++, allActions, score);
            }
        }

        scores.MoveNext();

        for (; t < 36; t++)
            serializeState(
                states[t].State, normCalls, Hand.EMPTY,
                t, allActions, scores.Current);
    }

    private unsafe void serializeState(
        double[] state, ReadOnlySpan<GameCall> normCalls, Hand hand, int t,
        ReadOnlySpan<GameAction> turnHistory, int[] augen)
    {
        if (state.Length < 90)
            throw new IndexOutOfRangeException("Memory overflow");

        // memory layout:
        //  - game call (6 floats)
        //  - hand (16 floats)
        //  - turn history (64 floats)
        //  - augen (4 floats)

        int actingPlayer = t < 32 ? turnHistory[t].PlayerId : t & 0x3;
        var call = normCalls[actingPlayer];

        fixed (double* stateArr = &state[0])
        {
            int offset = 0;
            offset += serializeGameCall(stateArr, call);
            offset += serializeHand(stateArr + offset, hand);
            offset += serializeTurnHistory(
                stateArr + offset, turnHistory, Math.Min(t, 31), actingPlayer);
            serializeAugen(stateArr + offset, augen, actingPlayer);
        }
    }

    private unsafe int serializeGameCall(
        double* stateArr, GameCall call)
    {
        int p = 0;
        stateArr[p++] = encode(call.Mode);
        stateArr[p++] = encode(call.IsTout);
        stateArr[p++] = (double)call.CallingPlayerId / 4;
        stateArr[p++] = (double)call.PartnerPlayerId / 4;
        stateArr[p++] = encode(call.Trumpf);
        stateArr[p++] = encode(call.GsuchteFarbe);
        return 6;
    }

    private unsafe int serializeHand(double* stateArr, Hand hand)
    {
        int p = 0;
        foreach (var card in hand)
            p += serializeCard(stateArr, card);
        while (p < 16)
            stateArr[p++] = NO_CARD;
        return 16;
    }

    private unsafe int serializeTurnHistory(
        double* stateArr, ReadOnlySpan<GameAction> turnHistory, int t, int p_id)
    {
        int offset = 0;
        for (int i = 0; i < 64; i++)
            stateArr[offset++] = NO_CARD;

        for (int i = 0; i < t; i++)
        {
            var action = turnHistory[i];
            int norm_pid = normPlayerId(action.PlayerId, p_id);
            offset = ((i & ~0x3) + norm_pid) * 2;
            serializeCard(stateArr + offset, turnHistory[i].CardPlayed);
        }
        return 64;
    }

    private unsafe int serializeAugen(
        double* stateArr, int[] scores, int p_id)
    {
        for (int i = 0; i < 4; i++)
            stateArr[i] = (double)scores[(p_id + i) & 0x3] / 120;
        return 4;
    }

    private unsafe int serializeCard(double* stateArr, Card card)
    {
        stateArr[0] = encode(card.Type);
        stateArr[1] = encode(card.Color);
        return 2;
    }

    private unsafe int encodeOnehot(double* stateArr, GameMode mode)
        => encodeOnehot(stateArr, (int)mode, 4);

    private unsafe int encodeOnehot(double* stateArr, CardColor color)
        => encodeOnehot(stateArr, (int)color, 4);

    private unsafe int encodeOnehot(double* stateArr, CardType type)
        => encodeOnehot(stateArr, (int)type, 8);

    private unsafe int encodeOnehot(double* stateArr, int id, int numClasses)
    {
        for (int i = 0; i < numClasses; i++)
            stateArr[i] = 0;
        stateArr[id] = 1;
        return numClasses;
    }

    private unsafe int encodeOnehot(double* stateArr, bool flag)
    {
        stateArr[0] = flag ? 1 : -1;
        return 1;
    }

    private double encode(GameMode mode) => (double)mode / 4;
    private double encode(CardColor color) => (double)color / 4;
    private double encode(CardType type) => (double)type / 8;
    private double encode(bool flag) => flag ? 1 : 0;

    private GameCall normCallForPlayer(GameCall call, int p_id)
    {
        if (call.Mode == GameMode.Weiter)
            return call;

        int callingPlayer = normPlayerId(call.CallingPlayerId, p_id);
        int partnerPlayer = normPlayerId(call.PartnerPlayerId, p_id);

        if (call.Mode == GameMode.Sauspiel)
            return GameCall.Sauspiel(callingPlayer, partnerPlayer, call.GsuchteFarbe);
        else if (call.Mode == GameMode.Wenz)
            return GameCall.Wenz(callingPlayer, call.IsTout);
        else // if (call.Mode == GameMode.Solo)
            return GameCall.Solo(callingPlayer, call.Trumpf, call.IsTout);
    }

    private int normPlayerId(int id, int offset)
        => (id - offset + 4) & 0x03;
}

public class GameReward
{
    public double Reward(GameLog log, int playerId)
    {
        // intention of this reward system:
        // - players receive reward 1 as soon as they are in a winning state
        // - if they are in a losing or undetermined state, they receive reward 0

        // info: players don't know yet who the sauspiel partner is
        //       -> no reward, even if it's already won
        var currentTurn = log.Turns[log.CardCount / 4];
        if (log.Call.Mode == GameMode.Sauspiel && !currentTurn.AlreadyGsucht)
            return 0;

        bool isCaller = log.CallerIds.Contains(playerId);
        var augen = log.UnrollAugen().Last();
        double callerScore = log.CallerIds.ToArray()
            .Select(i => augen[i]).Sum();

        if (log.Call.Mode != GameMode.Sauspiel && log.Call.IsTout)
            return isCaller && callerScore == 120 ? 1 : 0;
        else
            return (isCaller && callerScore >= 61) || !isCaller ? 1 : 0;
    }
}
