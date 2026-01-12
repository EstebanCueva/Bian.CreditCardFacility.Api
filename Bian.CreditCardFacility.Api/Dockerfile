FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Bian.CreditCardFacility.Api.csproj", "./"]
RUN dotnet restore "./Bian.CreditCardFacility.Api.csproj"

COPY . .
RUN dotnet publish "./Bian.CreditCardFacility.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bian.CreditCardFacility.Api.dll"]
