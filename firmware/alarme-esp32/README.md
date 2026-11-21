# Alarme de medicação (ESP32)

Caixinha que toca os alarmes cadastrados no Pawtinhas, pelo site ou pelo app. Ela busca os alarmes ativos no servidor do Pawtinhas na internet (https://pawtinhas.vercel.app) a cada 20 segundos, toca o buzzer no horário, mostra o nome do alarme e do pet no display e registra no histórico.

Funciona longe do computador:

- **Sem cabo**: ligada num carregador de celular, num power bank ou numa fonte de 5 V. Depois da primeira gravação, dá até para atualizar o programa pelo Wi-Fi.
- **Wi-Fi sem nada no código**: a rede é escolhida pelo celular. A placa guarda até 5 redes (casa, clínica, roteador do celular...) e conecta sozinha na que estiver por perto.
- **Qualquer Wi-Fi com internet**: não precisa estar na mesma rede do computador, nem do computador ligado. Clínica, casa ou o roteador do celular servem.
- **Sem internet continua tocando**: os alarmes ficam guardados na memória da placa, mesmo se ela for desligada da tomada. Os disparos feitos sem conexão vão para o histórico quando a conexão volta.

## Peças

- ESP32 DevKit (38 pinos) e cabo USB **de dados** para a primeira gravação (alguns cabos só carregam).
- Display LCD 16x2 com módulo I2C soldado atrás (a plaquinha preta com o parafuso azul).
- Módulo buzzer de 3 pinos (VCC, I/O, GND).
- 7 jumpers **fêmea-fêmea**. Os pinos da placa, do display e do buzzer são todos machos. Com jumpers macho-macho, é preciso uma protoboard no meio.
- Para usar sem o computador: carregador de celular (5 V, 1 A já basta) ou power bank. Alguns power banks desligam sozinhos quando o consumo é baixo. Se isso acontecer, use um carregador de tomada.

## Ligações

| De | Para (ESP32) |
|---|---|
| LCD GND | GND |
| LCD VCC | 5V (às vezes escrito VIN) |
| LCD SDA | 21 |
| LCD SCL | 22 |
| Buzzer GND | GND (outro pino GND) |
| Buzzer VCC | 3V3 |
| Buzzer I/O | 25 |

O botão **BOOT** é o da própria placa, não precisa ligar nada.

## Gravar o programa (PlatformIO)

```
firmware/alarme-esp32/
├── platformio.ini        placa, bibliotecas e os dois jeitos de gravar (cabo e Wi-Fi)
├── senha_ota.py          usa a chave do segredos.h como senha da gravação pelo Wi-Fi
├── src/main.cpp          o programa
└── include/
    ├── segredos.h        chave do dispositivo (fica fora do repositório)
    └── segredos.exemplo.h
```

1. No VS Code, instale a extensão **PlatformIO IDE** e espere ela terminar de configurar (a primeira vez demora alguns minutos).
2. **Arquivo > Abrir pasta** e escolha `firmware/alarme-esp32` (a pasta que tem o `platformio.ini`).
3. Se `include/segredos.h` não existir, copie `segredos.exemplo.h` com esse nome e cole a chave (`Dispositivo:Chave` da API). **Não tem Wi-Fi nesse arquivo.**
4. **Primeira vez, pelo cabo:** ligue o ESP32 no USB e, na barra azul de baixo do VS Code, use **→ Upload** (ambiente `esp32dev`). Se parar em `Connecting....`, segure o botão **BOOT** até começar a gravar.
5. **Depois, sem cabo:** com a placa ligada e no mesmo Wi-Fi do computador, escolha o ambiente `env:sem-cabo` (na barra azul) e use **→ Upload**. Se o computador não achar `pawtinhas-alarme.local`, troque `upload_port` no `platformio.ini` pelo IP que a placa mostra (um toque no BOOT).

Se a porta da placa não aparecer, instale o driver do chip USB: CP210x (Silicon Labs) ou CH340, conforme o que estiver escrito no chip ao lado do conector.

## Primeira vez num lugar novo (configurar o Wi-Fi)

1. Ligue a caixinha. Sem uma rede conhecida por perto, o display alterna entre **Configurar Wi-Fi / Pawtinhas-Alarme** e **Senha: pawtinhas / Abra 192.168.4.1**.
2. No celular, entre no Wi-Fi **Pawtinhas-Alarme** com a senha **pawtinhas**. A página de configuração abre sozinha. Se não abrir, acesse `192.168.4.1` no navegador.
3. Escolha a rede do lugar, digite a senha e toque em **Salvar e conectar**. O display mostra o nome da rede e o IP quando conectar.
4. Deixe o campo **Endereço da API** em branco: a placa usa o servidor na internet. Para testar com a API rodando no computador (mesma rede), digite `local`; para um endereço específico, digite-o (ex.: `http://192.168.0.10:5135`).

O roteador do celular também serve. Só redes de **2,4 GHz** (o ESP32 não conecta em 5 GHz).

## O botão BOOT

| Quando | O que faz |
|---|---|
| Alarme tocando | Desliga o alarme (fica no histórico como "Tocou e foi desligado no botão") |
| Tela parada, um toque | Mostra a rede, o IP, quantos alarmes estão guardados e se a API responde |
| Segurar 3 segundos | Abre a configuração do Wi-Fi (segurar de novo fecha) |

Sem ninguém apertar, o alarme toca por 45 segundos.

## O computador da API

A API precisa aceitar conexões da rede (o perfil padrão do `dotnet run` já usa `http://*:5135`) e o firewall do Windows precisa deixar passar. Uma vez, no PowerShell **como administrador**, na pasta `Pawtinhas-Back`:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\liberar-rede.ps1
```

Ele libera a porta 5135 (TCP) e a 5199 (UDP, onde a placa procura a API) só para aparelhos da rede local.

Com a API no **Docker**, a procura automática não chega ao contêiner. Nesse caso, digite o endereço no portal (`IP-do-computador:5135`).

## Conferindo

- Ao ligar, o buzzer dá um bipe curto e o display mostra **Pawtinhas / Alarme de remedio**.
- Na tela parada aparece a hora e o próximo alarme (`Proximo: 14:00`, `Amanha: 06:00` ou `Prox: sex 09:00`).
- Em **Configurações** do painel, o card do ESP32 mostra o horário do último contato.
- Para testar, crie um alarme para daqui a 2 minutos. A placa recebe em até 20 segundos, toca no horário e o disparo aparece em **Alarmes > Histórico**.

## Problemas comuns

| O que aparece | O que fazer |
|---|---|
| Display aceso, mas sem letras | Gire o parafuso azul atrás do display até as letras aparecerem |
| Display apagado | Confira VCC no 5V e GND |
| Buzzer apita sem parar ao ligar | Em `src/main.cpp`, troque `BUZZER_TOCA_EM_LOW` para `false` e grave de novo |
| `Configurar Wi-Fi` | Nenhuma rede guardada está por perto: configure pelo celular (veja acima) |
| `Sem Wi-Fi` (alternando com o próximo alarme) | A rede caiu. Os alarmes guardados continuam tocando e a placa tenta de novo a cada 30 s |
| `Procurando API` | A API não respondeu na rede: confira se ela está rodando, se o computador está no mesmo Wi-Fi e se rodou o `liberar-rede.ps1`. Ou digite o endereço no portal |
| `API fora do ar` | A API parou de responder. A placa procura de novo sozinha (o IP do computador pode ter mudado) |
| `Chave invalida` | A chave de `include/segredos.h` não é a mesma da API |
| `ESP32 desligado` | A API está sem `Dispositivo:Chave` configurada |
| `Sem alarmes` | Não há alarme ativo, ou "Alarmes no ESP32" está desligado em Configurações |

"Semanal" toca no dia da semana da data de início do alarme, e "Uma vez" toca só nessa data. São as mesmas regras do site e do app, então todos tocam no mesmo dia.
