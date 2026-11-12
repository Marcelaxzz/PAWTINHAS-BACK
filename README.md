# PAWTINHAS-BACK

API da plataforma Pawtinhas em **C# / ASP.NET Core (.NET 10)**, **Entity Framework Core + MySQL (Pomelo)** e autenticação **JWT em cookie httpOnly** com **verificação em duas etapas por e-mail**.

Atende o site (Pawtinhas-Front) e o app (Pawtinhas-Mobile).

## Arquitetura

A base é a da equipe: camadas, repositórios e UnitOfWork.

```
src/
├── Pawtinhas.Domain/          Entidades com regras (Animal, Responsavel, Evento e tipos de agenda, Alarme,
│                              Medicao, Medicamento, Prescricao, PlanoAlimentar, Usuario...), enums,
│                              interfaces dos repositórios e os cálculos clínicos (Calculos/).
├── Pawtinhas.Application/     DTOs e serviços de aplicação (*AppService), login em duas etapas
│                              (Autenticacao/), modelos de e-mail e opções de configuração.
├── Pawtinhas.Infrastructure/  AppDbContext, mapeamentos, migrações (Persistence/Migrations), repositórios,
│                              UnitOfWork, JWT, hash de senha, SMTP, fotos, backup, rotinas e importação.
└── Pawtinhas.API/             Controllers finos, middlewares (erros, cabeçalhos, CSRF, sessão ativa),
                               cookies da sessão, limites de requisição e comandos de manutenção (Cli/).
```

- Regras de negócio ficam nas entidades. Uma regra violada gera `DominioException` (campo + mensagem), e o middleware de erros responde `400 { message, code, fields }`.
- O tratamento de erros fica num middleware, não nos controllers. "Não encontrado" vira 404 e conflito vira 409; um erro inesperado vira 500 sem detalhes internos.
- A agenda usa herança em tabela única (TPH), como a equipe definiu: `Evento` → `Consulta`, `Vacina`, `Cirurgia`, `BanhoTosa`, `PetSitter`, `Emergencia`.
- Um animal é **paciente** quando tem responsável. Aparece na **vitrine de adoção** quando tem `PerfilAdocao`, com a situação em `SituacaoAnimal` (`EM_DOACAO`, `EM_PROCESSO_ADOCAO`, `ADOTADO`).

## Requisitos

