/*
 * ================================================================
 *  ESTACIÓN METEOROLÓGICA REMOTA 3 — ESP32
 *  Modo DUAL: Mosquitto local (10.40.20.10:1883) + HiveMQ Cloud
 *
 *  Sensores: DHT22 | TSL2561 | Anemómetro reed | Pluviómetro reed
 *            Veleta resistiva (GPIO 34, ADC1)
 *  Hardware adicional: RTC DS3231 (I2C) | SD Card (SPI)
 *
 *  Mejoras sobre la versión anterior:
 *  - Modo dual: publica JSON compacto a local y JSON completo a HiveMQ
 *  - RTC DS3231: hora sin internet, se sincroniza con NTP cuando hay
 *  - SD: guarda cada lectura en /datos/AAAAMMDD.csv
 *  - Pendientes en SD (/pendientes.txt); RAM solo si no hay SD
 *  - NTP sin bucle infinito: arranca aunque no haya internet
 *  - Publicación autónoma cada 2 minutos
 *  - Watchdog: reinicia si el firmware se cuelga
 *  v5: Sirena de alarma en GPIO 15 — topic estacion3/alarma
 *      Comandos extra en estacion3/comando: ALARMA_ON | ALARMA_OFF
 *  v6: Pluviómetro calibrado (11×5 cm = 55 cm², 2.25 ml/vuelco ≈ 0.41 mm/pulso)
 *  v7: RTC DS3231 (I2C SDA=21, SCL=22) + tarjeta SD (SPI, CS=5)
 *      - La hora sale del RTC al arrancar; NTP la corrige cuando hay internet
 *      - Lo que no llega al broker local va a /pendientes.txt y se reenvía
 *        por estacion3/historico al reconectar (sobrevive a reinicios)
 *      - Comandos: SD_ESTADO | SD_ENVIAR:AAAA-MM-DD  → respuesta en estacion3/sd
 * ================================================================
 */

#include "secrets.h"   // credenciales (copiar secrets.example.h)
#include <WiFi.h>
#include <WiFiClient.h>
#include <WiFiClientSecure.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <Adafruit_TSL2561_U.h>
#include <DHT.h>
#include <RTClib.h>
#include <SPI.h>
#include <SD.h>
#include <time.h>
#include <sys/time.h>
#include <esp_sntp.h>
#include <ArduinoJson.h>
#include <esp_task_wdt.h>
#include <vector>
#include <algorithm>

// ================================================================
// WIFI / NTP
// ================================================================
const char* ssid               = WIFI_SSID;
const char* password           = WIFI_PASSWORD;
const char* ntpServer          = "pool.ntp.org";
const long  gmtOffset_sec      = -5 * 3600;
const int   daylightOffset_sec = 0;
bool        horaValida         = false;
String      fuenteHora         = "sin_hora";   // NTP | RTC | sin_hora

// ================================================================
// BROKER LOCAL — Mosquitto en PC LESTOMA
// ================================================================
const char* mqtt_broker_local = "192.168.0.130";
const int   mqtt_port_local    = 1883;
const char* clientId_local     = "ESP32_E3_Local";

// ================================================================
// BROKER REMOTO — HiveMQ Cloud
// ================================================================
const char* mqtt_broker_remoto = HIVEMQ_HOST;
const int   mqtt_port_remoto   = 8883;
const char* mqtt_user_remoto   = HIVEMQ_USER;
const char* mqtt_pass_remoto   = HIVEMQ_PASS;
const char* clientId_remoto    = "ESP32_Estacion_Remota3";

// ================================================================
// TOPICS
// ================================================================
const char* topic_sub     = "estacion3/solicitud";
const char* topic_pub     = "estacion3/datos";
const char* topic_comando = "estacion3/comando";
const char* topic_estado  = "estacion3/estado";
const char* topic_alarma  = "estacion3/alarma";
// Solo broker local: lecturas atrasadas (pendientes o día pedido) y respuestas de la SD
const char* topic_historico = "estacion3/historico";
const char* topic_sd        = "estacion3/sd";

const int ESTACION_NUM = 3;

// ================================================================
// CLIENTES MQTT
// ================================================================
WiFiClient       espClientLocal;
WiFiClientSecure espClientRemoto;
PubSubClient     mqttLocal(espClientLocal);
PubSubClient     mqttRemoto(espClientRemoto);

// ================================================================
// PINES
// ================================================================
#define PIN_VIENTO  27
#define PIN_LLUVIA  13
#define DHTPIN       4
#define DHTTYPE      DHT22
#define PIN_SIRENA    15   // HIGH = sirena encendida
#define PIN_VELETA  34   // ADC1 — funciona con WiFi activo
#define I2C_SDA     21   // TSL2561 y RTC DS3231 comparten el bus
#define I2C_SCL     22
#define SD_CS        5   // SPI: SCK=18, MISO=19, MOSI=23

// ================================================================
// OBJETOS
// ================================================================
DHT                      dht(DHTPIN, DHTTYPE);
Adafruit_TSL2561_Unified tsl = Adafruit_TSL2561_Unified(TSL2561_ADDR_FLOAT, 12345);
// RTC y SD: ver la sección HORA / SD más abajo

// ================================================================
// CONSTANTES FÍSICAS ESTACIÓN 3
// ================================================================
const float DIAMETRO_M        = 0.14;
const float PULSOS_POR_VUELTA = 1.0;
const float FACTOR_ANEMOMETRO = 2.5;   // PENDIENTE CALIBRAR
// Pluviómetro de balancín calibrado volumétricamente:
//   boca rectangular 11 cm × 5 cm → área = 55 cm²
//   9 ml vertidos → 4 vuelcos → 9 / 4 = 2.25 ml por vuelco
//   2.25 cm³ / 55 cm² ≈ 0.0409 cm ≈ 0.41 mm/pulso
const float LARGO_CAPTACION_CM = 11.0;
const float ANCHO_CAPTACION_CM = 5.0;
const float AREA_CAPTACION_CM2 = LARGO_CAPTACION_CM * ANCHO_CAPTACION_CM; // 55 cm²
const float ML_POR_BALANCEO    = 9.0 / 4.0;                              // 2.25 ml

const float CIRCUNFERENCIA_M = PI * DIAMETRO_M;
const float MM_POR_BALANCEO  = (ML_POR_BALANCEO / AREA_CAPTACION_CM2) * 10.0; // ≈ 0.41 mm

// ================================================================
// ANTIRREBOTE ISR — reed switch necesita más tiempo que Hall
// ================================================================
const unsigned long REBOTE_ANEMO_US = 30000UL; // 30 ms
const unsigned long REBOTE_PLUVI_US = 100000UL; // 100 ms

