FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore em camada separada para aproveitar cache enquanto só o código muda.
COPY CoachGame.slnx ./
COPY src/CoachGame.Domain/CoachGame.Domain.csproj src/CoachGame.Domain/
COPY src/CoachGame.Application/CoachGame.Application.csproj src/CoachGame.Application/
COPY src/CoachGame.Infrastructure/CoachGame.Infrastructure.csproj src/CoachGame.Infrastructure/
COPY src/CoachGame.Api/CoachGame.Api.csproj src/CoachGame.Api/
RUN dotnet restore src/CoachGame.Api/CoachGame.Api.csproj

COPY src/ src/
RUN dotnet publish src/CoachGame.Api/CoachGame.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "CoachGame.Api.dll"]
