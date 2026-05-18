FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DataProcessor.sln ./
COPY DataProcessor.Presentation/DataProcessor.Presentation.csproj DataProcessor.Presentation/
COPY DataProcessor.Application/DataProcessor.Application.csproj DataProcessor.Application/
COPY DataProcessor.Infrastructure/DataProcessor.Infrastructure.csproj DataProcessor.Infrastructure/
COPY DataProcessor.Domain/DataProcessor.Domain.csproj DataProcessor.Domain/

RUN dotnet restore DataProcessor.Presentation/DataProcessor.Presentation.csproj

COPY . .
RUN dotnet publish DataProcessor.Presentation/DataProcessor.Presentation.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=docker

EXPOSE 8080

ENTRYPOINT ["dotnet", "DataProcessor.Presentation.dll"]
