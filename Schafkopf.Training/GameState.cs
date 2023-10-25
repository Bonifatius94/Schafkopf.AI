using System.Diagnostics.CodeAnalysis;

namespace Schafkopf.Training;

public struct GameHistory
{
    public GameHistory() { }

    public void Load(GameLog completeGame)
    {
        Call = completeGame.Call;
        for (int i = 0; i < 8; i++)
            History[i] = completeGame.Turns[i];
        for (int i = 0; i < 4; i++)
            InitialHands[i] = completeGame.InitialHands[i];
    }

    public GameCall Call = GameCall.Weiter();
    public Turn[] History = new Turn[8];
    public Hand[] InitialHands = new Hand[4];

    public IEnumerable<GameAction> UnrollActions()
    {
        var turnCache = new Card[4];

        foreach (var turn in History)
        {
            int p_id = turn.FirstDrawingPlayerId;
            turn.CopyCards(turnCache);

            for (int i = 0; i < 4; i++)
            {
                var card = turnCache[p_id];
                var action = new GameAction() {
                    PlayerId = (byte)p_id,
                    CardPlayed = card
                };
                yield return action;
                p_id = (p_id + 1) % 4;
            }
        }
    }

    public IEnumerable<Hand> UnrollHands()
    {
        var hands = InitialHands.ToArray();
        foreach (var action in UnrollActions())
        {
            yield return hands[action.PlayerId];
            hands[action.PlayerId] = hands[action.PlayerId].Discard(action.CardPlayed);
        }
        yield return hands[0];
    }

    public IEnumerable<int[]> UnrollAugen()
    {
        var augen = new int[4];
        foreach (var turn in History)
        {
            yield return augen;
            augen[turn.WinnerId] += turn.Augen;
        }
        yield return augen;
    }
}

public struct GameAction
{
    public Card CardPlayed { get; set; }
    public byte PlayerId { get; set; }

    public static readonly GameAction NO_OP
        = new GameAction() { CardPlayed = new Card(0), PlayerId = 0xFF };

    public override int GetHashCode()
        => CardPlayed.Id | (PlayerId << 8);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj?.GetType() == typeof(GameAction) && obj.GetHashCode() == this.GetHashCode();

    public static bool operator ==(GameAction a1, GameAction a2)
        => a1.CardPlayed == a2.CardPlayed && a1.PlayerId == a2.PlayerId;

    public static bool operator !=(GameAction a1, GameAction a2)
        => a1.CardPlayed != a2.CardPlayed || a1.PlayerId != a2.PlayerId;
}

public struct GameState
{
    public GameState() { }

    public double[] State = new double[90];
}

public class GameStateSerializer
{
    private const int NO_CARD = -1;

    public GameState[] NewBuffer()
        => Enumerable.Range(0, 33).Select(x => new GameState()).ToArray();

    public void Serialize(GameHistory completedGame, GameState[] states)
    {
        var hands = completedGame.UnrollHands().GetEnumerator();
        var scores = completedGame.UnrollAugen().GetEnumerator();
        var allActions = completedGame.UnrollActions().ToArray();

        int t = 0;
        foreach (var turn in completedGame.History)
        {
            scores.MoveNext();

            for (int i = 0; i < 4; i++)
            {
                hands.MoveNext();

                serializeState(
                    states[t], completedGame.Call, hands.Current,
                    t, allActions, scores.Current);

                t++;
            }
        }

        // TODO: think about 33rd state encoding, all hands are empty, just augen matter
        hands.MoveNext();
        scores.MoveNext();
        serializeState(
            states[t], completedGame.Call, hands.Current,
            t, allActions, scores.Current);
    }

    private unsafe void serializeState(
        GameState state, GameCall call, Hand hand, int t,
        ReadOnlySpan<GameAction> turnHistory, int[] augen)
    {
        // memory layout:
        //  - game call (6 floats)
        //  - hand (16 floats)
        //  - turn history (64 floats)
        //  - augen (4 floats)

        fixed (double* stateArr = &state.State[0])
        {
            // TODO: normalize the state such that the acting player always has id=0
            //       -> training should converge a lot faster
            serializeGameCall(stateArr, call);
            serializeHand(stateArr + 6, hand);
            serializeTurnHistory(stateArr + 22, turnHistory, t);
            serializeAugen(stateArr + 86, augen);
        }
    }

    private unsafe void serializeGameCall(double* stateArr, GameCall call)
    {
        int p = 0;
        stateArr[p++] = encode(call.Mode);
        stateArr[p++] = encode(call.IsTout);
        stateArr[p++] = (double)call.CallingPlayerId / 4;
        stateArr[p++] = (double)call.PartnerPlayerId / 4;
        stateArr[p++] = encode(call.Trumpf);
        stateArr[p++] = encode(call.GsuchteFarbe);
    }

    private unsafe void serializeHand(double* stateArr, Hand hand)
    {
        int p = 0;
        for (int i = 0; i < hand.CardsCount; i++)
        {
            var card = hand[i];
            if (card.Exists)
            {
                stateArr[p++] = encode(card.Type);
                stateArr[p++] = encode(card.Color);
            }
        }
        while (p < 16)
            stateArr[p++] = NO_CARD;
    }

    private unsafe void serializeTurnHistory(
        double* stateArr, ReadOnlySpan<GameAction> cachedHistory, int t)
    {
        int p = 0;
        for (int i = 0; i < t; i++)
        {
            var action = cachedHistory[i];
            stateArr[p++] = encode(action.CardPlayed.Color);
            stateArr[p++] = encode(action.CardPlayed.Type);
        }
        while (p < 64)
            stateArr[p++] = NO_CARD;
    }

    private unsafe void serializeAugen(double* stateArr, int[] scores)
    {
        int p = 0;
        for (int i = 0; i < 4; i++)
            stateArr[p++] = (double)scores[i] / 120;
    }

    private double encode(GameMode mode) => (double)mode / 4;
    private double encode(CardColor color) => (double)color / 4;
    private double encode(CardType type) => (double)type / 8;
    private double encode (bool flag) => flag ? 1 : 0;
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
        if (log.Call.Mode == GameMode.Sauspiel && !log.CurrentTurn.AlreadyGsucht)
            return 0;

        bool isCaller = log.CallerIds.Contains(playerId);
        double callerScore = log.CallerIds
            .Select(i => log.Scores[i]).Sum();

        if (log.Call.Mode != GameMode.Sauspiel && log.Call.IsTout)
            return isCaller && callerScore == 120 ? 1 : 0;
        else
            return (isCaller && callerScore >= 61) || !isCaller ? 1 : 0;
    }
}