volatile unsigned long pulsosViento   = 0;
volatile unsigned long ultimoVientoUs = 0;
volatile unsigned long pulsosLluvia   = 0;
volatile unsigned long ultimoLluviaUs = 0;

// Pulsos acumulados entre una publicación y la siguiente (~2 min).
// Es lo que se envía y se guarda en la SD; pulsosViento/pulsosLluvia son solo la lectura en vivo.
volatile unsigned long pulsosVientoVentana = 0;
volatile unsigned long pulsosLluviaVentana = 0;

portMUX_TYPE muxViento = portMUX_INITIALIZER_UNLOCKED;
portMUX_TYPE muxLluvia = portMUX_INITIALIZER_UNLOCKED;

void IRAM_ATTR contarViento() {
  unsigned long ahora = micros();
  if (ahora - ultimoVientoUs >= REBOTE_ANEMO_US) {
    ultimoVientoUs = ahora;
    portENTER_CRITICAL_ISR(&muxViento);
    pulsosViento++;
    pulsosVientoVentana++;
    portEXIT_CRITICAL_ISR(&muxViento);
  }
}

void IRAM_ATTR contarLluvia() {
  unsigned long ahora = micros();
  if (ahora - ultimoLluviaUs >= REBOTE_PLUVI_US) {
    ultimoLluviaUs = ahora;
    portENTER_CRITICAL_ISR(&muxLluvia);
    pulsosLluvia++;
    pulsosLluviaVentana++;
    portEXIT_CRITICAL_ISR(&muxLluvia);
  }
}

// ================================================================
// VARIABLES DE SENSORES
// ================================================================
float  temperatura        = 0;
float  humedad            = 0;
float  lux                = 0;
float  radiacion          = 0;
bool   luxSaturado        = false;
float  velocidadViento    = 0;
float  velocidadVientoKmh = 0;
float  lluviaMinuto       = 0;
float  intensidadLluvia   = 0;
String direccion          = "SIN";
float  anguloViento       = -1;

unsigned long pulsosVientoCrudo = 0;
unsigned long pulsosLluviaCrudo = 0;
unsigned long intervaloVientoMs = 0;

// Valores de la ventana de publicación (los que van en el JSON y en la SD)
unsigned long inicioVentana = 0;
unsigned long vviVentana    = 0;   // duración real de la ventana en ms
unsigned long vvpVentana    = 0;   // pulsos de viento en la ventana
unsigned long llupVentana   = 0;   // vuelcos del pluviómetro en la ventana
float         velVVentana   = 0;   // velocidad promedio de la ventana, m/s
float         lluvVentana   = 0;   // lluvia promedio de la ventana, mm/min

unsigned long fallosTemp = 0;
unsigned long fallosHum  = 0;

// ================================================================
// TIMERS
// ================================================================
unsigned long ultimoMinuto        = 0;
unsigned long tUltimaVentana      = 0;
unsigned long lastRead            = 0;
unsigned long lastSerial          = 0;
unsigned long lastReconnectLocal  = 0;
unsigned long lastReconnectRemoto = 0;
unsigned long lastPublicacion     = 0;

const unsigned long intervaloLluvia      = 60000UL;
const unsigned long intervaloRead        =  2500UL;
const unsigned long intervaloSerial      =  1000UL;
const unsigned long intervaloReconnect   =  5000UL;
const unsigned long intervaloReconnectRemoto = 30000UL; // TLS bloquea varios segundos
const unsigned long intervaloPublicacion = 120000UL; // 2 min → 300000 para 5 min

// ================================================================
// TSL2561
// ================================================================
const uint16_t TSL_MAX_COUNT_13MS = 5047;
const float    LUX_MAXIMA_SENSOR  = 40000.0;
const int      HORA_INICIO_DIA    = 6;
const int      HORA_FIN_DIA       = 18;

// ================================================================
// SIRENA / ALARMA — GPIO 15 (HIGH = encendida)
// La activa el worker publicando en estacionN/alarma:
//   {"accion":"ON","nivel":"Alta","duracionSeg":10}  |  {"accion":"OFF"}
// Patrón ON/OFF con millis(), sin delay(). Más grave = pitido más rápido.
// ================================================================
bool          alarmaActiva     = false;
bool          sirenaEncendida  = false;
unsigned long alarmaInicio     = 0;
unsigned long alarmaDuracionMs = 0;
unsigned long sirenaCambio     = 0;
unsigned long sirenaOnMs       = 2000;
unsigned long sirenaOffMs      = 1000;

const unsigned long ALARMA_DURACION_DEF_S = 10;
const unsigned long ALARMA_DURACION_MAX_S = 120;

void publicarEstado(const char* estado) {
  if (mqttLocal.connected())  mqttLocal.publish(topic_estado,  estado, false);
  if (mqttRemoto.connected()) mqttRemoto.publish(topic_estado, estado, false);
}

void sirena(bool encender) {
  sirenaEncendida = encender;
  digitalWrite(PIN_SIRENA, encender ? HIGH : LOW);
  sirenaCambio = millis();
}

// nivel: BAJA | MEDIA | ALTA
void activarAlarma(String nivel, unsigned long duracionSeg) {
  nivel.toUpperCase();
  if      (nivel == "ALTA")  { sirenaOnMs = 300;  sirenaOffMs = 200;  }
  else if (nivel == "MEDIA") { sirenaOnMs = 1000; sirenaOffMs = 500;  }
  else                       { sirenaOnMs = 2000; sirenaOffMs = 1000; }

  if (duracionSeg == 0)                    duracionSeg = ALARMA_DURACION_DEF_S;
  if (duracionSeg > ALARMA_DURACION_MAX_S) duracionSeg = ALARMA_DURACION_MAX_S;

  alarmaActiva     = true;
  alarmaInicio     = millis();
  alarmaDuracionMs = duracionSeg * 1000UL;
  sirena(true);
  Serial.printf("[ALARMA] ON — nivel %s por %lu s\n", nivel.c_str(), duracionSeg);
  publicarEstado("alarma_on");
}

void apagarAlarma() {
  if (!alarmaActiva && !sirenaEncendida) return;
  alarmaActiva = false;
  sirena(false);
  Serial.println("[ALARMA] OFF");
  publicarEstado("alarma_off");
}

// Se llama en cada loop(): alterna la sirena y la apaga al cumplir la duración
void actualizarSirena() {
  if (!alarmaActiva) return;
  unsigned long ahora = millis();
  if (ahora - alarmaInicio >= alarmaDuracionMs) {
    apagarAlarma();
    return;
  }
  unsigned long espera = sirenaEncendida ? sirenaOnMs : sirenaOffMs;
  if (ahora - sirenaCambio >= espera) sirena(!sirenaEncendida);
}

