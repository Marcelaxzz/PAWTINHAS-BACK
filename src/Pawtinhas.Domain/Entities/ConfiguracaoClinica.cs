using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>Configuração única da clínica: dados cadastrais, notificações e integrações.</summary>
public class ConfiguracaoClinica
{
    public int Id { get; private set; }

    public string NomeClinica { get; private set; } = string.Empty;
    public string Endereco { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;
    public string Cnpj { get; private set; } = string.Empty;

    /// <summary>Envia a agenda do dia por e-mail para a veterinária, às 7h.</summary>
    public bool ResumoDiarioEmail { get; private set; }

    /// <summary>Com a opção desligada, o ESP32 recebe a lista de alarmes vazia.</summary>
    public bool AlarmesEsp32 { get; private set; } = true;

    /// <summary>Cópia diária dos dados em JSON na pasta de backups, às 3h.</summary>
    public bool BackupAutomatico { get; private set; }

    public DateTime? DispositivoUltimoContato { get; private set; }
    public DateTime? UltimoBackup { get; private set; }
    public DateTime? UltimoResumo { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private ConfiguracaoClinica() { }

    public static ConfiguracaoClinica Padrao(DateTime agora) => new() { NomeClinica = "Clínica Pawtinhas", AtualizadoEm = agora };

    public void Atualizar(
        string nomeClinica, string? endereco, string? telefone, string? cnpj,
        bool resumoDiarioEmail, bool alarmesEsp32, bool backupAutomatico, DateTime agora)
    {
        NomeClinica = Validar.Texto(nomeClinica, "clinica.nome", 80, "Informe o nome da clínica.");
        Endereco = Validar.TextoLivre(endereco, "clinica.endereco", 160);
        Telefone = Validar.TextoLivre(telefone, "clinica.telefone", 20);
        Cnpj = NormalizarCnpj(cnpj);
        ResumoDiarioEmail = resumoDiarioEmail;
        AlarmesEsp32 = alarmesEsp32;
        BackupAutomatico = backupAutomatico;
        AtualizadoEm = agora;
    }

    /// <summary>Registra o contato do ESP32 no máximo uma vez por minuto (evita escrita a cada pedido).</summary>
    public bool RegistrarContatoDispositivo(DateTime agora)
    {
        if (DispositivoUltimoContato is { } ultimo && agora - ultimo < TimeSpan.FromMinutes(1)) return false;
        DispositivoUltimoContato = agora;
        return true;
    }

    public void RegistrarBackup(DateTime agora) => UltimoBackup = agora;

    public void RegistrarResumo(DateTime agora) => UltimoResumo = agora;

    /// <summary>CNPJ opcional; quando informado, os dígitos verificadores precisam conferir.</summary>
    private static string NormalizarCnpj(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return "";
        var d = new string(cnpj.Where(char.IsDigit).ToArray());
        if (d.Length != 14 || d.Distinct().Count() == 1 || !Confere(d, 12) || !Confere(d, 13))
            throw new DominioException("clinica.cnpj", "CNPJ inválido. Confira os números.");
        return $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}";

        static bool Confere(string d, int posicao)
        {
            int[] pesos = posicao == 12 ? [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2] : [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            var soma = 0;
            for (var i = 0; i < posicao; i++) soma += (d[i] - '0') * pesos[i];
            var resto = soma % 11;
            return (resto < 2 ? 0 : 11 - resto) == d[posicao] - '0';
        }
    }
}
