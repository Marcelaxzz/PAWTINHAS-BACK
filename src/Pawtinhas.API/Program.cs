
using Pawtinhas.API.Cli;
using Pawtinhas.API.Configuracao;
using Pawtinhas.API.Middlewares;
using Pawtinhas.Application;
using Pawtinhas.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var executandoComando = Comandos.Reconhece(args);

builder.Services.AddApi(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment, executandoComando);

builder.WebHost.ConfigureKestrel(k =>
{
    k.AddServerHeader = false;
    k.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});

var app = builder.Build();

// ---------- Comandos de manutenção (migrar, usuario, dados-exemplo, importar) ----------
if (executandoComando)
{
    return await Comandos.ExecutarAsync(app.Services, args);
}

if (app.Configuration.GetValue<bool>("BancoDeDados:MigrarAoIniciar"))
{
    await app.Services.MigrarBancoAsync();
}

// ---------- Pipeline ----------
if (ProxyReversoOptions.Opcoes(app.Configuration) is { } proxy) app.UseForwardedHeaders(proxy);

app.UseMiddleware<TratamentoErrosMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<CabecalhosSegurancaMiddleware>();

// Em produção, o build do frontend (dist) pode ser servido pela própria API a partir de wwwroot.
var temFrontend = File.Exists(Path.Combine(app.Environment.WebRootPath ?? "", "index.html"));
if (temFrontend) app.UseStaticFiles();

app.UseRouting();
if (DependenciasApi.OrigensCors(app.Configuration).Length > 0) app.UseCors();
app.UseRateLimiter();
app.UseMiddleware<ProtecaoCsrfMiddleware>();
app.UseAuthentication();
app.UseMiddleware<SessaoAtivaMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapFallback("/api/{**rota}", (HttpContext http) =>
        Pawtinhas.API.Http.Erros.EscreverAsync(http, 404, "Recurso não encontrado.", "nao_encontrado"))
    .AllowAnonymous();
if (temFrontend) app.MapFallbackToFile("index.html").AllowAnonymous();

await app.RunAsync();
return 0;
