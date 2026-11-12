using System.Globalization;
using System.Text.RegularExpressions;
using Pawtinhas.Application.DTOs;
using Pawtinhas.Application.Relatorios;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

/// <summary>Relatório que a veterinária preenche em cada atendimento da agenda.</summary>
public class RelatorioServicoAppService(
    IEventoRepository eventos,
    IRelatorioServicoRepository relatorios,
    IUnitOfWork unitOfWork)
{
    public async Task<RelatorioServicoDto?> ObterAsync(int eventoId) =>
        await relatorios.ObterPorEventoAsync(eventoId) is { } r ? RelatorioServicoDto.FromEntity(r) : null;

    public Task<IReadOnlyList<int>> EventosComRelatorioAsync() => relatorios.EventosComRelatorioAsync();

    /// <summary>Cria ou atualiza. Nulo quando o atendimento não existe.</summary>
    public async Task<RelatorioServicoDto?> SalvarAsync(int eventoId, SalvarRelatorioServicoDto d)
    {
        if (await eventos.ObterPorIdAsync(eventoId) is not { } evento) return null;
        var relatorio = await relatorios.ObterPorEventoAsync(eventoId);
        if (relatorio is null)
        {
            relatorio = new RelatorioServico(eventoId);
            await relatorios.AdicionarAsync(relatorio);
        }
        relatorio.Preencher(d.Queixa, d.Avaliacao, d.Procedimentos, d.Diagnostico, d.Orientacoes, d.PesoKg,
            d.TemperaturaC, d.Retorno, d.Responsavel, DateOnly.FromDateTime(evento.DataHora));
        await unitOfWork.CommitAsync();
        return RelatorioServicoDto.FromEntity(relatorio);
    }
}

