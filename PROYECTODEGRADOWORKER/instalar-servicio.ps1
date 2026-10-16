# Instala (o actualiza) el Worker como servicio de Windows: lectura serial (COM3), MQTT de las
# estaciones y control de cultivo. Ejecutar como ADMINISTRADOR:
#   clic derecho -> "Ejecutar con PowerShell" en una consola de administrador, o
#   powershell -ExecutionPolicy Bypass -File .\instalar-servicio.ps1
#
# Antes: publicar el Worker en $Ruta y dejar ahí appsettings.Production.json con la conexión
# a la base de Docker (localhost,14330). Ese archivo no va en git porque lleva la contraseña de sa.
#   dotnet publish ServicioSensorica.Worker.csproj -c Release -o C:\Servicios\ServicioSensorica
#
# Desinstalar: .\instalar-servicio.ps1 -Desinstalar
param(
    [string]$Ruta = 'C:\Servicios\ServicioSensorica',
    [switch]$Desinstalar
)

$ErrorActionPreference = 'Stop'
$nombre = 'ServicioSensorica'
$exe = Join-Path $Ruta 'ServicioSensorica.Worker.exe'

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host 'Este script debe ejecutarse como administrador.' -ForegroundColor Red
    exit 1
}

$existente = Get-Service -Name $nombre -ErrorAction SilentlyContinue

if ($Desinstalar) {
    if ($existente) {
        if ($existente.Status -ne 'Stopped') { Stop-Service $nombre -Force }
        sc.exe delete $nombre | Out-Null
        Write-Host "Servicio $nombre eliminado."
    } else {
        Write-Host "El servicio $nombre no está instalado."
    }
    exit 0
}

if (-not (Test-Path $exe)) {
    Write-Host "No se encontró $exe. Publique primero el Worker (ver encabezado)." -ForegroundColor Red
    exit 1
}
if (-not (Test-Path (Join-Path $Ruta 'appsettings.Production.json'))) {
    Write-Host "Falta appsettings.Production.json en $Ruta (cadena de conexión a la base de Docker)." -ForegroundColor Red
    exit 1
}

# Origen del visor de eventos que usa el Worker para sus logs
if (-not [System.Diagnostics.EventLog]::SourceExists('ServicioSensorica')) {
    New-EventLog -LogName Application -Source 'ServicioSensorica'
}

if ($existente) {
    if ($existente.Status -ne 'Stopped') { Stop-Service $nombre -Force }
    sc.exe config $nombre binPath= "`"$exe`"" start= delayed-auto | Out-Null
    Write-Host "Servicio $nombre actualizado."
} else {
    # Inicio automático retrasado: da tiempo a que Docker (SQL Server, Mosquitto, API) arranque primero
    sc.exe create $nombre binPath= "`"$exe`"" start= delayed-auto DisplayName= "Servicio Sensorica" | Out-Null
    sc.exe description $nombre "Lectura serial de acuaponía, MQTT de las estaciones meteorológicas y control de cultivo (UdeC Facatativá)" | Out-Null
    Write-Host "Servicio $nombre creado."
}

# Si el proceso se cae, Windows lo reinicia (10 s, 30 s y luego cada minuto)
sc.exe failure $nombre reset= 86400 actions= restart/10000/restart/30000/restart/60000 | Out-Null

Start-Service $nombre
Get-Service $nombre | Format-Table -AutoSize Name, DisplayName, Status, StartType
Write-Host 'Logs: Visor de eventos -> Registros de Windows -> Aplicación, origen ServicioSensorica.'
