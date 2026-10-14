# API Nueva Naturaleza + Estación Meteorológica (ASP.NET Core 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RR_Nueva_Naturaleza.csproj .
RUN dotnet restore RR_Nueva_Naturaleza.csproj
COPY . .
RUN dotnet publish RR_Nueva_Naturaleza.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
# QuestPDF (reportes) necesita fontconfig en Linux
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:5005
EXPOSE 5005
ENTRYPOINT ["dotnet", "RR_Nueva_Naturaleza.dll"]
