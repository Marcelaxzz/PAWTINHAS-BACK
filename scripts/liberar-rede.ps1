# Libera a API do Pawtinhas para o celular e o alarme ESP32 da mesma rede, em qualquer Wi-Fi
# (rede doméstica, da clínica ou o roteador do celular, que o Windows costuma marcar como "Pública").
#   TCP 5135: a API (site, app e ESP32)
#   UDP 5199: a placa procurando a API na rede
# Só aceita aparelhos da rede local (LocalSubnet), nunca da internet.
# Uso (PowerShell como administrador):  powershell -ExecutionPolicy Bypass -File scripts\liberar-rede.ps1
param(
  [int]$PortaApi = 5135,
  [int]$PortaDescoberta = 5199
)
$ErrorActionPreference = 'Stop'

$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
  [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $admin) { throw 'Abra o PowerShell como administrador (botão direito > Executar como administrador).' }

$regras = @(
  @{ Nome = "Pawtinhas API $PortaApi (rede local)"; Protocolo = 'TCP'; Porta = $PortaApi },
  @{ Nome = "Pawtinhas ESP32 procura $PortaDescoberta (rede local)"; Protocolo = 'UDP'; Porta = $PortaDescoberta }
)
foreach ($r in $regras) {
  Get-NetFirewallRule -DisplayName $r.Nome -ErrorAction SilentlyContinue | Remove-NetFirewallRule
  New-NetFirewallRule -DisplayName $r.Nome -Direction Inbound -Action Allow -Profile Any `
    -Protocol $r.Protocolo -LocalPort $r.Porta -RemoteAddress LocalSubnet | Out-Null
  Write-Host "Liberado: $($r.Protocolo) $($r.Porta)"
}
Write-Host 'Pronto. O celular e o ESP32 da mesma rede já alcançam a API.'
