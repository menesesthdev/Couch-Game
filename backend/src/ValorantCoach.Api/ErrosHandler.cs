using ValorantCoach.Application.Portas;
using ValorantCoach.Domain.Comum;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ValorantCoach.Api;

/// <summary>Traduz exceções de domínio/aplicação em ProblemDetails com mensagem legível para a UI.</summary>
public class ErrosHandler(IProblemDetailsService problemDetails, ILogger<ErrosHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var status = ex switch
        {
            DomainException => StatusCodes.Status400BadRequest,
            JogadorNaoEncontradoException => StatusCodes.Status404NotFound,
            SessaoRiotInvalidaException => StatusCodes.Status401Unauthorized,
            FonteIndisponivelException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError,
        };
        if (status == StatusCodes.Status500InternalServerError) logger.LogError(ex, "Erro não tratado");
        else logger.LogWarning("{Tipo}: {Mensagem}", ex.GetType().Name, ex.Message);

        ctx.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = status == 500 ? "Algo deu errado do nosso lado." : ex.Message,
            },
        });
    }
}
