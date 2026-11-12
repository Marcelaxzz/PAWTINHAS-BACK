using Pawtinhas.Domain;

namespace Pawtinhas.Domain.Interfaces;

public interface IAnimalRepository : IRepository<Animal>
{
    /// <summary>Animais com responsável (pacientes da clínica).</summary>
    Task<IReadOnlyList<Animal>> ListarPacientesAsync();

    /// <summary>Animais da vitrine com o perfil carregado. <paramref name="somentePublicos"/> esconde os já adotados.</summary>
    Task<IReadOnlyList<Animal>> ListarAdocaoAsync(bool somentePublicos);

    Task<Animal?> ObterComPerfilAsync(int id);
}
