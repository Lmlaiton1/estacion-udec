/* =====================================================================
   TODOS LOS CAMBIOS RECIENTES DE LA BASE DE DATOS — script único
   Base de datos: BdNuevaNaturaleza (SQL Server)
   Generado: 2026-10-14 con EF Core, desde la migración 20250929064151_seriaPort
   (última de acuaponía) hasta la más reciente de origin/main.

   ESTACIONES METEOROLÓGICAS
     1. 20260914020520_DominioEstacionMeteorologica
          Estaciones_Meteo, Sensores_Meteo, Lecturas_Meteo, Lecturas_Crudas,
          Umbrales_Meteo, Alertas_Meteo, Predicciones_Meteo, Imagenes_Meteo
     2. 20261002041919_AddForeignKeyLecturaMeteoCatalogo
          Llaves foráneas de Lecturas_Meteo hacia Estaciones_Meteo y Sensores_Meteo
     3. 20261002060645_AddActivoAUser
          Columna Activo en Users
     4. 20261002064903_AddActivoSensorYAuditoriaMeteo
          Columna Activo en Sensores_Meteo + tabla Auditoria_Meteo

   CONTROL DE CULTIVO (siembras, umbrales, alarmas, auditoría)
     5. 20261013030304_AddControlCultivo
          Cultivos, Etapas_Cultivo, Sensores_Cultivo, Actuadores_Cultivo,
          Umbrales_Cultivo, Lecturas_Cultivo, Alarmas_Cultivo,
          Bitacora_Alarmas_Cultivo, Eventos_Dispositivo_Cultivo
     6. 20261013034615_CultivoAuditoriaYEliminacionLogica
          Auditoria_Cultivo + columna Eliminado + índices únicos filtrados
     7. 20261013050841_CultivoVariasEstaciones
          Cultivo_Estaciones (un cultivo con una, varias o todas las estaciones)

   AUDITORÍA METEO (umbrales y alertas)
     8. 20261014141620_AuditoriaUmbralesYAlertasMeteo
          Auditoria_Meteo.Detalle + Alertas_Meteo.AccionTomada, FechaVisto, VistoPorId

   IDEMPOTENTE: revisa __EFMigrationsHistory y solo aplica lo que falte.
   Se puede ejecutar varias veces sin dañar nada; sirve tanto para una base
   que solo tiene acuaponía como para una que ya tiene parte de los cambios.

   Requisitos:
   - La base ya debe tener las migraciones de acuaponía (Users, Rols, etc.).
   - QUOTED_IDENTIFIER ON (por defecto en SSMS). Con sqlcmd usar la opción -I:
       sqlcmd -S SERVIDOR\SQLEXPRESS -E -I -d BdNuevaNaturaleza -i cambios_bd_completo.sql
   ===================================================================== */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE TABLE [Estaciones_Meteo] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] int NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Ubicacion] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(500) NULL,
        [Activa] bit NOT NULL,
        [FechaInstalacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Estaciones_Meteo] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE TABLE [Imagenes_Meteo] (
        [Id] uniqueidentifier NOT NULL,
        [EstacionNumero] int NOT NULL,
        [RutaArchivo] nvarchar(500) NOT NULL,
        [Clasificacion] nvarchar(50) NULL,
        [Confianza] decimal(5,2) NULL,
        [FechaHora] datetime2 NOT NULL,
        [Procesada] bit NOT NULL,
        CONSTRAINT [PK_Imagenes_Meteo] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE TABLE [Lecturas_Crudas] (
        [Id] uniqueidentifier NOT NULL,
        [EstacionNumero] int NOT NULL,
        [Topic] nvarchar(100) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [FechaRecepcion] datetime2 NOT NULL,
        [Procesado] bit NOT NULL,
        CONSTRAINT [PK_Lecturas_Crudas] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Lecturas_Crudas_Payload_ISJSON] CHECK (ISJSON(Payload) = 1)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE TABLE [Lecturas_Meteo] (
        [Id] uniqueidentifier NOT NULL,
        [EstacionNumero] int NOT NULL,
        [SensorId] uniqueidentifier NOT NULL,
        [Variable] nvarchar(50) NOT NULL,
        [Valor] decimal(10,4) NOT NULL,
        [ValorCardinal] nvarchar(10) NULL,
        [Unidad] nvarchar(20) NOT NULL,
        [FechaHora] datetime2 NOT NULL,
        [FechaRecepcion] datetime2 NOT NULL,
        [PulsosRaw] int NULL,
        [IntervaloMs] int NULL,
        CONSTRAINT [PK_Lecturas_Meteo] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE TABLE [Predicciones_Meteo] (
        [Id] uniqueidentifier NOT NULL,
        [EstacionNumero] int NOT NULL,
        [Variable] nvarchar(50) NOT NULL,
        [FechaPrediccion] datetime2 NOT NULL,
        [HorizonteHoras] int NOT NULL,
        [ValorPredicho] decimal(10,4) NOT NULL,
        [NivelConfianza] decimal(5,2) NOT NULL,
        [ModeloVersion] nvarchar(50) NOT NULL,
        [FechaGeneracion] datetime2 NOT NULL,
        CONSTRAINT [PK_Predicciones_Meteo] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE TABLE [Sensores_Meteo] (
        [Id] uniqueidentifier NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Tipo] nvarchar(50) NOT NULL,
        [Variables] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(500) NULL,
        CONSTRAINT [PK_Sensores_Meteo] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE TABLE [Umbrales_Meteo] (
        [Id] uniqueidentifier NOT NULL,
        [EstacionNumero] int NULL,
        [Variable] nvarchar(50) NOT NULL,
        [ValorMin] decimal(10,4) NULL,
        [ValorMax] decimal(10,4) NULL,
        [NivelGravedad] nvarchar(20) NOT NULL,
        [Activo] bit NOT NULL,
        [Descripcion] nvarchar(200) NULL,
        [CreadoPor] uniqueidentifier NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Umbrales_Meteo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Umbrales_Meteo_Users_CreadoPor] FOREIGN KEY ([CreadoPor]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE TABLE [Alertas_Meteo] (
        [Id] uniqueidentifier NOT NULL,
        [LecturaId] uniqueidentifier NOT NULL,
        [UmbralId] uniqueidentifier NOT NULL,
        [EstacionNumero] int NOT NULL,
        [Variable] nvarchar(50) NOT NULL,
        [ValorRegistrado] decimal(10,4) NOT NULL,
        [Mensaje] nvarchar(500) NOT NULL,
        [NivelGravedad] nvarchar(20) NOT NULL,
        [FechaHora] datetime2 NOT NULL,
        [Visto] bit NOT NULL,
        CONSTRAINT [PK_Alertas_Meteo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Alertas_Meteo_Lecturas_Meteo_LecturaId] FOREIGN KEY ([LecturaId]) REFERENCES [Lecturas_Meteo] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Alertas_Meteo_Umbrales_Meteo_UmbralId] FOREIGN KEY ([UmbralId]) REFERENCES [Umbrales_Meteo] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE INDEX [IX_Alertas_Meteo_LecturaId] ON [Alertas_Meteo] ([LecturaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE INDEX [IX_Alertas_Meteo_UmbralId] ON [Alertas_Meteo] ([UmbralId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Estaciones_Meteo_Numero] ON [Estaciones_Meteo] ([Numero]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE INDEX [IX_Lecturas_Meteo_EstacionNumero_Variable_FechaHora] ON [Lecturas_Meteo] ([EstacionNumero], [Variable], [FechaHora]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    CREATE INDEX [IX_Umbrales_Meteo_CreadoPor] ON [Umbrales_Meteo] ([CreadoPor]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914020520_DominioEstacionMeteorologica'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260914020520_DominioEstacionMeteorologica', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002041919_AddForeignKeyLecturaMeteoCatalogo'
)
BEGIN
    DROP INDEX [IX_Estaciones_Meteo_Numero] ON [Estaciones_Meteo];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002041919_AddForeignKeyLecturaMeteoCatalogo'
)
BEGIN
    ALTER TABLE [Estaciones_Meteo] ADD CONSTRAINT [AK_Estaciones_Meteo_Numero] UNIQUE ([Numero]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002041919_AddForeignKeyLecturaMeteoCatalogo'
)
BEGIN
    CREATE INDEX [IX_Lecturas_Meteo_SensorId] ON [Lecturas_Meteo] ([SensorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002041919_AddForeignKeyLecturaMeteoCatalogo'
)
BEGIN
    ALTER TABLE [Lecturas_Meteo] ADD CONSTRAINT [FK_Lecturas_Meteo_Estaciones_Meteo_EstacionNumero] FOREIGN KEY ([EstacionNumero]) REFERENCES [Estaciones_Meteo] ([Numero]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002041919_AddForeignKeyLecturaMeteoCatalogo'
)
BEGIN
    ALTER TABLE [Lecturas_Meteo] ADD CONSTRAINT [FK_Lecturas_Meteo_Sensores_Meteo_SensorId] FOREIGN KEY ([SensorId]) REFERENCES [Sensores_Meteo] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002041919_AddForeignKeyLecturaMeteoCatalogo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261002041919_AddForeignKeyLecturaMeteoCatalogo', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002060645_AddActivoAUser'
)
BEGIN
    ALTER TABLE [Users] ADD [Activo] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002060645_AddActivoAUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261002060645_AddActivoAUser', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002064903_AddActivoSensorYAuditoriaMeteo'
)
BEGIN
    ALTER TABLE [Sensores_Meteo] ADD [Activo] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002064903_AddActivoSensorYAuditoriaMeteo'
)
BEGIN
    CREATE TABLE [Auditoria_Meteo] (
        [Id] uniqueidentifier NOT NULL,
        [TipoEntidad] nvarchar(20) NOT NULL,
        [EntidadId] uniqueidentifier NOT NULL,
        [NombreEntidad] nvarchar(100) NOT NULL,
        [Accion] nvarchar(20) NOT NULL,
        [Motivo] nvarchar(500) NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Auditoria_Meteo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Auditoria_Meteo_Users_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002064903_AddActivoSensorYAuditoriaMeteo'
)
BEGIN
    CREATE INDEX [IX_Auditoria_Meteo_UsuarioId] ON [Auditoria_Meteo] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002064903_AddActivoSensorYAuditoriaMeteo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261002064903_AddActivoSensorYAuditoriaMeteo', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE TABLE [Cultivos] (
        [Id] uniqueidentifier NOT NULL,
        [Numero] int NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Especie] nvarchar(100) NOT NULL,
        [Variedad] nvarchar(100) NULL,
        [Ubicacion] nvarchar(200) NULL,
        [AreaM2] decimal(10,2) NULL,
        [FechaSiembra] datetime2 NULL,
        [EstacionNumero] int NULL,
        [Descripcion] nvarchar(500) NULL,
        [Activo] bit NOT NULL,
        [CreadoPor] uniqueidentifier NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Cultivos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cultivos_Users_CreadoPor] FOREIGN KEY ([CreadoPor]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE TABLE [Eventos_Dispositivo_Cultivo] (
        [Id] uniqueidentifier NOT NULL,
        [CultivoId] uniqueidentifier NOT NULL,
        [TipoDispositivo] nvarchar(20) NOT NULL,
        [DispositivoId] uniqueidentifier NOT NULL,
        [NombreDispositivo] nvarchar(100) NOT NULL,
        [Accion] nvarchar(30) NOT NULL,
        [Origen] nvarchar(20) NOT NULL,
        [Motivo] nvarchar(500) NOT NULL,
        [UsuarioId] uniqueidentifier NULL,
        [AlarmaCultivoId] uniqueidentifier NULL,
        [UmbralCultivoId] uniqueidentifier NULL,
        [Fecha] datetime2 NOT NULL,
        CONSTRAINT [PK_Eventos_Dispositivo_Cultivo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Eventos_Dispositivo_Cultivo_Users_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE TABLE [Actuadores_Cultivo] (
        [Id] uniqueidentifier NOT NULL,
        [CultivoId] uniqueidentifier NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Clave] nvarchar(50) NOT NULL,
        [Tipo] nvarchar(50) NOT NULL,
        [Marca] nvarchar(50) NULL,
        [Potencia] nvarchar(50) NULL,
        [Especificaciones] nvarchar(500) NULL,
        [Modo] nvarchar(20) NOT NULL,
        [Encendido] bit NOT NULL,
        [Activo] bit NOT NULL,
        [FechaUltimoCambio] datetime2 NULL,
        CONSTRAINT [PK_Actuadores_Cultivo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Actuadores_Cultivo_Cultivos_CultivoId] FOREIGN KEY ([CultivoId]) REFERENCES [Cultivos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE TABLE [Etapas_Cultivo] (
        [Id] uniqueidentifier NOT NULL,
        [CultivoId] uniqueidentifier NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Orden] int NOT NULL,
        [DuracionDias] int NULL,
        [FechaInicio] datetime2 NULL,
        [Activa] bit NOT NULL,
        [Descripcion] nvarchar(500) NULL,
        CONSTRAINT [PK_Etapas_Cultivo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Etapas_Cultivo_Cultivos_CultivoId] FOREIGN KEY ([CultivoId]) REFERENCES [Cultivos] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE TABLE [Sensores_Cultivo] (
        [Id] uniqueidentifier NOT NULL,
        [CultivoId] uniqueidentifier NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Clave] nvarchar(50) NOT NULL,
        [Tipo] nvarchar(50) NOT NULL,
        [Variable] nvarchar(50) NOT NULL,
        [Unidad] nvarchar(20) NOT NULL,
        [RangoMin] decimal(10,4) NULL,
        [RangoMax] decimal(10,4) NULL,
        [Precision] decimal(10,4) NULL,
        [Marca] nvarchar(50) NULL,
        [Especificaciones] nvarchar(500) NULL,
        [Activo] bit NOT NULL,
        [FechaInstalacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Sensores_Cultivo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sensores_Cultivo_Cultivos_CultivoId] FOREIGN KEY ([CultivoId]) REFERENCES [Cultivos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE TABLE [Lecturas_Cultivo] (
        [Id] uniqueidentifier NOT NULL,
        [CultivoId] uniqueidentifier NOT NULL,
        [SensorCultivoId] uniqueidentifier NOT NULL,
        [Variable] nvarchar(50) NOT NULL,
        [Valor] decimal(10,4) NOT NULL,
        [Unidad] nvarchar(20) NOT NULL,
        [FechaHora] datetime2 NOT NULL,
        CONSTRAINT [PK_Lecturas_Cultivo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Lecturas_Cultivo_Sensores_Cultivo_SensorCultivoId] FOREIGN KEY ([SensorCultivoId]) REFERENCES [Sensores_Cultivo] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE TABLE [Umbrales_Cultivo] (
        [Id] uniqueidentifier NOT NULL,
        [CultivoId] uniqueidentifier NOT NULL,
        [SensorCultivoId] uniqueidentifier NOT NULL,
        [EtapaCultivoId] uniqueidentifier NULL,
        [ValorMin] decimal(10,4) NULL,
        [ValorMax] decimal(10,4) NULL,
        [NivelGravedad] nvarchar(20) NOT NULL,
        [ActuadorCultivoId] uniqueidentifier NULL,
        [EncenderActuador] bit NULL,
        [RevertirAlNormalizar] bit NOT NULL,
        [MinutosEscalamientoSms] int NULL,
        [Activo] bit NOT NULL,
        [Descripcion] nvarchar(200) NULL,
        [CreadoPor] uniqueidentifier NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Umbrales_Cultivo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Umbrales_Cultivo_Actuadores_Cultivo_ActuadorCultivoId] FOREIGN KEY ([ActuadorCultivoId]) REFERENCES [Actuadores_Cultivo] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Umbrales_Cultivo_Cultivos_CultivoId] FOREIGN KEY ([CultivoId]) REFERENCES [Cultivos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Umbrales_Cultivo_Etapas_Cultivo_EtapaCultivoId] FOREIGN KEY ([EtapaCultivoId]) REFERENCES [Etapas_Cultivo] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Umbrales_Cultivo_Sensores_Cultivo_SensorCultivoId] FOREIGN KEY ([SensorCultivoId]) REFERENCES [Sensores_Cultivo] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Umbrales_Cultivo_Users_CreadoPor] FOREIGN KEY ([CreadoPor]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE TABLE [Alarmas_Cultivo] (
        [Id] uniqueidentifier NOT NULL,
        [CultivoId] uniqueidentifier NOT NULL,
        [UmbralCultivoId] uniqueidentifier NOT NULL,
        [SensorCultivoId] uniqueidentifier NOT NULL,
        [EtapaNombre] nvarchar(100) NULL,
        [Variable] nvarchar(50) NOT NULL,
        [ValorRegistrado] decimal(10,4) NOT NULL,
        [UltimoValor] decimal(10,4) NOT NULL,
        [Ocurrencias] int NOT NULL,
        [Mensaje] nvarchar(500) NOT NULL,
        [NivelGravedad] nvarchar(20) NOT NULL,
        [Estado] nvarchar(20) NOT NULL,
        [FechaDisparo] datetime2 NOT NULL,
        [FechaUltimaOcurrencia] datetime2 NOT NULL,
        [VistaPorId] uniqueidentifier NULL,
        [FechaVista] datetime2 NULL,
        [AtendidaPorId] uniqueidentifier NULL,
        [FechaAtencion] datetime2 NULL,
        [ResueltaPorId] uniqueidentifier NULL,
        [FechaResolucion] datetime2 NULL,
        [Solucion] nvarchar(1000) NULL,
        [SmsEnviado] bit NOT NULL,
        [FechaSms] datetime2 NULL,
        CONSTRAINT [PK_Alarmas_Cultivo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Alarmas_Cultivo_Cultivos_CultivoId] FOREIGN KEY ([CultivoId]) REFERENCES [Cultivos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Alarmas_Cultivo_Umbrales_Cultivo_UmbralCultivoId] FOREIGN KEY ([UmbralCultivoId]) REFERENCES [Umbrales_Cultivo] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Alarmas_Cultivo_Users_AtendidaPorId] FOREIGN KEY ([AtendidaPorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Alarmas_Cultivo_Users_ResueltaPorId] FOREIGN KEY ([ResueltaPorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Alarmas_Cultivo_Users_VistaPorId] FOREIGN KEY ([VistaPorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE TABLE [Bitacora_Alarmas_Cultivo] (
        [Id] uniqueidentifier NOT NULL,
        [AlarmaCultivoId] uniqueidentifier NOT NULL,
        [Accion] nvarchar(30) NOT NULL,
        [Comentario] nvarchar(1000) NULL,
        [UsuarioId] uniqueidentifier NULL,
        [Fecha] datetime2 NOT NULL,
        CONSTRAINT [PK_Bitacora_Alarmas_Cultivo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Bitacora_Alarmas_Cultivo_Alarmas_Cultivo_AlarmaCultivoId] FOREIGN KEY ([AlarmaCultivoId]) REFERENCES [Alarmas_Cultivo] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Bitacora_Alarmas_Cultivo_Users_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Actuadores_Cultivo_CultivoId_Clave] ON [Actuadores_Cultivo] ([CultivoId], [Clave]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Alarmas_Cultivo_AtendidaPorId] ON [Alarmas_Cultivo] ([AtendidaPorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Alarmas_Cultivo_CultivoId_Estado] ON [Alarmas_Cultivo] ([CultivoId], [Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Alarmas_Cultivo_ResueltaPorId] ON [Alarmas_Cultivo] ([ResueltaPorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Alarmas_Cultivo_UmbralCultivoId] ON [Alarmas_Cultivo] ([UmbralCultivoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Alarmas_Cultivo_VistaPorId] ON [Alarmas_Cultivo] ([VistaPorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Bitacora_Alarmas_Cultivo_AlarmaCultivoId] ON [Bitacora_Alarmas_Cultivo] ([AlarmaCultivoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Bitacora_Alarmas_Cultivo_UsuarioId] ON [Bitacora_Alarmas_Cultivo] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Cultivos_CreadoPor] ON [Cultivos] ([CreadoPor]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Cultivos_Numero] ON [Cultivos] ([Numero]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Etapas_Cultivo_CultivoId] ON [Etapas_Cultivo] ([CultivoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Eventos_Dispositivo_Cultivo_CultivoId_Fecha] ON [Eventos_Dispositivo_Cultivo] ([CultivoId], [Fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Eventos_Dispositivo_Cultivo_UsuarioId] ON [Eventos_Dispositivo_Cultivo] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Lecturas_Cultivo_SensorCultivoId_FechaHora] ON [Lecturas_Cultivo] ([SensorCultivoId], [FechaHora]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sensores_Cultivo_CultivoId_Clave] ON [Sensores_Cultivo] ([CultivoId], [Clave]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Umbrales_Cultivo_ActuadorCultivoId] ON [Umbrales_Cultivo] ([ActuadorCultivoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Umbrales_Cultivo_CreadoPor] ON [Umbrales_Cultivo] ([CreadoPor]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Umbrales_Cultivo_CultivoId] ON [Umbrales_Cultivo] ([CultivoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Umbrales_Cultivo_EtapaCultivoId] ON [Umbrales_Cultivo] ([EtapaCultivoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    CREATE INDEX [IX_Umbrales_Cultivo_SensorCultivoId] ON [Umbrales_Cultivo] ([SensorCultivoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013030304_AddControlCultivo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013030304_AddControlCultivo', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    DROP INDEX [IX_Sensores_Cultivo_CultivoId_Clave] ON [Sensores_Cultivo];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    DROP INDEX [IX_Cultivos_Numero] ON [Cultivos];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    DROP INDEX [IX_Actuadores_Cultivo_CultivoId_Clave] ON [Actuadores_Cultivo];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    ALTER TABLE [Umbrales_Cultivo] ADD [Eliminado] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    ALTER TABLE [Sensores_Cultivo] ADD [Eliminado] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    ALTER TABLE [Etapas_Cultivo] ADD [Eliminado] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    ALTER TABLE [Cultivos] ADD [Eliminado] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    ALTER TABLE [Alarmas_Cultivo] ADD [Eliminado] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    ALTER TABLE [Actuadores_Cultivo] ADD [Eliminado] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    CREATE TABLE [Auditoria_Cultivo] (
        [Id] uniqueidentifier NOT NULL,
        [CultivoId] uniqueidentifier NULL,
        [TipoEntidad] nvarchar(20) NOT NULL,
        [EntidadId] uniqueidentifier NOT NULL,
        [NombreEntidad] nvarchar(150) NOT NULL,
        [Accion] nvarchar(30) NOT NULL,
        [Detalle] nvarchar(2000) NULL,
        [Motivo] nvarchar(500) NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        [Fecha] datetime2 NOT NULL,
        CONSTRAINT [PK_Auditoria_Cultivo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Auditoria_Cultivo_Users_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Sensores_Cultivo_CultivoId_Clave] ON [Sensores_Cultivo] ([CultivoId], [Clave]) WHERE [Eliminado] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Cultivos_Numero] ON [Cultivos] ([Numero]) WHERE [Eliminado] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Actuadores_Cultivo_CultivoId_Clave] ON [Actuadores_Cultivo] ([CultivoId], [Clave]) WHERE [Eliminado] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    CREATE INDEX [IX_Auditoria_Cultivo_CultivoId_Fecha] ON [Auditoria_Cultivo] ([CultivoId], [Fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    CREATE INDEX [IX_Auditoria_Cultivo_UsuarioId] ON [Auditoria_Cultivo] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013034615_CultivoAuditoriaYEliminacionLogica'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013034615_CultivoAuditoriaYEliminacionLogica', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013050841_CultivoVariasEstaciones'
)
BEGIN
    CREATE TABLE [Cultivo_Estaciones] (
        [CultivoId] uniqueidentifier NOT NULL,
        [EstacionNumero] int NOT NULL,
        CONSTRAINT [PK_Cultivo_Estaciones] PRIMARY KEY ([CultivoId], [EstacionNumero]),
        CONSTRAINT [FK_Cultivo_Estaciones_Cultivos_CultivoId] FOREIGN KEY ([CultivoId]) REFERENCES [Cultivos] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Cultivo_Estaciones_Estaciones_Meteo_EstacionNumero] FOREIGN KEY ([EstacionNumero]) REFERENCES [Estaciones_Meteo] ([Numero]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013050841_CultivoVariasEstaciones'
)
BEGIN
    CREATE INDEX [IX_Cultivo_Estaciones_EstacionNumero] ON [Cultivo_Estaciones] ([EstacionNumero]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013050841_CultivoVariasEstaciones'
)
BEGIN
    -- EXEC: Cultivos.EstacionNumero se elimina en esta misma migración; sin EXEC el script
    -- no compila en una base que ya la aplicó (SQL Server valida todo el lote).
    EXEC(N'INSERT INTO Cultivo_Estaciones (CultivoId, EstacionNumero)
          SELECT c.Id, c.EstacionNumero FROM Cultivos c
          WHERE c.EstacionNumero IS NOT NULL
            AND EXISTS (SELECT 1 FROM Estaciones_Meteo e WHERE e.Numero = c.EstacionNumero)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013050841_CultivoVariasEstaciones'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cultivos]') AND [c].[name] = N'EstacionNumero');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Cultivos] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [Cultivos] DROP COLUMN [EstacionNumero];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013050841_CultivoVariasEstaciones'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013050841_CultivoVariasEstaciones', N'9.0.7');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014141620_AuditoriaUmbralesYAlertasMeteo'
)
BEGIN
    ALTER TABLE [Auditoria_Meteo] ADD [Detalle] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014141620_AuditoriaUmbralesYAlertasMeteo'
)
BEGIN
    ALTER TABLE [Alertas_Meteo] ADD [AccionTomada] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014141620_AuditoriaUmbralesYAlertasMeteo'
)
BEGIN
    ALTER TABLE [Alertas_Meteo] ADD [FechaVisto] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014141620_AuditoriaUmbralesYAlertasMeteo'
)
BEGIN
    ALTER TABLE [Alertas_Meteo] ADD [VistoPorId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014141620_AuditoriaUmbralesYAlertasMeteo'
)
BEGIN
    CREATE INDEX [IX_Alertas_Meteo_VistoPorId] ON [Alertas_Meteo] ([VistoPorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014141620_AuditoriaUmbralesYAlertasMeteo'
)
BEGIN
    ALTER TABLE [Alertas_Meteo] ADD CONSTRAINT [FK_Alertas_Meteo_Users_VistoPorId] FOREIGN KEY ([VistoPorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014141620_AuditoriaUmbralesYAlertasMeteo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014141620_AuditoriaUmbralesYAlertasMeteo', N'9.0.7');
END;

COMMIT;
GO

