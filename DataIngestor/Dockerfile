FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DataIngestor.sln ./
COPY src/DataIngestor.Presentation/DataIngestor.Presentation.csproj src/DataIngestor.Presentation/
COPY src/DataIngestor.Application/DataIngestor.Application.csproj src/DataIngestor.Application/
COPY src/DataIngestor.Infrastructure/DataIngestor.Infrastructure.csproj src/DataIngestor.Infrastructure/
COPY src/DataIngestor.Domain/DataIngestor.Domain.csproj src/DataIngestor.Domain/

RUN dotnet restore src/DataIngestor.Presentation/DataIngestor.Presentation.csproj

COPY . .
RUN dotnet publish src/DataIngestor.Presentation/DataIngestor.Presentation.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=docker

EXPOSE 8080

ENTRYPOINT ["dotnet", "DataIngestor.Presentation.dll"]