// Mensaje JSON recibido en estacionN/alarma
void procesarAlarma(const String& mensaje) {
  StaticJsonDocument<192> doc;
  if (deserializeJson(doc, mensaje)) {
    Serial.printf("[ALARMA] JSON invalido: %s\n", mensaje.c_str());
    return;
  }
  String accion = doc["accion"] | "ON";
  accion.toUpperCase();
  if (accion == "OFF") { apagarAlarma(); return; }
  activarAlarma(doc["nivel"] | "Baja", doc["duracionSeg"] | ALARMA_DURACION_DEF_S);
}

// ================================================================
// BUFFER EN RAM — respaldo solo cuando NO hay SD
// 60 lecturas = 2 horas a 2 minutos. Se pierde si reinicia el ESP32.
// ================================================================
const int BUFFER_MAX  = 60;
String    buffer[BUFFER_MAX];
int       bufferHead  = 0;
int       bufferTail  = 0;
int       bufferCount = 0;

void bufferAgregar(const String& json) {
  buffer[bufferHead] = json;
  bufferHead = (bufferHead + 1) % BUFFER_MAX;
  if (bufferCount < BUFFER_MAX) {
    bufferCount++;
  } else {
    bufferTail = (bufferTail + 1) % BUFFER_MAX;
    Serial.println("[BUFFER] Lleno — descartando lectura mas antigua");
  }
}

bool   bufferVacio()     { return bufferCount == 0; }
String bufferPrimero()   { return buffer[bufferTail]; }
void   bufferDescartar() { bufferTail = (bufferTail + 1) % BUFFER_MAX; bufferCount--; }

// ================================================================
// HORA — el reloj interno del ESP32 se carga desde el RTC al arrancar
// y NTP lo corrige cuando hay internet (y de paso corrige el RTC).
// ================================================================
RTC_DS3231 rtc;
bool          rtcDisponible         = false;
volatile bool ntpRecienSincronizado = false;   // lo marca el callback de SNTP

String obtenerFechaHora() {
  struct tm ti;
  if (!getLocalTime(&ti, 10)) return "sin_hora";   // 10 ms: no bloquea si no hay hora
  char buf[30];
  strftime(buf, sizeof(buf), "%Y-%m-%d %H:%M:%S", &ti);
  return String(buf);
}

bool esDeDia() {
  struct tm ti;
  if (!getLocalTime(&ti, 10)) return true;
  return (ti.tm_hour >= HORA_INICIO_DIA && ti.tm_hour < HORA_FIN_DIA);
}

// Corre en la tarea de red: solo levanta la bandera, el I2C se usa en el loop
void alSincronizarNTP(struct timeval* tv) { ntpRecienSincronizado = true; }

bool rtcHoraConfiable() {
  if (!rtcDisponible || rtc.lostPower()) return false;
  int anio = rtc.now().year();
  return anio >= 2024 && anio <= 2099;
}

// El RTC guarda la hora local de Colombia; el reloj del ESP32 va en UTC
bool relojDesdeRTC() {
  if (!rtcHoraConfiable()) return false;
  DateTime n = rtc.now();
  struct timeval tv;
  tv.tv_sec  = (time_t)n.unixtime() - gmtOffset_sec;
  tv.tv_usec = 0;
  settimeofday(&tv, NULL);
  return true;
}

void rtcDesdeReloj() {
  if (!rtcDisponible) return;
  struct tm ti;
  if (!getLocalTime(&ti, 10)) return;
  rtc.adjust(DateTime(ti.tm_year + 1900, ti.tm_mon + 1, ti.tm_mday,
                      ti.tm_hour, ti.tm_min, ti.tm_sec));
}

void iniciarRTC() {
  setenv("TZ", "<-05>5", 1);   // Colombia, UTC-5 sin horario de verano
  tzset();
  sntp_set_time_sync_notification_cb(alSincronizarNTP);

  if (!rtc.begin()) {
    Serial.println("[RTC] DS3231 no detectado — revisar SDA=21 / SCL=22");
    return;
  }
  rtcDisponible = true;
  if (relojDesdeRTC()) {
    horaValida = true;
    fuenteHora = "RTC";
    Serial.printf("[RTC] OK — %s\n", obtenerFechaHora().c_str());
  } else {
    Serial.println("[RTC] Sin hora valida (perdio energia) — se ajusta con NTP");
  }
}

// Se llama en cada loop(): cuando NTP sincroniza, copia la hora al RTC
void atenderNTP() {
  if (!ntpRecienSincronizado) return;
  ntpRecienSincronizado = false;
  horaValida = true;
  fuenteHora = "NTP";
  rtcDesdeReloj();
  Serial.printf("[NTP] Sincronizado — RTC %s\n", rtcDisponible ? "actualizado" : "no disponible");
}

// ================================================================
// SD — histórico permanente y cola de pendientes
//   /datos/AAAAMMDD.csv  una línea por lectura, siempre (haya o no red)
//   /pendientes.txt      JSON que no llegaron al broker local
//   /pendientes.pos      byte hasta donde ya se reenvió (retoma tras reinicio)
// ================================================================
bool sdDisponible = false;

const char* DIR_DATOS          = "/datos";
const char* ARCHIVO_PENDIENTES = "/pendientes.txt";
const char* ARCHIVO_POSICION   = "/pendientes.pos";
const char* ENCABEZADO_CSV     = "fecha,tem,hum,rad,velV,vvp,vvi,dirV,lluv,llup";

unsigned long pendientesSD  = 0;   // líneas de /pendientes.txt aún sin reenviar
uint32_t      posPendientes = 0;

const int           LOTE_ENVIO           = 10;       // mensajes por tanda
const unsigned long intervaloEnvioLote   = 300UL;    // una tanda cada 300 ms
const unsigned long intervaloReintentoSD = 60000UL;  // reintentar montar la SD
unsigned long lastEnvioPendientes = 0;
unsigned long lastEnvioDia        = 0;
unsigned long lastReintentoSD     = 0;

// Envío de un día completo pedido desde la web (SD_ENVIAR:AAAA-MM-DD)
bool          envioDiaActivo = false;
String        envioDiaFecha  = "";
String        envioDiaRuta   = "";
uint32_t      envioDiaPos    = 0;
unsigned long envioDiaLineas = 0;

bool iniciarSD() {
  SD.end();
  if (!SD.begin(SD_CS)) {
    sdDisponible = false;
    return false;
  }
  if (!SD.exists(DIR_DATOS)) SD.mkdir(DIR_DATOS);
  sdDisponible = true;
  return true;
}

