# ---------- BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj
COPY *.sln .
COPY Pricing.Api/*.csproj Pricing.Api/
COPY Pricing.Application/*.csproj Pricing.Application/
COPY Pricing.Domain/*.csproj Pricing.Domain/
COPY Pricing.Infrastructure/*.csproj Pricing.Infrastructure/

RUN dotnet restore

# copy source
COPY . .

WORKDIR /app/Pricing.Api
RUN dotnet publish -c Release -o /out

# ---------- RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /out .

# copy rules.json
COPY Pricing.Api/rules.json ./rules.json

EXPOSE 8080

ENTRYPOINT ["dotnet", "Pricing.Api.dll"]