FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DiscordEye.ProxyDistributor/DiscordEye.ProxyDistributor.csproj", "DiscordEye.ProxyDistributor/"]
COPY ["DiscordEye.Shared/DiscordEye.Shared.csproj", "DiscordEye.Shared/"]
RUN dotnet restore "DiscordEye.ProxyDistributor/DiscordEye.ProxyDistributor.csproj"
COPY . .
WORKDIR "/src/DiscordEye.ProxyDistributor"
RUN dotnet build "DiscordEye.ProxyDistributor.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DiscordEye.ProxyDistributor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiscordEye.ProxyDistributor.dll"]
