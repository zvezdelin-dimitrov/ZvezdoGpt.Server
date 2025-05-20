FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ZvezdoGpt.WebApi/ZvezdoGpt.WebApi.csproj", "ZvezdoGpt.WebApi/"]
RUN dotnet restore "./ZvezdoGpt.WebApi/ZvezdoGpt.WebApi.csproj"
COPY . .
WORKDIR "/src/ZvezdoGpt.WebApi"
RUN dotnet build "./ZvezdoGpt.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ZvezdoGpt.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZvezdoGpt.WebApi.dll"]