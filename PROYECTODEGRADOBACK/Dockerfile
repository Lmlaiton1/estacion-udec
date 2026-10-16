# API Nueva Naturaleza + Estación Meteorológica (ASP.NET Core 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RR_Nueva_Naturaleza.csproj .
RUN dotnet restore RR_Nueva_Naturaleza.csproj
COPY . .
RUN dotnet publish RR_Nueva_Naturaleza.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
# QuestPDF (reportes) necesita fontconfig en Linux
# Pronóstico IA: Python con TensorFlow para reentrenar la LSTM (ia/pronostico_ia se monta en /ia)
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 python3 python3-venv \
 && rm -rf /var/lib/apt/lists/*
RUN python3 -m venv /opt/pronostico \
 && /opt/pronostico/bin/pip install --no-cache-dir tensorflow-cpu==2.20.0 "numpy>=1.26,<3" "pandas>=2.2,<3" "matplotlib>=3.8,<4"
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:5005
EXPOSE 5005
ENTRYPOINT ["dotnet", "RR_Nueva_Naturaleza.dll"]
