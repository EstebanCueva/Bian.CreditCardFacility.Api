# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# OpenShift normalmente enruta por 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Bian.CreditCardFacility.Api/Bian.CreditCardFacility.Api.csproj", "Bian.CreditCardFacility.Api/"]
RUN dotnet restore "./Bian.CreditCardFacility.Api/Bian.CreditCardFacility.Api.csproj"

COPY . .
WORKDIR "/src/Bian.CreditCardFacility.Api"
RUN dotnet publish "./Bian.CreditCardFacility.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Permisos OpenShift: grupo 0 y g=u para que funcione con UID aleatorio
RUN chgrp -R 0 /app && chmod -R g=u /app

ENTRYPOINT ["dotnet", "Bian.CreditCardFacility.Api.dll"]
