using Microsoft.Extensions.Options;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Application.Configuracao;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Domain.Interfaces;
using Pawtinhas.Domain.Seguranca;

namespace Pawtinhas.Application.Autenticacao;

/// <summary>Conta de acesso. Só existe uma: a do e-mail permitido, criada pelo comando "usuario".</summary>
public sealed class ServicoUsuario(
    IUsuarioRepository usuarios,
    IUnitOfWork unitOfWork,
    IHashSenha senhas,
    ServicoSessoes sessoes,
    IOptions<AutenticacaoOptions> opcoes,
    TimeProvider relogio)
{
    /// <summary>Cria a conta ou, se já existir, troca nome e senha e encerra as sessões abertas.</summary>
    /// <returns>O e-mail da conta e se ela acabou de ser criada.</returns>
    public async Task<(string Email, bool Nova)> SalvarAsync(string nome, string senha)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 80)
            throw new DominioException("nome", "Informe o nome (até 80 caracteres).");
        if (PoliticaSenha.ProblemaDeForca(senha) is { } problema) throw new DominioException("senha", problema);

        var email = Usuario.NormalizarEmail(opcoes.Value.EmailPermitido);
        var agora = relogio.GetUtcNow().UtcDateTime;
        var usuario = await usuarios.ObterPorEmailAsync(email);

        if (usuario is null)
        {
            await usuarios.AdicionarAsync(new Usuario(nome, email, senhas.Gerar(senha), agora));
            await unitOfWork.CommitAsync();
            return (email, true);
        }

        usuario.AtualizarNome(nome, agora);
        usuario.DefinirSenha(senhas.Gerar(senha), agora);
        await unitOfWork.CommitAsync();
        await sessoes.RevogarTodasAsync(usuario.Id);
        return (email, false);
    }
}
