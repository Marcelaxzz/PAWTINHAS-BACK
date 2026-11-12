using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Application.Configuracao;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

public sealed record DadosClinicaDto([Required, StringLength(80)] string Nome, [StringLength(160)] string? Endereco,
    [StringLength(20)] string? Telefone, [StringLength(18)] string? Cnpj);

public sealed record NotificacoesDto(bool ResumoDiarioEmail, bool AlarmesEsp32);

public sealed record IntegracoesDto(bool BackupAutomatico);

public sealed record ConfiguracaoDto(
    int Id,
    [Required] DadosClinicaDto Clinica,
    [Required] NotificacoesDto Notificacoes,
    [Required] IntegracoesDto Integracoes,
    DateTime? AtualizadoEm = null)
{
    public static ConfiguracaoDto FromEntity(ConfiguracaoClinica c) => new(
        c.Id,
        new DadosClinicaDto(c.NomeClinica, c.Endereco, c.Telefone, c.Cnpj),
        new NotificacoesDto(c.ResumoDiarioEmail, c.AlarmesEsp32),
        new IntegracoesDto(c.BackupAutomatico),
        c.AtualizadoEm);
}

/// <summary>Situação real das integrações (o que a tela de configurações mostra como status).</summary>
public sealed record StatusIntegracoesDto(
    bool DispositivoConfigurado,
    DateTime? DispositivoUltimoContato,
    DateTime? UltimoBackup,
    DateTime? UltimoResumo,
    bool EmailConfigurado,
    string? EmailResumo,
    int BackupsSalvos);

public class ConfiguracaoAppService(
    IConfiguracaoRepository repository,
    IUnitOfWork unitOfWork,
    IServicoBackup backup,
    IEstadoEmail email,
    IOptions<DispositivoOptions> dispositivo,
    IOptions<AutenticacaoOptions> autenticacao,
    TimeProvider relogio)
{
    public async Task<ConfiguracaoDto> ObterAsync() => ConfiguracaoDto.FromEntity(await repository.ObterAsync());

    public async Task<ConfiguracaoDto> AtualizarAsync(ConfiguracaoDto dto)
    {
        var config = await repository.ObterAsync();
        config.Atualizar(dto.Clinica.Nome, dto.Clinica.Endereco, dto.Clinica.Telefone, dto.Clinica.Cnpj,
            dto.Notificacoes.ResumoDiarioEmail, dto.Notificacoes.AlarmesEsp32, dto.Integracoes.BackupAutomatico,
            relogio.GetLocalNow().DateTime);
        await unitOfWork.CommitAsync();
        return ConfiguracaoDto.FromEntity(config);
    }

    public async Task<StatusIntegracoesDto> StatusAsync()
    {
        var config = await repository.ObterAsync();
        return new StatusIntegracoesDto(
            DispositivoConfigurado: !string.IsNullOrEmpty(dispositivo.Value.Chave),
            DispositivoUltimoContato: config.DispositivoUltimoContato,
            UltimoBackup: config.UltimoBackup,
            UltimoResumo: config.UltimoResumo,
            EmailConfigurado: email.Configurado,
            EmailResumo: autenticacao.Value.EmailPermitido,
            BackupsSalvos: backup.Listar().Count);
    }

    /// <summary>Cópia completa dos dados da clínica (JSON).</summary>
    public Task<byte[]> GerarBackupAsync(CancellationToken ct) => backup.GerarAsync(ct);
}
