using ValorantCoach.Domain.Miras;

namespace ValorantCoach.Api.Dtos;

/// <summary>Um conjunto de linhas, em unidades do próprio jogo.</summary>
public sealed record LinhasResponse(
    bool Aparece,
    double Comprimento,
    double ComprimentoVertical,
    double Espessura,
    double Deslocamento,
    double Opacidade)
{
    public static LinhasResponse De(LinhasMira l) =>
        new(l.Aparece, l.Comprimento, l.ComprimentoVertical, l.Espessura, l.Deslocamento, l.Opacidade);
}

public sealed record PontoResponse(bool Aparece, double Espessura, double Opacidade)
{
    public static PontoResponse De(PontoCentral p) => new(p.Aparece, p.Espessura, p.Opacidade);
}

public sealed record ContornoResponse(bool Visivel, double Espessura, double Opacidade)
{
    public static ContornoResponse De(ContornoMira c) => new(c.Visivel, c.Espessura, c.Opacidade);
}

/// <summary>
/// O código já decodificado. Vai pronto para a tela desenhar a mira em SVG — assim o parser do
/// formato da Riot existe num lugar só, aqui no domínio, em vez de repetido no frontend.
/// </summary>
public sealed record DesenhoMiraResponse(
    string Cor,
    ContornoResponse Contorno,
    PontoResponse Ponto,
    LinhasResponse Internas,
    LinhasResponse Externas)
{
    public static DesenhoMiraResponse De(ConfiguracaoMira c) =>
        new(c.Cor, ContornoResponse.De(c.Contorno), PontoResponse.De(c.Ponto),
            LinhasResponse.De(c.Internas), LinhasResponse.De(c.Externas));
}

public sealed record MiraResponse(
    int Id,
    string Nome,
    string Codigo,
    TipoMira Tipo,
    int Copias,
    int CopiasNaSemana,
    DesenhoMiraResponse Desenho)
{
    public static MiraResponse De(Mira m) =>
        new(m.Id, m.Nome, m.Codigo, m.Tipo, m.Copias, m.CopiasNaSemana, DesenhoMiraResponse.De(m.Configuracao));
}
