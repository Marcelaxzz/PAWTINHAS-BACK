// Alarme de medicação do Pawtinhas: ESP32 + display LCD 16x2 (I2C) + buzzer.
//
// Funciona longe do computador, ligado num carregador de celular ou power bank:
// - Wi-Fi sem nada no código: se não conhece a rede do lugar, a placa abre a rede "Pawtinhas-Alarme".
//   Pelo celular, escolha o Wi-Fi e digite a senha. Ela guarda até 5 redes e conecta sozinha na que achar.
// - Fala com o servidor do Pawtinhas na internet (pawtinhas.vercel.app), sem computador ligado. Para usar
//   a API de um computador da rede, digite "local" no portal: a placa pergunta "PAWTINHAS?" por UDP.
// - Guarda os alarmes na memória: sem internet ou com a API desligada, continua tocando os que já recebeu.
// - Botão BOOT: aperte para desligar o alarme. Com a tela parada, um toque mostra a rede e o IP;
//   segurando 3 s abre (ou fecha) a configuração do Wi-Fi.
// - Pode ser regravado sem cabo, pela rede (ambiente "sem-cabo" do platformio.ini).

#include <Arduino.h>
#include <WiFi.h>
#include <WiFiUdp.h>
#include <HTTPClient.h>
#include <NetworkClientSecure.h>
#include <WebServer.h>
#include <DNSServer.h>
#include <Preferences.h>
#include <ArduinoOTA.h>
#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <ArduinoJson.h>
#include <time.h>
#include <sys/time.h>

#if __has_include("segredos.h")
#include "segredos.h"
#else
#error "Copie include/segredos.exemplo.h para include/segredos.h e cole a chave do dispositivo."
#endif

#ifndef API_ENDERECO_PADRAO
#define API_ENDERECO_PADRAO "https://pawtinhas.vercel.app"
#endif

// ---------- Ligações e ajustes ----------

const int PINO_SDA = 21;
const int PINO_SCL = 22;
const int PINO_BUZZER = 25;
const int PINO_BOTAO = 0;  // botão BOOT da própria placa

// Buzzer que apita sem parar ao ligar: troque para false.
const bool BUZZER_TOCA_EM_LOW = true;

const unsigned long BUSCAR_A_CADA_MS = 20000;  // um alarme novo chega à placa em até 20 s
const unsigned long TOCAR_POR_MS = 45000;      // tempo que o alarme fica tocando se ninguém apertar o botão
const unsigned long SEGURAR_PARA_CONFIGURAR_MS = 3000;
const char *FUSO = "<-03>3";  // horário de Brasília, sem horário de verão

const char *REDE_CONFIGURACAO = "Pawtinhas-Alarme";
const char *SENHA_CONFIGURACAO = "pawtinhas";
const char *NOME_NA_REDE = "pawtinhas-alarme";
const uint16_t PORTA_DESCOBERTA = 5199;

// ---------- Estado ----------

enum Recorrencia : uint8_t { DIARIO, A_CADA_8H, A_CADA_12H, DIAS_UTEIS, SEMANAL, UMA_VEZ };

struct Alarme {
  int32_t id;
  char nome[41];
  char pet[41];
  int16_t minutoDoDia;
  uint8_t recorrencia;
  int32_t inicio;  // dias desde 01/01/1970; antes disso o alarme não toca
};

struct Rede {
  char nome[33];
  char senha[65];
};

// Como o toque terminou: tocou até o fim, desligado no botão da placa, no painel (site) ou no app.
enum ComoParou : uint8_t { PAROU_SOZINHO, PAROU_NO_BOTAO, PAROU_NO_PAINEL, PAROU_NO_APP };

struct Disparo {
  int32_t alarmeId;
  time_t quando;
  uint8_t comoParou;
};

const int MAX_ALARMES = 30;
const int MAX_REDES = 5;
const int MAX_PENDENTES = 12;

Alarme alarmes[MAX_ALARMES];
int totalAlarmes = 0;
Rede redes[MAX_REDES];
int totalRedes = 0;
Disparo pendentes[MAX_PENDENTES];
int totalPendentes = 0;

char apiFixa[101] = "";        // digitada no portal (vazia = servidor na internet; "local" = procurar na rede)
char apiEncontrada[101] = "";  // última API que respondeu à procura

enum SituacaoApi { API_PROCURANDO, API_OK, API_SEM_RESPOSTA, API_CHAVE_ERRADA, API_DESLIGADA, API_ERRO };
SituacaoApi situacaoApi = API_PROCURANDO;
int ultimoCodigoHttp = 0;
bool alarmesDaMemoria = false;  // a lista veio da memória (ainda não falou com a API desde que ligou)

Preferences memoria;
LiquidCrystal_I2C *lcd = nullptr;
WebServer servidor(80);
DNSServer dns;
WiFiClient clienteHttp;
NetworkClientSecure clienteHttps;

bool portalAberto = false;
bool fecharPortalAgora = false;
unsigned long ultimoUsoDoPortal = 0;
unsigned long ultimaTentativaWifi = 0;
unsigned long conectadoDesde = 0;
bool tentarWifiAgora = true;
bool otaLigado = false;
bool horaPeloNtpLigada = false;

unsigned long ultimaBusca = 0;
bool jaBuscou = false;
unsigned long ultimaProcura = 0;
long ultimoMinutoConferido = -1;
unsigned long mostrarInfoAte = 0;

// ---------- Utilidades ----------

long diasDesdeEpoca(int ano, int mes, int dia) {
  ano -= mes <= 2;
  const long era = (ano >= 0 ? ano : ano - 399) / 400;
  const unsigned anoDaEra = (unsigned)(ano - era * 400);
  const unsigned diaDoAno = (153 * (mes + (mes > 2 ? -3 : 9)) + 2) / 5 + dia - 1;
  const unsigned diaDaEra = anoDaEra * 365 + anoDaEra / 4 - anoDaEra / 100 + diaDoAno;
  return era * 146097 + (long)diaDaEra - 719468;
}

long diaDe(const struct tm &t) { return diasDesdeEpoca(t.tm_year + 1900, t.tm_mon + 1, t.tm_mday); }

bool horaValida() { return time(nullptr) > 1700000000; }

