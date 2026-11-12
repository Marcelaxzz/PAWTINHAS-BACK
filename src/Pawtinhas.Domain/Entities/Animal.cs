
using Pawtinhas.Domain.Comum;
using Pawtinhas.Domain.Enums;

namespace Pawtinhas.Domain;

/// <summary>
/// Animal atendido pela clínica. É paciente quando tem um responsável (tutor) e aparece na
/// vitrine de adoção quando tem um <see cref="PerfilAdocao"/>. Um animal adotado pode ganhar
/// o adotante como responsável e passar a ser paciente.
/// </summary>
public class Animal
{
    public static readonly string[] Sexos = ["Macho", "Fêmea", "Casal"];

    public int Id { get; private set; }

    public int? ResponsavelId { get; private set; }
    public Responsavel? Responsavel { get; private set; }

    public SituacaoAnimal Status { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Sexo { get; private set; } = string.Empty;
    public string? Especie { get; private set; }
    public string? Raca { get; private set; }
    public DateTime? DataNascimento { get; private set; }
    public PorteAnimal? Porte { get; private set; }
    public decimal? PesoKg { get; private set; }
    public string? Observacoes { get; private set; }
    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;

    public string? FotoUrl { get; private set; }
    public string? Localizacao { get; private set; }
    public PersonalidadeAnimal? Personalidade { get; private set; }

    public PerfilAdocao? PerfilAdocao { get; private set; }

    private Animal() { }

    public Animal(
        int? responsavelId,
        string nome,
        string especie,
        string sexo,
        string? raca = null,
        DateTime? dataNascimento = null,
        PorteAnimal? porte = null,
        decimal? pesoKg = null,
        string? observacoes = null,
        string? fotoUrl = null,
        string? localizacao = null,
        PersonalidadeAnimal? personalidade = null)
    {
        Status = SituacaoAnimal.ATIVO;
        AtualizarDados(responsavelId, nome, especie, sexo, raca, dataNascimento, porte, pesoKg,
            observacoes, fotoUrl, localizacao, personalidade);
    }

    public bool EhPaciente => ResponsavelId is not null;

    public void AtualizarDados(
        int? responsavelId,
        string nome,
        string especie,
        string sexo,
        string? raca,
        DateTime? dataNascimento,
        PorteAnimal? porte,
        decimal? pesoKg,
        string? observacoes,
        string? fotoUrl,
        string? localizacao,
        PersonalidadeAnimal? personalidade)
    {
        if (dataNascimento is { } nascimento && nascimento.Date > DateTime.Today)
            throw new DominioException("dataNascimento", "A data de nascimento não pode estar no futuro.");
        if (pesoKg is { } peso) Validar.Faixa(peso, 0.01m, 150m, "pesoKg", "Informe o peso entre 0,01 e 150 kg.");

        ResponsavelId = responsavelId;
        Nome = Validar.Texto(nome, "nome", 60, "Informe o nome do animal.");
        Especie = Validar.Texto(especie, "especie", 40, "Informe a espécie.");
        Sexo = Validar.Opcao(sexo, Sexos, "sexo", "Escolha o sexo do animal.");
        Raca = Validar.TextoOpcional(raca, "raca", 60);
        DataNascimento = dataNascimento?.Date;
        Porte = porte;
        PesoKg = pesoKg is { } p ? Math.Round(p, 2) : null;
        Observacoes = Validar.TextoOpcional(observacoes, "observacoes", 1000);
        FotoUrl = Validar.TextoOpcional(fotoUrl, "fotoUrl", 500);
        Localizacao = Validar.TextoOpcional(localizacao, "localizacao", 60);
        Personalidade = personalidade;
    }

    /// <summary>Peso atualizado numa prescrição ou num plano alimentar.</summary>
    public void AtualizarPeso(decimal pesoKg) =>
        PesoKg = Math.Round(Validar.Faixa(pesoKg, 0.01m, 150m, "pesoKg", "Informe o peso entre 0,01 e 150 kg."), 2);

    /// <summary>Coloca o animal na vitrine (ou atualiza o perfil) e acerta a situação.</summary>
    public void DefinirAdocao(PerfilAdocao perfil, SituacaoAnimal situacao)
    {
        if (situacao is not (SituacaoAnimal.EM_DOACAO or SituacaoAnimal.EM_PROCESSO_ADOCAO or SituacaoAnimal.ADOTADO))
            throw new DominioException("status", "Escolha uma situação de adoção válida.");

        if (PerfilAdocao is null) PerfilAdocao = perfil;
        else PerfilAdocao.CopiarDe(perfil);
        Status = situacao;
    }

    public void AlterarSituacao(SituacaoAnimal situacao) => Status = situacao;

    /// <summary>Idade em texto ("3 meses", "1 ano", "4 anos"), calculada pela data de nascimento.</summary>
    public string? IdadeTexto(DateTime hoje)
    {
        if (DataNascimento is not { } nascimento) return null;
        var meses = (hoje.Year - nascimento.Year) * 12 + hoje.Month - nascimento.Month - (hoje.Day < nascimento.Day ? 1 : 0);
        if (meses < 1) return "Menos de 1 mês";
        if (meses < 12) return meses == 1 ? "1 mês" : $"{meses} meses";
        var anos = meses / 12;
        return anos == 1 ? "1 ano" : $"{anos} anos";
    }

    /// <summary>Filhote até 12 meses; adulto depois disso.</summary>
    public string? Fase(DateTime hoje) =>
        DataNascimento is { } nascimento ? (nascimento.AddYears(1) > hoje ? "Filhote" : "Adulto") : null;
}
