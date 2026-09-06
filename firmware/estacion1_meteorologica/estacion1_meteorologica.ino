/*
 * ================================================================
 *  ESTACIÓN METEOROLÓGICA REMOTA — ESP32  |  ESTACIÓN 1
 *  - MQTT sobre TLS (HiveMQ Cloud)
 *  - Suscribe: estacion1/solicitud  → al recibir cualquier msg
 *    publica:  estacion1/datos      → JSON con todos los sensores
 *  - Suscribe: estacion1/comando    → comandos de control (RESET)
 *    publica:  estacion1/estado     → "en_linea" / "reiniciando"
 *  Sensores: DHT22 | TSL2561 | Anemómetro | Pluviómetro | Veleta 4 reed
 * ================================================================
 */

#include <WiFi.h>
#include <WiFiClientSecure.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <Adafruit_TSL2561_U.h>
#include <DHT.h>
#include <time.h>
#include <ArduinoJson.h>

// ── WiFi / NTP ────────────────────────────────────────────────
const char* ssid               = "U-SIGLOXXI";
const char* password           = "UdeCsigloXXI";
const char* ntpServer          = "pool.ntp.org";
const long  gmtOffset_sec      = -5 * 3600;
const int   daylightOffset_sec = 0;

// ── MQTT ──────────────────────────────────────────────────────
const char* mqtt_broker   = "d9e45a5646094cd5b00b50268cbff192.s1.eu.hivemq.cloud";
const int   mqtt_port     = 8883;
const char* mqtt_user     = "Estacion1";
const char* mqtt_pass     = "Estacion1123@";
const char* topic_sub     = "estacion1/solicitud";
const char* topic_pub     = "estacion1/datos";
const char* topic_comando = "estacion1/comando";
const char* topic_estado  = "estacion1/estado";
const char* mqtt_clientId = "ESP32_Estacion_Remota12";

// ── Pines ─────────────────────────────────────────────────────
#define PIN_VIENTO  27
#define PIN_LLUVIA  13
#define DHTPIN       4
#define DHTTYPE      DHT22

// ── Pines dirección del viento (veleta 4 reed switches) ───────
#define PIN_NORTE     25
#define PIN_SUR       32
#define PIN_ORIENTE   26
#define PIN_OCCIDENTE 33

// ── Objetos ───────────────────────────────────────────────────
WiFiClientSecure         espClient;
PubSubClient             mqttClient(espClient);
DHT                      dht(DHTPIN, DHTTYPE);
Adafruit_TSL2561_Unified tsl = Adafruit_TSL2561_Unified(TSL2561_ADDR_FLOAT, 12345);

// ── Variables de sensores ─────────────────────────────────────
volatile unsigned long pulsosViento = 0;
volatile unsigned long pulsosLluvia = 0;

float  temperatura      = 0;
float  humedad          = 0;
float  lux              = 0;
float  radiacion        = 0;
float  velocidadViento  = 0;
float  lluviaMinuto     = 0;
float  intensidadLluvia = 0;
String direccion        = "SIN";
int    direccionGrados  = -1;   // -1 = sin dato / calma

// ── Diagnóstico DHT22 ──────────────────────────────────────────
unsigned long fallosTemp = 0;
unsigned long fallosHum  = 0;

// ── Timers ────────────────────────────────────────────────────
unsigned long ultimoMinuto   = 0;
unsigned long lastRead       = 0;
unsigned long lastSerial     = 0;
unsigned long lastReconnect  = 0;

const unsigned long intervaloLluvia    = 60000UL;
const unsigned long intervaloRead      =  2500UL; // DHT22 necesita >= 2 s entre lecturas
const unsigned long intervaloSerial    =  1000UL;
const unsigned long intervaloReconnect =  5000UL;
const float         mm_por_pulso       = 8.0;

// ── TSL2561 — saturacion y horario diurno ──────────────────────
const uint16_t TSL_MAX_COUNT_13MS = 5047;
const float    LUX_MAXIMA_SENSOR  = 40000.0;
const int      HORA_INICIO_DIA    = 6;
const int      HORA_FIN_DIA       = 18;

// ── ISR ───────────────────────────────────────────────────────
void IRAM_ATTR contarViento() { pulsosViento++; }
void IRAM_ATTR contarLluvia() { pulsosLluvia++; }

// =================================================================
//  UTILIDADES
// =================================================================
String obtenerFechaHora() {
  struct tm ti;
  if (!getLocalTime(&ti)) return "error_fecha";
  char buf[30];
  strftime(buf, sizeof(buf), "%Y-%m-%d %H:%M:%S", &ti);
  return String(buf);
}

bool esDeDia() {
  struct tm ti;
  if (!getLocalTime(&ti)) return true;
  return (ti.tm_hour >= HORA_INICIO_DIA && ti.tm_hour < HORA_FIN_DIA);
}

