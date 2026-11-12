namespace Pawtinhas.Application.Relatorios;

/// <summary>
/// Documento de relatório independente do gerador: título, cabeçalho da clínica e blocos em ordem.
/// O gerador (QuestPDF, na infraestrutura) só desenha; o conteúdo é montado aqui na aplicação.
/// </summary>
public sealed record DocumentoPdf(
    string Titulo,
    string Subtitulo,
    CabecalhoClinica Clinica,
    IReadOnlyList<BlocoPdf> Blocos,
    DateTime GeradoEm);

public sealed record CabecalhoClinica(string Nome, string Endereco, string Telefone, string Cnpj);

public sealed record Rotulado(string Rotulo, string Valor);

public abstract record BlocoPdf;

/// <summary>Números em destaque no topo (total de atendimentos, itens em falta...).</summary>
public sealed record BlocoIndicadores(IReadOnlyList<Rotulado> Itens) : BlocoPdf;

/// <summary>Pares rótulo/valor em colunas (dados do paciente, do atendimento...).</summary>
public sealed record BlocoCampos(string Titulo, IReadOnlyList<Rotulado> Campos, int Colunas = 3) : BlocoPdf;

/// <summary>Texto corrido com título (avaliação, orientações...). Vazio não é desenhado.</summary>
public sealed record BlocoTexto(string Titulo, string Texto) : BlocoPdf;

/// <summary>Tabela com cabeçalho; <see cref="Larguras"/> são pesos relativos das colunas.</summary>
public sealed record BlocoTabela(
    string Titulo,
    IReadOnlyList<string> Colunas,
    IReadOnlyList<IReadOnlyList<string>> Linhas,
    IReadOnlyList<float>? Larguras = null,
    string Vazio = "Nada no período.") : BlocoPdf;

/// <summary>Linha de assinatura no fim do documento.</summary>
public sealed record BlocoAssinatura(string Nome, string Detalhe) : BlocoPdf;

public interface IGeradorPdf
{
    byte[] Gerar(DocumentoPdf documento);
}

/// <summary>Arquivo pronto para download.</summary>
public sealed record ArquivoPdf(byte[] Conteudo, string Nome);
