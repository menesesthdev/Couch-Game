using Microsoft.AspNetCore.Mvc;
using ValorantCoach.Api.Dtos;
using ValorantCoach.Api.Loja;
using ValorantCoach.Application.Loja;
using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Jogadores;

namespace ValorantCoach.Api.Controllers;

/// <summary>
/// Loja pessoal do jogador. É a única parte da API que depende de conta conectada — todo o resto
/// (perfil, desempenho, estimativa) continua anônimo.
/// </summary>
[ApiController]
[Route("api/loja")]
public class LojaController(
    ConectarConta conectarConta,
    ObterMinhaLoja obterMinhaLoja,
    SessaoLojaStore sessoes) : ControllerBase
{
    /// <summary>Troca a URL de redirecionamento do login da Riot por uma sessão em cookie.</summary>
    [HttpPost("conectar")]
    public async Task<ConexaoLojaResponse> Conectar([FromBody] ConectarRequest pedido, CancellationToken ct)
    {
        var sessao = await conectarConta.ExecutarAsync(pedido.UrlRedirecionamento, pedido.Regiao, ct);
        var id = sessoes.Guardar(sessao);

        Response.Cookies.Append(SessaoLojaStore.NomeCookie, id, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = sessao.ExpiraEm,
        });

        return new ConexaoLojaResponse(true, sessao.ExpiraEm);
    }

    /// <summary>Estado da conexão, para a tela saber se pede login ou mostra a loja.</summary>
    [HttpGet("sessao")]
    public ConexaoLojaResponse Sessao()
    {
        var sessao = sessoes.Obter(Request.Cookies[SessaoLojaStore.NomeCookie]);
        return new ConexaoLojaResponse(sessao is not null, sessao?.ExpiraEm);
    }

    [HttpGet]
    public async Task<MinhaLojaResponse> Minha(CancellationToken ct)
    {
        var sessao = sessoes.Obter(Request.Cookies[SessaoLojaStore.NomeCookie])
            ?? throw new SessaoRiotInvalidaException("Conecte sua conta da Riot para ver a loja.");

        return MinhaLojaResponse.De(await obterMinhaLoja.ExecutarAsync(sessao, ct));
    }

    [HttpPost("desconectar")]
    public IActionResult Desconectar()
    {
        sessoes.Remover(Request.Cookies[SessaoLojaStore.NomeCookie]);
        Response.Cookies.Delete(SessaoLojaStore.NomeCookie);
        return NoContent();
    }
}

public sealed record ConectarRequest(string UrlRedirecionamento, Regiao Regiao);
