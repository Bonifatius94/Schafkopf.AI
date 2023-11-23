namespace Schafkopf.Training;

public class HeuristicAgent : ISchafkopfAIAgent
{
    private Random rng = new Random();
    private HeuristicGameCaller caller =
        new HeuristicGameCaller(new GameMode[] { GameMode.Sauspiel });

    public GameCall MakeCall(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => caller.MakeCall(possibleCalls, position, hand, klopfer);

    public Card ChooseCard(GameLog log, ReadOnlySpan<Card> possibleCards)
        => possibleCards[rng.Next(0, possibleCards.Length)];

    public bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards) => false;
    public bool CallKontra(GameLog log) => false;
    public bool CallRe(GameLog log) => false;
    public void OnGameFinished(GameLog final) => throw new NotImplementedException();
}

public class HeuristicGameCaller
{
    public HeuristicGameCaller(IEnumerable<GameMode> modes)
        => allowedModes = modes;

    private IEnumerable<GameMode> allowedModes;

    public GameCall MakeCall(
        ReadOnlySpan<GameCall> possibleCalls,
        int position, Hand hand, int klopfer)
    {
        if (allowedModes.Contains(GameMode.Solo))
        {
            var call = canCallSolo(possibleCalls, position, hand, klopfer);
            if (call.Mode == GameMode.Solo)
                return call;
        }

        if (allowedModes.Contains(GameMode.Wenz))
        {
            var call = canCallWenz(possibleCalls, position, hand, klopfer);
            if (call.Mode == GameMode.Wenz)
                return call;
        }

        if (allowedModes.Contains(GameMode.Sauspiel))
        {
            var call = canCallSauspiel(possibleCalls, hand);
            if (call.Mode == GameMode.Sauspiel)
                return call;
        }

        return GameCall.Weiter();
    }

    private static readonly CardColor[] rufbareFarben = new CardColor[] {
        CardColor.Schell, CardColor.Gras, CardColor.Eichel };

    private GameCall canCallSauspiel(
        ReadOnlySpan<GameCall> possibleCalls, Hand hand)
    {
        var sauspielCalls = possibleCalls.ToArray()
            .Where(x => x.Mode == GameMode.Sauspiel).ToArray();
        if (!sauspielCalls.Any())
            return GameCall.Weiter();

        hand = hand.CacheTrumpf(sauspielCalls[0].IsTrumpf);

        if (hand.TrumpfCount() < 4)
            return GameCall.Weiter();

        var trumpfOrdered = hand.OrderByDescending(
            x => x, new CardComparer(GameMode.Sauspiel));
        var bestTrumpf = trumpfOrdered.ElementAt(0);
        var secondBestTrumpf = trumpfOrdered.ElementAt(1);

        bool noRennerForOpponents = bestTrumpf.Type == CardType.Ober
            && bestTrumpf.Color >= CardColor.Herz;
        bool twoStammtrumpf = secondBestTrumpf.Type == CardType.Unter
            || secondBestTrumpf.Type == CardType.Ober && noRennerForOpponents;
        bool isFrei = rufbareFarben.Any(x => hand.FarbeCount(x) == 0);
        bool hasFiveOrMoreTrumpf = hand.TrumpfCount() >= 5;

        bool canPlay = noRennerForOpponents && twoStammtrumpf
            && (hasFiveOrMoreTrumpf || isFrei);

        if (!canPlay)
            return GameCall.Weiter();

        return sauspielCalls.OrderBy(x => hand.FarbeCount(x.GsuchteFarbe)).First();
    }

    private GameCall canCallSolo(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => GameCall.Weiter(); // TODO: implement logic for solo decision

    private GameCall canCallWenz(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => GameCall.Weiter(); // TODO: implement logic for wenz decision
}
