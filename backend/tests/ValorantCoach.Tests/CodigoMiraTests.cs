using ValorantCoach.Domain.Comum;
using ValorantCoach.Domain.Miras;

namespace ValorantCoach.Tests;

public class CodigoMiraTests
{
    [Fact]
    public void Codigo_minimo_usa_os_padroes_do_jogo()
    {
        var c = CodigoMira.Analisar("0");

        Assert.Equal("#FFFFFF", c.Cor);
        Assert.True(c.Contorno.Visivel);
        Assert.False(c.Ponto.Visivel);
        // Padrão do jogo: linhas internas e externas ligadas, com comprimento 6 e 2.
        Assert.Equal(6, c.Internas.Comprimento);
        Assert.Equal(2, c.Externas.Comprimento);
        Assert.Equal(TipoMira.Cruz, c.Tipo);
    }

    [Fact]
    public void Le_as_cores_predefinidas_pelo_indice()
    {
        Assert.Equal("#00FFFF", CodigoMira.Analisar("0;P;c;5").Cor);
        Assert.Equal("#FF0000", CodigoMira.Analisar("0;P;c;7").Cor);
    }

    [Fact]
    public void Indice_8_usa_a_cor_personalizada_e_descarta_o_alfa()
    {
        // A chave u vem como RRGGBBAA; a opacidade tem campo próprio, então o AA é ignorado.
        var c = CodigoMira.Analisar("0;c;1;P;c;8;u;E279AFFF;o;0.32;d;1");

        Assert.Equal("#E279AF", c.Cor);
    }

    [Fact]
    public void Separa_linhas_internas_de_externas_pelo_prefixo()
    {
        var c = CodigoMira.Analisar("0;P;0l;4;0o;2;0t;1;1l;9;1o;14;1t;3");

        Assert.Equal(4, c.Internas.Comprimento);
        Assert.Equal(2, c.Internas.Deslocamento);
        Assert.Equal(1, c.Internas.Espessura);
        Assert.Equal(9, c.Externas.Comprimento);
        Assert.Equal(14, c.Externas.Deslocamento);
        Assert.Equal(3, c.Externas.Espessura);
    }

    [Fact]
    public void Sem_destravar_o_comprimento_vertical_acompanha_o_horizontal()
    {
        // v está no código, mas g é 0: o jogo ignora v e usa l nos quatro lados.
        var c = CodigoMira.Analisar("0;P;0l;5;0v;1");

        Assert.Equal(5, c.Internas.Comprimento);
        Assert.Equal(5, c.Internas.ComprimentoVertical);
    }

    [Fact]
    public void Com_g_ligado_as_linhas_verticais_usam_o_proprio_comprimento()
    {
        var c = CodigoMira.Analisar("0;P;0l;13;0v;0;0g;1;1b;0");

        Assert.Equal(13, c.Internas.Comprimento);
        Assert.Equal(0, c.Internas.ComprimentoVertical);
        // Só horizontal ainda é linha: continua sendo uma cruz, não um ponto.
        Assert.True(c.Internas.Aparece);
        Assert.Equal(TipoMira.Cruz, c.Tipo);
    }

    [Fact]
    public void Valores_decimais_nao_dependem_da_cultura_da_maquina()
    {
        var c = CodigoMira.Analisar("0;P;o;0.35;a;0.137");

        Assert.Equal(0.35, c.Contorno.Opacidade, 3);
        Assert.Equal(0.137, c.Ponto.Opacidade, 3);
    }

    [Fact]
    public void Ignora_as_secoes_de_ads_e_de_mira_telescopica()
    {
        // Só o perfil primário aparece na galeria: a cor 7 do bloco A e a do S não podem vazar.
        var c = CodigoMira.Analisar("0;s;1;P;c;5;0l;4;A;c;7;0l;9;S;c;7;o;1");

        Assert.Equal("#00FFFF", c.Cor);
        Assert.Equal(4, c.Internas.Comprimento);
    }

    [Fact]
    public void Chave_desconhecida_nao_desalinha_os_pares_seguintes()
    {
        // "xy" não existe no formato; se o valor dele não fosse consumido, c;5 seria lido errado.
        var c = CodigoMira.Analisar("0;P;xy;9;c;5");

        Assert.Equal("#00FFFF", c.Cor);
    }

    // ---- Classificação: é o que separa as coleções da tela ----

    [Fact]
    public void So_ponto_com_as_linhas_desligadas_e_do_tipo_ponto()
    {
        var c = CodigoMira.Analisar("0;P;h;0;d;1;0b;0;1b;0");

        Assert.True(c.Ponto.Aparece);
        Assert.False(c.TemLinhas);
        Assert.Equal(TipoMira.Ponto, c.Tipo);
    }

    [Fact]
    public void Linhas_sem_ponto_sao_do_tipo_cruz()
    {
        var c = CodigoMira.Analisar("0;P;c;5;h;0;0l;4;0o;2;0a;1;0f;0;1b;0");

        Assert.Equal(TipoMira.Cruz, c.Tipo);
    }

    [Fact]
    public void Linhas_com_ponto_sao_cruz_com_ponto()
    {
        var c = CodigoMira.Analisar("0;P;d;1;0l;3;1b;0");

        Assert.Equal(TipoMira.CruzComPonto, c.Tipo);
    }

    [Fact]
    public void Linha_de_comprimento_zero_nao_conta_como_linha()
    {
        // Ligada, mas sem comprimento nenhum: na tela é um ponto, e é assim que tem de ser listada.
        var c = CodigoMira.Analisar("0;P;d;1;0b;1;0l;0;1b;1;1l;0");

        Assert.False(c.TemLinhas);
        Assert.Equal(TipoMira.Ponto, c.Tipo);
    }

    [Fact]
    public void Linha_com_opacidade_zero_tambem_nao_conta()
    {
        var c = CodigoMira.Analisar("0;P;d;1;0l;4;0a;0;1b;0");

        Assert.Equal(TipoMira.Ponto, c.Tipo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Codigo_vazio_e_recusado(string? codigo)
    {
        Assert.Throws<DomainException>(() => CodigoMira.Analisar(codigo));
    }

    [Fact]
    public void TentarAnalisar_devolve_falso_em_vez_de_estourar()
    {
        Assert.False(CodigoMira.TentarAnalisar("", out _));
        Assert.True(CodigoMira.TentarAnalisar("0;P;d;1", out var c));
        Assert.Equal(TipoMira.CruzComPonto, c.Tipo);
    }
}
