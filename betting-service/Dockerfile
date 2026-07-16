FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY shared/domain/Cog.Domain.csproj shared/domain/
COPY betting-service/src/BettingService.csproj betting-service/src/
RUN dotnet restore betting-service/src/BettingService.csproj

COPY shared/domain/ shared/domain/
COPY betting-service/src/ betting-service/src/
WORKDIR /src/betting-service/src
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BettingService.dll"]
