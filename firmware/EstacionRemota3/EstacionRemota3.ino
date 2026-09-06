/*
 * ================================================================
 *  ESTACIÓN METEOROLÓGICA REMOTA 3 — ESP32
 *  - MQTT sobre TLS (HiveMQ Cloud)
 *  - Suscribe: estacion3/solicitud  → al recibir cualquier msg
 *    publica:  estacion3/datos      → JSON con todos los sensores
 *  - Suscribe: estacion3/comando    → comandos de control (ver RESET)
 *    publica:  estacion3/estado     → "en_linea" / "reiniciando"
 *  Sensores: DHT22 | TSL2561 | Anemómetro | Pluviómetro | Veleta
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
const char* ssid               = "JEAR_LESTOMA";
const char* password           = "L3stom4U";
const char* ntpServer          = "pool.ntp.org";
const long  gmtOffset_sec      = -5 * 3600;
const int   daylightOffset_sec = 0;

// ── MQTT ──────────────────────────────────────────────────────
const char* mqtt_broker   = "d9e45a5646094cd5b00b50268cbff192.s1.eu.hivemq.cloud";
const int   mqtt_port     = 8883;
const char* mqtt_user     = "Estacion3";
const char* mqtt_pass     = "Estacion3123@";   // <-- verifica/crea estas credenciales en HiveMQ Cloud
const char* topic_sub     = "estacion3/solicitud";
const char* topic_pub     = "estacion3/datos";
const char* topic_comando = "estacion3/comando";
const char* topic_estado  = "estacion3/estado";
const char* mqtt_clientId = "ESP32_Estacion_Remota3";

// ── Pines ─────────────────────────────────────────────────────
#define PIN_VIENTO   27
#define PIN_LLUVIA   13
#define DHTPIN        4
#define DHTTYPE       DHT22
#define PIN_VELETA   34   // Entrada analógica de la veleta (solo lectura, ADC1)

// ── Objetos ───────────────────────────────────────────────────
WiFiClientSecure         espClient;
PubSubClient             mqttClient(espClient);
DHT                      dht(DHTPIN, DHTTYPE);
Adafruit_TSL2561_Unified tsl = Adafruit_TSL2561_Unified(TSL2561_ADDR_FLOAT, 12345);

// ── Anemómetro — constantes físicas propias de la Estación 3 ───
// (fuente: documento de calibración de la estación 3, NO reutilizar
//  los valores de otras estaciones)
const float DIAMETRO_M           = 0.14;   // diámetro del círculo de las copas (m)
const float PULSOS_POR_VUELTA    = 1.0;    // 1 imán = 1 pulso por vuelta
const float FACTOR_ANEMOMETRO    = 2.5;    // K = v_viento / v_copas — PENDIENTE DE CALIBRAR
const unsigned long REBOTE_ANEMO_US = 8000;   // antirrebote reed anemómetro: 8 ms

// ── Pluviómetro — constantes físicas propias de la Estación 3 ──
const float AREA_CAPTACION_CM2   = 55.0;   // área de la boca de captación (cm2) — MEDIR
const float ML_POR_BALANCEO      = 2.25;   // ml que provocan un balanceo del cubo — MEDIR
const unsigned long REBOTE_PLUVI_US = 100000; // antirrebote cubo basculante: 100 ms

// ── Derivadas (no tocar) ─────────────────────────────────────────
const float CIRCUNFERENCIA_M = PI * DIAMETRO_M;                               // m por vuelta
const float MM_POR_BALANCEO  = (ML_POR_BALANCEO / AREA_CAPTACION_CM2) * 10.0;  // mm por balanceo

// ── Variables de sensores ─────────────────────────────────────
volatile unsigned long pulsosViento   = 0;
volatile unsigned long ultimoVientoUs = 0;   // antirrebote (micros) del anemómetro
volatile unsigned long pulsosLluvia   = 0;
volatile unsigned long ultimoLluviaUs = 0;   // antirrebote (micros) del pluviómetro

// El ESP32 es multinúcleo: noInterrupts()/portDISABLE_INTERRUPTS() global
// no basta para proteger estos contadores frente a la otra tarea corriendo
// en el otro núcleo. Cada sensor usa su propio mutex de sección crítica.
portMUX_TYPE muxViento = portMUX_INITIALIZER_UNLOCKED;
portMUX_TYPE muxLluvia = portMUX_INITIALIZER_UNLOCKED;

float  temperatura        = 0;
float  humedad            = 0;
float  lux                = 0;
float  radiacion          = 0;
bool   luxSaturado        = false;
float  velocidadViento    = 0;   // m/s
float  velocidadVientoKmh = 0;   // km/h
unsigned long pulsosVientoUltima = 0; // pulsos crudos de la última ventana (diagnóstico)
float  lluviaMinuto       = 0;
float  intensidadLluvia   = 0;
unsigned long pulsosLluviaUltima = 0; // balanceos crudos del último ciclo (diagnóstico)
String direccion          = "SIN";
float  anguloViento       = -1;

// ── Diagnóstico DHT22 ──────────────────────────────────────────
unsigned long fallosTemp = 0;
unsigned long fallosHum  = 0;

// ── Timers ────────────────────────────────────────────────────
unsigned long ultimoMinuto    = 0;
unsigned long tUltimaVentana  = 0;   // último cálculo de velocidad de viento (dt real)
unsigned long lastRead        = 0;
unsigned long lastSerial      = 0;
unsigned long lastReconnect   = 0;

const unsigned long intervaloLluvia    = 60000UL;
const unsigned long intervaloRead      =  2500UL; // DHT22 necesita >= 2 s entre lecturas
const unsigned long intervaloSerial    =  1000UL;
const unsigned long intervaloReconnect =  5000UL;

// ── TSL2561 — saturacion y horario diurno ──────────────────────
// Cuenta ADC maxima segun tiempo de integracion (datasheet TSL2561).
// Con 13ms (el mas corto, el que usamos) el sensor satura en 5047 cuentas.
const uint16_t TSL_MAX_COUNT_13MS = 5047;

// Lux maxima practica que el TSL2561 puede reportar (limite del datasheet;
// mas alla de este punto el sensor deja de ser confiable de todas formas).
const float LUX_MAXIMA_SENSOR = 40000.0;

// Ventana horaria considerada "de dia" para decidir que reportar cuando
// el sensor esta saturado. Ajusta segun tu ubicacion/temporada si hace falta.
const int HORA_INICIO_DIA = 6;
const int HORA_FIN_DIA    = 18;

// ── ISR ───────────────────────────────────────────────────────
// Antirrebote por software en la propia ISR: se descarta cualquier flanco
// que llegue antes de que pase el tiempo mínimo físico del sensor.
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

// Determina si la hora actual (segun NTP) cae dentro de la ventana diurna
// configurada. Si no hay hora disponible, asumimos "de dia" por seguridad
// (preferimos reportar el maximo antes que un 0 falso por falta de hora).
bool esDeDia() {
  struct tm ti;
  if (!getLocalTime(&ti)) return true;
  return (ti.tm_hour >= HORA_INICIO_DIA && ti.tm_hour < HORA_FIN_DIA);
}

// =================================================================
//  SENSORES
// =================================================================

// Conversión de pulsos del anemómetro a velocidad de viento.
//   v = (pulsos / PPR) / dt_s * circunferencia * K      [m/s]
// Se usa el dt REAL transcurrido (no un intervalo fijo asumido), porque
// el loop() nunca llama a actualizarSensores() exactamente cada X ms.
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
  pulsosLluviaUltima = p;
  lluviaMinuto        = p * MM_POR_BALANCEO;   // mismas unidades (mm/min) que las otras estaciones
  intensidadLluvia    = lluviaMinuto * 60.0;
}

// Lectura de la veleta (dirección de viento) — tabla calibrada con 8 switches.
// Rangos calculados como punto medio entre voltajes consecutivos, para
// cubrir todo el rango sin huecos ni solapes.
void leerVeleta() {
  int   crudo   = analogRead(PIN_VELETA);
  float voltaje = crudo * 3.3 / 4095.0;

  if (voltaje < 0.40) {
    direccion = "Este";        anguloViento = 90;
  } else if (voltaje < 0.70) {
    direccion = "Sureste";     anguloViento = 135;
  } else if (voltaje < 1.19) {
    direccion = "Sur";         anguloViento = 180;
  } else if (voltaje < 1.74) {
    direccion = "Noreste";     anguloViento = 45;
  } else if (voltaje < 2.25) {
    direccion = "Suroeste";    anguloViento = 225;
  } else if (voltaje < 2.68) {
    direccion = "Norte";       anguloViento = 0;
  } else if (voltaje < 2.93) {
    direccion = "Noroeste";    anguloViento = 315;
  } else if (voltaje <= 3.30) {
    direccion = "Oeste";       anguloViento = 270;
  } else {
    direccion = "SIN";         anguloViento = -1;
  }
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

  // Lectura cruda del TSL2561 (canal full-spectrum "broadband" e IR).
  // Revisamos saturacion directamente sobre las cuentas ADC, que es mas
  // confiable que fijarnos solo en el valor de lux ya calculado.
  uint16_t broadband, ir;
  tsl.getLuminosity(&broadband, &ir);
  bool saturadoCrudo = (broadband >= TSL_MAX_COUNT_13MS) || (ir >= TSL_MAX_COUNT_13MS);

  if (saturadoCrudo) {
    luxSaturado = true;
    if (esDeDia()) {
      lux = LUX_MAXIMA_SENSOR;
      Serial.println("[TSL] Sensor saturado en horario diurno -> reportando lux maxima");
    } else {
      lux = 0;
      Serial.println("[TSL] Sensor saturado fuera de horario diurno -> reportando 0");
    }
  } else {
    uint32_t luxCalculado = tsl.calculateLux(broadband, ir);
    // calculateLux() usa 65536 como BANDERA de saturación, no como "sin luz".
    // Traducir esa bandera a 0 reporta justo lo contrario de lo que pasó
    // físicamente (sensor saturado -> se ve como "oscuridad total").
    if (luxCalculado == 65536) {
      lux = LUX_MAXIMA_SENSOR;
      luxSaturado = true;
    } else {
      lux = (float)luxCalculado;
      luxSaturado = false;
    }
  }
  radiacion = lux;

  // ── Anemómetro: velocidad real a partir de pulsos y del dt transcurrido ──
  unsigned long ahoraViento = millis();
  unsigned long dtViento    = ahoraViento - tUltimaVentana;
  tUltimaVentana = ahoraViento;

  portENTER_CRITICAL(&muxViento);
  unsigned long pv = pulsosViento; pulsosViento = 0;
  portEXIT_CRITICAL(&muxViento);

  pulsosVientoUltima = pv;
  velocidadViento    = velocidadDesdePulsos(pv, dtViento);   // m/s
  velocidadVientoKmh = velocidadViento * 3.6;                // km/h

  leerVeleta();
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
  o["saturado"] = luxSaturado;
  o["sensor"] = "6319081a-9f85-4e87-8759-6895d08d935a"; o["fecha"] = fecha;

  o = doc.createNestedObject("velocidadViento");
  o["valor"] = velocidadViento; o["unidades"] = "m/s";
  o["sensor"] = "id_sensorVelocidad"; o["fecha"] = fecha;

  o = doc.createNestedObject("direccion");
  o["valor"] = direccion; o["unidades"] = "na";
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
  delay(1000); // margen para que el ack de MQTT/TLS alcance a salir antes del reinicio
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

  // ── Solicitud de datos ──────────────────────────────────────
  if (topicStr == topic_sub) {
    Serial.printf("[MQTT] Solicitud recibida en '%s'\n", topic);

    actualizarSensores();
    String json = generarJSON();

    bool ok = mqttClient.publish(topic_pub, json.c_str(), false);
    Serial.printf("[MQTT] Publicado en '%s': %s\n", topic_pub, ok ? "OK" : "FALLO");
  }
  // ── Comandos de control ─────────────────────────────────────
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
  Serial.println("[REMOTA 3] " + obtenerFechaHora());
  Serial.printf("  Temperatura   : %.2f °C\n",    temperatura);
  Serial.printf("  Humedad       : %.2f %%\n",    humedad);
  Serial.printf("  Radiacion     : %.2f lx%s\n",  radiacion, luxSaturado ? " [SATURADO]" : "");
  Serial.printf("  Viento        : %.2f m/s (%.2f km/h) | pulsos=%lu\n",
                velocidadViento, velocidadVientoKmh, pulsosVientoUltima);
  Serial.printf("  Direccion     : %s (%.0f°)\n", direccion.c_str(), anguloViento);
  Serial.printf("  Lluvia/min    : %.2f mm/min | pulsos=%lu\n", lluviaMinuto, pulsosLluviaUltima);
  Serial.printf("  Intensidad    : %.2f mm/h\n",   intensidadLluvia);
  Serial.printf("  Fallos DHT    : temp=%lu hum=%lu\n", fallosTemp, fallosHum);
  Serial.printf("  MQTT          : %s\n",          mqttClient.connected() ? "conectado" : "desconectado");
  Serial.println("─────────────────────────────────────────");
}

// =================================================================
//  SETUP
// =================================================================
void setup() {
  Serial.begin(115200);
  delay(500);
  Serial.println("\n=== ESTACION METEOROLOGICA REMOTA 3 ===");

  // Interrupciones
  pinMode(PIN_VIENTO, INPUT_PULLUP);
  pinMode(PIN_LLUVIA, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(PIN_VIENTO), contarViento, FALLING);
  attachInterrupt(digitalPinToInterrupt(PIN_LLUVIA), contarLluvia,  FALLING);

  // Veleta (entrada analógica)
  analogReadResolution(12);        // ADC de 0-4095
  analogSetAttenuation(ADC_11db);  // Rango hasta 3.3V

  // Sensores
  dht.begin();
  Wire.begin(22, 21);

  if (!tsl.begin()) {
    Serial.println("[TSL] No detectado");
  } else {
    // Configuracion de sensibilidad MINIMA posible:
    // - autoRange desactivado: si no, la libreria sube la ganancia sola
    //   con poca luz, haciendo el sensor MAS sensible (lo contrario a lo
    //   que queremos).
    // - Ganancia 1x (TSL2561_GAIN_0X): la mas baja disponible.
    // - Integracion 13ms: el tiempo mas corto disponible, tambien reduce
    //   sensibilidad frente a 101ms o 402ms.
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
  configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);

  struct tm timeinfo;
  while (!getLocalTime(&timeinfo)) {
    Serial.println("Esperando NTP...");
    delay(1000);
  }
  Serial.println("Hora sincronizada");

  // NTP
  configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);

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
  ultimoMinuto   = millis();
  tUltimaVentana = millis();
  lastRead       = millis();
  lastSerial     = millis();
  lastReconnect  = millis();
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

  // Leer sensores cada intervaloRead ms
  if (ahora - lastRead >= intervaloRead) {
    lastRead = ahora;
    actualizarSensores();
  }

  // Calcular lluvia acumulada cada 1 min
  calcularLluviaMinuto();

  // Imprimir por serial cada 1 s
  if (ahora - lastSerial >= intervaloSerial) {
    lastSerial = ahora;
    imprimirSerial();
  }
}
