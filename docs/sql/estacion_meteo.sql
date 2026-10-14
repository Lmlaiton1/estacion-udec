/* =====================================================================
   ESTACIONES METEOROLÓGICAS — umbrales, alertas y auditoría (script de BD)
   Base de datos: BdNuevaNaturaleza (SQL Server)
   Generado: 2026-10-14 desde las migraciones de EF Core:
     1. 20260914020520_DominioEstacionMeteorologica
          Estaciones_Meteo, Sensores_Meteo, Lecturas_Meteo, Lecturas_Crudas,
          Umbrales_Meteo, Alertas_Meteo, Predicciones_Meteo, Imagenes_Meteo
     2. 20261002041919_AddForeignKeyLecturaMeteoCatalogo
          Llaves foráneas de Lecturas_Meteo hacia Estaciones_Meteo y Sensores_Meteo
     3. 20261002060645_AddActivoAUser
          Columna Activo en Users
     4. 20261002064903_AddActivoSensorYAuditoriaMeteo
          Columna Activo en Sensores_Meteo + tabla Auditoria_Meteo

   Incluye todo el dominio meteorológico porque Umbrales_Meteo, Alertas_Meteo
   y Auditoria_Meteo dependen de Estaciones_Meteo, Lecturas_Meteo y Users.

   Valores que usa la aplicación (columnas de texto, sin CHECK en la BD):
   - Umbrales_Meteo.NivelGravedad / Alertas_Meteo.NivelGravedad: Alta | Media | Baja
   - Auditoria_Meteo.TipoEntidad: Estacion | Sensor
   - Auditoria_Meteo.Accion: Activada | Desactivada | Reiniciada

   IDEMPOTENTE: revisa __EFMigrationsHistory y solo aplica lo que falte.
   Se puede ejecutar varias veces sin dañar nada.

   Requisitos:
   - La base ya debe tener las migraciones de acuaponía (Users, Rols, etc.).
   - QUOTED_IDENTIFIER ON (por defecto en SSMS). Con sqlcmd usar la opción -I:
       sqlcmd -S SERVIDOR\SQLEXPRESS -E -I -d BdNuevaNaturaleza -i estacion_meteo.sql
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

COMMIT;
GO

