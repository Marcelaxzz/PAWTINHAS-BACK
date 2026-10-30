
namespace Pawtinhas.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task AdicionarAsync(T entidade);

    void Atualizar(T entidade);

    Task<T?> ObterPorIdAsync(int id);

    Task<IReadOnlyList<T>> ObterTodosAsync();

    void Remover(T entidade);
}