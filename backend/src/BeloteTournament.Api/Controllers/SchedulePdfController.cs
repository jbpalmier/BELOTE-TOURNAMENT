using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BeloteTournament.Api.Controllers;

[ApiController]
[Route("api/schedule")]
public sealed class SchedulePdfController : ControllerBase
{
    // DTOs reçus du front
    public sealed record TeamRef(Guid Id, string Name);

    public sealed record MatchDto(int Table, TeamRef TeamA, TeamRef TeamB);

    public sealed record RoundDto(int Round, List<MatchDto> Matches);

    public sealed record SchedulePdfRequest(
        string? TournamentName,
        DateTime? GeneratedAt,
        int? Seed,
        List<RoundDto> Rounds
    );

    [HttpPost("pdf")]
    public IActionResult Pdf([FromBody] SchedulePdfRequest req)
    {
        // 1) Validation minimale (sécurité + robustesse)
        var error = Validate(req);
        if (error is not null)
            return BadRequest(error);

        QuestPDF.Settings.License = LicenseType.Community;

        var title = string.IsNullOrWhiteSpace(req.TournamentName)
            ? "Tournoi de belote — Planning"
            : $"{req.TournamentName.Trim()} — Planning";

        var generatedAt = req.GeneratedAt ?? DateTime.Now;

        var pdfBytes = Document
            .Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Column(col =>
                        {
                            col.Item().Text(title).FontSize(18).SemiBold();
                            col.Item().Text($"Généré le {generatedAt:dd/MM/yyyy HH:mm}");
                            if (req.Seed.HasValue)
                                col.Item().Text($"Seed : {req.Seed.Value}");
                            col.Item().LineHorizontal(1);
                        });

                    page.Content()
                        .Column(col =>
                        {
                            foreach (var round in req.Rounds.OrderBy(r => r.Round))
                            {
                                col.Item()
                                    .PaddingTop(10)
                                    .Text($"Manche {round.Round}")
                                    .FontSize(14)
                                    .SemiBold();

                                col.Item()
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(45); // Table
                                            columns.RelativeColumn(); // Équipe A
                                            columns.RelativeColumn(); // Équipe B
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Element(CellHeader).Text("Table");
                                            header.Cell().Element(CellHeader).Text("Équipe A");
                                            header.Cell().Element(CellHeader).Text("Équipe B");
                                        });

                                        foreach (var match in round.Matches.OrderBy(m => m.Table))
                                        {
                                            table
                                                .Cell()
                                                .Element(CellBody)
                                                .Text(match.Table.ToString());
                                            table.Cell().Element(CellBody).Text(match.TeamA.Name);
                                            table.Cell().Element(CellBody).Text(match.TeamB.Name);
                                        }
                                    });
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("BeloteTournament — Page ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            })
            .GeneratePdf();

        return File(pdfBytes, "application/pdf", "planning-belote.pdf");

        static IContainer CellHeader(IContainer c) =>
            c.DefaultTextStyle(x => x.SemiBold())
                .PaddingVertical(6)
                .PaddingHorizontal(6)
                .Background(Colors.Grey.Lighten3)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1);

        static IContainer CellBody(IContainer c) =>
            c.PaddingVertical(6).PaddingHorizontal(6).Border(1).BorderColor(Colors.Grey.Lighten2);
    }

    private static string? Validate(SchedulePdfRequest req)
    {
        if (req.Rounds is null || req.Rounds.Count == 0)
            return "Rounds manquant.";

        if (req.Rounds.Count != 5)
            return "Le planning doit contenir exactement 5 manches.";

        // Vérif structure + collecte
        var allTeamIds = new HashSet<Guid>();
        var seenPairs = new HashSet<string>(); // "min|max"
        foreach (var r in req.Rounds)
        {
            if (r.Matches is null || r.Matches.Count == 0)
                return $"La manche {r.Round} n'a aucun match.";

            var roundTeams = new HashSet<Guid>();

            foreach (var m in r.Matches)
            {
                if (m.Table <= 0)
                    return $"Table invalide dans la manche {r.Round}.";
                if (m.TeamA is null || m.TeamB is null)
                    return $"Équipe manquante dans la manche {r.Round}.";
                if (m.TeamA.Id == Guid.Empty || m.TeamB.Id == Guid.Empty)
                    return $"Id d'équipe invalide (manche {r.Round}).";
                if (m.TeamA.Id == m.TeamB.Id)
                    return $"Match invalide (même équipe) manche {r.Round}.";
                if (
                    string.IsNullOrWhiteSpace(m.TeamA.Name)
                    || string.IsNullOrWhiteSpace(m.TeamB.Name)
                )
                    return $"Nom d'équipe vide (manche {r.Round}).";

                // Longueurs max (anti abus)
                if (m.TeamA.Name.Length > 60 || m.TeamB.Name.Length > 60)
                    return $"Nom d'équipe trop long (manche {r.Round}).";

                // Une équipe ne joue qu'une fois par manche
                if (!roundTeams.Add(m.TeamA.Id) || !roundTeams.Add(m.TeamB.Id))
                    return $"Une équipe apparaît plusieurs fois dans la manche {r.Round}.";

                allTeamIds.Add(m.TeamA.Id);
                allTeamIds.Add(m.TeamB.Id);

                var a = m.TeamA.Id;
                var b = m.TeamB.Id;
                var min = a.CompareTo(b) < 0 ? a : b;
                var max = a.CompareTo(b) < 0 ? b : a;
                var key = $"{min:N}|{max:N}";

                // Pas de doublon de rencontre
                if (!seenPairs.Add(key))
                    return $"Rencontre dupliquée détectée (manche {r.Round}).";
            }
        }

        // Nombre pair + min 10
        if (allTeamIds.Count < 10)
            return "Minimum 10 équipes requis.";

        if (allTeamIds.Count % 2 != 0)
            return "Le nombre d'équipes doit être pair.";

        return null;
    }
}
