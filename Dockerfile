# Fase 1: Compilación
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["DensoEmergencyAPI.Web/DensoEmergencyAPI.Web.csproj", "DensoEmergencyAPI.Web/"]
RUN dotnet restore "DensoEmergencyAPI.Web/DensoEmergencyAPI.Web.csproj"

COPY . .
WORKDIR "/src/DensoEmergencyAPI.Web"
RUN dotnet publish -c Release -o /app/publish

# Fase 2: Ejecución
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render usa el puerto 8080 por defecto para servicios gratuitos
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DensoEmergencyAPI.Web.dll"]