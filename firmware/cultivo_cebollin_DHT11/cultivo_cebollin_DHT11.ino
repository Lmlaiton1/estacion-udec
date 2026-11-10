/*
 * ================================================================
 *  CONTROL DE CULTIVO — Cebollín — v1 — DHT11 (temperatura y humedad)
 *  UdeC Facatativá — LESTOMA
 * ================================================================
 *  Publica en Mosquitto local, en el formato del módulo de cultivos:
 *      cultivo/{N}/datos   {"tem_C": 24.1, "hum_C": 71.3}
 *  Las llaves del JSON deben ser iguales a la "Clave en el JSON del ESP" de cada
 *  sensor registrado en el cultivo (pantalla Control de cultivo → Sensores).
 *  Si una llave no coincide con ningún sensor activo del cultivo, se descarta.
 *
 *  No necesita hora: el worker marca la lectura con la hora de recepción.
 *
 *  Antes de compilar:
 *    1. Copiar secrets.example.h como secrets.h y poner el WiFi.
 *    2. Poner en CULTIVO_NUM el "Número" del cultivo creado en la página.
 *  Librerías: DHT sensor library (Adafruit), PubSubClient, ArduinoJson 6.
 * ================================================================
 */

#include "secrets.h"   // credenciales (copiar secrets.example.h)
#include <WiFi.h>
#include <PubSubClient.h>
#include <DHT.h>
#include <ArduinoJson.h>

// ================================================================
// SENSOR
// ================================================================
#define DHTPIN  4
#define DHTTYPE DHT11
DHT dht(DHTPIN, DHTTYPE);

// ================================================================
// CULTIVO
// ================================================================
const int CULTIVO_NUM = 1;   // ← mismo "Número" del cultivo en la página

// Claves del JSON = "Clave en el JSON del ESP" de cada sensor del cultivo
const char* CLAVE_TEMPERATURA = "tem_C";
const char* CLAVE_HUMEDAD     = "hum_C";

// ================================================================
// BROKER LOCAL — Mosquitto en PC LESTOMA
// ================================================================
const char* mqtt_broker_local = "192.168.0.130";
const int   mqtt_port_local   = 1883;
const char* clientId_local    = "ESP32_Cultivo_Cebollin";   // único por placa

char topic_pub[40];   // cultivo/{N}/datos, se arma en setup()

// ================================================================
// TIEMPOS (solo millis, nunca delay en el loop)
// ================================================================
const unsigned long intervaloRead        =  2500UL;  // DHT11: mínimo 1 s entre lecturas
const unsigned long intervaloPublicacion = 30000UL;  // 30 s: los umbrales del cultivo reaccionan rápido
const unsigned long intervaloReconnect   =  5000UL;

unsigned long lastRead        = 0;
unsigned long lastPublicacion = 0;
unsigned long lastReconnect   = 0;

// ================================================================
// ESTADO
// ================================================================
WiFiClient   espClient;
PubSubClient mqttLocal(espClient);

// Acumulado de la ventana de publicación (se publica el promedio)
float sumaTemp = 0, sumaHum = 0;
int   muestras = 0;

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
  sumaTemp += t;
  sumaHum  += h;
  muestras++;
}

// ================================================================
// PUBLICACIÓN
// ================================================================
void publicarDatos() {
  if (muestras == 0) {
    Serial.println("[MQTT] Sin lecturas válidas del DHT11, no se publica");
    return;
  }

  StaticJsonDocument<128> doc;
  doc[CLAVE_TEMPERATURA] = roundf(sumaTemp / muestras * 10) / 10.0;
  doc[CLAVE_HUMEDAD]     = roundf(sumaHum  / muestras * 10) / 10.0;

  char json[128];
  serializeJson(doc, json, sizeof(json));

  if (mqttLocal.connected() && mqttLocal.publish(topic_pub, json, false)) {
    Serial.printf("[MQTT] %s (%d muestras): %s\n", topic_pub, muestras, json);
    sumaTemp = 0; sumaHum = 0; muestras = 0;
  } else {
    Serial.println("[MQTT] No se pudo publicar, se reintenta en la próxima ventana");
  }
}

// ================================================================
// MQTT
// ================================================================
void conectarLocal() {
  Serial.printf("[LOCAL] Conectando como '%s'...\n", clientId_local);
  if (mqttLocal.connect(clientId_local)) {
    Serial.println("[LOCAL] Conectado");
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
  snprintf(topic_pub, sizeof(topic_pub), "cultivo/%d/datos", CULTIVO_NUM);
  Serial.printf("\n=== CULTIVO %d — Cebollin — DHT11 ===\n", CULTIVO_NUM);

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

  mqttLocal.setServer(mqtt_broker_local, mqtt_port_local);
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
