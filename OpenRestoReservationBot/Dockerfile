FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["OpenRestoReservationBot.csproj", "."]
RUN dotnet restore "./OpenRestoReservationBot.csproj"

COPY . .
RUN dotnet publish "OpenRestoReservationBot.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080
EXPOSE 8080

USER app
ENTRYPOINT ["dotnet", "OpenRestoReservationBot.dll"]
