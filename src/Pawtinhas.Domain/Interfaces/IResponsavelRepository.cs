using Pawtinhas.Domain;

namespace Pawtinhas.Domain.Interfaces;

public interface IResponsavelRepository : IRepository<Responsavel>
{
    Task<bool> CpfEmUsoAsync(string cpf, int? ignorarId);

    Task<bool> TemAnimaisAsync(int id);
}
