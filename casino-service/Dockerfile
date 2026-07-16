FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CasinoService.csproj", "casino-service/src/"]
COPY ["../../shared/domain/Cog.Domain.csproj", "shared/domain/"]
RUN dotnet restore "casino-service/src/CasinoService.csproj"
COPY src/ casino-service/src/
COPY ../../shared/domain/ shared/domain/
WORKDIR "/src/casino-service/src"
RUN dotnet build "CasinoService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CasinoService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CasinoService.dll"]
