namespace BeloteTournament.Domain.Entities;

public sealed class Match
{
    public Guid TeamAId { get; }
    public Guid TeamBId { get; }
    public int TableNumber { get; }

    public Match(Guid teamAId, Guid teamBId, int tableNumber)
    {
        if (teamAId == teamBId)
            throw new ArgumentException("Une équipe ne peut pas jouer contre elle-même.");

        TeamAId = teamAId;
        TeamBId = teamBId;
        TableNumber = tableNumber;
    }
}
