using BeloteTournament.Domain.Services;
using BeloteTournament.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeloteTournament.Api.Controllers;

[ApiController]
[Route("api/schedule")]
public sealed class ScheduleController : ControllerBase
{
    private readonly AppDbContext _db;

    public ScheduleController(AppDbContext db) => _db = db;

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(CancellationToken ct)
    {
        var teams = await _db.Teams.AsNoTracking().ToListAsync(ct);

        try
        {
            var generator = new ScheduleGenerator();
            var rounds = generator.Generate(teams);

            // Dictionnaire pour affichage lisible
            var teamNames = teams.ToDictionary(t => t.Id, t => t.Name);

            return Ok(
                new
                {
                    rounds = rounds.Select(r => new
                    {
                        round = r.RoundNumber,
                        matches = r.Matches.Select(m => new
                        {
                            table = m.TableNumber,

                            teamA = new { id = m.TeamAId, name = teamNames[m.TeamAId] },
                            teamB = new { id = m.TeamBId, name = teamNames[m.TeamBId] }
                        })
                    })
                }
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
