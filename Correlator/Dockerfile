# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuraci�n y c�mo Visual Studio usa este Dockerfile para compilar sus im�genes para una depuraci�n m�s r�pida.

# Esta fase se usa cuando se ejecuta desde VS en modo r�pido (valor predeterminado para la configuraci�n de depuraci�n)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
#EXPOSE 5245


# Esta fase se usa para compilar el proyecto de servicio
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Correlator.csproj", "."]
RUN dotnet restore "./Correlator.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Correlator.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase se usa para publicar el proyecto de servicio que se copiar� en la fase final.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Correlator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase se usa en producci�n o cuando se ejecuta desde VS en modo normal (valor predeterminado cuando no se usa la configuraci�n de depuraci�n)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Correlator.dll"]