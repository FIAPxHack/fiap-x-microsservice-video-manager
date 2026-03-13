FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/VideoManagerService.csproj", "./"]
RUN dotnet restore "VideoManagerService.csproj"
COPY src/ .
RUN dotnet build "VideoManagerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VideoManagerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VideoManagerService.dll"]