using System.Globalization;
using Pawtinhas.Application.Relatorios;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Pawtinhas.Infrastructure.Relatorios;

/// <summary>
/// Desenha os relatórios com o QuestPDF (licença Community: uso gratuito para projetos acadêmicos
/// e empresas pequenas). Mesma paleta do site: café, caramelo e creme.
/// </summary>
public sealed class GeradorPdf : IGeradorPdf
{
    private const string Cafe = "#6e4d36";
    private const string CafeEscuro = "#36241a";
    private const string Caramelo = "#d8a16b";
    private const string Taupe = "#897260";
    private const string Creme = "#fbf6eb";
    private const string Borda = "#e9dfca";

    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly byte[] Logo = CarregarLogo();

    static GeradorPdf() => QuestPDF.Settings.License = LicenseType.Community;

    public byte[] Gerar(DocumentoPdf doc) =>
        Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(34);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(t => t.FontSize(9.5f).FontColor(CafeEscuro).LineHeight(1.3f));

            page.Header().Element(c => Cabecalho(c, doc));
            page.Content().PaddingTop(18).Column(col =>
            {
                col.Spacing(16);
                foreach (var bloco in doc.Blocos) col.Item().Element(c => Bloco(c, bloco));
            });
            page.Footer().Element(c => Rodape(c, doc));
        }))
        .WithMetadata(new DocumentMetadata { Title = doc.Titulo, Author = doc.Clinica.Nome, Creator = "Pawtinhas" })
        .GeneratePdf();

    private static void Cabecalho(IContainer container, DocumentoPdf doc) => container.Column(col =>
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(150).AlignMiddle().Image(Logo).FitWidth();
            row.RelativeItem().PaddingLeft(16).AlignMiddle().Column(c =>
            {
                c.Item().Text(doc.Clinica.Nome).FontSize(11).Bold().FontColor(Cafe);
                var contato = string.Join(" · ", new[] { doc.Clinica.Endereco, doc.Clinica.Telefone }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                if (contato.Length > 0) c.Item().Text(contato).FontSize(8).FontColor(Taupe);
                if (!string.IsNullOrWhiteSpace(doc.Clinica.Cnpj)) c.Item().Text($"CNPJ {doc.Clinica.Cnpj}").FontSize(8).FontColor(Taupe);
            });
        });
        col.Item().PaddingTop(14).Background(Creme).Border(1).BorderColor(Borda).CornerRadius(8)
            .PaddingVertical(12).PaddingHorizontal(16).Row(row =>
            {
                row.ConstantItem(4).Background(Caramelo).CornerRadius(2);
                row.RelativeItem().PaddingLeft(12).Column(c =>
                {
                    c.Item().Text(doc.Titulo).FontSize(17).Bold().FontColor(CafeEscuro);
                    if (!string.IsNullOrWhiteSpace(doc.Subtitulo)) c.Item().Text(doc.Subtitulo).FontSize(9.5f).FontColor(Taupe);
                });
            });
    });

    private static void Rodape(IContainer container, DocumentoPdf doc) =>
        container.BorderTop(1).BorderColor(Borda).PaddingTop(8).Row(row =>
        {
            row.RelativeItem().Text($"Gerado pelo Pawtinhas em {doc.GeradoEm.ToString("dd/MM/yyyy 'às' HH:mm", Br)}")
                .FontSize(7.5f).FontColor(Taupe);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7.5f).FontColor(Taupe));
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        });

    private static void Bloco(IContainer container, BlocoPdf bloco)
    {
        switch (bloco)
        {
            case BlocoIndicadores b: Indicadores(container, b); break;
            case BlocoCampos b: Campos(container, b); break;
            case BlocoTexto b: Texto(container, b); break;
            case BlocoTabela b: Tabela(container, b); break;
            case BlocoAssinatura b: Assinatura(container, b); break;
        }
    }

    private static void TituloSecao(IContainer container, string titulo) =>
        container.PaddingBottom(6).Text(titulo).FontSize(11.5f).Bold().FontColor(Cafe);

    private static void Indicadores(IContainer container, BlocoIndicadores b) => container.Row(row =>
    {
        row.Spacing(8);
        foreach (var item in b.Itens)
        {
            row.RelativeItem().Border(1).BorderColor(Borda).CornerRadius(8).Background(Creme)
                .PaddingVertical(10).PaddingHorizontal(12).Column(c =>
                {
                    c.Item().Text(item.Valor).FontSize(18).Bold().FontColor(CafeEscuro);
                    c.Item().Text(item.Rotulo).FontSize(8).FontColor(Taupe);
                });
        }
    });

    private static void Campos(IContainer container, BlocoCampos b) => container.Column(col =>
    {
        col.Item().Element(c => TituloSecao(c, b.Titulo));
        col.Item().Border(1).BorderColor(Borda).CornerRadius(8).Padding(12).Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                for (var i = 0; i < b.Colunas; i++) c.RelativeColumn();
            });
            foreach (var campo in b.Campos)
            {
                t.Cell().PaddingVertical(4).PaddingRight(10).Column(c =>
                {
                    c.Item().Text(campo.Rotulo.ToUpper(Br)).FontSize(7).SemiBold().FontColor(Taupe).LetterSpacing(0.04f);
                    c.Item().Text(string.IsNullOrWhiteSpace(campo.Valor) ? "—" : campo.Valor).FontSize(10).SemiBold();
                });
            }
        });
    });

    private static void Texto(IContainer container, BlocoTexto b)
    {
        if (string.IsNullOrWhiteSpace(b.Texto)) return;
        container.Column(col =>
        {
            col.Item().Element(c => TituloSecao(c, b.Titulo));
            col.Item().BorderLeft(3).BorderColor(Caramelo).PaddingLeft(12).PaddingVertical(2).Text(b.Texto).FontSize(10);
        });
    }

    private static void Tabela(IContainer container, BlocoTabela b) => container.Column(col =>
    {
        col.Item().Element(c => TituloSecao(c, b.Titulo));
        if (b.Linhas.Count == 0)
        {
            col.Item().Border(1).BorderColor(Borda).CornerRadius(8).Padding(12).Text(b.Vazio).FontColor(Taupe).Italic();
            return;
        }
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                for (var i = 0; i < b.Colunas.Count; i++) c.RelativeColumn(b.Larguras?.ElementAtOrDefault(i) is > 0 and var w ? w : 1);
            });
            t.Header(h =>
            {
                foreach (var coluna in b.Colunas)
                    h.Cell().Background(Cafe).PaddingVertical(6).PaddingHorizontal(6)
                        .Text(coluna).FontSize(8).Bold().FontColor(Colors.White);
            });
            for (var i = 0; i < b.Linhas.Count; i++)
            {
                var fundo = i % 2 == 0 ? "#ffffff" : Creme;
                foreach (var celula in b.Linhas[i])
                    t.Cell().Background(fundo).BorderBottom(0.5f).BorderColor(Borda)
                        .PaddingVertical(5).PaddingHorizontal(6).Text(celula).FontSize(8.5f);
            }
        });
    });

    private static void Assinatura(IContainer container, BlocoAssinatura b) =>
        container.PaddingTop(36).AlignCenter().Width(240).Column(col =>
        {
            col.Item().BorderTop(1).BorderColor(CafeEscuro).PaddingTop(6).AlignCenter().Text(b.Nome).SemiBold();
            col.Item().AlignCenter().Text(b.Detalhe).FontSize(8).FontColor(Taupe);
        });

    private static byte[] CarregarLogo()
    {
        using var recurso = typeof(GeradorPdf).Assembly.GetManifestResourceStream("Pawtinhas.Infrastructure.Relatorios.logo-horizontal.png")
            ?? throw new InvalidOperationException("Logo dos relatórios não encontrado no assembly.");
        using var memoria = new MemoryStream();
        recurso.CopyTo(memoria);
        return memoria.ToArray();
    }
}
