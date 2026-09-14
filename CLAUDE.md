# Proyecto: Red de estaciones meteorológicas — UdeC Facatativá

## Contexto
Proyecto de grado de Laura Milena Laiton (Ing. de Sistemas, UdeC extensión Facatativá).
Co-autora: Valentina Julieth Sandoval Tarazona.
Red de 3 estaciones ESP32 para monitoreo y predicción de microclima en la UPA
FungíReal, asociado al laboratorio LESTOMA.
El software se construye SOBRE un sistema existente de acuaponía (Nueva Naturaleza,
desarrollado por Alejandro Florez), agregando un segundo dominio: Estación Meteorológica.
Plazo: 20 días. Prioriza soluciones que funcionen sobre soluciones elegantes.

## Restricción absoluta
TODO ES LOCAL. Sin CDNs, sin APIs de internet en producción.
La red JEAR_LESTOMA sí tiene internet, pero el sistema debe funcionar sin él.
No propongas HiveMQ, Azure, AWS, Firebase ni ningún servicio remoto para datos.
El firmware puede quedar con interruptor de compilación (#define USAR_BROKER_LOCAL)
para poder compilar contra HiveMQ o contra Mosquitto local.

---

## Rutas (repositorio único: https://github.com/Lmlaiton1/estacion-udec)

    PROYECTODEGRADOBACK/    API ASP.NET Core net8.0 — EF Core 9 + SQL Server, JWT, 4 hubs SignalR
    PROYECTODEGRADOFRONT/   Angular 20 standalone + Angular Material + ng-apexcharts
    PROYECTODEGRADOWORKER/  .NET Worker Service net9.0 — serial actual + MQTT nuevo
    ia/                     Entrenamiento Python/Keras y export ONNX
    firmware/               Sketches .ino de las 3 estaciones
    docs/                   IEEE 830, MCTr008, hallazgos-seguridad.md, manual técnico
    datos/                  Histórico capturado
    mosquitto/              mosquitto.conf (el broker local instalado como servicio Windows)

Rama principal: main. Commit al terminar cada módulo.
El .bak de la base de datos (182 MB) está fuera del control de versiones.

---

## Infraestructura local

**Broker MQTT:** Mosquitto 2.1.2 instalado como servicio Windows.
Config en C:\Program Files\mosquitto\mosquitto.conf
Datos y log en C:\proyectos\estacion-udec\mosquitto\
Puerto: 1883, allow_anonymous true (pendiente: agregar autenticación antes de entrega)
IP del computador: 192.168.20.10
Red WiFi de las estaciones: JEAR_LESTOMA

**SQL Server:** instancia SQLEXPRESS en LAURALAITON\SQLEXPRESS
Base de datos: BdNuevaNaturaleza
Servicio: MSSQL$SQLEXPRESS — configurar en arranque automático con:
  sc config "MSSQL$SQLEXPRESS" start= auto
User-secrets configurados (no van en appsettings.json):
  AppSettings:Secret = 28022004aa28022003bb27022004cc25022005dd
  ConnectionStrings:DefaultConnection = Server=LAURALAITON\SQLEXPRESS;...

**PROBLEMA CONOCIDO:** appsettings.json fue pisado por un commit de Valentina
con valores reales. Pendiente limpiar antes de la entrega (dejar valores vacíos "").

---

## Estado actual del código (verificado)

### Autenticación (PROYECTODEGRADOBACK)
- Login por CÉDULA (Id_Card), no por email.
- BCrypt.Net-Next 4.2.0 con migración transparente desde SHA-256 legado.
- Verificación anti-timing con hash dummy precalculado (factor 11).
- JWT expira en 8 horas (antes 60 días).
- Secretos en dotnet user-secrets, no en appsettings.json.
- CORS restringido a http://localhost:4200.
- AutoMapper actualizado a 15.1.1 (parcheaba vulnerabilidad NU1903).
- Bug corregido: UpdateUser ya hashea el password.
- Hallazgos documentados en docs/hallazgos-seguridad.md.

### DbContext (PROYECTODEGRADOBACK)
DAL/RR_Nueva_NaturalezaContext.cs, SQL Server, BD BdNuevaNaturaleza.
19 DbSet existentes de acuaponía (Users, Devices, Measurements, etc.)
17 migraciones aplicadas, la última 20250929064151_seriaPort.
**Las 8 tablas del dominio Estación aún no existen — se crean en el día 2.**

### Worker (PROYECTODEGRADOWORKER)
NO usa MQTT. Lee por puerto serial (System.IO.Ports).
Actúa como cliente SignalR hacia http://localhost:5005
Worker.cs:583-695 es código muerto comentado, no usar como referencia.
El suscriptor MQTT para estación va como WeatherMqttWorker.cs nuevo,
sin tocar Worker.cs.

### Frontend (PROYECTODEGRADOFRONT)
Compila limpio en modo development (ng build --configuration development).
Build de producción (ng build) falla por Google Fonts — PENDIENTE ARREGLAR.

**Trabajo de Laura (commiteado):**
- BCrypt, JWT 8h, CORS, user-secrets, pantalla /inicio con dos tarjetas.
- Componentes: inicio/ (dos tarjetas), estacion-placeholder/ (shell completo).

**Trabajo de Valentina (integrado en pull):**
- EstacionPlaceholder ya es el shell completo del módulo estación:
  barra superior, menú lateral, notificaciones, rol, logout.
  Estructura igual al Menu de acuaponía.
- Rutas hijas bajo /estacion: Sensors, SensorForm, registerUser, Ia, report.
  HOY apuntan a componentes de acuaponía como placeholder visual.
  Cada una necesita su versión propia para la estación.
- PROBLEMA: encoding en estacion-placeholder.html:
  "EstaciÃ³n" debe ser "Estación", "leÃdo" debe ser "leído".
  Corregir antes de la entrega.

**Componentes propios de la estación que faltan construir:**
- estacion/dashboard   → lecturas en tiempo real, gráficas por estación y sensor
- estacion/historial   → filtros por fecha/hora, exportar CSV/Excel/imagen
- estacion/umbrales    → CRUD de umbrales y alertas configurables
- estacion/ficha       → información técnica de cada estación (no técnica para usuario)
- estacion/ia          → predicción de microclima y clasificación de imágenes

**Ya existe en el frontend (no reinventar):**
src/app/security/: auth.guard.ts, role.guard.ts, jwt.interceptor.ts
src/app/service/auth/authservice.ts
src/app/shared/material.ts — centraliza módulos de Angular Material
Gráficas: ng-apexcharts. SignalR: @microsoft/signalr 9.0.6. JWT decode: jwt-decode 4.

---

## Hardware de las estaciones

**Estaciones 1 y 2:** DHT22, TSL2561, anemómetro Hall, pluviómetro Hall, veleta 4 reed
**Estación 3:** DHT22, TSL2561, anemómetro reed, pluviómetro reed, veleta resistiva GPIO 34

**JSON que publica cada estación (topic: estacionN/datos):**
```json
{
  "temperatura": {"valor": 16.7, "unidades": "C",
    "sensor": "aa07acd0-57e0-4ae0-bf46-f8d7c94b4040", "fecha": "2026-09-05 08:39:39"},
  "humedad":     {"valor": 56.6, "unidades": "%",
    "sensor": "aa07acd0-57e0-4ae0-bf46-f8d7c94b4040", "fecha": "..."},
  "radiacion":   {"valor": 3069, "unidades": "Lux",
    "sensor": "6319081a-9f85-4e87-8759-6895d08d935a", "fecha": "..."},
  "velocidadViento": {"valor": 0, "unidades": "m/s", "pulsos": 0, "intervaloMs": 2500,
    "sensor": "id_sensorVelocidad", "fecha": "..."},
  "direccion":   {"valor": "S", "grados": 180, "unidades": "cardinal",
    "sensor": "id_sensorDireccion", "fecha": "..."},
  "lluvia":      {"valor": 0, "unidades": "mm/min", "pulsos": 0,
    "sensor": "id_sensorlluvia", "fecha": "..."}
}
```

**Decisión de diseño:** opción B — se distinguen estaciones por número de topic,
no por GUID de sensor. Un mismo GUID puede aparecer en varias estaciones;
la columna EstacionNumero es el discriminante.

**GUIDs a asignar en el firmware (aún son placeholders):**
- velocidad viento: 7c2f1e94-5a3d-4b18-9e6c-2d8f4a1b7e30
- dirección viento: 3e8b6d52-9c17-4a2f-b5d3-6f1e9a4c8b72
- pluviómetro:      b91d4f27-6e83-45ca-8d19-4a7c2f5e3b60

**Advertencias del firmware:**
- NTP usa pool.ntp.org (internet). Sin internet, usar RTC DS1307 (TinyRTC).
- Sin antirrebote en ISR: pulsos inflados (6 vueltas → 14+ pulsos).
- mm_por_pulso = 8.0 sin calibrar. Lo normal: 0.2–0.5 mm.
- Dirección del viento en grados para la IA (no texto cardinal).
- Promedio de grados requiere sin/cos, no promedio aritmético.
- PubSubClient buffer: setBufferSize(1024) ya está en el sketch.

**Hardware pendiente:**
- TinyRTC (DS1307): conectar a I2C (SDA=22, SCL=21), alimentar a 3.3V.
  Prueba de escaneo I2C encontró 0 dispositivos — revisar cableado.
- Módulo SD: conectar a SPI (MISO=19, MOSI=23, SCK=18, CS=5).
  Prueba falló — revisar cableado y formato FAT32.
- SD y RTC pospuestos hasta después del software. Van como trabajo futuro documentado.

---

## Esquema de BD — dominio Estación (pendiente crear en día 2)

8 tablas nuevas, todas con prefijo _Meteo o sufijo _Meteo para no chocar:
Estaciones_Meteo, Sensores_Meteo, Lecturas_Meteo, Lecturas_Crudas,
Umbrales_Meteo, Alertas_Meteo, Predicciones_Meteo, Imagenes_Meteo

Índice compuesto en Lecturas_Meteo(EstacionNumero, Variable, FechaHora).
Constraint ISJSON en Lecturas_Crudas.Payload.
Imágenes en disco (datos/imagenes/estacionN/YYYY-MM-DD/), solo ruta en BD.

---

## Arquitectura del módulo nuevo

**Un solo backend, dos dominios.** El área Weather vive dentro del mismo API.
Reutiliza Usuario, Rol y Auditoría. Tras el login → /inicio → dos tarjetas.

**Worker MQTT:** WeatherMqttWorker.cs nuevo en ServicioSensorica.Worker.
NO modifica Worker.cs. Usa MQTTnet, Mosquitto local :1883, WeatherDbContext propio.
Notifica por SignalR con HubConnection cliente hacia WeatherHub nuevo en el backend.

**IA:** Python/Keras → export ONNX → inferencia en C# con Microsoft.ML.OnnxRuntime.
Sin Python en producción. Dos modelos: LSTM (predicción microclima) y
clasificador de imágenes (despejado/nublado/lluvioso).

**Persistencia mixta (sin motores adicionales):**
- Relacional (SQL Server): las 8 tablas del dominio estación.
- Documental: Lecturas_Crudas.Payload como NVARCHAR(MAX) con ISJSON.
- Archivos: imágenes en disco, solo ruta en BD.

---

## Patrón de código (obligatorio)
Entidad nueva = Model + DTO + perfil AutoMapper + IXService/XService en /Service
+ ApiController en /ApiControllers + migración EF Core.
Registrar como Scoped en Program.cs.
Sin lógica de negocio en controllers. Sin DbContext directo en controllers.

## Reglas de trabajo
- Antes de escribir código, muestra el plan y espera confirmación.
- Un módulo por sesión. /clear entre módulos.
- No refactorices código de acuaponía que ya funciona.
- Reutiliza guard, interceptor y authservice del frontend.
- Usa ng-apexcharts para gráficas y shared/material.ts para Material.
- Firmware: nunca delay() en loop, solo millis(). ADC1 para sensores analógicos
  (GPIO 34+), ADC2 no funciona con WiFi activo.
- Secretos fuera de appsettings.json. Passwords con BCrypt.
- Comentarios en español.

## Pendientes antes de entrega final
- [ ] Limpiar appsettings.json (valores vacíos, secretos en user-secrets)
- [ ] Arreglar encoding en estacion-placeholder.html (tildes)
- [ ] Build de producción Angular (Google Fonts local o deshabilitar inlining)
- [ ] SHA256Managed → SHA256.Create() en Tools/Encrypt.cs
- [ ] Autenticación MQTT (usuario/contraseña en mosquitto.conf)
- [ ] IP fija para el computador del laboratorio
- [ ] Servicios en arranque automático: MSSQL$SQLEXPRESS, mosquitto, backend, worker
- [ ] Calibración física del pluviómetro y anemómetro
- [ ] Asignar GUIDs reales a los 3 sensores placeholder en el firmware
- [ ] RTC y SD en el hardware (trabajo futuro documentado)