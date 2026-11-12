using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Application.Autenticacao;
using Pawtinhas.Domain;
using Pawtinhas.Infrastructure.Configuracao;

namespace Pawtinhas.Infrastructure.Seguranca;

/// <summary>Hash de senha com o PasswordHasher do ASP.NET Core Identity (PBKDF2-SHA512, sal aleatório).</summary>
public sealed class HashSenha : IHashSenha
{
    // O PasswordHasher não usa o usuário no cálculo; um objeto neutro basta.
    private readonly PasswordHasher<object> _hasher = new();
    private readonly object _neutro = new();

    // Usado quando a conta não existe, para que a resposta leve o mesmo tempo.
    private readonly string _hashFicticio;

    public HashSenha() => _hashFicticio = _hasher.HashPassword(_neutro, Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)));

    public string Gerar(string senha) => _hasher.HashPassword(_neutro, senha);

    public bool Conferir(string? hash, string senha)
    {
        var resultado = _hasher.VerifyHashedPassword(_neutro, hash ?? _hashFicticio, senha);
        return hash is not null && resultado != PasswordVerificationResult.Failed;
    }
}

/// <summary>
/// Códigos de 6 dígitos enviados por e-mail. No banco fica apenas um HMAC-SHA256 do código,
/// amarrado ao id do desafio: um vazamento do banco não revela códigos válidos.
/// </summary>
public sealed class ServicoCodigos(IOptions<JwtOptions> jwt) : IServicoCodigos
{
    private readonly byte[] _chave = SHA256.HashData(Encoding.UTF8.GetBytes("pawtinhas:codigos:" + jwt.Value.Chave));

    public string Gerar() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public string Hash(Guid desafioId, string codigo) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(_chave, Encoding.UTF8.GetBytes($"{desafioId:N}:{codigo}")));

    public bool Confere(Guid desafioId, string codigoInformado, string hashGuardado) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(hashGuardado),
            Encoding.ASCII.GetBytes(Hash(desafioId, codigoInformado)));
}

/// <summary>JWT de acesso (HMAC-SHA256), com validade curta e a sessão no claim "sid".</summary>
public sealed class GeradorTokenAcesso(IOptions<JwtOptions> opcoes, TimeProvider relogio) : IGeradorTokenAcesso
{
    private readonly JwtOptions _jwt = opcoes.Value;

    /// <summary>Chave de assinatura; a mesma é usada pela API para validar os tokens recebidos.</summary>
    public static SymmetricSecurityKey Chave(JwtOptions jwt) => new(Encoding.UTF8.GetBytes(jwt.Chave));

    public TokenAcesso Gerar(Usuario usuario, Guid sessao)
    {
        var agora = relogio.GetUtcNow().UtcDateTime;
        var expira = agora.AddMinutes(_jwt.MinutosAcesso);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Emissor,
            Audience = _jwt.Audiencia,
            IssuedAt = agora,
            NotBefore = agora,
            Expires = expira,
            SigningCredentials = new SigningCredentials(Chave(_jwt), SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(Claims.Sessao, sessao.ToString()),
                new Claim("name", usuario.Nome),
                new Claim("role", Papeis.Veterinaria),
            ]),
        };

        return new TokenAcesso(new JsonWebTokenHandler().CreateToken(descritor), expira);
    }
}