// =================================================================
//  SENSORES
// =================================================================
void calcularLluviaMinuto() {
  if (millis() - ultimoMinuto < intervaloLluvia) return;
  ultimoMinuto = millis();
  portDISABLE_INTERRUPTS();
  unsigned long p = pulsosLluvia; pulsosLluvia = 0;
  portENABLE_INTERRUPTS();
  lluviaMinuto     = p * mm_por_pulso;
  intensidadLluvia = lluviaMinuto * 60.0;
}

// Lee las 4 entradas de la veleta. Devuelve la direccion como texto y
// deja los grados en 'direccionGrados' (-1 si no hay direccion valida).
String leerDireccionViento() {
  bool norte     = digitalRead(PIN_NORTE)     == LOW;
  bool sur       = digitalRead(PIN_SUR)       == LOW;
  bool oriente   = digitalRead(PIN_ORIENTE)   == LOW;
  bool occidente = digitalRead(PIN_OCCIDENTE) == LOW;

  if (norte && !sur && !oriente && !occidente)  { direccionGrados = 0;   return "N";  }
  if (sur && !norte && !oriente && !occidente)  { direccionGrados = 180; return "S";  }
  if (oriente && !norte && !sur && !occidente)  { direccionGrados = 90;  return "E";  }
  if (occidente && !norte && !sur && !oriente)  { direccionGrados = 270; return "O";  }
  if (norte && oriente)   { direccionGrados = 45;  return "NE"; }
  if (norte && occidente) { direccionGrados = 315; return "NO"; }
  if (sur && oriente)     { direccionGrados = 135; return "SE"; }
  if (sur && occidente)   { direccionGrados = 225; return "SO"; }

  direccionGrados = -1;
  return "SIN";
}

void actualizarSensores() {
  float t = dht.readTemperature();
  float h = dht.readHumidity();

  if (!isnan(t)) {
    temperatura = t;
  } else {
    fallosTemp++;
    Serial.printf("[DHT] Fallo temperatura (NaN) — acumulados: %lu\n", fallosTemp);
  }

  if (!isnan(h)) {
    humedad = h;
  } else {
    fallosHum++;
    Serial.printf("[DHT] Fallo humedad (NaN) — acumulados: %lu\n", fallosHum);
  }

  // Lectura cruda del TSL2561: revisamos saturacion sobre las cuentas ADC,
  // mas confiable que fijarnos solo en el valor de lux ya calculado.
  uint16_t broadband, ir;
  tsl.getLuminosity(&broadband, &ir);
  bool saturado = (broadband >= TSL_MAX_COUNT_13MS) || (ir >= TSL_MAX_COUNT_13MS);

  if (saturado) {
    if (esDeDia()) {
      lux = LUX_MAXIMA_SENSOR;
      Serial.println("[TSL] Sensor saturado en horario diurno -> reportando lux maxima");
    } else {
      lux = 0;
      Serial.println("[TSL] Sensor saturado fuera de horario diurno -> reportando 0");
    }
  } else {
    uint32_t luxCalculado = tsl.calculateLux(broadband, ir);
    lux = (luxCalculado == 65536) ? 0 : (float)luxCalculado;
  }
  radiacion = lux;

  portDISABLE_INTERRUPTS();
  unsigned long pv = pulsosViento; pulsosViento = 0;
  portENABLE_INTERRUPTS();
  velocidadViento = pv * 0.56548 * 3.1;

  direccion = leerDireccionViento();
}

// =================================================================
//  JSON
// =================================================================
String generarJSON() {
  String fecha = obtenerFechaHora();
  StaticJsonDocument<768> doc;
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
  o["valor"] = direccion; o["grados"] = direccionGrados; o["unidades"] = "cardinal";
  o["sensor"] = "id_sensorDireccion"; o["fecha"] = fecha;

  o = doc.createNestedObject("lluvia");
  o["valor"] = lluviaMinuto; o["unidades"] = "mm/min";
  o["sensor"] = "id_sensorlluvia"; o["fecha"] = fecha;

  String json;
  serializeJson(doc, json);
  return json;
}

// =================================================================
//  COMANDOS REMOTOS
// =================================================================
void ejecutarReinicio() {
  Serial.println("[SISTEMA] Comando RESET recibido — reiniciando en 1 s...");
  mqttClient.publish(topic_estado, "reiniciando", false);
  delay(1000); // margen para que el ack de MQTT/TLS alcance a salir
  ESP.restart();
}

// =================================================================
//  MQTT — CALLBACK
// =================================================================
void mqttCallback(char* topic, byte* payload, unsigned int length) {
  String mensaje;
  mensaje.reserve(length);
  for (unsigned int i = 0; i < length; i++) mensaje += (char)payload[i];

  String topicStr = String(topic);

  if (topicStr == topic_sub) {
    Serial.printf("[MQTT] Solicitud recibida en '%s'\n", topic);

    actualizarSensores();
    String json = generarJSON();

    bool ok = mqttClient.publish(topic_pub, json.c_str(), false);
    Serial.printf("[MQTT] Publicado en '%s': %s\n", topic_pub, ok ? "OK" : "FALLO");
  }
  else if (topicStr == topic_comando) {
    mensaje.trim();
    mensaje.toUpperCase();
    Serial.printf("[MQTT] Comando recibido en '%s': '%s'\n", topic, mensaje.c_str());

    if (mensaje == "RESET") {
      ejecutarReinicio();
    } else {
      Serial.printf("[MQTT] Comando no reconocido: '%s'\n", mensaje.c_str());
    }
  }
}

