/*
 * ================================================================
 *  ESTACIÓN METEOROLÓGICA REMOTA 3 — ESP32
 *  Modo DUAL: Mosquitto local (10.40.20.10:1883) + HiveMQ Cloud
 *
 *  Sensores: DHT22 | TSL2561 | Anemómetro reed | Pluviómetro reed
 *            Veleta resistiva (GPIO 34, ADC1)
 *  Hardware adicional: RTC DS1307 (I2C) | SD Card (SPI)
 *
 *  Mejoras sobre la versión anterior:
 *  - Modo dual: publica JSON compacto a local y JSON completo a HiveMQ
 *  - RTC DS1307: hora sin internet, se sincroniza con NTP cuando hay
 *  - SD: guarda cada lectura en /datos/AAAAMMDD.csv
 *  - Buffer en RAM: 60 lecturas para el broker local
 *  - NTP sin bucle infinito: arranca aunque no haya internet
 *  - Publicación autónoma cada 2 minutos
 *  - Watchdog: reinicia si el firmware se cuelga
 * ================================================================
 */

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
#include <ArduinoJson.h>
#include <esp_task_wdt.h>

// ================================================================
// WIFI / NTP
// ================================================================
const char* ssid               = "JEAR_LESTOMA";
const char* password           = "L3stom4U";
const char* ntpServer          = "pool.ntp.org";
const long  gmtOffset_sec      = -5 * 3600;
const int   daylightOffset_sec = 0;
bool        horaValida         = false;
bool        horaDesdeRTC       = false;

// ================================================================
// BROKER LOCAL — Mosquitto en PC LESTOMA
// ================================================================
const char* mqtt_broker_local = "192.168.0.130";
const int   mqtt_port_local    = 1883;
const char* clientId_local     = "ESP32_E3_Local";

// ================================================================
// BROKER REMOTO — HiveMQ Cloud
// ================================================================
const char* mqtt_broker_remoto = "d9e45a5646094cd5b00b50268cbff192.s1.eu.hivemq.cloud";
const int   mqtt_port_remoto   = 8883;
const char* mqtt_user_remoto   = "Estacion3";
const char* mqtt_pass_remoto   = "Estacion3123@";
const char* clientId_remoto    = "ESP32_Estacion_Remota3";

// ================================================================
// TOPICS
// ================================================================
const char* topic_sub     = "estacion3/solicitud";
const char* topic_pub     = "estacion3/datos";
const char* topic_comando = "estacion3/comando";
const char* topic_estado  = "estacion3/estado";

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
#define PIN_VELETA  34   // ADC1 — funciona con WiFi activo
#define SD_CS        5

// ================================================================
// OBJETOS
// ================================================================
DHT                      dht(DHTPIN, DHTTYPE);
Adafruit_TSL2561_Unified tsl = Adafruit_TSL2561_Unified(TSL2561_ADDR_FLOAT, 12345);
RTC_DS1307               rtc;

bool sdDisponible  = false;
bool rtcDisponible = false;

// ================================================================
// CONSTANTES FÍSICAS ESTACIÓN 3
// ================================================================
const float DIAMETRO_M        = 0.14;
const float PULSOS_POR_VUELTA = 1.0;
const float FACTOR_ANEMOMETRO = 2.5;   // PENDIENTE CALIBRAR
const float AREA_CAPTACION_CM2 = 55.0; // MEDIR
const float ML_POR_BALANCEO    = 2.25; // MEDIR

const float CIRCUNFERENCIA_M = PI * DIAMETRO_M;
const float MM_POR_BALANCEO  = (ML_POR_BALANCEO / AREA_CAPTACION_CM2) * 10.0;

// ================================================================
// ANTIRREBOTE ISR — reed switch necesita más tiempo que Hall
// ================================================================
const unsigned long REBOTE_ANEMO_US = 30000UL; // 30 ms
const unsigned long REBOTE_PLUVI_US = 100000UL; // 100 ms

volatile unsigned long pulsosViento   = 0;
volatile unsigned long ultimoVientoUs = 0;
volatile unsigned long pulsosLluvia   = 0;
volatile unsigned long ultimoLluviaUs = 0;

portMUX_TYPE muxViento = portMUX_INITIALIZER_UNLOCKED;
portMUX_TYPE muxLluvia = portMUX_INITIALIZER_UNLOCKED;

void IRAM_ATTR contarViento() {
  unsigned long ahora = micros();
  if (ahora - ultimoVientoUs >= REBOTE_ANEMO_US) {
    ultimoVientoUs = ahora;
    portENTER_CRITICAL_ISR(&muxViento);
    pulsosViento++;
    portEXIT_CRITICAL_ISR(&muxViento);
  }
}

