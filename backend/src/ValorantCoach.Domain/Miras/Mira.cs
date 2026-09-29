namespace ValorantCoach.Domain.Miras;

/// <summary>
/// Como a mira se parece, que é o que separa as coleções na tela. Sai do próprio código —
/// ninguém marca isso na mão, senão a classificação e o desenho poderiam discordar.
/// </summary>
public enum TipoMira
{
    /// <summary>Só linhas: o "X" clássico, sem ponto no meio.</summary>
    Cruz,

    /// <summary>Só o ponto central, sem nenhuma linha.</summary>
    Ponto,

    /// <summary>Linhas e ponto ao mesmo tempo.</summary>
    CruzComPonto,
}

/// <summary>
/// Um dos dois conjuntos de linhas (internas ou externas) da mira.
///
/// O jogo permite destravar os comprimentos, e aí as linhas de cima e de baixo têm tamanho
/// próprio — é assim que se faz uma mira só horizontal, por exemplo. Por isso são dois campos.
/// </summary>
public sealed record LinhasMira(
    bool Visivel,
    double Comprimento,
    double ComprimentoVertical,
    double Espessura,
    double Deslocamento,
    double Opacidade)
{
    /// <summary>
    /// Estar ligada não basta: comprimento, espessura ou opacidade zerados deixam a linha
    /// invisível na prática, e é a aparência que decide a coleção.
    /// </summary>
    public bool Aparece =>
        Visivel && Espessura > 0 && Opacidade > 0 && (Comprimento > 0 || ComprimentoVertical > 0);
}

/// <summary>O ponto central. É ele que separa uma mira "de ponto" de uma mira "de cruz".</summary>
public sealed record PontoCentral(bool Visivel, double Espessura, double Opacidade)
{
    public bool Aparece => Visivel && Espessura > 0 && Opacidade > 0;
}

/// <summary>Contorno preto que a Riot desenha em volta de tudo, para a mira não sumir no claro.</summary>
public sealed record ContornoMira(bool Visivel, double Espessura, double Opacidade);

/// <summary>
/// A mira já decodificada: o bastante para desenhá-la e para dizer de que tipo ela é.
/// Só o perfil primário interessa — mira de mira telescópica e de ADS não aparecem na galeria.
/// </summary>
public sealed record ConfiguracaoMira(
    string Cor,
    ContornoMira Contorno,
    PontoCentral Ponto,
    LinhasMira Internas,
    LinhasMira Externas)
{
    public bool TemLinhas => Internas.Aparece || Externas.Aparece;

    public TipoMira Tipo => (TemLinhas, Ponto.Aparece) switch
    {
        (true, true) => TipoMira.CruzComPonto,
        (true, false) => TipoMira.Cruz,
        (false, true) => TipoMira.Ponto,
        // Nada visível é raro (mira "invisível", usada de brincadeira). Cai em Cruz para não
        // poluir a coleção de ponto, que é onde alguém procura justamente por um ponto.
        (false, false) => TipoMira.Cruz,
    };
}

/// <summary>
/// Uma mira da galeria. <see cref="Codigo"/> é o texto que o jogador cola no Valorant.
///
/// O <see cref="Tipo"/> vem gravado junto, e não calculado na leitura, porque é por ele que a
/// galeria filtra as coleções — como coluna, o banco resolve o filtro; como propriedade
/// calculada, seria preciso trazer tudo e peneirar na memória. Quem grava é a sincronização,
/// a partir do próprio código, então os dois nunca discordam.
/// </summary>
public sealed record Mira(
    int Id,
    string Nome,
    string Codigo,
    TipoMira Tipo,
    int Copias,
    int CopiasNaSemana)
{
    private ConfiguracaoMira? _configuracao;

    /// <summary>
    /// O código já decodificado, para desenhar a mira. Fica fora do banco de propósito: são
    /// cinco registros aninhados que o código sozinho reconstrói, e decodificar é barato.
    /// </summary>
    public ConfiguracaoMira Configuracao => _configuracao ??= CodigoMira.Analisar(Codigo);
}
