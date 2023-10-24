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

    public int PlayerId => History[0].FirstDrawingPlayerId;
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

    public void Serialize(GameHistory completedGame, GameState[] states)
    {
        var augen = new int[4];
        var call = completedGame.Call;
        var action = GameAction.NO_OP;
        var cards = new Card[4];

        var hands = new Hand[4];
        for (int i = 0; i < 4; i++)
            hands[i] = completedGame.InitialHands[i];

        var turnHistory = new double[64];
        for (int i = 0; i < 64; i++)
            turnHistory[i] = NO_CARD;

        unsafe
        {
            fixed (double* h = &turnHistory[0])
            {
                int firstPlayer = completedGame.History[0].FirstDrawingPlayerId;
                serializeState(states[0], call, hands[firstPlayer], action, 0, h, augen);

                int t = 0;
                for (int i = 0; i < 8; i++)
                {
                    var turn = completedGame.History[i];
                    int playerId = turn.FirstDrawingPlayerId;
                    turn.CopyCards(cards);

                    for (int j = 0; j < 4; j++)
                    {
                        action.PlayerId = (byte)playerId;
                        action.CardPlayed = cards[playerId];
                        serializeState(states[t+1], call, hands[playerId], action, t, h, augen);
                        hands[playerId].Discard(cards[playerId]);
                        playerId = ++playerId & 0x03;
                        t++;
                    }

                    augen[turn.WinnerId] += turn.Augen;
                }
            }
        }
    }

    private unsafe void serializeState(
        GameState state, GameCall call, Hand hand, GameAction action,
        int t, double* turnHistory, int[] augen)
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
            if (action != GameAction.NO_OP)
                serializeTurnHistory(stateArr + 22, turnHistory, action, t);
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
        double* stateArr, double* cachedHistory, GameAction action, int t)
    {
        int p = t << 1;
        for (int i = 0; i < p; i++)
            stateArr[i] = cachedHistory[i];

        cachedHistory[p] = stateArr[p] = encode(action.CardPlayed.Color);
        cachedHistory[p+1] = stateArr[p+1] = encode(action.CardPlayed.Type);
    }

    private unsafe void serializeAugen(double* stateArr, int[] scores)
    {
        int p = 0;
        for (int i = 0; i < 4; i++)
            stateArr[p++] = scores[i];
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
