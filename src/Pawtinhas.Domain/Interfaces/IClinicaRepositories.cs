using Pawtinhas.Domain;

namespace Pawtinhas.Domain.Interfaces;

public interface IAlarmeRepository : IRepository<Alarme>
{
    /// <summary>Alarmes ativos com o animal carregado, por horário (lista enviada ao ESP32).</summary>
    Task<IReadOnlyList<Alarme>> ListarAtivosAsync();

    Task<IReadOnlyList<DisparoAlarme>> UltimosDisparosAsync(int quantidade);

    Task AdicionarDisparoAsync(DisparoAlarme disparo);
}

public interface IPrescricaoRepository : IRepository<Prescricao>
{
    Task<IReadOnlyList<Prescricao>> ListarAsync(int? animalId);
}

public interface IPlanoAlimentarRepository : IRepository<PlanoAlimentar>
{
    Task<IReadOnlyList<PlanoAlimentar>> ListarAsync(int? animalId);
}

public interface IConfiguracaoRepository
{
    /// <summary>A configuração é única; é criada com os valores padrão no primeiro acesso.</summary>
    Task<ConfiguracaoClinica> ObterAsync();
}

public interface IEstoqueRepository
{
    /// <summary>Todos os itens com os lotes carregados.</summary>
    Task<IReadOnlyList<ItemEstoque>> ListarAsync();

    /// <summary>Item com os lotes, para movimentar. Nulo se o item ainda não tem estoque.</summary>
    Task<ItemEstoque?> ObterAsync(string categoria, int itemId);

    Task AdicionarAsync(ItemEstoque item);

    Task AdicionarMovimentacaoAsync(MovimentacaoEstoque movimentacao);

    /// <summary>Histórico mais recente primeiro, com o item carregado.</summary>
    Task<IReadOnlyList<MovimentacaoEstoque>> MovimentacoesAsync(string? categoria, int? itemId, DateTime? de, DateTime? ate, int limite);

    /// <summary>Remove o estoque de um item excluído do catálogo.</summary>
    Task RemoverItemAsync(string categoria, int itemId);
}

public interface IRelatorioServicoRepository
{
    Task<RelatorioServico?> ObterPorEventoAsync(int eventoId);

    Task AdicionarAsync(RelatorioServico relatorio);

    /// <summary>Ids dos atendimentos que já têm relatório preenchido.</summary>
    Task<IReadOnlyList<int>> EventosComRelatorioAsync();
}