// =================================================================
//  MQTT — CONEXIÓN / RECONEXIÓN
// =================================================================
void mqttConectar() {
  Serial.printf("[MQTT] Conectando como '%s'...\n", mqtt_clientId);
  if (mqttClient.connect(mqtt_clientId, mqtt_user, mqtt_pass)) {
    Serial.println("[MQTT] Conectado");
    mqttClient.subscribe(topic_sub);
    mqttClient.subscribe(topic_comando);
    Serial.printf("[MQTT] Suscrito a '%s' y '%s'\n", topic_sub, topic_comando);
    mqttClient.publish(topic_estado, "en_linea", false);
  } else {
    Serial.printf("[MQTT] Fallo rc=%d — reintento en %lu s\n",
                  mqttClient.state(), intervaloReconnect / 1000);
  }
}

// =================================================================
//  SERIAL PRINT
// =================================================================
void imprimirSerial() {
  Serial.println("─────────────────────────────────────────");
  Serial.println("[REMOTA 2] " + obtenerFechaHora());
  Serial.printf("  Temperatura   : %.2f °C\n",     temperatura);
  Serial.printf("  Humedad       : %.2f %%\n",     humedad);
  Serial.printf("  Lux           : %.2f lx\n",     lux);
  Serial.printf("  Radiacion     : %.4f W/m2\n",   radiacion);
  Serial.printf("  Viento        : %.2f m/s\n",    velocidadViento);
  Serial.printf("  Direccion     : %s (%d)\n",     direccion.c_str(), direccionGrados);
  Serial.printf("  Lluvia/min    : %.2f mm/min\n", lluviaMinuto);
  Serial.printf("  Intensidad    : %.2f mm/h\n",   intensidadLluvia);
  Serial.printf("  Fallos DHT    : temp=%lu hum=%lu\n", fallosTemp, fallosHum);
  Serial.printf("  MQTT          : %s\n", mqttClient.connected() ? "conectado" : "desconectado");
  Serial.println("─────────────────────────────────────────");
}

// =================================================================
//  SETUP
// =================================================================
void setup() {
  Serial.begin(115200);
  delay(500);
  Serial.println("\n=== ESTACION METEOROLOGICA REMOTA 1 ===");

  // Interrupciones
  pinMode(PIN_VIENTO, INPUT_PULLUP);
  pinMode(PIN_LLUVIA, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(PIN_VIENTO), contarViento, FALLING);
  attachInterrupt(digitalPinToInterrupt(PIN_LLUVIA), contarLluvia, FALLING);

  // Veleta (dirección del viento)
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
    // Sensibilidad MINIMA: autoRange OFF, ganancia 1x, integracion 13ms.
    tsl.enableAutoRange(false);
    tsl.setGain(TSL2561_GAIN_1X);
    tsl.setIntegrationTime(TSL2561_INTEGRATIONTIME_13MS);
    Serial.println("[TSL] OK - configurado en baja sensibilidad (ganancia 1x, 13ms)");
  }

  // WiFi
  WiFi.begin(ssid, password);
  Serial.print("[WiFi] Conectando");
  unsigned long t0 = millis();
  while (WiFi.status() != WL_CONNECTED && millis() - t0 < 15000) {
    delay(300); Serial.print(".");
  }
  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\n[WiFi] Conectado — IP: " + WiFi.localIP().toString());
  } else {
    Serial.println("\n[WiFi] Error de conexion");
  }

  // NTP
  configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);
  struct tm timeinfo;
  while (!getLocalTime(&timeinfo)) {
    Serial.println("Esperando NTP...");
    delay(1000);
  }
  Serial.println("Hora sincronizada");

  // TLS + MQTT
  espClient.setInsecure();
  mqttClient.setServer(mqtt_broker, mqtt_port);
  mqttClient.setCallback(mqttCallback);
  mqttClient.setBufferSize(1024);

  // Lectura inicial
  actualizarSensores();

  // Conectar MQTT
  mqttConectar();

  // Timers
  ultimoMinuto  = millis();
  lastRead      = millis();
  lastSerial    = millis();
  lastReconnect = millis();
}

// =================================================================
//  LOOP
// =================================================================
void loop() {
  unsigned long ahora = millis();

  // Mantener conexión MQTT
  if (!mqttClient.connected()) {
    if (ahora - lastReconnect >= intervaloReconnect) {
      lastReconnect = ahora;
      mqttConectar();
    }
  }
  mqttClient.loop();

  // Leer sensores
  if (ahora - lastRead >= intervaloRead) {
    lastRead = ahora;
    actualizarSensores();
  }

  // Lluvia acumulada cada 1 min
  calcularLluviaMinuto();

  // Serial cada 1 s
  if (ahora - lastSerial >= intervaloSerial) {
    lastSerial = ahora;
    imprimirSerial();
  }
}