// "2026-10-28 08:39:39" → "/datos/20261028.csv"
String rutaDelDia(const String& fecha) {
  if (fecha.length() < 10 || fecha == "sin_hora") return String(DIR_DATOS) + "/sin_hora.csv";
  String dia = fecha.substring(0, 10);
  dia.replace("-", "");
  return String(DIR_DATOS) + "/" + dia + ".csv";
}

void guardarHistorico(const String& fecha) {
  if (!sdDisponible) return;
  String ruta  = rutaDelDia(fecha);
  bool   nuevo = !SD.exists(ruta);
  File f = SD.open(ruta, FILE_APPEND);
  if (!f) {
    Serial.println("[SD] Error al escribir el historico — SD marcada como no disponible");
    sdDisponible = false;
    return;
  }
  if (nuevo) f.println(ENCABEZADO_CSV);
  f.printf("%s,%.1f,%.1f,%d,%.2f,%lu,%lu,%d,%.2f,%lu\n",
           fecha.c_str(), temperatura, humedad, (int)radiacion,
           velVVentana, vvpVentana, vviVentana,
           (int)anguloViento, lluvVentana, llupVentana);
  f.close();
}

void guardarPosicionPendientes() {
  File f = SD.open(ARCHIVO_POSICION, FILE_WRITE);
  if (f) { f.print(posPendientes); f.close(); }
}

void borrarPendientes() {
  SD.remove(ARCHIVO_PENDIENTES);
  SD.remove(ARCHIVO_POSICION);
  posPendientes = 0;
  pendientesSD  = 0;
}

// Al arrancar (o al recuperar la SD): retoma desde donde quedó y cuenta lo que falta
void cargarPendientes() {
  posPendientes = 0;
  pendientesSD  = 0;
  if (!sdDisponible || !SD.exists(ARCHIVO_PENDIENTES)) return;

  if (SD.exists(ARCHIVO_POSICION)) {
    File p = SD.open(ARCHIVO_POSICION, FILE_READ);
    if (p) { posPendientes = p.readString().toInt(); p.close(); }
  }
  File f = SD.open(ARCHIVO_PENDIENTES, FILE_READ);
  if (!f) return;
  if (posPendientes > f.size()) posPendientes = 0;
  f.seek(posPendientes);
  uint8_t bloque[256];
  int n;
  while ((n = f.read(bloque, sizeof(bloque))) > 0) {
    for (int i = 0; i < n; i++) if (bloque[i] == '\n') pendientesSD++;
    esp_task_wdt_reset();
  }
  f.close();
  Serial.printf("[SD] %lu lecturas pendientes por reenviar\n", pendientesSD);
}

// Mismo papel que el buffer en RAM, pero en la SD: no se pierde al reiniciar
void guardarPendiente(const String& json) {
  if (sdDisponible) {
    File f = SD.open(ARCHIVO_PENDIENTES, FILE_APPEND);
    if (f) {
      f.println(json);
      f.close();
      pendientesSD++;
      return;
    }
    sdDisponible = false;
  }
  bufferAgregar(json);   // sin SD: respaldo en RAM
}

// Reenvío no bloqueante por estacionN/historico: una tanda pequeña por vuelta del loop.
// Primero lo que quedó en RAM y luego lo de la SD.
void procesarPendientes() {
  if (!mqttLocal.connected()) return;
  if (bufferVacio() && pendientesSD == 0) return;
  if (millis() - lastEnvioPendientes < intervaloEnvioLote) return;
  lastEnvioPendientes = millis();

  int enviados = 0;
  while (enviados < LOTE_ENVIO && !bufferVacio()) {
    if (!mqttLocal.publish(topic_historico, bufferPrimero().c_str(), false)) return;
    bufferDescartar();
    enviados++;
  }
  if (enviados >= LOTE_ENVIO || pendientesSD == 0 || !sdDisponible) return;

  File f = SD.open(ARCHIVO_PENDIENTES, FILE_READ);
  if (!f) { pendientesSD = 0; return; }
  f.seek(posPendientes);
  bool fallo = false;
  while (enviados < LOTE_ENVIO && f.available()) {
    String   linea     = f.readStringUntil('\n');
    uint32_t siguiente = f.position();
    linea.trim();
    if (linea.length() > 0) {
      if (!mqttLocal.publish(topic_historico, linea.c_str(), false)) { fallo = true; break; }
      enviados++;
      if (pendientesSD > 0) pendientesSD--;
    }
    posPendientes = siguiente;
  }
  bool terminado = !fallo && !f.available();
  f.close();

  if (terminado) {
    borrarPendientes();
    Serial.println("[SD] Pendientes reenviados por completo");
  } else {
    guardarPosicionPendientes();
  }
}

// Línea del CSV → mismo JSON compacto que estacionN/datos
String csvAJson(const String& linea) {
  String campos[10];
  int n = 0;
  unsigned int inicio = 0;
  for (unsigned int i = 0; i <= linea.length() && n < 10; i++) {
    if (i == linea.length() || linea[i] == ',') {
      campos[n++] = linea.substring(inicio, i);
      inicio = i + 1;
    }
  }
  if (n < 10 || campos[0].length() != 19) return "";   // línea incompleta o sin hora

  StaticJsonDocument<256> doc;
  doc["e"]    = ESTACION_NUM;
  doc["t"]    = campos[0];
  doc["tem"]  = campos[1].toFloat();
  doc["hum"]  = campos[2].toFloat();
  doc["rad"]  = campos[3].toInt();
  doc["velV"] = campos[4].toFloat();
  doc["vvp"]  = campos[5].toInt();
  doc["vvi"]  = campos[6].toInt();
  doc["dirV"] = campos[7].toInt();
  doc["lluv"] = campos[8].toFloat();
  doc["llup"] = campos[9].toInt();
  String json;
  serializeJson(doc, json);
  return json;
}

void publicarFinEnvio(const String& dia, unsigned long lineas, const char* error) {
  StaticJsonDocument<192> doc;
  doc["tipo"]   = "fin_envio";
  doc["e"]      = ESTACION_NUM;
  doc["dia"]    = dia;
  doc["lineas"] = lineas;
  if (error) doc["error"] = error;
  String json;
  serializeJson(doc, json);
  mqttLocal.publish(topic_sd, json.c_str(), false);
  Serial.printf("[SD] Envio del dia %s: %lu lineas %s\n", dia.c_str(), lineas, error ? error : "OK");
}

