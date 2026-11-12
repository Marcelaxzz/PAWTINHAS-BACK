namespace Pawtinhas.Domain.Enums
{
    public enum SituacaoAnimal{
        ATIVO = 1,
        INATIVO,
        EM_CIRURGIA,
        EM_DOACAO,
        ADOTADO,

        /// <summary>Animal da vitrine com uma adoção em andamento (entrevista, visita).</summary>
        EM_PROCESSO_ADOCAO
    }
}
