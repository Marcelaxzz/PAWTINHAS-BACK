
using Pawtinhas.Domain;
using Pawtinhas.Domain.Interfaces;
using Pawtinhas.Infrastructure.Data;

namespace Pawtinhas.Infrastructure.Repositories;

public class ResponsavelRepository
    : RepositoryBase<Responsavel>, IResponsavelRepository
{
    public ResponsavelRepository(AppDbContext context)
        : base(context)
    {
    }
}