void IRAM_ATTR contarLluvia() {
  unsigned long ahora = micros();
  if (ahora - ultimoLluviaUs >= REBOTE_PLUVI_US) {
    ultimoLluviaUs = ahora;
    portENTER_CRITICAL_ISR(&muxLluvia);
    pulsosLluvia++;
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
const unsigned long intervaloPublicacion = 120000UL; // 2 min → 300000 para 5 min

// ================================================================
// TSL2561
// ================================================================
const uint16_t TSL_MAX_COUNT_13MS = 5047;
const float    LUX_MAXIMA_SENSOR  = 40000.0;
const int      HORA_INICIO_DIA    = 6;
const int      HORA_FIN_DIA       = 18;

// ================================================================
// BUFFER EN RAM — solo para broker local
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

bool   bufferVacio() { return bufferCount == 0; }

String bufferLeer() {
  String json = buffer[bufferTail];
  bufferTail  = (bufferTail + 1) % BUFFER_MAX;
  bufferCount--;
  return json;
}

void vaciarBuffer() {
  if (bufferVacio()) return;
  Serial.printf("[BUFFER] Reenviando %d lecturas...\n", bufferCount);
  while (!bufferVacio()) {
    String json = bufferLeer();
    bool ok = mqttLocal.publish(topic_pub, json.c_str(), false);
    if (!ok) {
      bufferAgregar(json);
      Serial.println("[BUFFER] Fallo — reintento en proxima reconexion");
      break;
    }
    delay(50);
  }
  if (bufferVacio()) Serial.println("[BUFFER] Vaciado");
}

// ================================================================
// HORA — cascada: NTP → RTC → sin_hora
// ================================================================
String obtenerFechaHora() {
  // 1. Intentar NTP
  struct tm ti;
  if (getLocalTime(&ti)) {
    // Si el RTC está disponible, actualizarlo con NTP
    if (rtcDisponible) {
      rtc.adjust(DateTime(ti.tm_year + 1900, ti.tm_mon + 1, ti.tm_mday,
                          ti.tm_hour, ti.tm_min, ti.tm_sec));
    }
    horaValida   = true;
    horaDesdeRTC = false;
    char buf[30];
    strftime(buf, sizeof(buf), "%Y-%m-%d %H:%M:%S", &ti);
    return String(buf);
  }

  // 2. Intentar RTC
  if (rtcDisponible && rtc.isrunning()) {
    DateTime now = rtc.now();
    horaValida   = true;
    horaDesdeRTC = true;
    char buf[30];
    snprintf(buf, sizeof(buf), "%04d-%02d-%02d %02d:%02d:%02d",
             now.year(), now.month(), now.day(),
             now.hour(), now.minute(), now.second());
    return String(buf);
  }

  // 3. Sin hora
  horaValida = false;
  return "sin_hora";
}

bool esDeDia() {
  struct tm ti;
  if (getLocalTime(&ti)) return (ti.tm_hour >= HORA_INICIO_DIA && ti.tm_hour < HORA_FIN_DIA);
  if (rtcDisponible && rtc.isrunning()) {
    DateTime now = rtc.now();
    return (now.hour() >= HORA_INICIO_DIA && now.hour() < HORA_FIN_DIA);
  }
  return true;
}

// ================================================================
// SD — guardar lectura en CSV
// ================================================================
void guardarEnSD(const String& fecha, float tem, float hum, float rad,
                 float velV, unsigned long vvp, unsigned long vvi,
                 float dirV, float lluv, unsigned long llup) {
  if (!sdDisponible) return;

  // Nombre del archivo: /datos/AAAAMMDD.csv
  String nombreArchivo = "/datos/";
  nombreArchivo += fecha.substring(0, 10); // AAAA-MM-DD
  nombreArchivo.replace("-", "");           // AAAAMMDD
  nombreArchivo += ".csv";

  bool archivoNuevo = !SD.exists(nombreArchivo);
  File f = SD.open(nombreArchivo, FILE_APPEND);
  if (!f) {
    Serial.println("[SD] No se pudo abrir el archivo para escritura");
    return;
  }

  // Encabezado si es archivo nuevo
  if (archivoNuevo) {
    f.println("fecha,tem,hum,rad,velV,vvp,vvi,dirV,lluv,llup");
  }

  // Línea de datos
  f.printf("%s,%.1f,%.1f,%d,%.2f,%lu,%lu,%.0f,%.2f,%lu\n",
           fecha.c_str(), tem, hum, (int)rad, velV, vvp, vvi, dirV, lluv, llup);
  f.close();
  Serial.printf("[SD] Guardado en %s\n", nombreArchivo.c_str());
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
  doc["velV"] = serialized(String(velocidadViento, 2));
  doc["vvp"]  = (unsigned int)pulsosVientoCrudo;
  doc["vvi"]  = (unsigned int)intervaloVientoMs;
  doc["dirV"] = (int)anguloViento;
  doc["lluv"] = serialized(String(lluviaMinuto, 2));
  doc["llup"] = (unsigned int)pulsosLluviaCrudo;
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
  o["valor"] = velocidadViento; o["unidades"] = "m/s";
  o["sensor"] = "id_sensorVelocidad"; o["fecha"] = fecha;

  o = doc.createNestedObject("direccion");
  o["valor"] = direccion; o["grados"] = anguloViento;
  o["unidades"] = "cardinal";
  o["sensor"] = "id_sensorDireccion"; o["fecha"] = fecha;

  o = doc.createNestedObject("lluvia");
  o["valor"] = lluviaMinuto; o["unidades"] = "mm/min";
  o["sensor"] = "id_sensorlluvia"; o["fecha"] = fecha;

  String json;
  serializeJson(doc, json);
  return json;
}

// ================================================================
// PUBLICAR A LOS DOS BROKERS + GUARDAR EN SD
// ================================================================
void publicarDatos() {
  String fecha      = obtenerFechaHora();
  String jsonLocal  = generarJSONLocal(fecha);
  String jsonRemoto = generarJSONRemoto(fecha);

  // Guardar en SD siempre, independiente de MQTT
  guardarEnSD(fecha, temperatura, humedad, radiacion,
              velocidadViento, pulsosVientoCrudo, intervaloVientoMs,
              anguloViento, lluviaMinuto, pulsosLluviaCrudo);

  // Broker local
  if (mqttLocal.connected()) {
    bool ok = mqttLocal.publish(topic_pub, jsonLocal.c_str(), false);
    Serial.printf("[LOCAL]  %s (%d bytes)\n", ok ? "OK" : "FALLO", jsonLocal.length());
    if (!ok) bufferAgregar(jsonLocal);
  } else {
    bufferAgregar(jsonLocal);
    Serial.printf("[LOCAL]  Sin conexion — buffer: %d\n", bufferCount);
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
  else if (topicStr == topic_comando) {
    mensaje.trim(); mensaje.toUpperCase();
    if (mensaje == "RESET") {
      mqttLocal.publish(topic_estado,  "reiniciando", false);
      mqttRemoto.publish(topic_estado, "reiniciando", false);
      delay(1000);
      ESP.restart();
    }
  }
}

// ================================================================
// CONEXIONES
// ================================================================
void conectarLocal() {
  Serial.printf("[LOCAL] Conectando como '%s'...\n", clientId_local);
  if (mqttLocal.connect(clientId_local)) {
    Serial.println("[LOCAL] Conectado");
    mqttLocal.subscribe(topic_sub);
    mqttLocal.subscribe(topic_comando);
    mqttLocal.publish(topic_estado, "en_linea", false);
    vaciarBuffer();
  } else {
    Serial.printf("[LOCAL] Fallo rc=%d\n", mqttLocal.state());
  }
}

void conectarRemoto() {
  Serial.printf("[REMOTO] Conectando como '%s'...\n", clientId_remoto);
  if (mqttRemoto.connect(clientId_remoto, mqtt_user_remoto, mqtt_pass_remoto)) {
    Serial.println("[REMOTO] Conectado");
    mqttRemoto.subscribe(topic_sub);
    mqttRemoto.subscribe(topic_comando);
    mqttRemoto.publish(topic_estado, "en_linea", false);
  } else {
    Serial.printf("[REMOTO] Fallo rc=%d\n", mqttRemoto.state());
  }
}

// ================================================================
// SERIAL
// ================================================================
void imprimirSerial() {
  Serial.println("─────────────────────────────────────────");
  Serial.println("[REMOTA 3] " + obtenerFechaHora()
    + (horaDesdeRTC ? " (RTC)" : horaValida ? " (NTP)" : " (sin hora)"));
  Serial.printf("  tem : %.1f C\n",       temperatura);
  Serial.printf("  hum : %.1f %%\n",      humedad);
  Serial.printf("  rad : %.0f Lux%s\n",   radiacion, luxSaturado ? " [SAT]" : "");
  Serial.printf("  velV: %.2f m/s (%.2f km/h) | %lu pulsos %lu ms\n",
                velocidadViento, velocidadVientoKmh, pulsosVientoCrudo, intervaloVientoMs);
  Serial.printf("  dirV: %s (%.0f grados)\n", direccion.c_str(), anguloViento);
  Serial.printf("  lluv: %.2f mm/min | %lu pulsos\n", lluviaMinuto, pulsosLluviaCrudo);
  Serial.printf("  RTC : %s | SD: %s\n",
                rtcDisponible ? "OK" : "NO",
                sdDisponible  ? "OK" : "NO");
  Serial.printf("  Local : %s | Remoto: %s | Buffer: %d\n",
                mqttLocal.connected()  ? "OK" : "OFF",
                mqttRemoto.connected() ? "OK" : "OFF",
                bufferCount);
  Serial.println("─────────────────────────────────────────");
}

// ================================================================
// SETUP
// ================================================================
void setup() {
  Serial.begin(115200);
  delay(500);
  Serial.println("\n=== ESTACION 3 — MODO DUAL + SD + RTC ===");

  // Watchdog 30 segundos
  // Watchdog — compatible con ESP32 core 3.x
  esp_task_wdt_config_t wdt_config = {
    .timeout_ms    = 30000,
    .idle_core_mask = 0,
    .trigger_panic  = true
  };
  esp_task_wdt_init(&wdt_config);
  esp_task_wdt_add(NULL);

  // Interrupciones reed switch
  pinMode(PIN_VIENTO, INPUT_PULLUP);
  pinMode(PIN_LLUVIA, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(PIN_VIENTO), contarViento, FALLING);
  attachInterrupt(digitalPinToInterrupt(PIN_LLUVIA), contarLluvia, FALLING);

  // Veleta resistiva — ADC1
  analogReadResolution(12);
  analogSetAttenuation(ADC_11db);

  // I2C — SDA=22, SCL=21 (orden invertido de esta estación)
  Wire.begin(21, 22); // SDA=21, SCL=22 — orden correcto estacion 3

  // RTC DS1307
  if (!rtc.begin()) {
    Serial.println("[RTC] No detectado — continuando sin RTC");
  } else {
    rtcDisponible = true;
    if (!rtc.isrunning()) {
      Serial.println("[RTC] Detenido — se ajustara con NTP al conectar");
    }
    DateTime now = rtc.now();
    Serial.printf("[RTC] OK — %04d-%02d-%02d %02d:%02d:%02d\n",
                  now.year(), now.month(), now.day(),
                  now.hour(), now.minute(), now.second());
  }

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
  if (!SD.begin(SD_CS)) {
    Serial.println("[SD] No disponible — datos no se guardaran en tarjeta");
  } else {
    sdDisponible = true;
    // Crear carpeta de datos si no existe
    if (!SD.exists("/datos")) SD.mkdir("/datos");
    Serial.printf("[SD] OK — %.0f MB disponibles\n",
                  (float)SD.cardSize() / (1024 * 1024));
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

  // NTP — maximo 10 segundos, no bloquea el arranque
  configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);
  struct tm timeinfo;
  unsigned long tNtp = millis();
  while (!getLocalTime(&timeinfo) && millis() - tNtp < 10000) {
    delay(500);
    esp_task_wdt_reset();
  }
  if (getLocalTime(&timeinfo)) {
    horaValida = true;
    Serial.println("[NTP] Sincronizado");
    // Actualizar RTC con NTP
    if (rtcDisponible) {
      rtc.adjust(DateTime(timeinfo.tm_year + 1900, timeinfo.tm_mon + 1,
                          timeinfo.tm_mday, timeinfo.tm_hour,
                          timeinfo.tm_min, timeinfo.tm_sec));
      Serial.println("[RTC] Actualizado con NTP");
    }
  } else {
    Serial.println("[NTP] Sin sincronizar — usando RTC si disponible");
  }

  // MQTT local
  mqttLocal.setServer(mqtt_broker_local, mqtt_port_local);
  mqttLocal.setCallback(mqttCallback);
  mqttLocal.setBufferSize(512);

  // MQTT remoto TLS
  espClientRemoto.setInsecure();
  mqttRemoto.setServer(mqtt_broker_remoto, mqtt_port_remoto);
  mqttRemoto.setCallback(mqttCallback);
  mqttRemoto.setBufferSize(1024);

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
    if (ahora - lastReconnectRemoto >= intervaloReconnect) {
      lastReconnectRemoto = ahora;
      conectarRemoto();
    }
  }
  mqttRemoto.loop();

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