/// <summary>
/// Monta os relatórios em PDF: o do atendimento (preenchido pela veterinária) e os automáticos
/// (agenda do período, estoque, ficha do paciente, alarmes e adoção).
/// </summary>
public class RelatorioAppService(
    IEventoRepository eventos,
    IRelatorioServicoRepository relatorios,
    IAnimalRepository animais,
    IPrescricaoRepository prescricoes,
    IPlanoAlimentarRepository planos,
    IMedicaoRepository medicoes,
    IAlarmeRepository alarmes,
    IConfiguracaoRepository configuracao,
    EstoqueAppService estoque,
    IGeradorPdf gerador,
    TimeProvider relogio)
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Tipos aceitos no filtro do relatório da agenda (o mesmo nome da classe do atendimento).</summary>
    public static readonly IReadOnlyDictionary<string, string> TiposAgenda = new Dictionary<string, string>
    {
        ["Consulta"] = "Consultas",
        ["Vacina"] = "Vacinas",
        ["Cirurgia"] = "Cirurgias",
        ["BanhoTosa"] = "Banho e tosa",
        ["PetSitter"] = "Pet sitter",
        ["Emergencia"] = "Emergências",
    };

    private DateTime Agora => relogio.GetLocalNow().DateTime;

    // ---------- Atendimento ----------

    public async Task<ArquivoPdf?> AtendimentoAsync(int eventoId)
    {
        if (await eventos.ObterCompletoAsync(eventoId) is not { } e) return null;
        var r = await relatorios.ObterPorEventoAsync(eventoId);
        var animal = e.Animal;
        var tipo = AgendaAppService.Tipo(e);

        var blocos = new List<BlocoPdf>
        {
            new BlocoCampos("Atendimento",
            [
                new("Serviço", tipo),
                new("Data", e.DataHora.ToString("dd/MM/yyyy 'às' HH:mm", Br)),
                new("Situação", e.Status),
                new("Detalhe", AgendaAppService.Detalhe(e)),
                new("Profissional", e.Profissional?.Nome ?? "Não definido"),
            ]),
            new BlocoCampos("Paciente",
            [
                new("Nome", AgendaAppService.Pet(e)),
                new("Espécie", animal?.Especie ?? "—"),
                new("Raça", animal?.Raca ?? "—"),
                new("Idade", animal?.IdadeTexto(Agora) ?? "—"),
                new("Sexo", animal?.Sexo ?? "—"),
                new("Tutor(a)", animal?.Responsavel?.NomeCompleto ?? "—"),
                new("Telefone", animal?.Responsavel?.Telefone ?? "—"),
            ]),
        };

        if (r is null)
        {
            blocos.Add(new BlocoTexto("Relatório", "O relatório deste atendimento ainda não foi preenchido."));
        }
        else
        {
            var sinais = new List<Rotulado>();
            if (r.PesoKg is { } p) sinais.Add(new("Peso", $"{p.ToString("0.##", Br)} kg"));
            if (r.TemperaturaC is { } t) sinais.Add(new("Temperatura", $"{t.ToString("0.0", Br)} °C"));
            if (r.Retorno is { } ret) sinais.Add(new("Retorno", ret.ToString("dd/MM/yyyy", Br)));
            if (sinais.Count > 0) blocos.Add(new BlocoCampos("Sinais e retorno", sinais));

            blocos.Add(new BlocoTexto("Queixa principal", r.Queixa));
            blocos.Add(new BlocoTexto("Avaliação e exame físico", r.Avaliacao));
            blocos.Add(new BlocoTexto("Procedimentos realizados", r.Procedimentos));
            blocos.Add(new BlocoTexto("Diagnóstico", r.Diagnostico));
            blocos.Add(new BlocoTexto("Orientações ao tutor", r.Orientacoes));
            blocos.Add(new BlocoAssinatura(
                string.IsNullOrEmpty(r.Responsavel) ? e.Profissional?.Nome ?? "Responsável técnico" : r.Responsavel,
                $"Atualizado em {Local(r.AtualizadoEm):dd/MM/yyyy HH:mm}"));
        }

        var doc = await DocumentoAsync($"Relatório de {tipo.ToLowerInvariant()}",
            $"{AgendaAppService.Pet(e)} · {e.DataHora.ToString("dd/MM/yyyy", Br)}", blocos);
        return new(gerador.Gerar(doc), $"relatorio-{Slug(tipo)}-{Slug(AgendaAppService.Pet(e))}-{e.DataHora:yyyyMMdd}.pdf");
    }

    // ---------- Agenda do período ----------

    public async Task<ArquivoPdf> AgendaAsync(DateOnly de, DateOnly ate, string? tipo)
    {
        (de, ate) = Periodo(de, ate);
        if (tipo is not null && !TiposAgenda.ContainsKey(tipo))
            throw new DominioException("tipo", "Tipo de atendimento inválido.");

        var inicio = de.ToDateTime(TimeOnly.MinValue);
        var fim = ate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var lista = (await eventos.ListarAsync(null, null))
            .Where(e => e.DataHora >= inicio && e.DataHora < fim)
            .Where(e => tipo is null || e.GetType().Name == tipo)
            .OrderBy(e => e.DataHora)
            .ToList();
        var comRelatorio = (await relatorios.EventosComRelatorioAsync()).ToHashSet();

        var concluidos = lista.Count(e => e.Status is "Concluído" or "Concluída" or "Aplicada" or "Alta médica");
        var cancelados = lista.Count(e => e.Status == "Cancelado");
        var blocos = new List<BlocoPdf>
        {
            new BlocoIndicadores(
            [
                new("Atendimentos", lista.Count.ToString(Br)),
                new("Concluídos", concluidos.ToString(Br)),
                new("Em aberto", (lista.Count - concluidos - cancelados).ToString(Br)),
                new("Cancelados", cancelados.ToString(Br)),
                new("Com relatório", lista.Count(e => comRelatorio.Contains(e.Id)).ToString(Br)),
            ]),
        };

        var grupos = TiposAgenda.Keys.Where(k => tipo is null || k == tipo);
        foreach (var chave in grupos)
        {
            var doTipo = lista.Where(e => e.GetType().Name == chave).ToList();
            if (tipo is null && doTipo.Count == 0) continue;
            blocos.Add(new BlocoTabela(
                $"{TiposAgenda[chave]} ({doTipo.Count})",
                ["Data", "Paciente", "Tutor(a)", "Serviço", "Profissional", "Situação", "Relatório"],
                doTipo.Select(e => (IReadOnlyList<string>)
                [
                    e.DataHora.ToString("dd/MM HH:mm", Br),
                    AgendaAppService.Pet(e),
                    e.Animal?.Responsavel?.NomeCompleto ?? "—",
                    AgendaAppService.Detalhe(e),
                    e.Profissional?.Nome ?? "—",
                    e.Status,
                    comRelatorio.Contains(e.Id) ? "Sim" : "—",
                ]).ToList(),
                [1.1f, 1.3f, 1.5f, 2.2f, 1.3f, 1.2f, 0.9f],
                "Nenhum atendimento deste tipo no período."));
        }
        if (lista.Count == 0) blocos.Add(new BlocoTexto("Atendimentos", "Nenhum atendimento no período."));

        var titulo = tipo is null ? "Relatório da agenda" : $"Relatório de {TiposAgenda[tipo].ToLowerInvariant()}";
        var doc = await DocumentoAsync(titulo, PeriodoTexto(de, ate), blocos);
        return new(gerador.Gerar(doc), $"{Slug(titulo)}-{de:yyyyMMdd}-{ate:yyyyMMdd}.pdf");
    }

    // ---------- Estoque ----------

    public async Task<ArquivoPdf> EstoqueAsync(DateOnly de, DateOnly ate)
    {
        (de, ate) = Periodo(de, ate);
        var itens = await estoque.ListarAsync();
        var movs = await estoque.MovimentacoesAsync(null, null, de, ate, 1000);
        var nomes = itens.ToDictionary(i => (i.Categoria, i.ItemId), i => i);

        var blocos = new List<BlocoPdf>
        {
            new BlocoIndicadores(
            [
                new("Itens no catálogo", itens.Count.ToString(Br)),
                new("Zerados", itens.Count(i => i.Situacao == SituacaoEstoque.Zerado).ToString(Br)),
                new("Estoque baixo", itens.Count(i => i.Situacao == SituacaoEstoque.Baixo).ToString(Br)),
                new("Vencendo em 30 dias", itens.Count(i => i.Lotes.Any(l => l.VenceEmBreve)).ToString(Br)),
                new("Com lote vencido", itens.Count(i => i.Lotes.Any(l => l.Vencido)).ToString(Br)),
            ]),
        };

        foreach (var (categoria, titulo) in new[] { (CategoriaEstoque.Medicamento, "Medicamentos"), (CategoriaEstoque.Racao, "Rações"), (CategoriaEstoque.Material, "Materiais e insumos") })
        {
            blocos.Add(new BlocoTabela(titulo,
                ["Item", "Saldo", "Mínimo", "Próxima validade", "Lotes", "Situação"],
                itens.Where(i => i.Categoria == categoria).Select(i => (IReadOnlyList<string>)
                [
                    i.Nome,
                    $"{Numero(i.Saldo)} {i.Unidade}",
                    i.Minimo is { } m ? $"{Numero(m)} {i.Unidade}" : "—",
                    i.ProximaValidade?.ToString("dd/MM/yyyy", Br) ?? "—",
                    i.Lotes.Count.ToString(Br),
                    i.Situacao,
                ]).ToList(),
                [2.6f, 1.3f, 1.2f, 1.3f, 0.7f, 1f],
                "Nenhum item cadastrado no catálogo."));
        }

        blocos.Add(new BlocoTabela($"Movimentações no período ({movs.Count})",
            ["Data", "Item", "Tipo", "Quantidade", "Saldo após", "Motivo"],
            movs.Select(m =>
            {
                var item = nomes.GetValueOrDefault((m.Categoria, m.ItemId));
                var unidade = item?.Unidade ?? "";
                return (IReadOnlyList<string>)
                [
                    Local(m.CriadoEm).ToString("dd/MM HH:mm", Br),
                    item?.Nome ?? "Item excluído",
                    m.Tipo,
                    $"{Numero(m.Quantidade)} {unidade}",
                    $"{Numero(m.SaldoApos)} {unidade}",
                    string.IsNullOrEmpty(m.Lotes) ? m.Motivo : $"{m.Motivo} (lote {m.Lotes})",
                ];
            }).ToList(),
            [1f, 2.2f, 0.9f, 1.1f, 1.1f, 2.2f],
            "Nenhuma movimentação no período."));

        var doc = await DocumentoAsync("Relatório de estoque", $"Posição atual e movimentações · {PeriodoTexto(de, ate)}", blocos);
        return new(gerador.Gerar(doc), $"relatorio-estoque-{Agora:yyyyMMdd}.pdf");
    }

    // ---------- Ficha do paciente ----------

    public async Task<ArquivoPdf?> PacienteAsync(int animalId)
    {
        if (await animais.ObterPorIdAsync(animalId) is not { } a) return null;
        var historico = (await eventos.ListarAsync(null, animalId)).OrderByDescending(e => e.DataHora).ToList();
        var comRelatorio = (await relatorios.EventosComRelatorioAsync()).ToHashSet();

        var blocos = new List<BlocoPdf>
        {
            new BlocoCampos("Paciente",
            [
                new("Nome", a.Nome),
                new("Espécie", a.Especie ?? "—"),
                new("Raça", a.Raca ?? "—"),
                new("Sexo", a.Sexo),
                new("Idade", a.IdadeTexto(Agora) ?? "—"),
                new("Peso", a.PesoKg is { } p ? $"{p.ToString("0.##", Br)} kg" : "—"),
            ]),
            new BlocoCampos("Tutor(a)",
            [
                new("Nome", a.Responsavel?.NomeCompleto ?? "—"),
                new("Telefone", a.Responsavel?.Telefone ?? "—"),
                new("E-mail", a.Responsavel?.Email ?? "—"),
                new("Endereço", string.Join(", ", new[] { a.Responsavel?.Endereco, a.Responsavel?.Cidade, a.Responsavel?.UF }
                    .Where(s => !string.IsNullOrWhiteSpace(s))) is { Length: > 0 } end ? end : "—"),
            ]),
            new BlocoTexto("Observações", a.Observacoes ?? ""),
            new BlocoTabela("Tratamentos",
                ["Início", "Medicamento", "Dose", "Intervalo", "Duração", "Situação"],
                (await prescricoes.ListarAsync(animalId)).Select(p => (IReadOnlyList<string>)
                [
                    p.Inicio.ToString("dd/MM/yyyy", Br),
                    p.Medicamento,
                    $"{Numero(p.QuantidadePorDose)} {EstoqueAppService.UnidadePlural(p.Unidade)} ({Numero(p.DoseRealMg)} mg)",
                    $"a cada {p.IntervaloHoras} h",
                    $"{p.DuracaoDias} dias",
                    p.Status,
                ]).ToList(),
                [1f, 2f, 1.8f, 1f, 0.8f, 0.9f],
                "Nenhuma prescrição registrada."),
            new BlocoTabela("Alimentação",
                ["Data", "Ração", "Condição", "Energia (MER)", "Quantidade"],
                (await planos.ListarAsync(animalId)).Select(p => (IReadOnlyList<string>)
                [
                    p.CriadoEm.ToString("dd/MM/yyyy", Br),
                    p.Racao,
                    p.Condicao,
                    $"{Numero(p.Mer)} kcal/dia",
                    $"{Numero(p.GramasDia)} g/dia em {p.Refeicoes} refeições",
                ]).ToList(),
                [1f, 2f, 1.6f, 1.2f, 1.8f],
                "Nenhum plano alimentar calculado."),
            new BlocoTabela("Medições para prótese 3D",
                ["Data", "Membro", "Comprimento", "Altura ao chão", "Pata", "Encaixe", "Método"],
                (await medicoes.ListarAsync(animalId)).Select(m => (IReadOnlyList<string>)
                [
                    m.CriadoEm.ToString("dd/MM/yyyy", Br),
                    m.Membro ?? "—",
                    $"{Numero(m.ComprimentoMembro)} cm",
                    $"{Numero(m.AlturaChao)} cm",
                    $"{Numero(m.LarguraPata)} cm",
                    m.LarguraEncaixe is { } le ? $"{Numero(le)} cm" : "—",
                    m.Metodo,
                ]).ToList(),
                [1f, 1.4f, 1f, 1f, 0.8f, 0.8f, 1.3f],
                "Nenhuma medição registrada."),
            new BlocoTabela($"Histórico de atendimentos ({historico.Count})",
                ["Data", "Serviço", "Detalhe", "Profissional", "Situação", "Relatório"],
                historico.Select(e => (IReadOnlyList<string>)
                [
                    e.DataHora.ToString("dd/MM/yyyy HH:mm", Br),
                    AgendaAppService.Tipo(e),
                    AgendaAppService.Detalhe(e),
                    e.Profissional?.Nome ?? "—",
                    e.Status,
                    comRelatorio.Contains(e.Id) ? "Sim" : "—",
                ]).ToList(),
                [1.3f, 1.2f, 2.2f, 1.3f, 1.1f, 0.8f],
                "Nenhum atendimento registrado."),
        };

        var doc = await DocumentoAsync($"Ficha de {a.Nome}", "Dados, tratamentos e histórico do paciente", blocos);
        return new(gerador.Gerar(doc), $"ficha-{Slug(a.Nome)}-{Agora:yyyyMMdd}.pdf");
    }

    // ---------- Alarmes ----------

    public async Task<ArquivoPdf> AlarmesAsync()
    {
        var todos = (await alarmes.ObterTodosAsync()).OrderBy(a => a.Hora).ToList();
        var disparos = await alarmes.UltimosDisparosAsync(200);
        var blocos = new List<BlocoPdf>
        {
            new BlocoIndicadores(
            [
                new("Alarmes", todos.Count.ToString(Br)),
                new("Ativos", todos.Count(a => a.Ativo).ToString(Br)),
                new("Pausados", todos.Count(a => !a.Ativo).ToString(Br)),
                new("Disparos registrados", disparos.Count.ToString(Br)),
            ]),
            new BlocoTabela("Alarmes cadastrados",
                ["Hora", "Alarme", "Paciente", "Repetição", "Situação"],
                todos.Select(a => (IReadOnlyList<string>)
                [
                    a.Hora.ToString("HH:mm", Br),
                    string.IsNullOrEmpty(a.Descricao) ? a.Nome : $"{a.Nome} · {a.Descricao}",
                    a.Animal?.Nome ?? "—",
                    a.Recorrencia,
                    a.Ativo ? "Ativo" : "Pausado",
                ]).ToList(),
                [0.7f, 2.6f, 1.2f, 1.6f, 0.9f],
                "Nenhum alarme cadastrado."),
            new BlocoTabela("Últimos disparos no dispositivo",
                ["Quando", "Alarme", "Resultado"],
                disparos.Select(d => (IReadOnlyList<string>)
                [
                    d.Quando.ToString("dd/MM/yyyy HH:mm", Br),
                    d.Nome,
                    d.Resultado,
                ]).ToList(),
                [1.2f, 2.6f, 1.4f],
                "Nenhum disparo registrado."),
        };
        var doc = await DocumentoAsync("Relatório de alarmes", "Alarmes de medicação e disparos do ESP32", blocos);
        return new(gerador.Gerar(doc), $"relatorio-alarmes-{Agora:yyyyMMdd}.pdf");
    }

    // ---------- Adoção ----------

    public async Task<ArquivoPdf> AdocaoAsync()
    {
        var lista = (await animais.ListarAdocaoAsync(false)).OrderBy(a => a.Nome).ToList();
        var situacao = (Animal a) => SituacaoAdocao.Texto(a.Status);
        var blocos = new List<BlocoPdf>
        {
            new BlocoIndicadores(
            [
                new("Animais na vitrine", lista.Count.ToString(Br)),
                new("Disponíveis", lista.Count(a => situacao(a) == "Disponível").ToString(Br)),
                new("Em processo", lista.Count(a => situacao(a) == "Em processo").ToString(Br)),
                new("Adotados", lista.Count(a => situacao(a) == "Adotado").ToString(Br)),
            ]),
            new BlocoTabela("Animais",
                ["Nome", "Espécie", "Sexo", "Idade", "Cidade", "Vacinado", "Castrado", "Situação"],
                lista.Select(a => (IReadOnlyList<string>)
                [
                    a.Nome,
                    a.Especie ?? "—",
                    a.Sexo,
                    a.IdadeTexto(Agora) ?? "—",
                    a.Localizacao ?? "—",
                    a.PerfilAdocao?.Vacinado == true ? "Sim" : "Não",
                    a.PerfilAdocao?.Castrado == true ? "Sim" : "Não",
                    situacao(a),
                ]).ToList(),
                [1.4f, 1f, 0.8f, 0.9f, 1.2f, 0.8f, 0.8f, 1f],
                "Nenhum animal na vitrine."),
        };
        var doc = await DocumentoAsync("Relatório de adoção", "Animais cadastrados na vitrine e a situação de cada um", blocos);
        return new(gerador.Gerar(doc), $"relatorio-adocao-{Agora:yyyyMMdd}.pdf");
    }

    // ---------- Apoio ----------

    private async Task<DocumentoPdf> DocumentoAsync(string titulo, string subtitulo, List<BlocoPdf> blocos)
    {
        var c = await configuracao.ObterAsync();
        return new DocumentoPdf(titulo, subtitulo,
            new CabecalhoClinica(c.NomeClinica, c.Endereco, c.Telefone, c.Cnpj), blocos, Agora);
    }

    private (DateOnly, DateOnly) Periodo(DateOnly de, DateOnly ate)
    {
        if (ate < de) throw new DominioException("ate", "A data final precisa ser depois da inicial.");
        if (ate.DayNumber - de.DayNumber > 366) throw new DominioException("ate", "Escolha um período de até um ano.");
        return (de, ate);
    }

    private static string PeriodoTexto(DateOnly de, DateOnly ate) =>
        de == ate ? de.ToString("dd/MM/yyyy", Br) : $"{de.ToString("dd/MM/yyyy", Br)} a {ate.ToString("dd/MM/yyyy", Br)}";

    private static string Numero(decimal n) => n.ToString("0.##", Br);

    /// <summary>Datas gravadas em UTC (DateTime.UtcNow) aparecem no horário local.</summary>
    private static DateTime Local(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();

    /// <summary>"Banho e tosa · Paçoca" → "banho-e-tosa-pacoca", para o nome do arquivo.</summary>
    private static string Slug(string texto)
    {
        var semAcento = new string(texto.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Replace(semAcento.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
    }
}
