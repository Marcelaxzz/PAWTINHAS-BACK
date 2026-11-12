# Banco local do Pawtinhas sem Docker: uma instância própria do MySQL já instalado no Windows,
# com pasta de dados separada e porta 3307 (não mexe no serviço MySQL que já existir na 3306).
#
# Na primeira vez, cria a pasta de dados, o banco e o usuário com os valores do .env
# (MYSQL_ROOT_PASSWORD, MYSQL_DATABASE, MYSQL_USER, MYSQL_PASSWORD). Depois, só liga o servidor.
# Uso:  powershell -ExecutionPolicy Bypass -File scripts\banco-local.ps1
param(
  [int]$Porta = 3307,
  [string]$Dados = "$env:LOCALAPPDATA\Pawtinhas\mysql"
)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path $PSScriptRoot -Parent

$envArquivo = Join-Path $raiz '.env'
if (-not (Test-Path $envArquivo)) { throw "Crie o .env a partir do .env.example antes de subir o banco." }
$cfg = @{}
foreach ($linha in Get-Content $envArquivo) {
  if ($linha -match '^\s*([A-Z_]+)\s*=\s*(.*)$') { $cfg[$Matches[1]] = $Matches[2].Trim() }
}
foreach ($chave in 'MYSQL_ROOT_PASSWORD', 'MYSQL_DATABASE', 'MYSQL_USER', 'MYSQL_PASSWORD') {
  if (-not $cfg[$chave]) { throw "Defina $chave no .env." }
}

$bin = Get-ChildItem 'C:\Program Files\MySQL' -Directory -Filter 'MySQL Server*' -ErrorAction SilentlyContinue |
  Sort-Object Name -Descending | Select-Object -First 1
if (-not $bin) { throw 'MySQL Server não encontrado em C:\Program Files\MySQL.' }
$mysqld = Join-Path $bin.FullName 'bin\mysqld.exe'
$mysql = Join-Path $bin.FullName 'bin\mysql.exe'

if (Get-NetTCPConnection -LocalPort $Porta -State Listen -ErrorAction SilentlyContinue) {
  Write-Host "Banco do Pawtinhas já está ligado na porta $Porta."
  exit 0
}

$primeiraVez = -not (Test-Path (Join-Path $Dados 'mysql'))
if ($primeiraVez) {
  New-Item -ItemType Directory -Force $Dados | Out-Null
  Write-Host 'Criando a pasta de dados do banco...'
  & $mysqld --initialize-insecure "--datadir=$Dados" | Out-Null
}

Write-Host "Ligando o MySQL do Pawtinhas na porta $Porta..."
$servidor = Start-Process $mysqld -ArgumentList "--datadir=`"$Dados`"", "--port=$Porta", '--mysqlx=OFF', '--bind-address=127.0.0.1' -WindowStyle Hidden -PassThru

for ($i = 0; $i -lt 60 -and -not (Get-NetTCPConnection -LocalPort $Porta -State Listen -ErrorAction SilentlyContinue); $i++) { Start-Sleep -Milliseconds 500 }
if (-not (Get-NetTCPConnection -LocalPort $Porta -State Listen -ErrorAction SilentlyContinue)) { throw 'O MySQL não respondeu.' }

if ($primeiraVez) {
  $sql = @"
ALTER USER 'root'@'localhost' IDENTIFIED BY '$($cfg.MYSQL_ROOT_PASSWORD)';
CREATE DATABASE IF NOT EXISTS ``$($cfg.MYSQL_DATABASE)`` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '$($cfg.MYSQL_USER)'@'localhost' IDENTIFIED BY '$($cfg.MYSQL_PASSWORD)';
GRANT ALL PRIVILEGES ON ``$($cfg.MYSQL_DATABASE)``.* TO '$($cfg.MYSQL_USER)'@'localhost';
FLUSH PRIVILEGES;
"@
  $sql | & $mysql --protocol=tcp --host=127.0.0.1 "--port=$Porta" --user=root
  if ($LASTEXITCODE -ne 0) { throw 'Não foi possível criar o banco e o usuário.' }
  Write-Host "Banco $($cfg.MYSQL_DATABASE) e usuário $($cfg.MYSQL_USER) criados."
}

Write-Host "Banco do Pawtinhas ligado (processo $($servidor.Id), porta $Porta, dados em $Dados)."
