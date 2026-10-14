/*
 * ================================================================
 *  ESTACIÓN METEOROLÓGICA REMOTA — ESP32  |  ESTACIÓN 1
 *  Modo DUAL: publica simultáneamente a Mosquitto local y HiveMQ
 *
 *  Broker local  (192.168.20.10:1883) → grupo local
 *  Broker remoto (HiveMQ :8883 TLS)   → grupo web
 *
 *  JSON local  : compacto, campos cortos, sin unidades, ~120 bytes
 *  JSON remoto : formato original del grupo web, con unidades
 *
 *  Sensores: DHT22 | TSL2561 | Anemómetro Hall | Pluviómetro Hall
 *            Veleta 4 reed switches
 *  v5: Sirena de alarma en GPIO 15 — topic estacion1/alarma
 *      Comandos extra en estacion1/comando: ALARMA_ON | ALARMA_OFF
 *  v6: Pluviómetro calibrado (8 ml/vuelco, embudo Ø10.6 cm ≈ 0.91 mm/pulso)
 * ================================================================
 */

#include <WiFi.h>
#include <WiFiClient.h>
#include <WiFiClientSecure.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <Adafruit_TSL2561_U.h>
#include <DHT.h>
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

// ================================================================
// BROKER LOCAL — Mosquitto
// ================================================================
const char* mqtt_broker_local  = "192.168.0.130";
const int   mqtt_port_local    = 1883;
const char* clientId_local     = "ESP32_E1_Local";

// ================================================================
// BROKER REMOTO — HiveMQ Cloud
// ================================================================
const char* mqtt_broker_remoto = "d9e45a5646094cd5b00b50268cbff192.s1.eu.hivemq.cloud";
const int   mqtt_port_remoto   = 8883;
const char* mqtt_user_remoto   = "Estacion1";
const char* mqtt_pass_remoto   = "Estacion1123@";
const char* clientId_remoto    = "ESP32_Estacion_Remota11";

// ================================================================
// TOPICS — mismos en ambos brokers
// ================================================================
const char* topic_sub     = "estacion1/solicitud";
const char* topic_pub     = "estacion1/datos";
const char* topic_comando = "estacion1/comando";
const char* topic_estado  = "estacion1/estado";
const char* topic_alarma  = "estacion1/alarma";

// Número de estación — se incluye en el JSON local
const int ESTACION_NUM = 1;

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
#define PIN_VIENTO    27
#define PIN_LLUVIA    13
#define DHTPIN         4
#define DHTTYPE        DHT22
#define PIN_SIRENA    15   // HIGH = sirena encendida
#define PIN_NORTE     25
#define PIN_SUR       32
#define PIN_ORIENTE   26
#define PIN_OCCIDENTE 33

// ================================================================
// OBJETOS
// ================================================================
DHT                      dht(DHTPIN, DHTTYPE);
Adafruit_TSL2561_Unified tsl = Adafruit_TSL2561_Unified(TSL2561_ADDR_FLOAT, 12345);

// ================================================================
// ANTIRREBOTE ISR
// Pendiente calibrar experimentalmente por unidad física.
// ================================================================
const unsigned long DEBOUNCE_VIENTO_US = 15000UL; // 15 ms
const unsigned long DEBOUNCE_LLUVIA_US = 50000UL; // 50 ms

volatile unsigned long pulsosViento      = 0;
volatile unsigned long pulsosLluvia      = 0;
volatile unsigned long ultimoPulsoViento = 0;
volatile unsigned long ultimoPulsoLluvia = 0;

void IRAM_ATTR contarViento() {
  unsigned long ahora = micros();
  if (ahora - ultimoPulsoViento >= DEBOUNCE_VIENTO_US) {
    pulsosViento++;
    ultimoPulsoViento = ahora;
  }
}

void IRAM_ATTR contarLluvia() {
  unsigned long ahora = micros();
  if (ahora - ultimoPulsoLluvia >= DEBOUNCE_LLUVIA_US) {
    pulsosLluvia++;
    ultimoPulsoLluvia = ahora;
  }
}

// ================================================================
// VARIABLES DE SENSORES
// ================================================================
float  temperatura      = 0;
float  humedad          = 0;
float  lux              = 0;
float  radiacion        = 0;
float  velocidadViento  = 0;
float  lluviaMinuto     = 0;
float  intensidadLluvia = 0;
String direccionTexto   = "SIN";
int    direccionGrados  = -1;

