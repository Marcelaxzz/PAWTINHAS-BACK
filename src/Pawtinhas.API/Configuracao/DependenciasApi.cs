using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pawtinhas.API.Http;
using Pawtinhas.API.Middlewares;
using Pawtinhas.Application.Autenticacao;
using Pawtinhas.Infrastructure.Configuracao;
using Pawtinhas.Infrastructure.Seguranca;

namespace Pawtinhas.API.Configuracao;

public static class DependenciasApi
{
    /// <summary>Controllers, autenticação por cookie (JWT), autorização, limites de requisição e CORS.</summary>
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<CookiesSessao>();
        services.AddMemoryCache();
        services.AddHostedService<DescobertaDispositivo>();

        services
            .AddControllers()
            .AddJsonOptions(o =>
            {
                // Enums em texto ("ATIVO", "PEQUENO") e números aceitos também como texto vindo de formulários.
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
            })
            .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = contexto =>
            {
                var campos = contexto.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .GroupBy(e => Erros.Campo(e.Key))
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().Value!.Errors[0] is { Exception: null, ErrorMessage.Length: > 0 } erro
                            ? erro.ErrorMessage
                            : "Valor em formato inválido.");
                return new BadRequestObjectResult(new RespostaErro("Confira os campos destacados.", "validacao", campos));
            });

        AddAutenticacao(services);

        var limites = config.GetSection(LimitesRequisicao.Secao).Get<LimitesRequisicao>() ?? new LimitesRequisicao();
        services.AddRateLimiter(o => LimitesRequisicao.Configurar(o, limites));

        var origensCors = OrigensCors(config);
        if (origensCors.Length > 0)
        {
            services.AddCors(o => o.AddDefaultPolicy(p => p
                .WithOrigins(origensCors)
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .WithHeaders("Content-Type", "Accept", "X-Requested-With", ProtecaoCsrfMiddleware.Cabecalho)
                .WithExposedHeaders(ProtecaoCsrfMiddleware.Cabecalho, "Content-Disposition")
                .AllowCredentials()));
        }

        return services;
    }

    public static string[] OrigensCors(IConfiguration config) =>
        config.GetSection(CorsOptions.Secao).Get<CorsOptions>()?.Origens ?? [];

    /// <summary>JWT lido do cookie httpOnly. Tudo exige a sessão da veterinária, exceto o que for [AllowAnonymous].</summary>
    private static void AddAutenticacao(IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((o, jwtOpcoes) =>
            {
                var jwt = jwtOpcoes.Value;
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Emissor,
                    ValidAudience = jwt.Audiencia,
                    IssuerSigningKey = GeradorTokenAcesso.Chave(jwt),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };
                o.Events = new JwtBearerEvents
                {
                    // O token vem só do cookie httpOnly; o cabeçalho Authorization não é aceito.
                    OnMessageReceived = c =>
                    {
                        c.Token = c.Request.Cookies[CookiesSessao.CookieAcesso];
                        return Task.CompletedTask;
                    },
                    OnChallenge = async c =>
                    {
                        c.HandleResponse();
                        await Erros.EscreverAsync(c.HttpContext, 401, "Sua sessão expirou. Entre novamente.", "nao_autenticado");
                    },
                    OnForbidden = c => Erros.EscreverAsync(c.HttpContext, 403, "Você não tem permissão para esta ação.", "proibido"),
                };
            });

        var politicaVeterinaria = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole(Papeis.Veterinaria).Build();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(politicaVeterinaria)
            .SetDefaultPolicy(politicaVeterinaria);
    }
}
