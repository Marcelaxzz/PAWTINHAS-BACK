
# Etapa 1: compilação da aplicação
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copia a solução e os projetos
COPY . .

# Restaura as dependências e publica a API
RUN dotnet restore src/Pawtinhas.API/Pawtinhas.API.csproj

RUN dotnet publish src/Pawtinhas.API/Pawtinhas.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Etapa 2: execução da aplicação
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

# Fotos e backups ficam em /app/dados (volume). A API roda sem privilégios de root.
RUN mkdir -p /app/dados && chown -R $APP_UID /app/dados
USER $APP_UID

EXPOSE 8080

# Plataformas como o Railway escolhem a porta pela variável PORT; sem ela, fica a 8080.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet Pawtinhas.API.dll \"$@\"", "--"]