// Conteos crudos para recalibración posterior
unsigned long pulsosVientoCrudo = 0;
unsigned long pulsosLluviaCrudo = 0;
unsigned long intervaloVientoMs = 0;

// Diagnóstico DHT22
unsigned long fallosTemp = 0;
unsigned long fallosHum  = 0;

// ================================================================
// CONSTANTES DE CALIBRACIÓN
// Pluviómetro de balancín calibrado volumétricamente:
//   diámetro embudo 10.6 cm → área = π·(5.3 cm)² ≈ 88.25 cm²
//   8 ml por vuelco → 8 cm³ / 88.25 cm² ≈ 0.0906 cm ≈ 0.91 mm/pulso
// Los campos crudos (llup, vvp, vvi) permiten recalcular el histórico.
// ================================================================
const float DIAMETRO_EMBUDO_CM = 10.6;
const float ML_POR_BALANCEO    = 8.0;
const float AREA_CAPTACION_CM2 = PI * (DIAMETRO_EMBUDO_CM / 2.0) * (DIAMETRO_EMBUDO_CM / 2.0);
const float mm_por_pulso       = (ML_POR_BALANCEO / AREA_CAPTACION_CM2) * 10.0; // ≈ 0.91 mm

// ================================================================
// TIMERS
// ================================================================
unsigned long ultimoMinuto        = 0;
unsigned long lastRead            = 0;
unsigned long lastSerial          = 0;
unsigned long lastReconnectLocal  = 0;
unsigned long lastReconnectRemoto = 0;
unsigned long lastPublicacion     = 0;
unsigned long lastVientoMs        = 0;

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
// BUFFER EN RAM — solo para broker local
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

bool   bufferVacio() { return bufferCount == 0; }

String bufferLeer() {
  String json = buffer[bufferTail];
  bufferTail  = (bufferTail + 1) % BUFFER_MAX;
  bufferCount--;
  return json;
}

void vaciarBuffer() {
  if (bufferVacio()) return;
  Serial.printf("[BUFFER] Reenviando %d lecturas pendientes...\n", bufferCount);
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
  if (bufferVacio()) Serial.println("[BUFFER] Vaciado correctamente");
}

// ================================================================
// UTILIDADES
// ================================================================
String obtenerFechaHora() {
  struct tm ti;
  if (!getLocalTime(&ti)) return "sin_hora";
  char buf[30];
  strftime(buf, sizeof(buf), "%Y-%m-%d %H:%M:%S", &ti);
  return String(buf);
}

bool esDeDia() {
  struct tm ti;
  if (!getLocalTime(&ti)) return true;
  return (ti.tm_hour >= HORA_INICIO_DIA && ti.tm_hour < HORA_FIN_DIA);
}

// ================================================================
// SENSORES
// ================================================================
void calcularLluviaMinuto() {
  if (millis() - ultimoMinuto < intervaloLluvia) return;
  ultimoMinuto = millis();
  portDISABLE_INTERRUPTS();
  unsigned long p = pulsosLluvia;
  pulsosLluvia = 0;
  portENABLE_INTERRUPTS();
  pulsosLluviaCrudo = p;
  lluviaMinuto      = p * mm_por_pulso;
  intensidadLluvia  = lluviaMinuto * 60.0;
}

String leerDireccionViento() {
  bool norte     = digitalRead(PIN_NORTE)     == LOW;
  bool sur       = digitalRead(PIN_SUR)       == LOW;
  bool oriente   = digitalRead(PIN_ORIENTE)   == LOW;
  bool occidente = digitalRead(PIN_OCCIDENTE) == LOW;

  if (norte && !sur && !oriente && !occidente)  { direccionGrados = 0;   return "N";  }
  if (sur && !norte && !oriente && !occidente)  { direccionGrados = 180; return "S";  }
  if (oriente && !norte && !sur && !occidente)  { direccionGrados = 90;  return "E";  }
  if (occidente && !norte && !sur && !oriente)  { direccionGrados = 270; return "O";  }
  if (norte && oriente)                         { direccionGrados = 45;  return "NE"; }
  if (norte && occidente)                       { direccionGrados = 315; return "NO"; }
  if (sur && oriente)                           { direccionGrados = 135; return "SE"; }
  if (sur && occidente)                         { direccionGrados = 225; return "SO"; }
  direccionGrados = -1;
  return "SIN";
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
    lux = esDeDia() ? LUX_MAXIMA_SENSOR : 0;
  } else {
    uint32_t luxCalc = tsl.calculateLux(broadband, ir);
    lux = (luxCalc == 65536) ? 0 : (float)luxCalc;
  }
  radiacion = lux;

  // Anemómetro — divide por tiempo real transcurrido
  unsigned long ahora = millis();
  intervaloVientoMs = ahora - lastVientoMs;
  lastVientoMs = ahora;
  portDISABLE_INTERRUPTS();
  unsigned long pv = pulsosViento;
  pulsosViento = 0;
  portENABLE_INTERRUPTS();
  pulsosVientoCrudo = pv;
  if (intervaloVientoMs > 0) {
    float seg = intervaloVientoMs / 1000.0;
    velocidadViento = (pv / seg) * 0.56548 * 3.1;
  } else {
    velocidadViento = 0;
  }

  // Veleta
  direccionTexto = leerDireccionViento();
}

