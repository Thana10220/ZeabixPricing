FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Pricing.Tests/Pricing.Tests.csproj", "Pricing.Tests/"]
COPY ["Pricing.Api/Pricing.Api.csproj", "Pricing.Api/"]
COPY ["Pricing.Application/Pricing.Application.csproj", "Pricing.Application/"]
COPY ["Pricing.Domain/Pricing.Domain.csproj", "Pricing.Domain/"]
COPY ["Pricing.Infrastructure/Pricing.Infrastructure.csproj", "Pricing.Infrastructure/"]
RUN dotnet restore "Pricing.Tests/Pricing.Tests.csproj"
COPY . .
WORKDIR "/src/Pricing.Tests"
RUN dotnet build "./Pricing.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Pricing.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pricing.Tests.dll"]
