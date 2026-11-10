/*
 * ================================================================
 *  ESTACIÓN 4 — v1 — Solo DHT11 (temperatura y humedad)
 *  UdeC Facatativá — LESTOMA
 * ================================================================
 *  Publica en Mosquitto local el mismo JSON compacto que las estaciones 1-3,
 *  pero solo con las variables que tiene esta placa:
 *      {"e":4,"t":"2026-11-10 08:39:39","tem":16.7,"hum":56.0}
 *  El worker solo guarda las variables que vienen en el JSON, así que no aparecen
 *  radiación, viento ni lluvia en cero para esta estación.
 *
 *  Topics:
 *      estacion4/datos      → lectura (promedio de la ventana de publicación)
 *      estacion4/solicitud  → cualquier mensaje pide una lectura inmediata
 *      estacion4/comando    → RESET (reinicio remoto desde el dashboard)
 *      estacion4/estado     → en_linea | reiniciando
 *
 *  Hora: NTP. Si no hay internet, se publica sin "t" y el worker usa la hora de
 *  recepción (no se pierden lecturas por falta de hora).
 *
 *  Antes de compilar: copiar secrets.example.h como secrets.h y poner el WiFi.
 *  Librerías: DHT sensor library (Adafruit), PubSubClient, ArduinoJson 6.
 * ================================================================
 */

#include "secrets.h"   // credenciales (copiar secrets.example.h)
#include <WiFi.h>
#include <PubSubClient.h>
#include <DHT.h>
#include <ArduinoJson.h>
#include <time.h>

// ================================================================
// SENSOR
// ================================================================
#define DHTPIN  4
#define DHTTYPE DHT11
DHT dht(DHTPIN, DHTTYPE);

// ================================================================
// HORA — NTP (Colombia UTC-5)
// ================================================================
const char* ntpServer          = "pool.ntp.org";
const long  gmtOffset_sec      = -5 * 3600;
const int   daylightOffset_sec = 0;

// ================================================================
// BROKER LOCAL — Mosquitto en PC LESTOMA
// ================================================================
const char* mqtt_broker_local = "192.168.0.130";
const int   mqtt_port_local   = 1883;
const char* clientId_local    = "ESP32_E4_Local";   // único por placa

// ================================================================
// TOPICS
// ================================================================
const char* topic_pub     = "estacion4/datos";
const char* topic_sub     = "estacion4/solicitud";
const char* topic_comando = "estacion4/comando";
const char* topic_estado  = "estacion4/estado";

const int ESTACION_NUM = 4;

// ================================================================
// TIEMPOS (solo millis, nunca delay en el loop)
// ================================================================
const unsigned long intervaloRead        =   2500UL;  // DHT11: mínimo 1 s entre lecturas
const unsigned long intervaloPublicacion = 120000UL;  // 2 min, igual que las estaciones 1-3
const unsigned long intervaloReconnect   =   5000UL;

unsigned long lastRead        = 0;
unsigned long lastPublicacion = 0;
unsigned long lastReconnect   = 0;

// ================================================================
// ESTADO
// ================================================================
WiFiClient   espClient;
PubSubClient mqttLocal(espClient);

// Acumulado de la ventana de publicación (se publica el promedio)
float  sumaTemp = 0, sumaHum = 0;
int    muestras = 0;
float  ultimaTemp = NAN, ultimaHum = NAN;

// ================================================================
// HORA
// ================================================================
// Devuelve false si NTP todavía no sincronizó (sin internet)
bool obtenerFechaHora(char* buf, size_t len) {
  struct tm ti;
  if (!getLocalTime(&ti, 10)) return false;   // 10 ms: no bloquea el loop
  if (ti.tm_year < (2024 - 1900)) return false;
  strftime(buf, len, "%Y-%m-%d %H:%M:%S", &ti);
  return true;
}

// ================================================================
// SENSOR
// ================================================================
void leerSensor() {
  float t = dht.readTemperature();
  float h = dht.readHumidity();
  if (isnan(t) || isnan(h)) {
    Serial.println("[DHT11] Error de lectura");
    return;
  }
  ultimaTemp = t;
  ultimaHum  = h;
  sumaTemp += t;
  sumaHum  += h;
  muestras++;
}

