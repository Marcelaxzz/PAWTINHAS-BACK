using System.Globalization;
using Pawtinhas.Application.DTOs;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Calculos;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

/// <summary>Dados conferidos para um cálculo de dose: paciente, medicamento e faixa de referência.</summary>
public sealed record ContextoDose(EntradaDose Entrada, Animal? Animal, Medicamento? Medicamento, string Nome, string Via);

public sealed record ContextoAlimentacao(EntradaAlimentacao Entrada, Animal? Animal, Racao? Racao, string Nome);

/// <summary>
/// Monta as entradas dos cálculos a partir do banco: o catálogo prevalece sobre o que o navegador
/// enviou (concentração, faixa de dose, energia da ração).
/// </summary>
public class ResolvedorCalculos(IAnimalRepository animais, IRepository<Medicamento> medicamentos, IRepository<Racao> racoes)
{
    public async Task<ContextoDose> DoseAsync(DoseRequisicao req, bool exigirPaciente)
    {
        var erros = new Dictionary<string, string>();
        var animal = await PacienteAsync(req.AnimalId, exigirPaciente, erros);

        Medicamento? med = null;
        if (req.MedicamentoId is { } medicamentoId)
        {
            med = await medicamentos.ObterPorIdAsync(medicamentoId);
            if (med is null) erros["medicamentoId"] = "Medicamento não encontrado no catálogo.";
        }

        var especie = animal?.Especie ?? req.Especie ?? "";
        var peso = req.PesoKg ?? animal?.PesoKg;
        if (peso is null) erros["pesoKg"] = "Informe o peso do paciente.";
        if (req.DoseMgKg is null) erros["doseMgKg"] = "Informe a dose em mg/kg.";
        var concentracao = med?.Concentracao ?? req.Concentracao;
        if (concentracao is null) erros["concentracao"] = "Informe a concentração do medicamento.";
        var intervalo = req.IntervaloHoras ?? med?.IntervaloPadraoHoras;
        if (intervalo is null) erros["intervaloHoras"] = "Escolha o intervalo entre as doses.";
        if (req.DuracaoDias is null) erros["duracaoDias"] = "Informe a duração do tratamento.";
        ValidacaoException.SeHouver(erros);

        var (min, max) = med is null ? (null, null) : CalculosClinicos.FaixaPara(med, especie);
        var entrada = new EntradaDose(
            Especie: especie,
            PesoKg: peso!.Value,
            DoseMgKg: req.DoseMgKg!.Value,
            Concentracao: concentracao!.Value,
            Unidade: med?.Unidade ?? req.Unidade?.Trim() ?? "",
            Fracionavel: med?.Fracionavel ?? req.Fracionavel,
            IntervaloHoras: intervalo!.Value,
            DuracaoDias: req.DuracaoDias!.Value,
            DoseMinMgKg: min,
            DoseMaxMgKg: max,
            DoseMaximaMg: med?.DoseMaximaMg);

        return new ContextoDose(entrada, animal, med, med?.Nome ?? req.Medicamento?.Trim() ?? "", med?.Via ?? req.Via?.Trim() ?? "");
    }

    public async Task<ContextoAlimentacao> AlimentacaoAsync(AlimentacaoRequisicao req, bool exigirPaciente)
    {
        var erros = new Dictionary<string, string>();
        var animal = await PacienteAsync(req.AnimalId, exigirPaciente, erros);

        Racao? racao = null;
        if (req.RacaoId is { } racaoId)
        {
            racao = await racoes.ObterPorIdAsync(racaoId);
            if (racao is null) erros["racaoId"] = "Ração não encontrada no catálogo.";
        }

        var peso = req.PesoAtualKg ?? animal?.PesoKg;
        if (peso is null) erros["pesoAtualKg"] = "Informe o peso atual do paciente.";
        var kcal = racao?.KcalPorKg ?? req.KcalPorKg;
        if (kcal is null) erros["kcalPorKg"] = "Informe a energia da ração (kcal/kg).";
        if (string.IsNullOrWhiteSpace(req.Condicao)) erros["condicao"] = "Selecione a condição do paciente.";
        ValidacaoException.SeHouver(erros);

        var entrada = new EntradaAlimentacao(
            Especie: animal?.Especie ?? req.Especie ?? "",
            PesoAtualKg: peso!.Value,
            PesoMetaKg: req.PesoMetaKg,
            Condicao: req.Condicao!.Trim(),
            FatorPersonalizado: req.FatorPersonalizado,
            KcalPorKg: kcal!.Value,
            Refeicoes: req.Refeicoes ?? 2);

        return new ContextoAlimentacao(entrada, animal, racao, racao?.Nome ?? req.Racao?.Trim() ?? "");
    }

