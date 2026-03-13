FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5002

# Instalar FFmpeg
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/UserActionsService.csproj", "./"]
RUN dotnet restore "UserActionsService.csproj"
COPY src/ .
RUN dotnet build "UserActionsService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserActionsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Criar diretórios de storage
RUN mkdir -p /app/storage/uploads /app/storage/outputs /app/storage/temp

ENTRYPOINT ["dotnet", "UserActionsService.dll"]