void iniciarEnvioDia(String dia) {
  dia.trim();
  if (envioDiaActivo)    { publicarFinEnvio(dia, 0, "ocupado");   return; }
  if (!sdDisponible)     { publicarFinEnvio(dia, 0, "sin_sd");    return; }
  if (dia.length() != 10) { publicarFinEnvio(dia, 0, "fecha_invalida"); return; }
  String ruta = rutaDelDia(dia + " 00:00:00");
  if (!SD.exists(ruta))  { publicarFinEnvio(dia, 0, "no_existe"); return; }

  envioDiaActivo = true;
  envioDiaFecha  = dia;
  envioDiaRuta   = ruta;
  envioDiaPos    = 0;
  envioDiaLineas = 0;
  Serial.printf("[SD] Enviando el dia %s...\n", dia.c_str());
}

// Igual que los pendientes: por tandas y sin delay(). Si se cae la conexión, retoma al volver.
void procesarEnvioDia() {
  if (!envioDiaActivo || !mqttLocal.connected()) return;
  if (millis() - lastEnvioDia < intervaloEnvioLote) return;
  lastEnvioDia = millis();

  File f = SD.open(envioDiaRuta, FILE_READ);
  if (!f) {
    envioDiaActivo = false;
    publicarFinEnvio(envioDiaFecha, envioDiaLineas, "error_lectura");
    return;
  }
  f.seek(envioDiaPos);
  int  enviados = 0;
  bool fallo    = false;
  while (enviados < LOTE_ENVIO && f.available()) {
    String   linea     = f.readStringUntil('\n');
    uint32_t siguiente = f.position();
    linea.trim();
    String json = linea.startsWith("fecha") ? "" : csvAJson(linea);
    if (json.length() > 0) {
      if (!mqttLocal.publish(topic_historico, json.c_str(), false)) { fallo = true; break; }
      enviados++;
      envioDiaLineas++;
    }
    envioDiaPos = siguiente;
  }
  bool terminado = !fallo && !f.available();
  f.close();

  if (terminado) {
    envioDiaActivo = false;
    publicarFinEnvio(envioDiaFecha, envioDiaLineas, nullptr);
  }
}

// Estado de la SD y el RTC + días guardados (los más recientes primero)
struct DiaSD { String dia; uint32_t bytes; };
const size_t MAX_DIAS_REPORTE = 45;

void publicarEstadoSD() {
  if (!mqttLocal.connected()) return;

  DynamicJsonDocument doc(2048);
  doc["tipo"]        = "estado";
  doc["e"]           = ESTACION_NUM;
  doc["sd"]          = sdDisponible;
  doc["rtc"]         = rtcDisponible;
  doc["rtcOk"]       = rtcHoraConfiable();
  doc["fuenteHora"]  = fuenteHora;
  doc["hora"]        = obtenerFechaHora();
  doc["pendientes"]  = pendientesSD;
  doc["bufferRam"]   = bufferCount;
  doc["enviandoDia"] = envioDiaActivo ? envioDiaFecha : "";

  if (sdDisponible) {
    doc["totalMB"] = (uint32_t)(SD.totalBytes() / (1024ULL * 1024ULL));
    doc["usadoMB"] = (uint32_t)(SD.usedBytes()  / (1024ULL * 1024ULL));

    std::vector<DiaSD> dias;
    File dir = SD.open(DIR_DATOS);
    if (dir) {
      File f = dir.openNextFile();
      while (f) {
        String nombre = f.name();
        int barra = nombre.lastIndexOf('/');
        if (barra >= 0) nombre = nombre.substring(barra + 1);
        if (!f.isDirectory() && nombre.length() == 12 && nombre.endsWith(".csv")) {
          String d = nombre.substring(0, 4) + "-" + nombre.substring(4, 6) + "-" + nombre.substring(6, 8);
          dias.push_back({ d, (uint32_t)f.size() });
        }
        f.close();
        f = dir.openNextFile();
        esp_task_wdt_reset();
      }
      dir.close();
    }
    std::sort(dias.begin(), dias.end(), [](const DiaSD& a, const DiaSD& b) { return a.dia > b.dia; });

    doc["totalDias"] = dias.size();
    JsonArray arr = doc.createNestedArray("dias");
    for (size_t i = 0; i < dias.size() && i < MAX_DIAS_REPORTE; i++) {
      JsonObject o = arr.createNestedObject();
      o["d"] = dias[i].dia;
      o["b"] = dias[i].bytes;
    }
  }

  String json;
  serializeJson(doc, json);
  bool ok = mqttLocal.publish(topic_sd, json.c_str(), false);
  Serial.printf("[SD] Estado publicado (%d bytes) %s\n", json.length(), ok ? "OK" : "FALLO");
}

// Si la SD falló o no estaba al arrancar, se intenta montar de nuevo cada minuto
void reintentarSD() {
  if (sdDisponible) return;
  if (millis() - lastReintentoSD < intervaloReintentoSD) return;
  lastReintentoSD = millis();
  if (iniciarSD()) {
    Serial.println("[SD] Tarjeta montada de nuevo");
    cargarPendientes();
  }
}

// ================================================================
// SENSORES
// ================================================================
float velocidadDesdePulsos(unsigned long pulsos, unsigned long dt_ms) {
  if (dt_ms == 0) return 0.0;
  float vueltas     = pulsos / PULSOS_POR_VUELTA;
  float vueltasPorS = vueltas / (dt_ms / 1000.0);
  float vCopas      = vueltasPorS * CIRCUNFERENCIA_M;
  return vCopas * FACTOR_ANEMOMETRO;
}

void calcularLluviaMinuto() {
  if (millis() - ultimoMinuto < intervaloLluvia) return;
  ultimoMinuto = millis();
  portENTER_CRITICAL(&muxLluvia);
  unsigned long p = pulsosLluvia; pulsosLluvia = 0;
  portEXIT_CRITICAL(&muxLluvia);
  pulsosLluviaCrudo = p;
  lluviaMinuto      = p * MM_POR_BALANCEO;
  intensidadLluvia  = lluviaMinuto * 60.0;
}

void leerVeleta() {
  int   crudo   = analogRead(PIN_VELETA);
  float voltaje = crudo * 3.3 / 4095.0;

  if      (voltaje < 0.40) { direccion = "Este";     anguloViento = 90;  }
  else if (voltaje < 0.70) { direccion = "Sureste";  anguloViento = 135; }
  else if (voltaje < 1.19) { direccion = "Sur";      anguloViento = 180; }
  else if (voltaje < 1.74) { direccion = "Noreste";  anguloViento = 45;  }
  else if (voltaje < 2.25) { direccion = "Suroeste"; anguloViento = 225; }
  else if (voltaje < 2.68) { direccion = "Norte";    anguloViento = 0;   }
  else if (voltaje < 2.93) { direccion = "Noroeste"; anguloViento = 315; }
  else if (voltaje <= 3.30){ direccion = "Oeste";    anguloViento = 270; }
  else                     { direccion = "SIN";      anguloViento = -1;  }
}

