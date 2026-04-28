FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Esto para cache
COPY DevOps/DevOps.csproj DevOps/
RUN dotnet restore DevOps/DevOps.csproj

COPY . .
RUN dotnet publish DevOps/DevOps.csproj -c Release -o /app/publish /p:UseAppHost=false

# Saco el artefacto
FROM scratch AS publish-artifacts
COPY --from=build /app/publish/ /

# Para run
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "DevOps.dll"]
