FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["backend/Qubix.Api/Qubix.Api.csproj", "backend/Qubix.Api/"]
COPY ["backend/Qubix.Core/Qubix.Core.csproj", "backend/Qubix.Core/"]
COPY ["backend/Qubix.Infrastructure/Qubix.Infrastructure.csproj", "backend/Qubix.Infrastructure/"]
RUN dotnet restore "backend/Qubix.Api/Qubix.Api.csproj"

COPY backend/ backend/
RUN dotnet publish "backend/Qubix.Api/Qubix.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Qubix.Api.dll"]
