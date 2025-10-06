# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar todo el contenido del proyecto
COPY . ./

# Restaurar dependencias
RUN dotnet restore

# Publicar el proyecto (asegurate que el archivo se llame EventProcessor.csproj)
RUN dotnet publish Correlator.csproj -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copiar los archivos publicados desde la etapa de build
COPY --from=build /app/out ./

# Ejecutar el .dll generado
ENTRYPOINT ["dotnet", "Correlator.dll"]