// ================================================================
// PUBLICACIÓN
// ================================================================
void publicarDatos() {
  float temp, hum;
  if (muestras > 0) {
    temp = sumaTemp / muestras;
    hum  = sumaHum  / muestras;
  } else if (!isnan(ultimaTemp)) {
    temp = ultimaTemp;   // solicitud justo después de publicar: se manda la última lectura
    hum  = ultimaHum;
  } else {
    Serial.println("[MQTT] Sin lecturas válidas del DHT11, no se publica");
    return;
  }

  StaticJsonDocument<160> doc;
  doc["e"] = ESTACION_NUM;
  char fecha[20];
  if (obtenerFechaHora(fecha, sizeof(fecha))) {
    doc["t"] = fecha;   // sin "t" el worker usa la hora de recepción
  }
  doc["tem"] = roundf(temp * 10) / 10.0;
  doc["hum"] = roundf(hum  * 10) / 10.0;

  char json[160];
  serializeJson(doc, json, sizeof(json));

  if (mqttLocal.connected() && mqttLocal.publish(topic_pub, json, false)) {
    Serial.printf("[MQTT] Publicado (%d muestras): %s\n", muestras, json);
    sumaTemp = 0; sumaHum = 0; muestras = 0;
  } else {
    Serial.println("[MQTT] No se pudo publicar, se reintenta en la próxima ventana");
  }
}

// ================================================================
// MQTT
// ================================================================
void mqttCallback(char* topic, byte* payload, unsigned int length) {
  String mensaje;
  mensaje.reserve(length);
  for (unsigned int i = 0; i < length; i++) mensaje += (char)payload[i];
  String topicStr = String(topic);

  if (topicStr == topic_sub) {
    Serial.println("[MQTT] Solicitud de lectura");
    leerSensor();
    publicarDatos();
  }
  else if (topicStr == topic_comando) {
    mensaje.trim(); mensaje.toUpperCase();
    if (mensaje == "RESET") {
      mqttLocal.publish(topic_estado, "reiniciando", false);
      mqttLocal.loop();
      delay(500);   // deja salir el mensaje antes de reiniciar
      ESP.restart();
    }
  }
}

void conectarLocal() {
  Serial.printf("[LOCAL] Conectando como '%s'...\n", clientId_local);
  if (mqttLocal.connect(clientId_local)) {
    Serial.println("[LOCAL] Conectado");
    mqttLocal.subscribe(topic_sub);
    mqttLocal.subscribe(topic_comando);
    mqttLocal.publish(topic_estado, "en_linea", false);
  } else {
    Serial.printf("[LOCAL] Fallo rc=%d\n", mqttLocal.state());
  }
}

// Reconexión sin bloquear: un intento cada intervaloReconnect
void atenderConexiones() {
  if (WiFi.status() != WL_CONNECTED) return;   // WiFi.setAutoReconnect se encarga del WiFi
  if (mqttLocal.connected()) return;
  if (millis() - lastReconnect < intervaloReconnect) return;
  lastReconnect = millis();
  conectarLocal();
}

// ================================================================
// SETUP
// ================================================================
void setup() {
  Serial.begin(115200);
  delay(500);
  Serial.println("\n=== ESTACION 4 — v1 — DHT11 ===");

  dht.begin();

  WiFi.mode(WIFI_STA);
  WiFi.setAutoReconnect(true);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  Serial.print("[WiFi] Conectando");
  unsigned long tWifi = millis();
  while (WiFi.status() != WL_CONNECTED && millis() - tWifi < 15000) {   // máximo 15 s
    delay(500);
    Serial.print(".");
  }
  Serial.println(WiFi.status() == WL_CONNECTED
    ? "\n[WiFi] Conectado — IP: " + WiFi.localIP().toString()
    : "\n[WiFi] Sin conexion, se reintenta en segundo plano");

  // NTP sincroniza en segundo plano; no se espera aquí
  configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);

  mqttLocal.setServer(mqtt_broker_local, mqtt_port_local);
  mqttLocal.setCallback(mqttCallback);
  mqttLocal.setBufferSize(512);
  if (WiFi.status() == WL_CONNECTED) conectarLocal();

  lastPublicacion = millis();
}

// ================================================================
// LOOP
// ================================================================
void loop() {
  atenderConexiones();
  mqttLocal.loop();

  unsigned long ahora = millis();

  if (ahora - lastRead >= intervaloRead) {
    lastRead = ahora;
    leerSensor();
  }

  if (ahora - lastPublicacion >= intervaloPublicacion) {
    lastPublicacion = ahora;
    publicarDatos();
  }
}
