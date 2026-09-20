FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore em camada separada para aproveitar cache enquanto só o código muda.
COPY ValorantCoach.slnx ./
COPY src/ValorantCoach.Domain/ValorantCoach.Domain.csproj src/ValorantCoach.Domain/
COPY src/ValorantCoach.Application/ValorantCoach.Application.csproj src/ValorantCoach.Application/
COPY src/ValorantCoach.Infrastructure/ValorantCoach.Infrastructure.csproj src/ValorantCoach.Infrastructure/
COPY src/ValorantCoach.Api/ValorantCoach.Api.csproj src/ValorantCoach.Api/
RUN dotnet restore src/ValorantCoach.Api/ValorantCoach.Api.csproj

COPY src/ src/
RUN dotnet publish src/ValorantCoach.Api/ValorantCoach.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ValorantCoach.Api.dll"]
