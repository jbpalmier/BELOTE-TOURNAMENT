namespace BeloteTournament.Domain.Entities;

public sealed class Round
{
    public int RoundNumber { get; }
    public IReadOnlyList<Match> Matches { get; }

    public Round(int roundNumber, IReadOnlyList<Match> matches)
    {
        RoundNumber = roundNumber;
        Matches = matches;
    }
}
