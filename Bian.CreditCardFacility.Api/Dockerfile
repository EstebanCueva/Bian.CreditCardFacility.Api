# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Bian.CreditCardFacility.Api/Bian.CreditCardFacility.Api.csproj", "Bian.CreditCardFacility.Api/"]
RUN dotnet restore "./Bian.CreditCardFacility.Api/Bian.CreditCardFacility.Api.csproj"
COPY . .
WORKDIR "/src/Bian.CreditCardFacility.Api"
RUN dotnet build "./Bian.CreditCardFacility.Api.csproj" -c %BUILD_CONFIGURATION% -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Bian.CreditCardFacility.Api.csproj" -c %BUILD_CONFIGURATION% -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bian.CreditCardFacility.Api.dll"]