void semAcento(char *destino, const char *origem, size_t tamanho) {
  static const char TABELA[] = "AAAAAAACEEEEIIIIDNOOOOOxOUUUUYPsaaaaaaaceeeeiiiidnooooo/ouuuuypy";
  size_t j = 0;
  for (size_t i = 0; origem[i] && j < tamanho - 1; i++) {
    unsigned char c = origem[i];
    if (c < 0x80) {
      destino[j++] = c;
    } else if (c == 0xC3 && origem[i + 1]) {
      unsigned char d = origem[++i];
      destino[j++] = (d >= 0x80 && d <= 0xBF) ? TABELA[d - 0x80] : '?';
    } else if (c == 0xC2 && origem[i + 1]) {
      unsigned char d = origem[++i];
      destino[j++] = d == 0xBA ? 'o' : d == 0xAA ? 'a' : ' ';
    } else {
      while ((origem[i + 1] & 0xC0) == 0x80) i++;
      destino[j++] = '?';
    }
  }
  destino[j] = '\0';
}

void escrever(int linha, const char *texto) {
  if (!lcd) return;
  char limpo[41];
  semAcento(limpo, texto, sizeof limpo);
  char linhaLcd[17];
  snprintf(linhaLcd, sizeof linhaLcd, "%-16s", limpo);
  lcd->setCursor(0, linha);
  lcd->print(linhaLcd);
}

void tela(const char *linha0, const char *linha1) {
  escrever(0, linha0);
  escrever(1, linha1);
}

void buzzer(bool ligado) { digitalWrite(PINO_BUZZER, (ligado != BUZZER_TOCA_EM_LOW) ? HIGH : LOW); }

bool botaoApertado() { return digitalRead(PINO_BOTAO) == LOW; }

void bipe(int vezes, int ms = 120) {
  for (int i = 0; i < vezes; i++) {
    buzzer(true);
    delay(ms);
    buzzer(false);
    delay(ms);
  }
}

byte enderecoDoLcd() {
  const byte candidatos[] = {0x27, 0x3F};
  for (byte e : candidatos) {
    Wire.beginTransmission(e);
    if (Wire.endTransmission() == 0) return e;
  }
  return 0x27;
}

String escaparHtml(const char *texto) {
  String s;
  for (const char *p = texto; *p; p++) {
    switch (*p) {
      case '&': s += F("&amp;"); break;
      case '<': s += F("&lt;"); break;
      case '>': s += F("&gt;"); break;
      case '"': s += F("&quot;"); break;
      case '\'': s += F("&#39;"); break;
      default: s += *p;
    }
  }
  return s;
}

// ---------- Memória (sobrevive a desligar da tomada) ----------

void carregarMemoria() {
  memoria.begin("pawtinhas", true);
  size_t bytes = memoria.getBytesLength("redes");
  if (bytes > 0 && bytes % sizeof(Rede) == 0 && bytes <= sizeof redes) {
    memoria.getBytes("redes", redes, bytes);
    totalRedes = bytes / sizeof(Rede);
  }
  bytes = memoria.getBytesLength("alarmes");
  if (bytes > 0 && bytes % sizeof(Alarme) == 0 && bytes <= sizeof alarmes) {
    memoria.getBytes("alarmes", alarmes, bytes);
    totalAlarmes = bytes / sizeof(Alarme);
    alarmesDaMemoria = true;
  }
  memoria.getString("apiFixa", apiFixa, sizeof apiFixa);
  memoria.getString("apiAchada", apiEncontrada, sizeof apiEncontrada);
  memoria.end();
  Serial.printf("Memória: %d rede(s), %d alarme(s).\n", totalRedes, totalAlarmes);
}

void salvarRedes() {
  memoria.begin("pawtinhas", false);
  if (totalRedes > 0) memoria.putBytes("redes", redes, totalRedes * sizeof(Rede));
  else memoria.remove("redes");
  memoria.end();
}

void salvarTexto(const char *chave, const char *valor) {
  memoria.begin("pawtinhas", false);
  memoria.putString(chave, valor);
  memoria.end();
}

void salvarAlarmes() {
  memoria.begin("pawtinhas", false);
  if (totalAlarmes > 0) memoria.putBytes("alarmes", alarmes, totalAlarmes * sizeof(Alarme));
  else memoria.remove("alarmes");
  memoria.end();
}

// Rede nova (ou senha nova) vai para o começo da lista; a mais antiga sai se passar de 5.
void lembrarRede(const char *nome, const char *senha) {
  Rede nova{};
  strlcpy(nova.nome, nome, sizeof nova.nome);
  strlcpy(nova.senha, senha, sizeof nova.senha);
  int j = 1;
  Rede lista[MAX_REDES];
  lista[0] = nova;
  for (int i = 0; i < totalRedes && j < MAX_REDES; i++) {
    if (strcmp(redes[i].nome, nome) != 0) lista[j++] = redes[i];
  }
  memcpy(redes, lista, sizeof(Rede) * j);
  totalRedes = j;
  salvarRedes();
}

void esquecerRede(int indice) {
  if (indice < 0 || indice >= totalRedes) return;
  for (int i = indice; i < totalRedes - 1; i++) redes[i] = redes[i + 1];
  totalRedes--;
  salvarRedes();
}

// ---------- Wi-Fi ----------

void ligarServicosDeRede() {
  if (!horaPeloNtpLigada) {
    configTzTime(FUSO, "a.st1.ntp.br", "pool.ntp.org", "time.google.com");
    horaPeloNtpLigada = true;
  }
  if (!otaLigado) {
    ArduinoOTA.setHostname(NOME_NA_REDE);
    ArduinoOTA.setPassword(CHAVE_DISPOSITIVO);
    ArduinoOTA.onStart([]() {
      buzzer(false);
      tela("Atualizando...", "Nao desligue");
    });
    ArduinoOTA.onEnd([]() { tela("Atualizado!", "Reiniciando"); });
    ArduinoOTA.onError([](ota_error_t) { tela("Falha ao gravar", "Tente de novo"); });
    ArduinoOTA.begin();
    otaLigado = true;
  }
}

