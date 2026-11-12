using Pawtinhas.Application.Abstracoes;
using Pawtinhas.Application.DTOs;
using Pawtinhas.Domain;
using Pawtinhas.Domain.Comum;
using Pawtinhas.Domain.Interfaces;

namespace Pawtinhas.Application.Services;

/// <summary>
/// Vitrine de adoção. O animal da vitrine é um <see cref="Animal"/> com <see cref="PerfilAdocao"/>;
/// quem não está logado só vê os disponíveis e em processo, e nunca o adotante.
/// </summary>
public class AdocaoAppService
{
    private readonly IAnimalRepository _animais;
    private readonly IResponsavelRepository _responsaveis;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IArmazenamentoFotos _fotos;

    public AdocaoAppService(
        IAnimalRepository animais,
        IResponsavelRepository responsaveis,
        IUnitOfWork unitOfWork,
        IArmazenamentoFotos fotos)
    {
        _animais = animais;
        _responsaveis = responsaveis;
        _unitOfWork = unitOfWork;
        _fotos = fotos;
    }

    public async Task<IReadOnlyList<AnimalAdocaoResponseDto>> ListarAsync(bool autenticado)
    {
        var animais = await _animais.ListarAdocaoAsync(somentePublicos: !autenticado);
        return animais.Select(a => AnimalAdocaoResponseDto.FromEntity(a, autenticado)).ToList();
    }

    public async Task<AnimalAdocaoResponseDto?> ObterAsync(int id, bool autenticado)
    {
        var animal = await _animais.ObterComPerfilAsync(id);
        if (animal?.PerfilAdocao is null) return null;
        if (!autenticado && animal.Status == Domain.Enums.SituacaoAnimal.ADOTADO) return null;
        return AnimalAdocaoResponseDto.FromEntity(animal, autenticado);
    }

    public async Task<AnimalAdocaoResponseDto> CadastrarAsync(SalvarAnimalAdocaoDto dto)
    {
        var (situacao, responsavelId) = await ValidarAsync(dto);

        var animal = new Animal(responsavelId, dto.Nome, dto.Especie, dto.Sexo, dto.Raca, dto.DataNascimento, dto.Porte,
            dto.PesoKg, null, _fotos.ValidarUrl(dto.FotoUrl, "fotoUrl"), dto.Localizacao, dto.Personalidade);
        animal.DefinirAdocao(Perfil(dto), situacao);

        await _animais.AdicionarAsync(animal);
        await _unitOfWork.CommitAsync();
        return AnimalAdocaoResponseDto.FromEntity(animal, true);
    }

    public async Task<AnimalAdocaoResponseDto?> AtualizarAsync(int id, SalvarAnimalAdocaoDto dto)
    {
        var animal = await _animais.ObterComPerfilAsync(id);
        if (animal?.PerfilAdocao is null) return null;

        var (situacao, responsavelId) = await ValidarAsync(dto);
        var fotoAnterior = animal.FotoUrl;

        animal.AtualizarDados(responsavelId, dto.Nome, dto.Especie, dto.Sexo, dto.Raca, dto.DataNascimento, dto.Porte,
            dto.PesoKg, animal.Observacoes, _fotos.ValidarUrl(dto.FotoUrl, "fotoUrl"), dto.Localizacao, dto.Personalidade);
        animal.DefinirAdocao(Perfil(dto), situacao);

        await _unitOfWork.CommitAsync();
        if (fotoAnterior != animal.FotoUrl) _fotos.RemoverSeLocal(fotoAnterior);
        return AnimalAdocaoResponseDto.FromEntity(animal, true);
    }

    /// <summary>Tira o animal da vitrine. Se ele já virou paciente de um adotante, o cadastro clínico continua.</summary>
    public async Task<bool> ExcluirAsync(int id)
    {
        var animal = await _animais.ObterComPerfilAsync(id);
        if (animal?.PerfilAdocao is null) return false;

        if (animal.EhPaciente)
            throw new ConflitoException("animal_paciente",
                "Este animal já é paciente do adotante. Para removê-lo, exclua o paciente na ficha.");

        _animais.Remover(animal);
        await _unitOfWork.CommitAsync();
        _fotos.RemoverSeLocal(animal.FotoUrl);
        return true;
    }

    private async Task<(Domain.Enums.SituacaoAnimal Situacao, int? ResponsavelId)> ValidarAsync(SalvarAnimalAdocaoDto dto)
    {
        var situacao = SituacaoAdocao.Status(dto.Situacao)
            ?? throw new DominioException("situacao", "Escolha a situação: Disponível, Em processo ou Adotado.");

        if (dto.ResponsavelId is { } id)
        {
            if (situacao != Domain.Enums.SituacaoAnimal.ADOTADO)
                throw new DominioException("responsavelId", "O adotante só é informado quando a adoção é concluída.");
            if (await _responsaveis.ObterPorIdAsync(id) is null)
                throw new DominioException("responsavelId", "Adotante não encontrado no cadastro de tutores.");
        }

        return (situacao, dto.ResponsavelId);
    }

    private static PerfilAdocao Perfil(SalvarAnimalAdocaoDto dto) => new(
        dto.DataEntrada ?? DateOnly.FromDateTime(DateTime.Today),
        dto.Descricao, dto.Frase, dto.Sobre, dto.Tracos, dto.InfoAdicional, dto.Necessidades,
        dto.ConviveAnimais, dto.ConviveCriancas, dto.Vacinado, dto.Castrado);
}
