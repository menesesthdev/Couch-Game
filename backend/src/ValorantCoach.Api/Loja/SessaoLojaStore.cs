using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using ValorantCoach.Application.Portas;

namespace ValorantCoach.Api.Loja;

/// <summary>
/// Guarda a sessão da Riot enquanto ela vale, referenciada por um id opaco em cookie HttpOnly.
///
/// Por que não o token direto no cookie: os JWTs da Riot somados passam perto do limite de 4 KB por
/// cookie, e manter o token fora do navegador tira ele do alcance de XSS. Por que memória e não
/// banco: a sessão dura ~1h e é credencial de terceiro — não é coisa que se persista em disco.
/// O preço é que reiniciar a API desconecta todo mundo, o que é aceitável para algo que expira sozinho.
/// </summary>
public class SessaoLojaStore(IMemoryCache cache)
{
    public const string NomeCookie = "vc_loja";

    public string Guardar(SessaoRiot sessao)
    {
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        cache.Set(Chave(id), sessao, sessao.ExpiraEm);
        return id;
    }

    public SessaoRiot? Obter(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : cache.Get<SessaoRiot>(Chave(id));

    public void Remover(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id)) cache.Remove(Chave(id));
    }

    private static string Chave(string id) => $"loja:sessao:{id}";
}
