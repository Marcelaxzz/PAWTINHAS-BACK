using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Calculos;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Domain.Enums;
using Pawtinhas.Infrastructure.Data;

namespace Pawtinhas.Infrastructure.Importacao;

/// <summary>
/// Importa os dados de exemplo (Seed/dados-exemplo.json) ou um backup da versão anterior da API.
/// Formato: cada coleção é uma lista de objetos; "id" é uma referência local ("p-hayley") usada
/// pelos campos "...Id"; "foto": "arquivo:nome.webp" copia a imagem da pasta "imagens" ao lado do JSON;
/// datas aceitam {hoje}, {hoje+3}, {hoje-1}, para a agenda sempre ter itens do dia.
/// Tudo é criado pelos construtores do domínio, com as mesmas validações da API.
/// No modo "acrescentar" nada é apagado: catálogos e animais para adoção com o mesmo nome são pulados.
/// </summary>
public sealed partial class ImportadorDados(AppDbContext db, IArmazenamentoFotos fotos, TimeProvider relogio)
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    /// <summary>Unidade de administração a partir da forma farmacêutica (dados antigos não tinham o campo).</summary>
    private static readonly Dictionary<string, string> UnidadePorForma = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Comprimido"] = "comprimido",
        ["Cápsula"] = "cápsula",
        ["Gotas"] = "gota",
        ["Suspensão oral"] = "mL",
        ["Solução oral"] = "mL",
        ["Injetável"] = "mL",
    };

    [GeneratedRegex(@"\{hoje([+-]\d+)?\}")]
    private static partial Regex MarcadorData();

    [GeneratedRegex(@"(\d+)\s*(ano|anos|mês|mes|meses|semana|semanas)", RegexOptions.IgnoreCase)]
    private static partial Regex Idade();

    private readonly Dictionary<string, object> _refs = [];
    private readonly HashSet<string> _existentes = new(StringComparer.OrdinalIgnoreCase);
    private string _pastaImagens = "";
    private int _total;
    private int _pulados;

    public async Task<int> ImportarAsync(string arquivo, bool substituir, bool acrescentar = false)
    {
        if (!File.Exists(arquivo)) throw new FileNotFoundException($"Arquivo não encontrado: {Path.GetFullPath(arquivo)}");
        _pastaImagens = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(arquivo))!, "imagens");

        var temDados = await db.Animais.AnyAsync() || await db.Medicamentos.AnyAsync();
        if (temDados && !substituir && !acrescentar)
        {
            Console.WriteLine("O banco já tem dados. Use --acrescentar para somar a eles ou --substituir para apagar e importar de novo.");
            return 1;
        }
        if (acrescentar) await CarregarExistentesAsync();

        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);
        var texto = MarcadorData().Replace(await File.ReadAllTextAsync(arquivo), m =>
            hoje.AddDays(m.Groups[1].Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0).ToString("yyyy-MM-dd"));
        using var documento = JsonDocument.Parse(texto);
        var raiz = documento.RootElement;

        await using var transacao = await db.Database.BeginTransactionAsync();
        if (substituir) await ApagarDadosAsync();

        await Colecao(raiz, "tutores", j => new Responsavel(Txt(j, "nome"), Txt(j, "telefone"), endereco: Opc(j, "endereco")));
        await Colecao(raiz, "profissionais", j => new Profissional(Txt(j, "nome"), Txt(j, "funcao"), Opc(j, "email")));
        await Colecao(raiz, "medicamentos", j => Novo("medicamento", j) ? Medicamento(j) : null);
        await Colecao(raiz, "racoes", j => Novo("racao", j)
            ? new Racao(Txt(j, "nome"), Txt(j, "especie"), Txt(j, "fase"), Txt(j, "tipo"), Dec(j, "kcalPorKg") ?? 0, Opc(j, "observacoes"), Opc(j, "fotoUrl"))
            : null);
        await Colecao(raiz, "materiais", j => Novo("material", j)
            ? new Material(Txt(j, "nome"), Txt(j, "tipo"), Txt(j, "unidade"), Opc(j, "especificacao"), Opc(j, "observacoes"))
            : null);
        await Colecao(raiz, "pets", async j => new Animal(Ref<Responsavel>(j, "tutorId")!.Id, Txt(j, "nome"), Txt(j, "especie"),
            Txt(j, "sexo"), Opc(j, "raca"), Nascimento(Opc(j, "idade"), hoje), null, Peso(Opc(j, "peso")),
            Opc(j, "observacoes"), await FotoAsync(j)));
        await Colecao(raiz, "animais", async j => Novo("adocao", j) ? await AdocaoAsync(j, hoje) : null);

        await Colecao(raiz, "consultas", j => Agendar(new Consulta(Pet(j), DataHora(j), Txt(j, "motivo"), Opc(j, "observacoes")), j, "veterinarioId"));
        await Colecao(raiz, "vacinas", j => Agendar(new Vacina(Pet(j), DataHora(j), Txt(j, "nome"), Txt(j, "dose"), Opc(j, "observacoes")), j, "profissionalId"));
        await Colecao(raiz, "cirurgias", j => Agendar(new Cirurgia(Pet(j), DataHora(j), Txt(j, "procedimento"), Opc(j, "observacoes")), j, "cirurgiaoId"));
        await Colecao(raiz, "banhos", j => Agendar(new BanhoTosa(Pet(j), DataHora(j), Txt(j, "servico"), Opc(j, "observacoes")), j, "profissionalId"));
        await Colecao(raiz, "visitas", j =>
        {
            var hora = TimeOnly.ParseExact(Txt(j, "hora"), "HH:mm", CultureInfo.InvariantCulture);
            var inicio = DateOnly.Parse(Txt(j, "dataInicio"), CultureInfo.InvariantCulture).ToDateTime(hora);
            var fim = DateOnly.Parse(Txt(j, "dataFim"), CultureInfo.InvariantCulture).ToDateTime(hora);
            return Agendar(new PetSitter(Pet(j), inicio, fim, Txt(j, "endereco"), Opc(j, "observacoes"), Txt(j, "servico"), Txt(j, "periodo")), j, "cuidadorId");
        });
        await Colecao(raiz, "emergencias", j => Agendar(new Emergencia(Ref<Animal>(j, "petId")?.Id, DataHora(j), Txt(j, "motivo"),
            Txt(j, "prioridade"), Opc(j, "observacoes"), Opc(j, "petNome")), j, "profissionalId"));

        await Colecao(raiz, "alarmes", j => new Alarme(Txt(j, "nome"), Ref<Animal>(j, "petId")?.Id, Opc(j, "descricao"),
            TimeOnly.ParseExact(Txt(j, "hora"), "HH:mm", CultureInfo.InvariantCulture), Txt(j, "recorrencia"),
            Opc(j, "inicio") is { } inicioAlarme ? DateOnly.Parse(inicioAlarme, CultureInfo.InvariantCulture) : hoje, Bool(j, "ativo")));
        await Colecao(raiz, "historicoAlarmes", j => Ref<Alarme>(j, "alarmeId") is { } alarme
            ? new DisparoAlarme(alarme, DateTime.Parse(Txt(j, "quando"), CultureInfo.InvariantCulture), Txt(j, "resultado"))
            : null);
        await Colecao(raiz, "prescricoes", j => Prescricao(j));
        await Colecao(raiz, "planosAlimentares", j => Plano(j));

        if (raiz.TryGetProperty("configuracoes", out var config)) await ConfiguracaoAsync(config);

        await transacao.CommitAsync();
        Console.WriteLine(_pulados > 0
            ? $"{_total} registros importados ({_pulados} já existiam e foram pulados)."
            : $"{_total} registros importados.");
        return 0;
    }

    /// <summary>Nomes já cadastrados nos catálogos e na adoção, para o modo "acrescentar" não duplicar.</summary>
    private async Task CarregarExistentesAsync()
    {
        foreach (var n in await db.Medicamentos.Select(m => m.Nome).ToListAsync()) _existentes.Add($"medicamento:{n}");
        foreach (var n in await db.Racoes.Select(r => r.Nome).ToListAsync()) _existentes.Add($"racao:{n}");
        foreach (var n in await db.Materiais.Select(m => m.Nome).ToListAsync()) _existentes.Add($"material:{n}");
        foreach (var n in await db.Animais.Where(a => db.PerfisAdocao.Any(p => p.AnimalId == a.Id)).Select(a => a.Nome).ToListAsync()) _existentes.Add($"adocao:{n}");
    }

    private bool Novo(string tipo, JsonElement j)
    {
        if (_existentes.Add($"{tipo}:{Txt(j, "nome")}")) return true;
        _pulados++;
        return false;
    }

    private Task Colecao<T>(JsonElement raiz, string nome, Func<JsonElement, T?> criar) where T : class =>
        Colecao(raiz, nome, j => Task.FromResult(criar(j)));

    /// <summary>Cria cada item da coleção, salva e guarda a referência local para os próximos "...Id".</summary>
    private async Task Colecao<T>(JsonElement raiz, string nome, Func<JsonElement, Task<T?>> criar) where T : class
    {
        if (!raiz.TryGetProperty(nome, out var itens) || itens.ValueKind != JsonValueKind.Array) return;

        var criados = new List<(string? Ref, T Entidade)>();
        foreach (var item in itens.EnumerateArray())
        {
            var refLocal = item.TryGetProperty("id", out var id) ? id.GetString() : null;
            try
            {
                if (await criar(item) is not { } entidade) continue;
                db.Add(entidade);
                criados.Add((refLocal, entidade));
            }
            catch (ArgumentException ex)
            {
                var campo = ex is DominioException d ? $" ({d.Campo})" : "";
                throw new InvalidDataException($"{nome}/{refLocal}{campo}: {ex.Message}", ex);
            }
        }

        await db.SaveChangesAsync();
        foreach (var (refLocal, entidade) in criados)
            if (refLocal is not null) _refs[refLocal] = entidade;
        _total += criados.Count;
    }

    private async Task<Animal> AdocaoAsync(JsonElement j, DateOnly hoje)
    {
        var tracos = Opc(j, "personalidade");
        var animal = new Animal(null, Txt(j, "nome"), Txt(j, "especie"), Txt(j, "sexo"), Opc(j, "raca"),
            Nascimento(Opc(j, "idade"), hoje), Porte(Opc(j, "porte")), Peso(Opc(j, "peso")), null, await FotoAsync(j),
            Opc(j, "cidade"), Personalidade(tracos));

        var situacao = Opc(j, "status") switch
        {
            "Em processo" => SituacaoAnimal.EM_PROCESSO_ADOCAO,
            "Adotado" => SituacaoAnimal.ADOTADO,
            _ => SituacaoAnimal.EM_DOACAO,
        };
        var entrada = Opc(j, "dataEntrada") is { } d ? DateOnly.Parse(d, CultureInfo.InvariantCulture) : hoje;
        animal.DefinirAdocao(new PerfilAdocao(entrada, Txt(j, "descricao"), Opc(j, "frase"), Opc(j, "sobre"), tracos,
            Opc(j, "infoAdicional"), Opc(j, "necessidades"), Bool(j, "conviveAnimais"), Bool(j, "conviveCriancas"),
            Bool(j, "vacinado"), Bool(j, "castrado")), situacao);
        return animal;
    }

    private Evento Agendar(Evento evento, JsonElement j, string campoProfissional)
    {
        evento.DefinirProfissional(Ref<Profissional>(j, campoProfissional)?.Id);
        if (Opc(j, "status") is { } status) evento.AlterarStatus(status);
        return evento;
    }

    private static Medicamento Medicamento(JsonElement j)
    {
        var forma = Txt(j, "forma");
        var unidade = Opc(j, "unidade") ?? (UnidadePorForma.TryGetValue(forma, out var u)
            ? u
            : throw new DominioException("unidade", $"Informe a unidade de \"{forma}\"."));
        return new Medicamento(Txt(j, "nome"), Txt(j, "principioAtivo"), forma, unidade, Dec(j, "concentracao") ?? 0,
            Bool(j, "fracionavel"), Opc(j, "via"), Dec(j, "doseMinCao"), Dec(j, "doseMaxCao"), Dec(j, "doseMinGato"),
            Dec(j, "doseMaxGato"), Dec(j, "doseMaximaMg"), (int?)Dec(j, "intervaloPadraoHoras"), Opc(j, "observacoes"),
            Opc(j, "fonte"));
    }

    /// <summary>Prescrições de backup: recalculadas a partir dos dados de entrada guardados (o resultado é o mesmo).</summary>
    private Prescricao Prescricao(JsonElement j)
    {
        var animal = Ref<Animal>(j, "petId") ?? throw new DominioException("petId", "Paciente não encontrado.");
        var entrada = new EntradaDose(animal.Especie ?? "", Dec(j, "pesoKg") ?? 0, Dec(j, "doseMgKg") ?? 0,
            Dec(j, "concentracao") ?? 0, Txt(j, "unidade"), Bool(j, "fracionavel"), (int)(Dec(j, "intervaloHoras") ?? 0),
            (int)(Dec(j, "duracaoDias") ?? 0), null, null, null);
        var prescricao = new Prescricao(animal.Id, Ref<Medicamento>(j, "medicamentoId")?.Id, Txt(j, "medicamento"), Opc(j, "via") ?? "",
            entrada, CalculosClinicos.Dose(entrada), DateOnly.Parse(Txt(j, "inicio"), CultureInfo.InvariantCulture),
            TimeOnly.Parse(Txt(j, "horaPrimeiraDose"), CultureInfo.InvariantCulture), Opc(j, "observacoes"),
            DateTime.Parse(Txt(j, "criadoEm"), CultureInfo.InvariantCulture));
        if (Opc(j, "status") is { } status) prescricao.AlterarStatus(status);
        return prescricao;
    }

    /// <summary>Planos de backup: recalculados com o fator e o peso de cálculo que foram usados na época.</summary>
    private PlanoAlimentar Plano(JsonElement j)
    {
        var animal = Ref<Animal>(j, "petId") ?? throw new DominioException("petId", "Paciente não encontrado.");
        var entrada = new EntradaAlimentacao(Txt(j, "especie"), Dec(j, "pesoAtualKg") ?? 0, Dec(j, "pesoCalculoKg"),
            TabelaFatores.Personalizado, Dec(j, "fator"), Dec(j, "kcalPorKg") ?? 0, (int)(Dec(j, "refeicoes") ?? 2));
        return new PlanoAlimentar(animal.Id, Ref<Racao>(j, "racaoId")?.Id, Txt(j, "racao"), entrada,
            CalculosClinicos.Alimentacao(entrada), Opc(j, "observacoes"), DateTime.Parse(Txt(j, "criadoEm"), CultureInfo.InvariantCulture));
    }

    private async Task ConfiguracaoAsync(JsonElement j)
    {
        if (j.ValueKind == JsonValueKind.Array) j = j.EnumerateArray().FirstOrDefault();
        if (j.ValueKind != JsonValueKind.Object) return;

        var clinica = j.GetProperty("clinica");
        var notificacoes = j.TryGetProperty("notificacoes", out var n) ? n : default;
        var integracoes = j.TryGetProperty("integracoes", out var i) ? i : default;

        var config = await db.Configuracoes.OrderBy(c => c.Id).FirstOrDefaultAsync();
        if (config is null)
        {
            config = ConfiguracaoClinica.Padrao(relogio.GetLocalNow().DateTime);
            db.Configuracoes.Add(config);
        }

        config.Atualizar(Txt(clinica, "nome"), Opc(clinica, "endereco"), Opc(clinica, "telefone"), Opc(clinica, "cnpj"),
            notificacoes.ValueKind == JsonValueKind.Object && Bool(notificacoes, "resumoDiarioEmail"),
            notificacoes.ValueKind != JsonValueKind.Object || Bool(notificacoes, "alarmesEsp32", true),
            integracoes.ValueKind == JsonValueKind.Object && Bool(integracoes, "backupAutomatico"),
            relogio.GetLocalNow().DateTime);
        await db.SaveChangesAsync();
        _total++;
    }

    private async Task<string?> FotoAsync(JsonElement j)
    {
        if (Opc(j, "foto") is not { } foto) return null;
        if (!foto.StartsWith("arquivo:", StringComparison.Ordinal)) return fotos.ValidarUrl(foto, "foto");

        var caminho = Path.Combine(_pastaImagens, Path.GetFileName(foto["arquivo:".Length..]));
        await using var arquivo = File.OpenRead(caminho);
        return await fotos.SalvarAsync(arquivo, CancellationToken.None);
    }

    private async Task ApagarDadosAsync()
    {
        await db.DisparosAlarme.ExecuteDeleteAsync();
        await db.Prescricoes.ExecuteDeleteAsync();
        await db.PlanosAlimentares.ExecuteDeleteAsync();
        await db.Eventos.ExecuteDeleteAsync();
        await db.Alarmes.ExecuteDeleteAsync();
        await db.PerfisAdocao.ExecuteDeleteAsync();
        await db.Animais.ExecuteDeleteAsync();
        await db.Responsaveis.ExecuteDeleteAsync();
        await db.Profissionais.ExecuteDeleteAsync();
        await db.Medicamentos.ExecuteDeleteAsync();
        await db.Racoes.ExecuteDeleteAsync();
        await db.Materiais.ExecuteDeleteAsync();
    }

    // ---------- Leitura dos campos ----------

    private T? Ref<T>(JsonElement j, string campo) where T : class =>
        Opc(j, campo) is { } chave ? _refs.GetValueOrDefault(chave) as T ?? throw new InvalidDataException($"Referência \"{chave}\" ({campo}) não encontrada.") : null;

    private int Pet(JsonElement j) => Ref<Animal>(j, "petId")?.Id ?? throw new DominioException("petId", "Paciente obrigatório.");

    private static DateTime DataHora(JsonElement j) =>
        DateOnly.Parse(Txt(j, "data"), CultureInfo.InvariantCulture)
            .ToDateTime(TimeOnly.ParseExact(Txt(j, "hora"), "HH:mm", CultureInfo.InvariantCulture));

    private static string Txt(JsonElement j, string campo) => Opc(j, campo) ?? "";

    private static string? Opc(JsonElement j, string campo) =>
        j.TryGetProperty(campo, out var v) && v.ValueKind == JsonValueKind.String && v.GetString() is { Length: > 0 } s ? s : null;

    private static decimal? Dec(JsonElement j, string campo) =>
        j.TryGetProperty(campo, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null;

    private static bool Bool(JsonElement j, string campo, bool padrao = false) =>
        j.TryGetProperty(campo, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : padrao;

    private static decimal? Peso(string? texto) =>
        decimal.TryParse(texto?.Replace("kg", "").Trim(), NumberStyles.Number, PtBr, out var p) && p > 0 ? p : null;

    /// <summary>"2 anos", "6 meses" viram uma data de nascimento aproximada.</summary>
    private static DateTime? Nascimento(string? idade, DateOnly hoje)
    {
        if (idade is null || Idade().Match(idade) is not { Success: true } m) return null;
        var n = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var unidade = m.Groups[2].Value.ToLowerInvariant();
        var data = unidade.StartsWith("ano") ? hoje.AddYears(-n) : unidade.StartsWith("sem") ? hoje.AddDays(-7 * n) : hoje.AddMonths(-n);
        return data.ToDateTime(TimeOnly.MinValue);
    }

    private static PorteAnimal? Porte(string? texto) => texto switch
    {
        "Pequeno" => PorteAnimal.PEQUENO,
        "Médio" => PorteAnimal.MEDIO,
        "Grande" => PorteAnimal.GRANDE,
        _ => null,
    };

    /// <summary>Traço principal a partir do texto livre ("Brincalhão, dócil, sociável").</summary>
    private static PersonalidadeAnimal? Personalidade(string? tracos)
    {
        if (tracos is null) return null;
        var primeiro = tracos.Split(',')[0].Trim().ToUpperInvariant()
            .Replace("Ã", "A").Replace("Í", "I").Replace("Ó", "O").Replace("Á", "A").Replace("É", "E");
        return Enum.TryParse<PersonalidadeAnimal>(primeiro, out var p) ? p : primeiro switch
        {
            "CURIOSO" or "CURIOSA" or "SOCIAVEIS" => PersonalidadeAnimal.SOCIAVEL,
            "DELICADA" or "TRANQUILO" or "TRANQUILA" or "CALMA" => PersonalidadeAnimal.CALMO,
            "BRINCALHONA" or "TRAVESSA" => PersonalidadeAnimal.BRINCALHAO,
            _ => null,
        };
    }
}
