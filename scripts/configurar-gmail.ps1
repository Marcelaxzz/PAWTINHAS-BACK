# Liga o envio real dos códigos de login pelo Gmail (pawtinhas04@gmail.com).
# Pede a senha de app sem mostrá-la e guarda nos user-secrets da API (fora do código).
# Gere a senha em https://myaccount.google.com/apppasswords, entrando com pawtinhas04@gmail.com.
# Uso:  powershell -ExecutionPolicy Bypass -File scripts\configurar-gmail.ps1
$ErrorActionPreference = 'Stop'
$api = Join-Path (Split-Path $PSScriptRoot -Parent) 'src\Pawtinhas.API'

$segura = Read-Host 'Senha de app do Gmail (16 letras)' -AsSecureString
$senha = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($segura))
$senha = $senha -replace '\s', ''
if ($senha.Length -ne 16) { throw 'A senha de app do Gmail tem 16 letras.' }

Push-Location $api
try {
  dotnet user-secrets set 'Email:Senha' $senha | Out-Null
  # Sai do modo de teste (códigos em arquivo) e volta ao envio pelo Gmail.
  dotnet user-secrets remove 'Email:Modo' | Out-Null
} finally {
  Pop-Location
}
Write-Host 'Pronto: os códigos de login agora saem de pawtinhas04@gmail.com. Reinicie a API.'