// ================================================================
// JSON LOCAL — compacto, sin unidades, ~120 bytes
// Campos: e=estacion, t=timestamp, tem, hum, rad, velV, dirV, lluv
// Campos crudos: vvp=pulsos viento, vvi=intervalo ms, llup=pulsos lluvia
// ================================================================
String generarJSONLocal() {
  StaticJsonDocument<256> doc;
  doc["e"]    = ESTACION_NUM;
  doc["t"]    = obtenerFechaHora();
  doc["tem"]  = serialized(String(temperatura, 1));
  doc["hum"]  = serialized(String(humedad, 1));
  doc["rad"]  = (int)radiacion;
  doc["velV"] = serialized(String(velocidadViento, 2));
  doc["vvp"]  = (unsigned int)pulsosVientoCrudo;
  doc["vvi"]  = (unsigned int)intervaloVientoMs;
  doc["dirV"] = direccionGrados;
  doc["lluv"] = serialized(String(lluviaMinuto, 2));
  doc["llup"] = (unsigned int)pulsosLluviaCrudo;
  String json;
  serializeJson(doc, json);
  return json;
}

// ================================================================
// JSON REMOTO — formato original del grupo web, con unidades
// ================================================================
String generarJSONRemoto() {
  String fecha = obtenerFechaHora();
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
  o["sensor"] = "6319081a-9f85-4e87-8759-6895d08d935a"; o["fecha"] = fecha;

  o = doc.createNestedObject("velocidadViento");
  o["valor"] = velocidadViento; o["unidades"] = "m/s";
  o["sensor"] = "id_sensorVelocidad"; o["fecha"] = fecha;

  o = doc.createNestedObject("direccion");
  o["valor"] = direccionTexto; o["grados"] = direccionGrados;
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
// PUBLICAR A LOS DOS BROKERS
// ================================================================
void publicarDatos() {
  String jsonLocal  = generarJSONLocal();
  String jsonRemoto = generarJSONRemoto();

  // Broker local — con buffer si falla
  if (mqttLocal.connected()) {
    bool ok = mqttLocal.publish(topic_pub, jsonLocal.c_str(), false);
    Serial.printf("[LOCAL]  %s (%d bytes)\n",
                  ok ? "OK" : "FALLO", jsonLocal.length());
    if (!ok) bufferAgregar(jsonLocal);
  } else {
    bufferAgregar(jsonLocal);
    Serial.printf("[LOCAL]  Sin conexion — buffer: %d\n", bufferCount);
  }

  // Broker remoto — sin buffer
  if (mqttRemoto.connected()) {
    bool ok = mqttRemoto.publish(topic_pub, jsonRemoto.c_str(), false);
    Serial.printf("[REMOTO] %s (%d bytes)\n",
                  ok ? "OK" : "FALLO", jsonRemoto.length());
  } else {
    Serial.println("[REMOTO] Sin conexion — lectura no enviada al grupo web");
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
    Serial.printf("[MQTT] Comando: '%s'\n", mensaje.c_str());
    if (mensaje == "RESET") {
      digitalWrite(PIN_SIRENA, LOW);
      Serial.println("[SISTEMA] Reiniciando en 1 s...");
      mqttLocal.publish(topic_estado,  "reiniciando", false);
      mqttRemoto.publish(topic_estado, "reiniciando", false);
      delay(1000);
      ESP.restart();
    }
    else if (mensaje == "ALARMA_ON")  activarAlarma("ALTA", ALARMA_DURACION_DEF_S);
    else if (mensaje == "ALARMA_OFF") apagarAlarma();
  }
}

// ================================================================
// CONEXIÓN LOCAL
// ================================================================
void conectarLocal() {
  Serial.printf("[LOCAL] Conectando como '%s'...\n", clientId_local);
  if (mqttLocal.connect(clientId_local)) {
    Serial.println("[LOCAL] Conectado");
    mqttLocal.subscribe(topic_sub);
    mqttLocal.subscribe(topic_comando);
    mqttLocal.subscribe(topic_alarma);
    mqttLocal.publish(topic_estado, "en_linea", false);
    vaciarBuffer();
  } else {
    Serial.printf("[LOCAL] Fallo rc=%d\n", mqttLocal.state());
  }
}

// ================================================================
// CONEXIÓN REMOTA
// ================================================================
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
  Serial.println("[REMOTA 1] " + obtenerFechaHora()
    + (horaValida ? "" : " (sin hora)"));
  Serial.printf("  tem : %.1f C\n",       temperatura);
  Serial.printf("  hum : %.1f %%\n",      humedad);
  Serial.printf("  rad : %.0f Lux\n",     radiacion);
  Serial.printf("  velV: %.2f m/s (%lu pulsos, %lu ms)\n",
                velocidadViento, pulsosVientoCrudo, intervaloVientoMs);
  Serial.printf("  dirV: %s (%d grados)\n",
                direccionTexto.c_str(), direccionGrados);
  Serial.printf("  lluv: %.2f mm/min (%lu pulsos)\n",
                lluviaMinuto, pulsosLluviaCrudo);
  Serial.printf("  DHT fallos: temp=%lu hum=%lu\n", fallosTemp, fallosHum);
  Serial.printf("  Local : %s | Remoto: %s | Buffer: %d\n",
                mqttLocal.connected()  ? "OK" : "OFF",
                mqttRemoto.connected() ? "OK" : "OFF",
                bufferCount);
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
  Serial.println("\n=== ESTACION 1 — MODO DUAL ===");

  // Watchdog 30 segundos
  // Watchdog — compatible con ESP32 core 3.x
  esp_task_wdt_config_t wdt_config = {
    .timeout_ms    = 30000,
    .idle_core_mask = 0,
    .trigger_panic  = true
  };
  esp_task_wdt_init(&wdt_config);
  esp_task_wdt_add(NULL);

  // Interrupciones
  pinMode(PIN_VIENTO, INPUT_PULLUP);
  pinMode(PIN_LLUVIA, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(PIN_VIENTO), contarViento, FALLING);
  attachInterrupt(digitalPinToInterrupt(PIN_LLUVIA), contarLluvia, FALLING);

  // Veleta
  pinMode(PIN_NORTE,     INPUT_PULLUP);
  pinMode(PIN_SUR,       INPUT_PULLUP);
  pinMode(PIN_ORIENTE,   INPUT_PULLUP);
  pinMode(PIN_OCCIDENTE, INPUT_PULLUP);

  // Sensores
  dht.begin();
  Wire.begin(22, 21);
  if (!tsl.begin()) {
    Serial.println("[TSL] No detectado");
  } else {
    tsl.enableAutoRange(false);
    tsl.setGain(TSL2561_GAIN_1X);
    tsl.setIntegrationTime(TSL2561_INTEGRATIONTIME_13MS);
    Serial.println("[TSL] OK");
  }

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
  horaValida = getLocalTime(&timeinfo);
  Serial.println(horaValida ? "[NTP] Sincronizado" : "[NTP] Sin hora");

  // MQTT local
  mqttLocal.setServer(mqtt_broker_local, mqtt_port_local);
  mqttLocal.setCallback(mqttCallback);
  mqttLocal.setBufferSize(512); // JSON local es pequeno

  // MQTT remoto con TLS
  espClientRemoto.setInsecure();
  mqttRemoto.setServer(mqtt_broker_remoto, mqtt_port_remoto);
  mqttRemoto.setCallback(mqttCallback);
  mqttRemoto.setBufferSize(1024);

  // Lectura inicial
  actualizarSensores();
  lastVientoMs = millis();

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

  // Mantener conexión remota — independiente
  if (!mqttRemoto.connected()) {
    if (ahora - lastReconnectRemoto >= intervaloReconnect) {
      lastReconnectRemoto = ahora;
      conectarRemoto();
    }
  }
  mqttRemoto.loop();

  // Sirena (patrón no bloqueante)
  actualizarSirena();

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
