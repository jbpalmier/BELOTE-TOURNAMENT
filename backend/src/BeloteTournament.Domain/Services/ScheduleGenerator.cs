using BeloteTournament.Domain.Entities;

namespace BeloteTournament.Domain.Services;

/// <summary>
/// Génère un planning round-robin aléatoire,
/// sans jamais répéter une rencontre.
/// </summary>
public sealed class ScheduleGenerator
{
    private const int RequiredRounds = 5;
    private const int MinTeams = 10;

    public IReadOnlyList<Round> Generate(IReadOnlyList<Team> teams, int? seed = null)
    {
        if (teams is null)
            throw new ArgumentNullException(nameof(teams));

        if (teams.Count < MinTeams)
            throw new InvalidOperationException($"Minimum {MinTeams} équipes requis.");

        if (teams.Count % 2 != 0)
            throw new InvalidOperationException("Le nombre d'équipes doit être pair.");

        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;

        // 1️⃣ Shuffle initial des équipes
        var teamIds = teams.Select(t => t.Id).OrderBy(_ => rng.Next()).ToList();

        var rounds = new List<Round>();
        int teamCount = teamIds.Count;

        for (int roundIndex = 0; roundIndex < RequiredRounds; roundIndex++)
        {
            var matches = new List<(Guid A, Guid B)>();

            for (int i = 0; i < teamCount / 2; i++)
            {
                var teamA = teamIds[i];
                var teamB = teamIds[teamCount - 1 - i];
                matches.Add((teamA, teamB));
            }

            // 2️⃣ Shuffle des matchs (tables aléatoires)
            matches = matches.OrderBy(_ => rng.Next()).ToList();

            var roundMatches = matches
                .Select((m, index) => new Match(m.A, m.B, tableNumber: index + 1))
                .ToList();

            rounds.Add(new Round(roundIndex + 1, roundMatches));

            // 3️⃣ Rotation circulaire (sans équipe fixe)
            var last = teamIds[^1];
            teamIds.RemoveAt(teamIds.Count - 1);
            teamIds.Insert(1, last);
        }

        return rounds;
    }
}
