using Pawtinhas.Domain.Comum;

namespace Pawtinhas.Domain;

/// <summary>Registro de um alarme tocado (enviado pelo ESP32). Guarda o nome para o histórico sobreviver à exclusão.</summary>
public class DisparoAlarme
{
    public int Id { get; private set; }
    public int? AlarmeId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public DateTime Quando { get; private set; }
    public string Resultado { get; private set; } = string.Empty;

    private DisparoAlarme() { }

    public DisparoAlarme(Alarme alarme, DateTime quando, string resultado)
    {
        AlarmeId = alarme.Id;
        Nome = alarme.Nome;
        Quando = quando;
        Resultado = Validar.Texto(resultado, "resultado", 60, "Informe o resultado do disparo.");
    }
}