    private async Task<Animal?> PacienteAsync(int? animalId, bool exigir, Dictionary<string, string> erros)
    {
        if (animalId is { } id)
        {
            var animal = await animais.ObterPorIdAsync(id);
            if (animal is { EhPaciente: true }) return animal;
            erros["animalId"] = "Paciente não encontrado.";
        }
        else if (exigir) erros["animalId"] = "Selecione o paciente.";
        return null;
    }
}

/// <summary>Simulações: a tela mostra o resultado enquanto a veterinária digita, sem salvar nada.</summary>
public class CalculoAppService(ResolvedorCalculos resolvedor)
{
    public static FatoresResposta Fatores() => new(
        TabelaFatores.Todos,
        CalculosClinicos.Intervalos,
        [CalculosClinicos.FonteRer, CalculosClinicos.FonteAgua, CalculosClinicos.FontePetiscos]);

    public async Task<ResultadoDose> SimularDoseAsync(DoseRequisicao req) =>
        CalculosClinicos.Dose((await resolvedor.DoseAsync(req, exigirPaciente: false)).Entrada);

    public async Task<ResultadoAlimentacao> SimularAlimentacaoAsync(AlimentacaoRequisicao req) =>
        CalculosClinicos.Alimentacao((await resolvedor.AlimentacaoAsync(req, exigirPaciente: false)).Entrada);
}