void actualizarSensores() {
  // DHT22
  float t = dht.readTemperature();
  float h = dht.readHumidity();
  if (!isnan(t)) { temperatura = t; }
  else { fallosTemp++; Serial.printf("[DHT] Fallo temp — acum: %lu\n", fallosTemp); }
  if (!isnan(h)) { humedad = h; }
  else { fallosHum++; Serial.printf("[DHT] Fallo hum — acum: %lu\n", fallosHum); }

  // TSL2561
  uint16_t broadband, ir;
  tsl.getLuminosity(&broadband, &ir);
  bool saturado = (broadband >= TSL_MAX_COUNT_13MS) || (ir >= TSL_MAX_COUNT_13MS);
  if (saturado) {
    luxSaturado = true;
    lux = esDeDia() ? LUX_MAXIMA_SENSOR : 0;
  } else {
    uint32_t luxCalc = tsl.calculateLux(broadband, ir);
    lux = (luxCalc == 65536) ? LUX_MAXIMA_SENSOR : (float)luxCalc;
    luxSaturado = (luxCalc == 65536);
  }
  radiacion = lux;

  // Anemómetro
  unsigned long ahoraV = millis();
  intervaloVientoMs = ahoraV - tUltimaVentana;
  tUltimaVentana = ahoraV;
  portENTER_CRITICAL(&muxViento);
  unsigned long pv = pulsosViento; pulsosViento = 0;
  portEXIT_CRITICAL(&muxViento);
  pulsosVientoCrudo  = pv;
  velocidadViento    = velocidadDesdePulsos(pv, intervaloVientoMs);
  velocidadVientoKmh = velocidadViento * 3.6;

  // Veleta
  leerVeleta();
}

// Cierra la ventana de publicación: toma los pulsos acumulados desde la publicación
// anterior y calcula promedios con el tiempo real transcurrido.
void cerrarVentana() {
  unsigned long ahora = millis();
  vviVentana    = ahora - inicioVentana;
  inicioVentana = ahora;
  portENTER_CRITICAL(&muxViento);
  vvpVentana = pulsosVientoVentana; pulsosVientoVentana = 0;
  portEXIT_CRITICAL(&muxViento);
  portENTER_CRITICAL(&muxLluvia);
  llupVentana = pulsosLluviaVentana; pulsosLluviaVentana = 0;
  portEXIT_CRITICAL(&muxLluvia);

  float seg   = vviVentana / 1000.0;
  velVVentana = velocidadDesdePulsos(vvpVentana, vviVentana);
  lluvVentana = (seg > 0) ? (llupVentana * MM_POR_BALANCEO) / (seg / 60.0) : 0;
}

// ================================================================
// JSON LOCAL — compacto, sin unidades, ~130 bytes
// ================================================================
String generarJSONLocal(const String& fecha) {
  StaticJsonDocument<256> doc;
  doc["e"]    = ESTACION_NUM;
  doc["t"]    = fecha;
  doc["tem"]  = serialized(String(temperatura, 1));
  doc["hum"]  = serialized(String(humedad, 1));
  doc["rad"]  = (int)radiacion;
  doc["velV"] = serialized(String(velVVentana, 2));
  doc["vvp"]  = (unsigned int)vvpVentana;
  doc["vvi"]  = (unsigned int)vviVentana;
  doc["dirV"] = (int)anguloViento;
  doc["lluv"] = serialized(String(lluvVentana, 2));
  doc["llup"] = (unsigned int)llupVentana;
  String json;
  serializeJson(doc, json);
  return json;
}

// ================================================================
// JSON REMOTO — formato original HiveMQ con unidades
// ================================================================
String generarJSONRemoto(const String& fecha) {
  StaticJsonDocument<1024> doc;
  JsonObject o;

  o = doc.createNestedObject("temperatura");
  o["valor"] = temperatura; o["unidades"] = "C";
  o["sensor"] = "aa07acd0-57e0-4ae0-bf46-f8d7c94b4040"; o["fecha"] = fecha;

  o = doc.createNestedObject("humedad");
  o["valor"] = humedad; o["unidades"] = "%";
  o["sensor"] = "aa07acd0-57e0-4ae0-bf46-f8d7c94b4040"; o["fecha"] = fecha;

  o = doc.createNestedObject("radiacion");
  o["valor"] = radiacion; o["unidades"] = "Lux";
  o["saturado"] = luxSaturado;
  o["sensor"] = "6319081a-9f85-4e87-8759-6895d08d935a"; o["fecha"] = fecha;

  o = doc.createNestedObject("velocidadViento");
  o["valor"] = velVVentana; o["unidades"] = "m/s";
  o["sensor"] = "id_sensorVelocidad"; o["fecha"] = fecha;

  o = doc.createNestedObject("direccion");
  o["valor"] = direccion; o["grados"] = anguloViento;
  o["unidades"] = "cardinal";
  o["sensor"] = "id_sensorDireccion"; o["fecha"] = fecha;

  o = doc.createNestedObject("lluvia");
  o["valor"] = lluvVentana; o["unidades"] = "mm/min";
  o["sensor"] = "id_sensorlluvia"; o["fecha"] = fecha;

  String json;
  serializeJson(doc, json);
  return json;
}

// ================================================================
// PUBLICAR A LOS DOS BROKERS + GUARDAR EN SD
// ================================================================
void publicarDatos() {
  cerrarVentana();   // viento y lluvia = pulsos acumulados desde la publicación anterior
  String fecha      = obtenerFechaHora();
  String jsonLocal  = generarJSONLocal(fecha);
  String jsonRemoto = generarJSONRemoto(fecha);

  // Histórico en SD siempre, haya o no conexión
  guardarHistorico(fecha);

  // Broker local — si falla, queda pendiente en la SD (o en RAM si no hay SD)
  if (mqttLocal.connected()) {
    bool ok = mqttLocal.publish(topic_pub, jsonLocal.c_str(), false);
    Serial.printf("[LOCAL]  %s (%d bytes)\n", ok ? "OK" : "FALLO", jsonLocal.length());
    if (!ok) guardarPendiente(jsonLocal);
  } else {
    guardarPendiente(jsonLocal);
    Serial.printf("[LOCAL]  Sin conexion — pendientes SD: %lu | RAM: %d\n", pendientesSD, bufferCount);
  }

  // Broker remoto
  if (mqttRemoto.connected()) {
    bool ok = mqttRemoto.publish(topic_pub, jsonRemoto.c_str(), false);
    Serial.printf("[REMOTO] %s (%d bytes)\n", ok ? "OK" : "FALLO", jsonRemoto.length());
  } else {
    Serial.println("[REMOTO] Sin conexion");
  }
}

