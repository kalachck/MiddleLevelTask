FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NotificationService.sln ./
COPY NotificationService.Presentation/NotificationService.Presentation.csproj NotificationService.Presentation/
COPY NotificationService.Application/NotificationService.Application.csproj NotificationService.Application/
COPY NotificationService.Infrastructure/NotificationService.Infrastructure.csproj NotificationService.Infrastructure/

RUN dotnet restore NotificationService.Presentation/NotificationService.Presentation.csproj

COPY . .
RUN dotnet publish NotificationService.Presentation/NotificationService.Presentation.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=docker

EXPOSE 8080

ENTRYPOINT ["dotnet", "NotificationService.Presentation.dll"]