/// <summary>
/// Prescrições: a dose é recalculada a partir do catálogo e só é salva com os alertas confirmados.
/// Opcionalmente cria o alarme do ESP32 para os horários das doses.
/// </summary>
public class PrescricaoAppService(
    IPrescricaoRepository repository,
    IAlarmeRepository alarmes,
    ResolvedorCalculos resolvedor,
    IUnitOfWork unitOfWork,
    TimeProvider relogio)
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public async Task<IReadOnlyList<PrescricaoResponseDto>> ListarAsync(int? animalId) =>
        (await repository.ListarAsync(animalId)).Select(PrescricaoResponseDto.FromEntity).ToList();

    public async Task<PrescricaoResponseDto> CriarAsync(DoseRequisicao req)
    {
        var contexto = await resolvedor.DoseAsync(req, exigirPaciente: true);

        var erros = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(contexto.Nome)) erros["medicamento"] = "Informe o medicamento.";
        if (req.Inicio is null) erros["inicio"] = "Informe a data de início.";
        if (req.HoraPrimeiraDose is null) erros["horaPrimeiraDose"] = "Informe o horário da primeira dose.";
        string? recorrencia = null;
        if (req.CriarAlarme)
        {
            recorrencia = CalculosClinicos.RecorrenciaAlarme(contexto.Entrada.IntervaloHoras);
            if (recorrencia is null) erros["intervaloHoras"] = "O alarme do ESP32 aceita intervalos de 8, 12 ou 24 horas.";
        }
        ValidacaoException.SeHouver(erros);

        var resultado = CalculosClinicos.Dose(contexto.Entrada);
        if (resultado.Alertas.Count > 0 && !req.ConfirmarAlertas)
            throw new ConflitoException("confirmar_alertas", "Revise e confirme os alertas da prescrição antes de salvar.");

        var animal = contexto.Animal!;
        var prescricao = new Prescricao(animal.Id, contexto.Medicamento?.Id, contexto.Nome, contexto.Via, contexto.Entrada,
            resultado, req.Inicio!.Value, req.HoraPrimeiraDose!.Value, req.Observacoes, relogio.GetLocalNow().DateTime);

        if (req.AtualizarPesoPaciente && req.PesoKg is { } novoPeso) animal.AtualizarPeso(novoPeso);

        Alarme? alarme = null;
        if (recorrencia is not null)
        {
            var nome = $"{prescricao.Medicamento} · {animal.Nome}";
            var via = prescricao.Via.Length > 0 ? $", via {prescricao.Via.ToLowerInvariant()}" : "";
            var descricao = $"{Quantidade(prescricao.QuantidadePorDose, prescricao.Unidade)} ({prescricao.DoseRealMg.ToString("0.##", PtBr)} mg){via}";
            alarme = new Alarme(nome.Length > 60 ? nome[..60] : nome, animal.Id, descricao.Length > 140 ? descricao[..140] : descricao,
                prescricao.HoraPrimeiraDose, recorrencia, prescricao.Inicio, ativo: true);
            await alarmes.AdicionarAsync(alarme);
        }

        await repository.AdicionarAsync(prescricao);
        await unitOfWork.CommitAsync();

        if (alarme is not null)
        {
            prescricao.VincularAlarme(alarme);
            await unitOfWork.CommitAsync();
        }

        return PrescricaoResponseDto.FromEntity(prescricao);
    }

    public async Task<PrescricaoResponseDto?> AtualizarAsync(int id, PrescricaoAlteracao req)
    {
        if (await repository.ObterPorIdAsync(id) is not { } prescricao) return null;

        if (req.Status is { } status)
        {
            prescricao.AlterarStatus(status);
            // Tratamento suspenso ou concluído não deve mais tocar o alarme.
            if (prescricao.AlarmeId is { } alarmeId && await alarmes.ObterPorIdAsync(alarmeId) is { } alarme)
                alarme.DefinirAtivo(prescricao.Status == "Ativa");
        }
        if (req.Observacoes is not null) prescricao.AlterarObservacoes(req.Observacoes);

        await unitOfWork.CommitAsync();
        return PrescricaoResponseDto.FromEntity(prescricao);
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        if (await repository.ObterPorIdAsync(id) is not { } prescricao) return false;

        if (prescricao.AlarmeId is { } alarmeId && await alarmes.ObterPorIdAsync(alarmeId) is { } alarme)
            alarmes.Remover(alarme);
        repository.Remover(prescricao);
        await unitOfWork.CommitAsync();
        return true;
    }

    private static string Quantidade(decimal valor, string unidade)
    {
        var texto = valor.ToString("0.##", PtBr);
        var plural = valor > 1 && unidade is "comprimido" or "cápsula" or "gota" ? unidade + "s" : unidade;
        return $"{texto} {plural}";
    }
}

/// <summary>Planos alimentares. Cada cálculo salvo vira um novo plano, mantendo o histórico.</summary>
public class PlanoAlimentarAppService(
    IPlanoAlimentarRepository repository,
    ResolvedorCalculos resolvedor,
    IUnitOfWork unitOfWork,
    TimeProvider relogio)
{
    public async Task<IReadOnlyList<PlanoAlimentarResponseDto>> ListarAsync(int? animalId) =>
        (await repository.ListarAsync(animalId)).Select(PlanoAlimentarResponseDto.FromEntity).ToList();

    public async Task<PlanoAlimentarResponseDto> CriarAsync(AlimentacaoRequisicao req)
    {
        var contexto = await resolvedor.AlimentacaoAsync(req, exigirPaciente: true);
        if (string.IsNullOrWhiteSpace(contexto.Nome)) throw new DominioException("racao", "Informe a ração.");

        var resultado = CalculosClinicos.Alimentacao(contexto.Entrada);
        var animal = contexto.Animal!;
        var plano = new PlanoAlimentar(animal.Id, contexto.Racao?.Id, contexto.Nome, contexto.Entrada, resultado,
            req.Observacoes, relogio.GetLocalNow().DateTime);

        if (req.AtualizarPesoPaciente && req.PesoAtualKg is { } novoPeso) animal.AtualizarPeso(novoPeso);

        await repository.AdicionarAsync(plano);
        await unitOfWork.CommitAsync();
        return PlanoAlimentarResponseDto.FromEntity(plano);
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        if (await repository.ObterPorIdAsync(id) is not { } plano) return false;
        repository.Remover(plano);
        await unitOfWork.CommitAsync();
        return true;
    }
}