// ================================================================
// MQTT CALLBACK
// ================================================================
void mqttCallback(char* topic, byte* payload, unsigned int length) {
  String mensaje;
  mensaje.reserve(length);
  for (unsigned int i = 0; i < length; i++) mensaje += (char)payload[i];
  String topicStr = String(topic);

  if (topicStr == topic_sub) {
    Serial.printf("[MQTT] Solicitud en '%s'\n", topic);
    actualizarSensores();
    publicarDatos();
  }
  else if (topicStr == topic_alarma) {
    procesarAlarma(mensaje);
  }
  else if (topicStr == topic_comando) {
    mensaje.trim(); mensaje.toUpperCase();
    if (mensaje == "RESET") {
      digitalWrite(PIN_SIRENA, LOW);
      mqttLocal.publish(topic_estado,  "reiniciando", false);
      mqttRemoto.publish(topic_estado, "reiniciando", false);
      delay(1000);
      ESP.restart();
    }
    else if (mensaje == "ALARMA_ON")  activarAlarma("ALTA", ALARMA_DURACION_DEF_S);
    else if (mensaje == "ALARMA_OFF") apagarAlarma();
    else if (mensaje == "SD_ESTADO")  publicarEstadoSD();
    else if (mensaje.startsWith("SD_ENVIAR:")) iniciarEnvioDia(mensaje.substring(10));
  }
}

// ================================================================
// CONEXIONES
// ================================================================
void conectarLocal() {
  if (WiFi.status() != WL_CONNECTED) return;
  esp_task_wdt_reset();
  Serial.printf("[LOCAL] Conectando como '%s'...\n", clientId_local);
  bool ok = mqttLocal.connect(clientId_local);
  esp_task_wdt_reset();
  if (ok) {
    Serial.println("[LOCAL] Conectado");
    mqttLocal.subscribe(topic_sub);
    mqttLocal.subscribe(topic_comando);
    mqttLocal.subscribe(topic_alarma);
    mqttLocal.publish(topic_estado, "en_linea", false);
    publicarEstadoSD();   // los pendientes se reenvían desde el loop, por tandas
  } else {
    Serial.printf("[LOCAL] Fallo rc=%d\n", mqttLocal.state());
  }
}

// Diagnóstico cuando falla HiveMQ: dice si el problema es DNS, puerto o TLS
unsigned long msUltimoIntentoRemoto = 0;

void diagnosticarRemoto() {
  Serial.printf("[DIAG] Heap libre: %u bytes\n", ESP.getFreeHeap());
  Serial.printf("[DIAG] El intento tardo %lu ms (cerca de 20000 = timeout del TLS)\n", msUltimoIntentoRemoto);

  // 1) Error TLS de la última conexión
  char errTls[100];
  int codTls = espClientRemoto.lastError(errTls, sizeof(errTls));
  Serial.printf("[DIAG] TLS error %d: %s\n", codTls, errTls);

  // 2) DNS
  IPAddress ipRemoto;
  esp_task_wdt_reset();
  if (!WiFi.hostByName(mqtt_broker_remoto, ipRemoto)) {
    Serial.println("[DIAG] DNS FALLO -> la red no resuelve nombres (sin internet o DNS bloqueado)");
    return;
  }
  Serial.printf("[DIAG] DNS OK -> %s\n", ipRemoto.toString().c_str());

  // 3) Puerto 8883 sin TLS
  WiFiClient prueba;
  esp_task_wdt_reset();
  bool tcpOk = prueba.connect(ipRemoto, mqtt_port_remoto, 5000);
  prueba.stop();
  esp_task_wdt_reset();
  if (tcpOk) Serial.println("[DIAG] Puerto 8883 ABIERTO -> el problema es el TLS");
  else       Serial.println("[DIAG] Puerto 8883 BLOQUEADO -> la red no deja salir al 8883");
}

void conectarRemoto() {
  if (WiFi.status() != WL_CONNECTED) return;
  esp_task_wdt_reset();
  Serial.printf("[REMOTO] Conectando como '%s'...\n", clientId_remoto);
  unsigned long tIntento = millis();
  bool ok = mqttRemoto.connect(clientId_remoto, mqtt_user_remoto, mqtt_pass_remoto);
  msUltimoIntentoRemoto = millis() - tIntento;
  esp_task_wdt_reset();
  if (ok) {
    Serial.println("[REMOTO] Conectado");
    mqttRemoto.subscribe(topic_sub);
    mqttRemoto.subscribe(topic_comando);
    mqttRemoto.publish(topic_estado, "en_linea", false);
  } else {
    Serial.printf("[REMOTO] Fallo rc=%d\n", mqttRemoto.state());
    diagnosticarRemoto();
  }
}

// ================================================================
// SERIAL
// ================================================================
void imprimirSerial() {
  Serial.println("─────────────────────────────────────────");
  Serial.println("[REMOTA 3] " + obtenerFechaHora() + " (" + fuenteHora + ")");
  Serial.printf("  tem : %.1f C\n",       temperatura);
  Serial.printf("  hum : %.1f %%\n",      humedad);
  Serial.printf("  rad : %.0f Lux%s\n",   radiacion, luxSaturado ? " [SAT]" : "");
  Serial.printf("  velV: %.2f m/s (%.2f km/h) | %lu pulsos %lu ms\n",
                velocidadViento, velocidadVientoKmh, pulsosVientoCrudo, intervaloVientoMs);
  Serial.printf("  dirV: %s (%.0f grados)\n", direccion.c_str(), anguloViento);
  Serial.printf("  lluv: %.2f mm/min | %lu pulsos\n", lluviaMinuto, pulsosLluviaCrudo);
  Serial.printf("  RTC : %s | SD: %s | Pendientes SD: %lu | RAM: %d%s\n",
                rtcDisponible ? (rtcHoraConfiable() ? "OK" : "SIN HORA") : "NO",
                sdDisponible  ? "OK" : "NO",
                pendientesSD, bufferCount,
                envioDiaActivo ? " | enviando dia" : "");
  Serial.printf("  Local : %s | Remoto: %s\n",
                mqttLocal.connected()  ? "OK" : "OFF",
                mqttRemoto.connected() ? "OK" : "OFF");
  Serial.printf("  Alarma: %s\n", alarmaActiva ? "ON" : "OFF");
  Serial.println("─────────────────────────────────────────");
}