// Procura as redes guardadas que estão no alcance e tenta da mais forte para a mais fraca.
bool conectarWifi() {
  ultimaTentativaWifi = millis();
  if (totalRedes == 0) return false;

  tela("Procurando Wi-Fi", "");
  int achadas = WiFi.scanNetworks();
  int ordem[MAX_REDES];
  int sinal[MAX_REDES];
  int candidatas = 0;
  for (int i = 0; i < totalRedes; i++) {
    int melhor = -1000;
    for (int k = 0; k < achadas; k++) {
      if (WiFi.SSID(k) == redes[i].nome && WiFi.RSSI(k) > melhor) melhor = WiFi.RSSI(k);
    }
    if (melhor > -1000) {
      ordem[candidatas] = i;
      sinal[candidatas++] = melhor;
    }
  }
  WiFi.scanDelete();
  // Só uma rede guardada e ela não apareceu: tenta assim mesmo (pode ser uma rede oculta).
  if (candidatas == 0 && totalRedes == 1) {
    ordem[0] = 0;
    sinal[0] = 0;
    candidatas = 1;
  }
  for (int a = 0; a < candidatas; a++) {
    for (int b = a + 1; b < candidatas; b++) {
      if (sinal[b] > sinal[a]) {
        std::swap(sinal[a], sinal[b]);
        std::swap(ordem[a], ordem[b]);
      }
    }
  }

  for (int c = 0; c < candidatas; c++) {
    const Rede &r = redes[ordem[c]];
    tela("Conectando Wi-Fi", r.nome);
    Serial.printf("Conectando em %s...\n", r.nome);
    WiFi.begin(r.nome, r.senha);
    for (int i = 0; i < 30 && WiFi.status() != WL_CONNECTED; i++) {
      delay(500);
      if (portalAberto) {
        dns.processNextRequest();
        servidor.handleClient();
      }
    }
    if (WiFi.status() == WL_CONNECTED) {
      Serial.printf("Wi-Fi conectado em %s: %s\n", r.nome, WiFi.localIP().toString().c_str());
      tela(r.nome, WiFi.localIP().toString().c_str());
      conectadoDesde = millis();
      // A rede que funcionou vai para o começo da lista.
      if (ordem[c] != 0) lembrarRede(r.nome, r.senha);
      ligarServicosDeRede();
      delay(1500);
      return true;
    }
    WiFi.disconnect();
  }
  Serial.println("Nenhuma rede guardada conectou.");
  return false;
}

// ---------- Portal de configuração (pelo celular) ----------

String opcoesDeRedes;

void procurarRedesParaOPortal() {
  int achadas = WiFi.scanNetworks();
  opcoesDeRedes = "";
  int mostradas = 0;
  for (int k = 0; k < achadas && mostradas < 15; k++) {
    String nome = WiFi.SSID(k);
    if (nome.isEmpty() || opcoesDeRedes.indexOf("value=\"" + escaparHtml(nome.c_str()) + "\"") >= 0) continue;
    opcoesDeRedes += "<option value=\"" + escaparHtml(nome.c_str()) + "\">" + escaparHtml(nome.c_str()) + "</option>";
    mostradas++;
  }
  WiFi.scanDelete();
}

