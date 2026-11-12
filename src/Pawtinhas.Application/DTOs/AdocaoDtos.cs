using System.ComponentModel.DataAnnotations;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Enums;

namespace Pawtinhas.Application.DTOs;

/// <summary>Cadastro e edição de um animal da vitrine de adoção.</summary>
public class SalvarAnimalAdocaoDto
{
    [Required(ErrorMessage = "Informe o nome do animal."), StringLength(60)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a espécie."), StringLength(40)]
    public string Especie { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escolha o sexo.")]
    public string Sexo { get; set; } = string.Empty;

    [StringLength(60)] public string? Raca { get; set; }
    public DateTime? DataNascimento { get; set; }
    public PorteAnimal? Porte { get; set; }
    public decimal? PesoKg { get; set; }

    [StringLength(60)] public string? Localizacao { get; set; }
    public PersonalidadeAnimal? Personalidade { get; set; }
    [StringLength(500)] public string? FotoUrl { get; set; }

    /// <summary>"Disponível", "Em processo" ou "Adotado".</summary>
    [Required(ErrorMessage = "Escolha a situação.")]
    public string Situacao { get; set; } = "Disponível";

    /// <summary>Adotante, quando a adoção é concluída e ele vira tutor do animal (opcional).</summary>
    public int? ResponsavelId { get; set; }

    public DateOnly? DataEntrada { get; set; }
    [StringLength(600)] public string Descricao { get; set; } = string.Empty;
    [StringLength(140)] public string? Frase { get; set; }
    [StringLength(1200)] public string? Sobre { get; set; }
    [StringLength(100)] public string? Tracos { get; set; }
    [StringLength(140)] public string? InfoAdicional { get; set; }
    [StringLength(140)] public string? Necessidades { get; set; }
    public bool ConviveAnimais { get; set; }
    public bool ConviveCriancas { get; set; }
    public bool Vacinado { get; set; }
    public bool Castrado { get; set; }
}

public class AnimalAdocaoResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Especie { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public string? Raca { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Idade { get; set; }
    public string? Fase { get; set; }
    public PorteAnimal? Porte { get; set; }
    public decimal? PesoKg { get; set; }
    public string? Localizacao { get; set; }
    public PersonalidadeAnimal? Personalidade { get; set; }
    public string? FotoUrl { get; set; }
    public string Situacao { get; set; } = string.Empty;

    /// <summary>Só aparece para a veterinária; a vitrine pública não expõe o adotante.</summary>
    public int? ResponsavelId { get; set; }

    public DateOnly DataEntrada { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Frase { get; set; } = string.Empty;
    public string Sobre { get; set; } = string.Empty;
    public string Tracos { get; set; } = string.Empty;
    public string InfoAdicional { get; set; } = string.Empty;
    public string Necessidades { get; set; } = string.Empty;
    public bool ConviveAnimais { get; set; }
    public bool ConviveCriancas { get; set; }
    public bool Vacinado { get; set; }
    public bool Castrado { get; set; }

    public static AnimalAdocaoResponseDto FromEntity(Animal animal, bool incluirAdotante)
    {
        var perfil = animal.PerfilAdocao!;
        var hoje = DateTime.Today;
        return new AnimalAdocaoResponseDto
        {
            Id = animal.Id,
            Nome = animal.Nome,
            Especie = animal.Especie,
            Sexo = animal.Sexo,
            Raca = animal.Raca,
            DataNascimento = animal.DataNascimento,
            Idade = animal.IdadeTexto(hoje),
            Fase = animal.Fase(hoje),
            Porte = animal.Porte,
            PesoKg = animal.PesoKg,
            Localizacao = animal.Localizacao,
            Personalidade = animal.Personalidade,
            FotoUrl = animal.FotoUrl,
            Situacao = SituacaoAdocao.Texto(animal.Status),
            ResponsavelId = incluirAdotante ? animal.ResponsavelId : null,
            DataEntrada = perfil.DataEntrada,
            Descricao = perfil.Descricao,
            Frase = perfil.Frase,
            Sobre = perfil.Sobre,
            Tracos = perfil.Tracos,
            InfoAdicional = perfil.InfoAdicional,
            Necessidades = perfil.Necessidades,
            ConviveAnimais = perfil.ConviveAnimais,
            ConviveCriancas = perfil.ConviveCriancas,
            Vacinado = perfil.Vacinado,
            Castrado = perfil.Castrado
        };
    }
}

/// <summary>Converte a situação de adoção entre o texto da tela e o <see cref="SituacaoAnimal"/> da equipe.</summary>
public static class SituacaoAdocao
{
    public static string Texto(SituacaoAnimal status) => status switch
    {
        SituacaoAnimal.EM_PROCESSO_ADOCAO => "Em processo",
        SituacaoAnimal.ADOTADO => "Adotado",
        _ => "Disponível",
    };

    public static SituacaoAnimal? Status(string? texto) => texto?.Trim() switch
    {
        "Disponível" => SituacaoAnimal.EM_DOACAO,
        "Em processo" => SituacaoAnimal.EM_PROCESSO_ADOCAO,
        "Adotado" => SituacaoAnimal.ADOTADO,
        _ => null,
    };
}
