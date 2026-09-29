using ValorantCoach.Application.Miras;

namespace ValorantCoach.Api.Miras;

/// <summary>
/// Mantém a galeria de miras em dia com o vcrdb, em segundo plano.
///
/// Roda pouco depois da subida e depois uma vez por dia. A sincronização na subida existe porque
/// hospedagem que hiberna por inatividade derruba qualquer temporizador: sem ela, um serviço que
/// dorme todo dia nunca chegaria às 24 horas. O custo é uma requisição por reinício a uma página
/// que já vem de CDN — desprezível para eles e para nós.
///
/// Falha aqui nunca derruba a aplicação: a galeria continua servindo o que já está no banco.
/// </summary>
public class SincronizacaoMirasService(
    IServiceScopeFactory escopos,
    ILogger<SincronizacaoMirasService> logger) : BackgroundService
{
    private static readonly TimeSpan EsperaInicial = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Deixa a API responder antes de sair buscando coisa na rede.
        try
        {
            await Task.Delay(EsperaInicial, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Intervalo);
        do
        {
            await SincronizarAsync(ct);
        }
        while (await EsperarProximaAsync(timer, ct));
    }

    private async Task SincronizarAsync(CancellationToken ct)
    {
        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            var sincronizar = escopo.ServiceProvider.GetRequiredService<SincronizarMiras>();

            var resultado = await sincronizar.ExecutarAsync(ct);
            if (resultado.Aplicada)
                logger.LogInformation(
                    "Miras sincronizadas: {Importadas} importadas, {Invalidas} com código inválido.",
                    resultado.Importadas, resultado.Invalidas);
            else
                logger.LogWarning("Miras: a fonte não devolveu nenhuma mira; mantendo o que já estava no banco.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A galeria segue de pé com o que já foi importado, então isto é aviso, não erro fatal.
            logger.LogWarning(ex, "Miras: não foi possível sincronizar agora. Tentando de novo no próximo ciclo.");
        }
    }

    private static async Task<bool> EsperarProximaAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