// ================================================================
// SETUP
// ================================================================
void setup() {
  // Sirena apagada lo antes posible (GPIO 15 queda en alto durante el arranque)
  pinMode(PIN_SIRENA, OUTPUT);
  digitalWrite(PIN_SIRENA, LOW);

  Serial.begin(115200);
  delay(500);
  Serial.println("\n=== ESTACION 3 — v7 — MODO DUAL + SD + RTC ===");

  // Watchdog 30 segundos — el core 3.x ya inicia el TWDT con 5 s, así que
  // esp_task_wdt_init() falla y hay que reconfigurarlo
  esp_task_wdt_config_t wdt_config = {
    .timeout_ms    = 30000,
    .idle_core_mask = 0,
    .trigger_panic  = true
  };
  esp_err_t wdtErr = esp_task_wdt_reconfigure(&wdt_config);
  if (wdtErr != ESP_OK) wdtErr = esp_task_wdt_init(&wdt_config);
  Serial.printf("[WDT] 30 s: %s\n", esp_err_to_name(wdtErr));
  esp_task_wdt_add(NULL);

  // Interrupciones reed switch
  pinMode(PIN_VIENTO, INPUT_PULLUP);
  pinMode(PIN_LLUVIA, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(PIN_VIENTO), contarViento, FALLING);
  attachInterrupt(digitalPinToInterrupt(PIN_LLUVIA), contarLluvia, FALLING);

  // Veleta resistiva — ADC1
  analogReadResolution(12);
  analogSetAttenuation(ADC_11db);

  // I2C — SDA=21, SCL=22 (igual en las 3 estaciones)
  Wire.begin(I2C_SDA, I2C_SCL);

  // RTC DS3231 — deja la hora lista antes de tener WiFi
  iniciarRTC();

  // TSL2561 — comparte I2C con el RTC
  if (!tsl.begin()) {
    Serial.println("[TSL] No detectado");
  } else {
    tsl.enableAutoRange(false);
    tsl.setGain(TSL2561_GAIN_1X);
    tsl.setIntegrationTime(TSL2561_INTEGRATIONTIME_13MS);
    Serial.println("[TSL] OK");
  }

  // SD — SPI independiente del I2C
  if (iniciarSD()) {
    Serial.printf("[SD] OK — %llu MB, usados %llu MB\n",
                  SD.totalBytes() / (1024ULL * 1024ULL), SD.usedBytes() / (1024ULL * 1024ULL));
    cargarPendientes();
  } else {
    Serial.println("[SD] No disponible — revisar cableado y formato FAT32 (se reintenta cada minuto)");
  }

  // DHT22
  dht.begin();

  // WiFi
  WiFi.begin(ssid, password);
  Serial.print("[WiFi] Conectando");
  unsigned long t0 = millis();
  while (WiFi.status() != WL_CONNECTED && millis() - t0 < 15000) {
    delay(300); Serial.print(".");
    esp_task_wdt_reset();
  }
  Serial.println(WiFi.status() == WL_CONNECTED
    ? "\n[WiFi] Conectado — IP: " + WiFi.localIP().toString()
    : "\n[WiFi] Sin conexion");

  // NTP — maximo 10 segundos, no bloquea el arranque. Si no llega, sigue con la hora del RTC
  // y cuando sincronice más tarde, atenderNTP() corrige el RTC desde el loop.
  configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);
  unsigned long tNtp = millis();
  while (!ntpRecienSincronizado && WiFi.status() == WL_CONNECTED && millis() - tNtp < 10000) {
    delay(200);
    esp_task_wdt_reset();
  }
  atenderNTP();
  if (!horaValida) Serial.println("[HORA] Sin NTP ni RTC — las lecturas quedan como sin_hora");

  // MQTT local — 2048 para que quepa el estado de la SD con la lista de días
  mqttLocal.setServer(mqtt_broker_local, mqtt_port_local);
  mqttLocal.setCallback(mqttCallback);
  mqttLocal.setBufferSize(2048);

  // MQTT remoto TLS
  espClientRemoto.setInsecure();
  espClientRemoto.setHandshakeTimeout(20); // por defecto 120 s; con 10 s fallaba en JEAR_LESTOMA
  mqttRemoto.setServer(mqtt_broker_remoto, mqtt_port_remoto);
  mqttRemoto.setCallback(mqttCallback);
  mqttRemoto.setBufferSize(1024);
  mqttRemoto.setSocketTimeout(10);
  mqttLocal.setSocketTimeout(5);

  // Lectura inicial
  actualizarSensores();
  tUltimaVentana = millis();

  // Conectar
  conectarLocal();
  conectarRemoto();

  // Timers
  ultimoMinuto        = millis();
  lastRead            = millis();
  lastSerial          = millis();
  lastReconnectLocal  = millis();
  lastReconnectRemoto = millis();
  lastPublicacion     = millis();

  // Arranca la primera ventana de publicación desde cero
  portENTER_CRITICAL(&muxViento);
  pulsosVientoVentana = 0;
  portEXIT_CRITICAL(&muxViento);
  portENTER_CRITICAL(&muxLluvia);
  pulsosLluviaVentana = 0;
  portEXIT_CRITICAL(&muxLluvia);
  inicioVentana = millis();
}

// ================================================================
// LOOP
// ================================================================
void loop() {
  esp_task_wdt_reset();
  unsigned long ahora = millis();

  // Mantener conexión local
  if (!mqttLocal.connected()) {
    if (ahora - lastReconnectLocal >= intervaloReconnect) {
      lastReconnectLocal = ahora;
      conectarLocal();
    }
  }
  mqttLocal.loop();

  // Mantener conexión remota
  if (!mqttRemoto.connected()) {
    if (ahora - lastReconnectRemoto >= intervaloReconnectRemoto) {
      lastReconnectRemoto = ahora;
      conectarRemoto();
    }
  }
  mqttRemoto.loop();

  // Sirena (patrón no bloqueante)
  actualizarSirena();

  // Hora y SD
  atenderNTP();
  reintentarSD();
  procesarPendientes();
  procesarEnvioDia();

  // Leer sensores cada 2.5 s
  if (ahora - lastRead >= intervaloRead) {
    lastRead = ahora;
    actualizarSensores();
  }

  // Lluvia cada 1 min
  calcularLluviaMinuto();

  // Publicación autónoma cada 2 minutos
  if (ahora - lastPublicacion >= intervaloPublicacion) {
    lastPublicacion = ahora;
    publicarDatos();
  }

  // Serial cada 1 s
  if (ahora - lastSerial >= intervaloSerial) {
    lastSerial = ahora;
    imprimirSerial();
  }
}
