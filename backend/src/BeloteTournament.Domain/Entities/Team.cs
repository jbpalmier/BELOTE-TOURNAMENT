namespace BeloteTournament.Domain.Entities;

public sealed class Team
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }

    private Team()
    {
        Name = string.Empty;
    } // EF

    public Team(string name)
    {
        Name = NormalizeName(name);
    }

    public void Rename(string newName) => Name = NormalizeName(newName);

    private static string NormalizeName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();

        if (trimmed.Length < 2)
            throw new ArgumentException("Le nom d'équipe doit contenir au moins 2 caractères.");

        if (trimmed.Length > 40)
            throw new ArgumentException("Le nom d'équipe ne doit pas dépasser 40 caractères.");

        return trimmed;
    }
}
