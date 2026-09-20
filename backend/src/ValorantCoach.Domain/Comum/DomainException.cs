namespace ValorantCoach.Domain.Comum;

/// <summary>Violação de uma regra de negócio. A API traduz para HTTP 400.</summary>
public class DomainException(string message) : Exception(message);
