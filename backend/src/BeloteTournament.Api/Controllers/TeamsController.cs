using BeloteTournament.Domain.Entities;
using BeloteTournament.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeloteTournament.Api.Controllers;

[ApiController]
[Route("api/teams")]
public sealed class TeamsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TeamsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var teams = await _db.Teams.OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(ct);

        return Ok(teams);
    }

    public sealed record CreateTeamRequest(string Name);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest req, CancellationToken ct)
    {
        if (req is null)
            return BadRequest("Payload manquant.");

        var team = new Team(req.Name);
        _db.Teams.Add(team);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Conflict("Une équipe avec ce nom existe déjà.");
        }

        return Ok(new { team.Id, team.Name });
    }

    public sealed record UpdateTeamRequest(string Name);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTeamRequest req,
        CancellationToken ct
    )
    {
        if (req is null)
            return BadRequest("Payload manquant.");

        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (team is null)
            return NotFound();

        team.Rename(req.Name);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Conflict("Une équipe avec ce nom existe déjà.");
        }

        return Ok(new { team.Id, team.Name });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (team is null)
            return NotFound();

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ✅ Supprimer toutes les équipes
    [HttpDelete]
    public async Task<IActionResult> DeleteAll(CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM Teams;", ct);
        return NoContent();
    }
}