const char ESTILO[] PROGMEM = R"(<meta name=viewport content="width=device-width,initial-scale=1"><meta charset=utf-8>
<style>body{font-family:system-ui,sans-serif;margin:0;background:#fbf6ef;color:#36241a}main{max-width:420px;margin:auto;padding:20px}
h1{font-size:22px;margin:8px 0 4px}p{line-height:1.45}label{display:block;font-weight:600;margin:14px 0 6px}
input,select,button{width:100%;box-sizing:border-box;font-size:16px;padding:12px;border-radius:12px;border:1px solid #d8c2ad;background:#fff}
button{background:#6e4d36;color:#fff;border:0;font-weight:700;margin-top:18px}.leve{background:#fff;color:#6e4d36;border:1px solid #6e4d36}
.cartao{background:#fff;border-radius:16px;padding:14px 16px;margin:14px 0;box-shadow:0 1px 4px #0001}
.linha{display:flex;align-items:center;gap:10px}.linha span{flex:1}.linha button{width:auto;margin:0;padding:8px 12px}small{color:#7a6656}</style>)";

void responderPagina(const String &corpo) {
  servidor.sendHeader("Cache-Control", "no-store");
  servidor.send(200, "text/html; charset=utf-8", String(FPSTR(ESTILO)) + "<main>" + corpo + "</main>");
}

void paginaInicial() {
  ultimoUsoDoPortal = millis();
  String h = F("<h1>Alarme Pawtinhas</h1><p>Escolha o Wi-Fi do lugar onde a caixinha vai ficar. "
               "Ela guarda até 5 redes e conecta sozinha na que estiver por perto.</p>");

  h += F("<div class=cartao><b>Agora:</b> ");
  if (WiFi.status() == WL_CONNECTED) h += "conectada em " + escaparHtml(WiFi.SSID().c_str()) + " (" + WiFi.localIP().toString() + ")";
  else h += F("sem Wi-Fi");
  h += "<br><small>" + String(totalAlarmes) + F(" alarme(s) guardado(s)</small></div>");

  h += F("<form method=post action=/salvar><label for=rede>Rede Wi-Fi</label><select id=rede name=rede>");
  h += opcoesDeRedes;
  h += F("<option value=''>Outra (digitar o nome)</option></select>"
         "<input name=outra placeholder='Nome da rede, se não estiver na lista' style='margin-top:8px'>"
         "<label for=senha>Senha do Wi-Fi</label><input id=senha name=senha type=password autocomplete=off>"
         "<small>Só redes de 2,4 GHz. Vale também o roteador do celular.</small>"
         "<label for=api>Endereço da API (opcional)</label><input id=api name=api placeholder='Em branco: servidor do Pawtinhas na internet' value='");
  h += escaparHtml(apiFixa);
  h += F("'><small>Deixe em branco. Para usar a API rodando no computador da clínica, digite local.</small>"
         "<button>Salvar e conectar</button></form>"
         "<form method=post action=/procurar><button class=leve>Procurar redes de novo</button></form>");

  if (totalRedes > 0) {
    h += F("<div class=cartao><b>Redes guardadas</b>");
    for (int i = 0; i < totalRedes; i++) {
      h += "<form method=post action=/esquecer class=linha><span>" + escaparHtml(redes[i].nome) +
           "</span><input type=hidden name=i value=" + String(i) + "><button class=leve>Esquecer</button></form>";
    }
    h += F("</div>");
  }
  h += F("<form method=post action=/testar><button class=leve>Testar o som</button></form>"
         "<form method=post action=/fechar><button class=leve>Fechar a configuração</button></form>");
  responderPagina(h);
}

void salvarRedePeloPortal() {
  ultimoUsoDoPortal = millis();
  String nome = servidor.arg("rede");
  if (nome.isEmpty()) nome = servidor.arg("outra");
  nome.trim();
  String senha = servidor.arg("senha");
  String api = servidor.arg("api");
  api.trim();
  while (api.endsWith("/")) api.remove(api.length() - 1);
  if (api.endsWith("/api")) api.remove(api.length() - 4);

  if (!api.isEmpty() && !api.startsWith("http://") && !api.startsWith("https://")) api = "http://" + api;
  strlcpy(apiFixa, api.c_str(), sizeof apiFixa);
  salvarTexto("apiFixa", apiFixa);
  situacaoApi = API_PROCURANDO;
  jaBuscou = false;

  if (nome.isEmpty() || nome.length() > 32 || senha.length() > 64) {
    responderPagina(F("<h1>Confira os dados</h1><p>Escolha uma rede (até 32 letras) e uma senha de até 64.</p>"
                      "<p><a href=/>Voltar</a></p>"));
    return;
  }
  lembrarRede(nome.c_str(), senha.c_str());
  tentarWifiAgora = true;
  responderPagina("<h1>Guardado!</h1><p>A caixinha vai conectar em <b>" + escaparHtml(nome.c_str()) +
                  "</b>. Acompanhe pelo display: o nome da rede e o IP aparecem quando der certo.</p>"
                  "<p>Se a senha estiver errada, o display volta a mostrar <b>Configurar Wi-Fi</b>. "
                  "É só entrar de novo na rede Pawtinhas-Alarme.</p>");
}

void abrirPortal() {
  if (portalAberto) return;
  Serial.println("Abrindo a configuração do Wi-Fi.");
  tela("Abrindo config.", "do Wi-Fi...");
  WiFi.mode(WIFI_AP_STA);
  procurarRedesParaOPortal();
  WiFi.softAP(REDE_CONFIGURACAO, SENHA_CONFIGURACAO);
  delay(100);
  dns.start(53, "*", WiFi.softAPIP());
  servidor.begin();
  portalAberto = true;
  ultimoUsoDoPortal = millis();
  bipe(1, 60);
}

void fecharPortal() {
  if (!portalAberto) return;
  Serial.println("Fechando a configuração do Wi-Fi.");
  servidor.stop();
  dns.stop();
  WiFi.softAPdisconnect(true);
  WiFi.mode(WIFI_STA);
  portalAberto = false;
}

void prepararPortal() {
  servidor.on("/", HTTP_GET, paginaInicial);
  servidor.on("/salvar", HTTP_POST, salvarRedePeloPortal);
  servidor.on("/procurar", HTTP_POST, []() {
    procurarRedesParaOPortal();
    servidor.sendHeader("Location", "/");
    servidor.send(303);
  });
  servidor.on("/esquecer", HTTP_POST, []() {
    esquecerRede(servidor.arg("i").toInt());
    servidor.sendHeader("Location", "/");
    servidor.send(303);
  });
  servidor.on("/testar", HTTP_POST, []() {
    servidor.sendHeader("Location", "/");
    servidor.send(303);
    bipe(1, 300);
  });
  servidor.on("/fechar", HTTP_POST, []() {
    responderPagina(F("<h1>Pronto</h1><p>A rede Pawtinhas-Alarme vai sumir. Para abrir de novo, "
                      "segure o botão BOOT da placa por 3 segundos.</p>"));
    fecharPortalAgora = true;  // fecha no loop, depois que a resposta saiu
  });
  // Qualquer outro endereço leva à página de configuração (o celular abre ela sozinho).
  servidor.onNotFound([]() {
    servidor.sendHeader("Location", "http://" + WiFi.softAPIP().toString() + "/");
    servidor.send(302);
  });
}

void cuidarDoWifi() {
  bool conectado = WiFi.status() == WL_CONNECTED;
  if (conectado && conectadoDesde == 0) {
    conectadoDesde = millis();
    ligarServicosDeRede();
  }
  if (!conectado) conectadoDesde = 0;

  if (portalAberto) {
    dns.processNextRequest();
    servidor.handleClient();
    if (fecharPortalAgora) {
      fecharPortalAgora = false;
      delay(300);
      fecharPortal();
      return;
    }
    bool ninguemNoPortal = WiFi.softAPgetStationNum() == 0;
    // Já conectou e ninguém está mexendo: fecha a configuração.
    if (conectado && millis() - conectadoDesde > 15000 &&
        millis() - ultimoUsoDoPortal > (ninguemNoPortal ? 60000UL : 300000UL)) {
      fecharPortal();
      return;
    }
    // Rede nova salva, ou ninguém no portal: tenta as redes guardadas (o roteador pode ter voltado).
    if (!conectado && (tentarWifiAgora || (ninguemNoPortal && millis() - ultimaTentativaWifi > 60000))) {
      tentarWifiAgora = false;
      conectarWifi();
    }
    return;
  }

  if (conectado) return;
  if (tentarWifiAgora || millis() - ultimaTentativaWifi > 30000) {
    tentarWifiAgora = false;
    if (conectarWifi()) return;
    // Sem rede conhecida e sem hora certa (acabou de ligar num lugar novo): pede a configuração.
    // Com a hora certa, os alarmes guardados continuam tocando e a placa segue tentando.
    if (totalRedes == 0 || !horaValida()) abrirPortal();
  }
}

// ---------- API ----------

bool comecar(HTTPClient &http, const String &url) {
  http.setTimeout(8000);
  http.setConnectTimeout(5000);
  if (url.startsWith("https://")) {
    clienteHttps.setInsecure();  // a placa não guarda certificados; a chave só vale para este dispositivo
    return http.begin(clienteHttps, url);
  }
  return http.begin(clienteHttp, url);
}

// Pergunta na rede "PAWTINHAS?" e espera a API responder "PAWTINHAS http 5135".
bool procurarApi() {
  ultimaProcura = millis();
  WiFiUDP udp;
  if (!udp.begin(PORTA_DESCOBERTA + 1)) return false;
  IPAddress destinos[] = {WiFi.broadcastIP(), IPAddress(255, 255, 255, 255)};
  for (int tentativa = 0; tentativa < 2; tentativa++) {
    for (IPAddress destino : destinos) {
      udp.beginPacket(destino, PORTA_DESCOBERTA);
      udp.print("PAWTINHAS?");
      udp.endPacket();
    }
    unsigned long inicio = millis();
    while (millis() - inicio < 1200) {
      if (udp.parsePacket() > 0) {
        char resposta[40] = {0};
        udp.read(resposta, sizeof resposta - 1);
        char esquema[8] = {0};
        int porta = 0;
        if (sscanf(resposta, "PAWTINHAS %7s %d", esquema, &porta) == 2 && porta > 0 &&
            (!strcmp(esquema, "http") || !strcmp(esquema, "https"))) {
          char url[101];
          snprintf(url, sizeof url, "%s://%s:%d", esquema, udp.remoteIP().toString().c_str(), porta);
          udp.stop();
          if (strcmp(url, apiEncontrada) != 0) {
            strlcpy(apiEncontrada, url, sizeof apiEncontrada);
            salvarTexto("apiAchada", apiEncontrada);
          }
          Serial.printf("API encontrada em %s\n", apiEncontrada);
          return true;
        }
      }
      delay(20);
    }
  }
  udp.stop();
  Serial.println("Nenhuma API respondeu na rede.");
  return false;
}

// "local" no portal: usa a API rodando num computador da mesma rede, achada pela procura UDP.
bool usaApiLocal() { return !strcasecmp(apiFixa, "local"); }

// Sem nada no portal, a caixinha fala com o servidor do Pawtinhas na internet: funciona em qualquer
// Wi-Fi com internet, sem computador ligado.
String enderecoDaApi() {
  if (usaApiLocal()) return apiEncontrada;
  if (apiFixa[0]) return apiFixa;
  return API_ENDERECO_PADRAO;
}

// Acerta o relógio pelo cabeçalho Date da API, para redes sem acesso ao servidor de hora (NTP).
void acertarHoraPelaApi(const String &data) {
  char mes[4] = {0};
  int dia, ano, h, m, s;
  if (sscanf(data.c_str(), "%*[^,], %d %3s %d %d:%d:%d", &dia, mes, &ano, &h, &m, &s) != 6) return;
  const char *meses = "JanFebMarAprMayJunJulAugSepOctNovDec";
  const char *p = strstr(meses, mes);
  if (!p) return;
  time_t utc = (time_t)diasDesdeEpoca(ano, (p - meses) / 3 + 1, dia) * 86400 + h * 3600 + m * 60 + s;
  time_t agora = time(nullptr);
  if (horaValida() && llabs((long long)(utc - agora)) < 90) return;
  timeval tv{utc, 0};
  settimeofday(&tv, nullptr);
  setenv("TZ", FUSO, 1);
  tzset();
  Serial.println("Hora acertada pela API.");
}

uint8_t recorrenciaDe(const char *texto) {
  if (!strcmp(texto, "A cada 8 horas")) return A_CADA_8H;
  if (!strcmp(texto, "A cada 12 horas")) return A_CADA_12H;
  if (!strcmp(texto, "Dias úteis")) return DIAS_UTEIS;
  if (!strcmp(texto, "Semanal")) return SEMANAL;
  if (!strcmp(texto, "Somente hoje")) return UMA_VEZ;
  return DIARIO;
}

void buscarAlarmes() {
  ultimaBusca = millis();
  jaBuscou = true;
  if (WiFi.status() != WL_CONNECTED) return;

  String base = enderecoDaApi();
  if (base.isEmpty()) {
    if (millis() - ultimaProcura < 15000 && ultimaProcura != 0) return;
    situacaoApi = API_PROCURANDO;
    if (!procurarApi()) return;
    base = enderecoDaApi();
  }

  HTTPClient http;
  if (!comecar(http, base + "/api/dispositivo/alarmes")) return;
  const char *cabecalhos[] = {"Date"};
  http.collectHeaders(cabecalhos, 1);
  http.addHeader("X-Device-Key", CHAVE_DISPOSITIVO);
  http.addHeader("Accept", "application/json");
  ultimoCodigoHttp = http.GET();
  if (ultimoCodigoHttp > 0 && http.hasHeader("Date")) acertarHoraPelaApi(http.header("Date"));

  if (ultimoCodigoHttp != 200) {
    Serial.printf("API (%s) respondeu %d ao buscar alarmes.\n", base.c_str(), ultimoCodigoHttp);
    http.end();
    if (ultimoCodigoHttp < 0) {
      situacaoApi = API_SEM_RESPOSTA;
      // O computador da API pode ter mudado de IP ou de rede: procura de novo.
      if (usaApiLocal() && millis() - ultimaProcura > 15000) {
        if (procurarApi()) ultimaBusca = 0;
      }
    } else {
      situacaoApi = ultimoCodigoHttp == 401 ? API_CHAVE_ERRADA : ultimoCodigoHttp == 404 ? API_DESLIGADA : API_ERRO;
    }
    return;
  }

  String corpo = http.getString();
  http.end();
  JsonDocument doc;
  if (deserializeJson(doc, corpo) || !doc.is<JsonArray>()) {
    Serial.println("Resposta da API não é uma lista de alarmes.");
    situacaoApi = API_ERRO;
    return;
  }

  static Alarme novos[MAX_ALARMES];
  memset(novos, 0, sizeof novos);
  int n = 0;
  for (JsonObject a : doc.as<JsonArray>()) {
    if (n >= MAX_ALARMES) break;
    Alarme &x = novos[n];
    x.id = a["id"] | 0;
    strlcpy(x.nome, a["nome"] | "", sizeof x.nome);
    strlcpy(x.pet, a["pet"] | "", sizeof x.pet);
    x.recorrencia = recorrenciaDe(a["recorrencia"] | "");
    int h = 0, m = 0;
    sscanf(a["hora"] | "00:00", "%d:%d", &h, &m);
    x.minutoDoDia = constrain(h, 0, 23) * 60 + constrain(m, 0, 59);
    int ano = 0, mes = 0, dia = 0;
    x.inicio = sscanf(a["inicio"] | "", "%d-%d-%d", &ano, &mes, &dia) == 3 ? diasDesdeEpoca(ano, mes, dia) : 0;
    n++;
  }
  bool mudou = n != totalAlarmes || memcmp(novos, alarmes, sizeof(Alarme) * n) != 0;
  memcpy(alarmes, novos, sizeof(Alarme) * n);
  totalAlarmes = n;
  situacaoApi = API_OK;
  alarmesDaMemoria = false;
  if (mudou) {
    salvarAlarmes();  // só grava quando muda, para poupar a memória da placa
    Serial.printf("%d alarme(s) ativo(s) recebidos e guardados.\n", totalAlarmes);
  }
}

void enviarDisparos() {
  if (totalPendentes == 0 || WiFi.status() != WL_CONNECTED || situacaoApi != API_OK) return;
  String base = enderecoDaApi();
  while (totalPendentes > 0) {
    const Disparo &d = pendentes[0];
    struct tm t;
    localtime_r(&d.quando, &t);
    char corpo[128];
    snprintf(corpo, sizeof corpo,
             "{\"alarmeId\":%ld,\"resultado\":\"%s\",\"quando\":\"%04d-%02d-%02dT%02d:%02d:00\"}", (long)d.alarmeId,
             d.comoParou == PAROU_NO_BOTAO    ? "Tocou e foi desligado no botão"
             : d.comoParou == PAROU_NO_PAINEL ? "Tocou e foi desligado no painel"
             : d.comoParou == PAROU_NO_APP    ? "Tocou e foi desligado no app"
                                              : "Tocou",
             t.tm_year + 1900, t.tm_mon + 1,
             t.tm_mday, t.tm_hour, t.tm_min);
    HTTPClient http;
    if (!comecar(http, base + "/api/dispositivo/disparos")) return;
    http.addHeader("X-Device-Key", CHAVE_DISPOSITIVO);
    http.addHeader("Content-Type", "application/json");
    int codigo = http.POST(corpo);
    http.end();
    Serial.printf("Disparo do alarme %ld enviado (HTTP %d).\n", (long)d.alarmeId, codigo);
    if (codigo < 0 || codigo >= 500) return;  // tenta de novo depois
    // 200 ou erro definitivo (alarme excluído): sai da fila.
    for (int i = 0; i < totalPendentes - 1; i++) pendentes[i] = pendentes[i + 1];
    totalPendentes--;
  }
}

void guardarDisparo(const Alarme &a, time_t quando, uint8_t comoParou) {
  if (totalPendentes == MAX_PENDENTES) {
    for (int i = 0; i < totalPendentes - 1; i++) pendentes[i] = pendentes[i + 1];
    totalPendentes--;
  }
  pendentes[totalPendentes++] = {a.id, quando, comoParou};
}

// ---------- Alarmes ----------

bool deveTocar(const Alarme &a, const struct tm &t) {
  long dia = diaDe(t);
  if (dia < a.inicio) return false;
  int agora = t.tm_hour * 60 + t.tm_min;
  int diferenca = (agora - a.minutoDoDia + 1440) % 1440;
  switch (a.recorrencia) {
    case A_CADA_8H: return diferenca % 480 == 0;
    case A_CADA_12H: return diferenca % 720 == 0;
    default: break;
  }
  if (diferenca != 0) return false;
  switch (a.recorrencia) {
    case DIAS_UTEIS: return t.tm_wday >= 1 && t.tm_wday <= 5;
    case SEMANAL: return (dia - a.inicio) % 7 == 0;
    case UMA_VEZ: return dia == a.inicio;
    default: return true;
  }
}

// Próximo toque (até 8 dias à frente), já no texto do display.
void proximoHorario(char *saida, size_t tamanho) {
  static const char *DIAS[] = {"dom", "seg", "ter", "qua", "qui", "sex", "sab"};
  time_t agora = time(nullptr);
  struct tm hoje;
  localtime_r(&agora, &hoje);
  int minutoAgora = hoje.tm_hour * 60 + hoje.tm_min;
  for (int d = 0; d <= 8; d++) {
    time_t diaFuturo = agora + (time_t)d * 86400;
    struct tm t;
    localtime_r(&diaFuturo, &t);
    for (int m = (d == 0 ? minutoAgora + 1 : 0); m < 1440; m++) {
      t.tm_hour = m / 60;
      t.tm_min = m % 60;
      for (int i = 0; i < totalAlarmes; i++) {
        if (!deveTocar(alarmes[i], t)) continue;
        if (d == 0) snprintf(saida, tamanho, "Proximo: %02d:%02d", t.tm_hour, t.tm_min);
        else if (d == 1) snprintf(saida, tamanho, "Amanha: %02d:%02d", t.tm_hour, t.tm_min);
        else snprintf(saida, tamanho, "Prox: %s %02d:%02d", DIAS[t.tm_wday], t.tm_hour, t.tm_min);
        return;
      }
    }
  }
  snprintf(saida, tamanho, "Sem proximos");
}

// ---------- Desligar em todos os aparelhos ----------
// O minuto do toque é o número de minutos desde 1970 (UTC), o mesmo no painel e no app.

// Desligado no botão: avisa o servidor na hora, para o painel e o app pararem também.
void avisarParada(const Alarme *lista[], int quantos, long minuto) {
  if (WiFi.status() != WL_CONNECTED || situacaoApi != API_OK) return;
  String corpo = "{\"alarmeIds\":[";
  for (int i = 0; i < quantos; i++) {
    if (i) corpo += ',';
    corpo += String((long)lista[i]->id);
  }
  corpo += "],\"minuto\":" + String(minuto) + "}";
  HTTPClient http;
  if (!comecar(http, enderecoDaApi() + "/api/dispositivo/paradas")) return;
  http.addHeader("X-Device-Key", CHAVE_DISPOSITIVO);
  http.addHeader("Content-Type", "application/json");
  int codigo = http.POST(corpo);
  http.end();
  Serial.printf("Parada avisada ao servidor (HTTP %d).\n", codigo);
}

// Enquanto toca: alguém desligou no painel ou no app? Devolve PAROU_NO_PAINEL, PAROU_NO_APP ou PAROU_SOZINHO (não).
uint8_t paradaRemota(const Alarme *lista[], int quantos, long minuto) {
  if (WiFi.status() != WL_CONNECTED || situacaoApi != API_OK) return PAROU_SOZINHO;
  HTTPClient http;
  if (!comecar(http, enderecoDaApi() + "/api/dispositivo/paradas")) return PAROU_SOZINHO;
  http.setTimeout(3000);
  http.addHeader("X-Device-Key", CHAVE_DISPOSITIVO);
  http.addHeader("Accept", "application/json");
  int codigo = http.GET();
  String corpo = codigo == 200 ? http.getString() : String();
  http.end();
  if (corpo.isEmpty()) return PAROU_SOZINHO;
  JsonDocument doc;
  if (deserializeJson(doc, corpo) || !doc.is<JsonArray>()) return PAROU_SOZINHO;
  for (JsonObject p : doc.as<JsonArray>()) {
    long id = p["alarmeId"] | 0L;
    long m = p["minuto"] | 0L;
    if (labs(m - minuto) > 1) continue;
    for (int i = 0; i < quantos; i++) {
      if (lista[i]->id != id) continue;
      const char *origem = p["origem"] | "";
      return !strcmp(origem, "app") ? PAROU_NO_APP : PAROU_NO_PAINEL;
    }
  }
  return PAROU_SOZINHO;
}

// Toca até alguém apertar o botão, desligar no painel ou no app, ou passar TOCAR_POR_MS. Devolve como parou.
uint8_t tocar(const Alarme *lista[], int quantos, long minuto) {
  Serial.printf("Tocando: %s%s\n", lista[0]->nome, quantos > 1 ? " e outros" : "");
  lcd->backlight();
  unsigned long inicio = millis();
  int passo = 0;
  while (millis() - inicio < TOCAR_POR_MS) {
    const Alarme &a = *lista[(passo / 2) % quantos];
    escrever(0, a.nome);
    escrever(1, passo % 2 == 0 && a.pet[0] ? a.pet : "Hora do remedio!");
    for (int i = 0; i < 2; i++) {
      buzzer(true);
      for (int k = 0; k < 40; k++) {
        if (botaoApertado()) goto desligado;
        delay(20);
      }
      buzzer(false);
      for (int k = 0; k < 10; k++) {
        if (botaoApertado()) goto desligado;
        delay(20);
      }
    }
    passo++;
    // A cada ~4 s, entre um bipe e outro, pergunta se desligaram no painel ou no app.
    if (passo % 2 == 0) {
      uint8_t remota = paradaRemota(lista, quantos, minuto);
      if (botaoApertado()) goto desligado;
      if (remota != PAROU_SOZINHO) {
        buzzer(false);
        tela("Alarme desligado", remota == PAROU_NO_APP ? "pelo app" : "pelo painel");
        delay(2000);
        return remota;
      }
    }
  }
  buzzer(false);
  return PAROU_SOZINHO;

desligado:
  buzzer(false);
  tela("Alarme desligado", "");
  avisarParada(lista, quantos, minuto);
  while (botaoApertado()) delay(20);
  delay(1200);
  return PAROU_NO_BOTAO;
}

void conferirAlarmes() {
  if (!horaValida()) return;
  long minutoAgora = time(nullptr) / 60;
  // Primeira conferência ou relógio acertado para trás: começa do minuto atual.
  if (ultimoMinutoConferido < 0 || ultimoMinutoConferido > minutoAgora) ultimoMinutoConferido = minutoAgora - 1;

  // Se a placa ficou ocupada (tocando, conectando), confere os minutos que passaram (até 10).
  long desde = max(ultimoMinutoConferido + 1, minutoAgora - 10);
  for (long m = desde; m <= minutoAgora; m++) {
    time_t instante = (time_t)m * 60;
    struct tm t;
    localtime_r(&instante, &t);
    const Alarme *agora[MAX_ALARMES];
    int quantos = 0;
    for (int i = 0; i < totalAlarmes; i++) {
      if (deveTocar(alarmes[i], t)) agora[quantos++] = &alarmes[i];
    }
    if (quantos == 0) continue;
    ultimoMinutoConferido = m;  // não toca o mesmo minuto duas vezes, mesmo se algo der errado no meio
    uint8_t comoParou = tocar(agora, quantos, m);
    for (int i = 0; i < quantos; i++) guardarDisparo(*agora[i], instante, comoParou);
    enviarDisparos();
  }
  ultimoMinutoConferido = max(ultimoMinutoConferido, minutoAgora);
}

// ---------- Tela parada e botão ----------

void telaParada() {
  static unsigned long ultimaTela = 0;
  static char proximo[17] = "";
  static long minutoDoProximo = -1;
  if (millis() - ultimaTela < 1000) return;
  ultimaTela = millis();

  if (millis() < mostrarInfoAte) {
    bool primeira = (mostrarInfoAte - millis()) > 3000;
    char linha[24];
    if (primeira) {
      if (WiFi.status() == WL_CONNECTED) tela(WiFi.SSID().c_str(), WiFi.localIP().toString().c_str());
      else tela("Sem Wi-Fi", portalAberto ? "Config. aberta" : "Tentando...");
    } else {
      snprintf(linha, sizeof linha, "%d alarme(s)", totalAlarmes);
      const char *api = situacaoApi == API_OK ? "API ok" : situacaoApi == API_PROCURANDO ? "Procurando API" : "API sem contato";
      tela(linha, api);
    }
    return;
  }

  if (portalAberto) {
    // Alterna as instruções a cada 3 s; se já conectou, mostra também a rede e o IP.
    int fase = (millis() / 3000) % (WiFi.status() == WL_CONNECTED ? 3 : 2);
    if (fase == 0) tela("Configurar Wi-Fi", REDE_CONFIGURACAO);
    else if (fase == 1) tela("Senha: pawtinhas", "Abra 192.168.4.1");
    else tela(WiFi.SSID().c_str(), WiFi.localIP().toString().c_str());
    return;
  }

  char linha[24];
  if (horaValida()) {
    time_t agora = time(nullptr);
    struct tm t;
    localtime_r(&agora, &t);
    snprintf(linha, sizeof linha, "Pawtinhas  %02d:%02d", t.tm_hour, t.tm_min);
  } else {
    snprintf(linha, sizeof linha, "Pawtinhas  --:--");
  }
  escrever(0, linha);

  // Problema de conexão: alterna o aviso com o próximo alarme (os guardados continuam valendo).
  const char *aviso = nullptr;
  if (WiFi.status() != WL_CONNECTED) aviso = "Sem Wi-Fi";
  else if (!horaValida()) aviso = "Acertando hora";
  else if (situacaoApi == API_PROCURANDO) aviso = "Procurando API";
  else if (situacaoApi == API_CHAVE_ERRADA) aviso = "Chave invalida";
  else if (situacaoApi == API_DESLIGADA) aviso = "ESP32 desligado";
  else if (situacaoApi == API_SEM_RESPOSTA) aviso = "API fora do ar";
  else if (situacaoApi == API_ERRO) {
    snprintf(linha, sizeof linha, "API erro %d", ultimoCodigoHttp);
    aviso = linha;
  }

  bool mostrarAviso = aviso && (!horaValida() || totalAlarmes == 0 || (millis() / 3000) % 2 == 0);
  if (mostrarAviso) {
    escrever(1, aviso);
    return;
  }
  if (totalAlarmes == 0) {
    escrever(1, "Sem alarmes");
    return;
  }
  long minuto = time(nullptr) / 60;
  if (minuto != minutoDoProximo) {
    proximoHorario(proximo, sizeof proximo);
    minutoDoProximo = minuto;
  }
  escrever(1, proximo);
}

void cuidarDoBotao() {
  static unsigned long apertadoDesde = 0;
  static bool jaAgiu = false;
  if (botaoApertado()) {
    if (apertadoDesde == 0) apertadoDesde = millis();
    if (!jaAgiu && millis() - apertadoDesde >= SEGURAR_PARA_CONFIGURAR_MS) {
      jaAgiu = true;
      if (portalAberto) {
        fecharPortal();
        tela("Config. fechada", "");
        bipe(1, 60);
      } else {
        abrirPortal();
      }
    }
    return;
  }
  if (apertadoDesde != 0 && !jaAgiu && millis() - apertadoDesde > 40) mostrarInfoAte = millis() + 6000;
  apertadoDesde = 0;
  jaAgiu = false;
}

// Configuração pelo cabo USB, sem precisar do celular. Linhas que começam com '#':
//   #wifi <rede><TAB><senha>   guarda a rede e conecta
//   #api <endereço>            fixa o endereço da API (#api sozinho volta ao servidor na internet)
//   #estado                    mostra rede, IP, API e alarmes
// Letras soltas continuam valendo: t = toque de teste (um bipe curto), w = abre o portal de Wi-Fi.
void comandoSerial(String linha) {
  linha.trim();
  if (linha.startsWith("wifi ")) {
    String resto = linha.substring(5);
    int tab = resto.indexOf('\t');
    String nome = tab < 0 ? resto : resto.substring(0, tab);
    String senha = tab < 0 ? "" : resto.substring(tab + 1);
    nome.trim();
    if (nome.isEmpty() || nome.length() > 32 || senha.length() > 64) {
      Serial.println("ERRO: use #wifi <rede><TAB><senha> (rede até 32 letras, senha até 64).");
      return;
    }
    lembrarRede(nome.c_str(), senha.c_str());
    tentarWifiAgora = true;
    Serial.printf("OK: rede %s guardada. Conectando...\n", nome.c_str());
  } else if (linha == "api" || linha.startsWith("api ")) {
    String api = linha.length() > 3 ? linha.substring(4) : "";
    api.trim();
    while (api.endsWith("/")) api.remove(api.length() - 1);
    strlcpy(apiFixa, api.c_str(), sizeof apiFixa);
    salvarTexto("apiFixa", apiFixa);
    situacaoApi = API_PROCURANDO;
    jaBuscou = false;
    Serial.printf("OK: API em %s\n", enderecoDaApi().c_str());
  } else if (linha == "estado") {
    Serial.printf("ESTADO wifi=%s rede=%s ip=%s api=%s situacao=%d http=%d alarmes=%d\n",
                  WiFi.status() == WL_CONNECTED ? "conectado" : "desconectado", WiFi.SSID().c_str(),
                  WiFi.localIP().toString().c_str(), enderecoDaApi().c_str(), (int)situacaoApi, ultimoCodigoHttp,
                  totalAlarmes);
  } else {
    Serial.println("ERRO: comando desconhecido.");
  }
}

void testePeloSerial() {
  static String linha;
  static bool emComando = false;
  while (Serial.available()) {
    char c = Serial.read();
    if (emComando) {
      if (c == '\n' || c == '\r') {
        emComando = false;
        comandoSerial(linha);
      } else if (linha.length() < 120) {
        linha += c;
      }
    } else if (c == '#') {
      emComando = true;
      linha = "";
    } else if (c == 't' || c == 'T') {
      Serial.println("Teste do toque.");
      tela("Teste de alarme", "Hora do remedio!");
      bipe(1, 300);
    } else if (c == 'w' || c == 'W') {
      abrirPortal();
    }
  }
}

// ---------- Início ----------

void setup() {
  Serial.begin(115200);
  setCpuFrequencyMhz(80);  // gasta menos energia no power bank; o Wi-Fi funciona bem a 80 MHz
  pinMode(PINO_BUZZER, OUTPUT);
  buzzer(false);
  pinMode(PINO_BOTAO, INPUT_PULLUP);

  Wire.begin(PINO_SDA, PINO_SCL);
  byte endereco = enderecoDoLcd();
  Serial.printf("Display no endereço 0x%02X\n", endereco);
  lcd = new LiquidCrystal_I2C(endereco, 16, 2);
  lcd->init();
  lcd->backlight();
  tela("Pawtinhas", "Alarme de remedio");
  bipe(1, 60);  // um bipe curto só para mostrar que o buzzer está ligado

  setenv("TZ", FUSO, 1);
  tzset();
  carregarMemoria();
  prepararPortal();

  WiFi.setHostname(NOME_NA_REDE);
  WiFi.mode(WIFI_STA);
  WiFi.setAutoReconnect(true);
  if (totalRedes == 0) abrirPortal();
}

void loop() {
  cuidarDoWifi();
  if (otaLigado) ArduinoOTA.handle();

  if (!jaBuscou || millis() - ultimaBusca >= BUSCAR_A_CADA_MS) {
    buscarAlarmes();
    enviarDisparos();
  }

  cuidarDoBotao();
  testePeloSerial();
  conferirAlarmes();
  telaParada();
  delay(portalAberto ? 10 : 50);
}
