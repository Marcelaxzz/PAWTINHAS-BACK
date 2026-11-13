using System.Security.Cryptography;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Application.Email;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Domain.Enums;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Autenticacao;

/// <summary>
/// Contas de tutor. Só a veterinária cria: não existe cadastro aberto. A conta nasce com uma senha aleatória
/// que ninguém conhece; o tutor define a dele pelo "Esqueceu a senha?", que confirma o e-mail com um código.
/// </summary>
public sealed class ServicoContas(
    IUsuarioRepository usuarios,
    IResponsavelRepository responsaveis,
    IUnitOfWork unitOfWork,
    IHashSenha senhas,
    IFilaEmails emails,
    ModelosEmail modelos,
    TimeProvider relogio)
{
    public async Task<IReadOnlyList<ContaTutorResposta>> ListarAsync() =>
        (await usuarios.ListarTutoresAsync()).Select(ContaTutorResposta.De).ToList();

    public async Task<ContaTutorResposta> CriarAsync(CriarContaTutorRequisicao req)
    {
        var responsavel = await responsaveis.ObterPorIdAsync(req.ResponsavelId)
            ?? throw new KeyNotFoundException("Tutor não encontrado.");

        var email = Usuario.NormalizarEmail(req.Email);
        if (await usuarios.ObterPorEmailAsync(email) is not null)
            throw new ConflitoException("email_em_uso", "Já existe uma conta com este e-mail.");
        if (await usuarios.ObterPorResponsavelAsync(responsavel.Id) is not null)
            throw new ConflitoException("tutor_com_conta", "Este tutor já tem uma conta de acesso.");

        // O nome do tutor tem até 150 caracteres; o da conta, 80.
        var nome = responsavel.NomeCompleto.Length > 80 ? responsavel.NomeCompleto[..80] : responsavel.NomeCompleto;
        var senhaInicial = senhas.Gerar(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        var conta = Usuario.NovoTutor(nome, email, senhaInicial, responsavel.Id, relogio.GetUtcNow().UtcDateTime);

        await usuarios.AdicionarAsync(conta);
        await unitOfWork.CommitAsync();

        emails.Enfileirar(modelos.ContaCriada(conta.Email, conta.Nome));
        return ContaTutorResposta.De(conta);
    }

    /// <summary>Exclui a conta (as sessões saem junto, em cascata). A conta da veterinária nunca sai por aqui.</summary>
    public async Task<bool> ExcluirAsync(int id)
    {
        var conta = await usuarios.ObterPorIdAsync(id);
        if (conta is null || conta.Papel != PapelUsuario.TUTOR) return false;

        usuarios.Remover(conta);
        await unitOfWork.CommitAsync();
        return true;
    }
}