- [.NET SDK 10](https://dot.net)
- MySQL 8 (ou Docker)
- A conta Gmail que envia os códigos (pawtinhas04@gmail.com), com uma **senha de app**

## Configuração

Nenhum segredo fica no código nem no `appsettings.json`. Em desenvolvimento, use *user-secrets* (na pasta `src/Pawtinhas.API`):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=pawtinhas;User=pawtinhas;Password=SUA_SENHA"
dotnet user-secrets set "Jwt:Chave" "$([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48)))"
dotnet user-secrets set "Email:Senha" "SENHA_DE_APP_DO_GMAIL"
dotnet user-secrets set "Dispositivo:Chave" "$([Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24)))"
```

A **senha de app** é gerada em <https://myaccount.google.com/apppasswords>, entrando com pawtinhas04@gmail.com. A conta Google precisa ter a verificação em duas etapas ativada. Use as 16 letras, sem espaços, e nunca a senha normal da conta.

Sem senha de app, dá para testar com `Email:Modo = Pasta`: os e-mails viram arquivos `.eml` em `dados/emails` (só em Development).

| Chave | O que é |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Conexão do MySQL |
| `Jwt:Chave` | Chave de assinatura do JWT (mínimo 32 caracteres) |
| `Autenticacao:EmailPermitido` | Único e-mail que entra no painel: `carolpawtinhas@gmail.com` |
| `Email:Usuario`, `Email:Remetente` | `pawtinhas04@gmail.com` (já no appsettings) |
| `Email:Senha` | Senha de app do Gmail (segredo) |
| `Dispositivo:Chave` | Chave do ESP32 (cabeçalho `X-Device-Key`). Vazia desliga a integração |
| `Dispositivo:PortaDescoberta` | Porta UDP em que a API responde ao ESP32 que procura por ela na rede (padrão 5199, 0 desliga) |
| `Frontend:Url` | Endereço do site, usado no rodapé dos e-mails |
| `Cors:Origens` | Origens liberadas, só se o site ficar em outro domínio |
| `ProxyReverso:Enderecos` | IPs do proxy reverso (Nginx, Caddy, balanceador) |
| `BancoDeDados:MigrarAoIniciar` | Aplica as migrações ao subir (ligado em Development) |

## Rodando

```powershell
cd src\Pawtinhas.API
dotnet run -- migrar                                    # cria/atualiza o banco
dotnet run -- usuario --nome "Carolina Gonçalves"       # cria a conta de carolpawtinhas@gmail.com (pede a senha)
dotnet run -- dados-exemplo                             # opcional: dados fictícios para demonstração
dotnet run                                              # API em http://localhost:5135 (Swagger em /swagger), aberta para a rede local
```

Com Docker: copie `.env.example` para `.env`, preencha e rode `docker compose up --build`. A API sobe em <http://localhost:5135> (a mesma porta do `dotnet run`), aplica as migrações sozinha e roda sem privilégios de root. O MySQL do Docker fica na porta 3307, para não conflitar com um MySQL instalado no Windows. Fotos e backups ficam no volume `pawtinhas_dados`.

### Comandos de manutenção

| Comando | O que faz |
| --- | --- |
| `migrar` | Aplica as migrações |
| `usuario --nome "Nome"` | Cria ou atualiza a conta do e-mail permitido. A senha é digitada sem aparecer, ou lida com `--senha-stdin` |
| `dados-exemplo [--substituir]` | Importa `Seed/dados-exemplo.json` (dados fictícios) |
| `importar arquivo.json [--substituir]` | Importa dados de exemplo ou um backup da versão anterior da API |
| `importar Seed/complemento-catalogo.json --acrescentar` | Soma ao banco atual, sem apagar nada: 45 animais para adoção (fotos reais em domínio público, ver `Seed/imagens/CREDITOS-ADOCAO.md`), 40 medicamentos e 53 materiais. Nomes que já existem são pulados |

A senha nunca vai como argumento da linha de comando, porque ficaria no histórico do terminal.

## Publicar no Railway

São três serviços no mesmo projeto do Railway: **MySQL**, **API** (este repositório) e **site** ([PAWTINHAS-FRONT](https://github.com/Marcelaxzz/PAWTINHAS-FRONT)). Só o site ganha domínio público: ele repassa `/api` para a API pela rede interna, e assim os cookies de sessão (SameSite=Strict) funcionam.

1. **MySQL:** *New* > *Database* > *MySQL*.
2. **API:** *New* > *GitHub Repo* > `PAWTINHAS-BACK`. O Railway monta pelo `Dockerfile`. Não gere domínio público para ela. Em *Settings* > *Volumes*, crie um volume montado em `/app/dados` (fotos e backups). Em *Variables*:

   | Variável | Valor |
   | --- | --- |
   | `ConnectionStrings__DefaultConnection` | `Server=${{MySQL.MYSQLHOST}};Port=${{MySQL.MYSQLPORT}};Database=${{MySQL.MYSQLDATABASE}};User=${{MySQL.MYSQLUSER}};Password=${{MySQL.MYSQLPASSWORD}}` |
   | `Jwt__Chave` | 48+ caracteres aleatórios (veja `.env.example`) |
   | `Email__Senha` | Senha de app do Gmail de pawtinhas04@gmail.com |
   | `Frontend__Url` | Endereço público do site, ex.: `https://pawtinhas.up.railway.app` (links dos e-mails) |
   | `BancoDeDados__MigrarAoIniciar` | `true` |
   | `ProxyReverso__Enderecos__0` | `*` (a API só recebe pedidos pelo proxy do Railway) |
   | `Dispositivo__PortaDescoberta` | `0` (o Railway não tem UDP) |
   | `Dispositivo__Chave` | Chave do alarme ESP32 (vazia desliga) |
   | `RAILWAY_RUN_UID` | `0` (o volume do Railway só aceita gravação do root) |
   | `PORT` | `8080` (a porta que o site usa para chegar na API) |

3. **Site:** siga a seção "Railway" do README do Front.
4. **Conta da veterinária**, uma vez, depois do primeiro deploy. Com a [CLI do Railway](https://docs.railway.com/guides/cli), na pasta deste repositório:

   ```powershell
   railway link                     # escolha o projeto e o serviço da API
   railway ssh
   dotnet Pawtinhas.API.dll usuario --nome "Carolina Gonçalves"
   dotnet Pawtinhas.API.dll importar Seed/complemento-catalogo.json --acrescentar   # opcional
   ```

A caixinha ESP32 acha a API por UDP na rede local, o que não existe no Railway: ela continua funcionando com a API rodando no computador da clínica.

## Segurança

**Login em duas etapas**
- O usuário informa e-mail e senha, e um código de 6 dígitos chega por e-mail. Só depois a sessão é aberta.
- Só `Autenticacao:EmailPermitido` entra. Qualquer outro e-mail recebe exatamente a mesma resposta ("E-mail ou senha incorretos."), e a senha é conferida mesmo assim, para o tempo de resposta não revelar nada.
- O código vale 10 minutos, aceita 5 tentativas e é guardado só como HMAC-SHA256. Reenvio só a cada 60 s, no máximo 5 vezes.
- Depois de 5 senhas erradas, a conta fica bloqueada por 15 minutos. A troca de senha gera código no máximo 3 vezes por hora.
- A senha é guardada com PBKDF2 (PasswordHasher do ASP.NET Identity) e precisa de 8+ caracteres, maiúscula, minúscula, número e símbolo.

**Sessão (middleware)**
- O JWT de acesso dura 15 minutos e vai num cookie `httpOnly`, `SameSite=Strict`, `Secure` em produção, restrito a `/api`. O token de renovação é rotativo, fica só em `/api/auth` e é guardado como hash.
- Se um token de renovação já usado for reapresentado, a família inteira é revogada (cookie copiado).
- O `SessaoAtivaMiddleware` confere a cada requisição se a sessão do JWT ainda existe. O logout derruba o acesso na hora, mesmo para quem copiou o cookie.
- Toda rota exige a sessão da veterinária (política padrão). As exceções públicas são marcadas com `[AllowAnonymous]`: login, vitrine de adoção, fotos e ESP32, este com chave própria.

**Requisições**
- CSRF por *double submit*: o cookie `XSRF-TOKEN` precisa voltar no cabeçalho `X-XSRF-TOKEN`. O token também sai no cabeçalho de resposta, para o app mobile.
- Limite de requisições por IP, com um limite mais rígido no login.
- Cabeçalhos de segurança: CSP, `nosniff`, `DENY` para frames, HSTS e HTTPS em produção. O worker de visão computacional do site recebe uma CSP própria, que libera WebAssembly e bloqueia a rede.
- Fotos validadas pela assinatura do arquivo (PNG, JPEG, WebP), com até 5 MB e nome aleatório de 128 bits.
- Os DTOs só aceitam os campos declarados (sem mass assignment), e os erros 500 não expõem detalhes.
- Pacotes fixados em versões sem vulnerabilidade conhecida: MySqlConnector 2.6.2 e System.Security.Cryptography.Pkcs 10.0.13.

## Endpoints

Todos sob `/api`. As rotas da equipe foram mantidas (`/animals`, `/responsaveis`, `/eventos`).

| Rota | Métodos | Observação |
| --- | --- | --- |
| `/auth/login`, `/auth/login/verify`, `/auth/code/resend` | POST | Login em duas etapas |
| `/auth/password/forgot`, `/auth/password/verify` | POST | Troca de senha com código |
| `/auth/refresh`, `/auth/logout`, `/auth/me` | POST, POST, GET | Sessão |
| `/animals` | GET, POST, `/{id}` GET, PUT, DELETE | Pacientes (animais com responsável) |
| `/responsaveis` | GET, POST, `/{id}` GET, PUT, DELETE | Tutores (CPF validado, único) |
| `/eventos?tipo=&animalId=` | GET, POST, `/{id}` GET, PUT, DELETE, `/{id}/status` PATCH | Agenda |
| `/adocao` | GET e `/{id}` públicos; POST, PUT, DELETE | Vitrine de adoção |
| `/profissionais`, `/medicamentos`, `/racoes` | GET, POST, PUT, DELETE | Cadastros |
| `/alarmes` | GET, POST, PUT, DELETE, `/{id}/ativo` PATCH, `/historico` GET | Alarmes (ESP32, app e navegador). `inicio` (data) define o dia de "Semanal" e "Somente hoje" |
| `/medicoes?animalId=` | GET, POST, DELETE | Medições da prótese 3D |
| `/calculos/fatores`, `/calculos/dose`, `/calculos/alimentacao` | GET, POST, POST | Simulações |
| `/prescricoes`, `/planos-alimentares` | GET, POST, DELETE (+ PATCH na prescrição) | Salvos na ficha |
| `/configuracoes`, `/configuracoes/status`, `/configuracoes/backup` | GET, PUT, GET, GET | Clínica |
| `/arquivos` | POST (multipart, campo `arquivo`), `/{nome}` GET público | Fotos |
| `/dispositivo/alarmes`, `/dispositivo/disparos` | GET, POST | ESP32 (`X-Device-Key`). O disparo aceita `quando`, para os que tocaram sem internet |

## Cálculos e fontes

| Cálculo | Fórmula / valor | Fonte |
| --- | --- | --- |
| Energia de repouso | RER = 70 × peso(kg)^0,75 | Merck Veterinary Manual; AAHA 2021, Box 1 |
| Manutenção | MER = RER × fator (cão castrado 1,6; inteiro 1,8; filhote < 4 meses 3,0; 4 meses até adulto 2,0; gato castrado 1,2; inteiro 1,4; filhote 2,5...) | Merck, tabela *Daily Maintenance Energy Requirements*; AAHA 2021 (perda de peso, gestação, lactação, trabalho) |
| Água | 44 a 66 mL/kg/dia | Merck Veterinary Manual |
| Petiscos | até 10% das calorias | AAHA, manejo de peso |
| Dose | dose (mg) = peso × mg/kg; quantidade = dose ÷ concentração | Faixas por medicamento, com a fonte gravada no catálogo (bulas FDA/EMA, consenso ACVIM...) |

Todo fator e todo medicamento guardam a fonte, que aparece nas telas.

## Alarme ESP32

O código da caixinha de alarme (ESP32, display LCD 16x2 I2C e buzzer) e o passo a passo de montagem estão em [`firmware/alarme-esp32`](firmware/alarme-esp32/README.md). Ela usa `GET /api/dispositivo/alarmes` e `POST /api/dispositivo/disparos` com a chave `Dispositivo:Chave` no cabeçalho `X-Device-Key`.

A placa não tem Wi-Fi nem IP no código: a rede é escolhida pelo celular e a API é achada pela rede (a placa manda `PAWTINHAS?` por UDP na porta 5199 e a API responde com a porta HTTP). Para o celular e a placa alcançarem a API em qualquer Wi-Fi, rode uma vez, como administrador:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\liberar-rede.ps1
```

Os alarmes também tocam no app do celular (notificação agendada, funciona sem internet) e no painel aberto no navegador. As regras de quando cada um toca são as mesmas nos três (`Pawtinhas-Front/src/utils/alarmes.js` e `firmware/alarme-esp32/src/main.cpp`).

## Migrações

Nova migração (na pasta `Pawtinhas-Back`):

```powershell
dotnet tool restore
dotnet ef migrations add NomeDaMigracao --project src/Pawtinhas.Infrastructure --startup-project src/Pawtinhas.API --output-dir Persistence/Migrations
```

A migração `PlataformaCompleta` vem depois das migrações da equipe: um banco já criado por elas é atualizado sem perder dados.
