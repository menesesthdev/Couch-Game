namespace CoachGame.Domain.Calendario;

/// <summary>Ato da temporada competitiva (ex.: "V26 · Ato V"), com início e fim em UTC.</summary>
public sealed record AtoCompetitivo(string Nome, DateTimeOffset Inicio, DateTimeOffset Fim)
{
    public bool EmAndamento(DateTimeOffset agora) => Inicio <= agora && agora < Fim;
}
