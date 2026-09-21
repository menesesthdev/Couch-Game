using System.Text.Json.Serialization;
using ValorantCoach.Api;
using ValorantCoach.Api.Loja;
using ValorantCoach.Application;
using ValorantCoach.Infrastructure;
using ValorantCoach.Infrastructure.Persistencia;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ErrosHandler>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [])
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// No Render o TLS termina no proxy: sem isto Request.IsHttps é falso e o cookie da loja sai sem Secure.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddSingleton<SessaoLojaStore>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

// Em Development ou no Docker (Banco__MigrarAoIniciar=true) o schema é atualizado na subida.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Banco:MigrarAoIniciar"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ValorantCoachDbContext>().Database.MigrateAsync();
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseCors();
app.MapControllers();

app.Run();
