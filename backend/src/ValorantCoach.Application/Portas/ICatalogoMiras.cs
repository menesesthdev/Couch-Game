namespace ValorantCoach.Application.Portas;

/// <summary>Uma mira como a fonte externa entrega, antes de decodificarmos o código.</summary>
public sealed record MiraImportada(int IdExterno, string Nome, string Codigo, int Copias, int CopiasNaSemana);

/// <summary>
/// Catálogo de miras de pro player. Hoje vem do vcrdb.net, que mantém a lista atualizada
/// quando um pro troca de mira — por isso importamos em vez de manter uma lista na mão.
///
/// Não é um endpoint documentado: pode mudar sem aviso. Quem chama deve tratar a falha como
/// "não atualizou agora", nunca como motivo para deixar a galeria vazia — o que já foi
/// importado continua no nosso banco.
/// </summary>
public interface ICatalogoMiras
{
    Task<IReadOnlyList<MiraImportada>> ObterMirasDeProsAsync(CancellationToken ct);
}
