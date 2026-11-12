using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>Textos e informações da vitrine pública de adoção (1:1 com o <see cref="Animal"/>).</summary>
public class PerfilAdocao
{
    public int AnimalId { get; private set; }

    public DateOnly DataEntrada { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public string Frase { get; private set; } = string.Empty;
    public string Sobre { get; private set; } = string.Empty;
    public string Tracos { get; private set; } = string.Empty;
    public string InfoAdicional { get; private set; } = string.Empty;
    public string Necessidades { get; private set; } = string.Empty;
    public bool ConviveAnimais { get; private set; }
    public bool ConviveCriancas { get; private set; }
    public bool Vacinado { get; private set; }
    public bool Castrado { get; private set; }

    private PerfilAdocao() { }

    public PerfilAdocao(
        DateOnly dataEntrada,
        string descricao,
        string? frase,
        string? sobre,
        string? tracos,
        string? infoAdicional,
        string? necessidades,
        bool conviveAnimais,
        bool conviveCriancas,
        bool vacinado,
        bool castrado)
    {
        if (dataEntrada > DateOnly.FromDateTime(DateTime.Today))
            throw new DominioException("dataEntrada", "A data de entrada não pode estar no futuro.");

        DataEntrada = dataEntrada;
        Descricao = Validar.Texto(descricao, "descricao", 600, "Escreva uma descrição curta para o card.");
        Frase = Validar.TextoLivre(frase, "frase", 140);
        Sobre = Validar.TextoLivre(sobre, "sobre", 1200);
        Tracos = Validar.TextoLivre(tracos, "personalidade", 100);
        InfoAdicional = Validar.TextoLivre(infoAdicional, "infoAdicional", 140);
        Necessidades = Validar.TextoLivre(necessidades, "necessidades", 140);
        ConviveAnimais = conviveAnimais;
        ConviveCriancas = conviveCriancas;
        Vacinado = vacinado;
        Castrado = castrado;
    }

    internal void CopiarDe(PerfilAdocao outro)
    {
        DataEntrada = outro.DataEntrada;
        Descricao = outro.Descricao;
        Frase = outro.Frase;
        Sobre = outro.Sobre;
        Tracos = outro.Tracos;
        InfoAdicional = outro.InfoAdicional;
        Necessidades = outro.Necessidades;
        ConviveAnimais = outro.ConviveAnimais;
        ConviveCriancas = outro.ConviveCriancas;
        Vacinado = outro.Vacinado;
        Castrado = outro.Castrado;
    }
}
