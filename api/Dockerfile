# ── build ──────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia so os projetos primeiro: a camada de restore so invalida quando uma
# dependencia muda, nao a cada edicao de codigo.
COPY Directory.Build.props ./
COPY src/Alphractal.Fees.Api/Alphractal.Fees.Api.csproj src/Alphractal.Fees.Api/
RUN dotnet restore src/Alphractal.Fees.Api/Alphractal.Fees.Api.csproj

COPY src/ src/
RUN dotnet publish src/Alphractal.Fees.Api/Alphractal.Fees.Api.csproj \
    -c Release -o /app --no-restore

# ── runtime ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# O healthcheck do Compose consulta /api/v1/health com wget. A imagem slim do
# ASP.NET nao inclui o binario por padrao, o que marcaria uma API funcional como
# unhealthy com `/bin/sh: wget: not found`.
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*

# Usuario nao-root: o container escreve no spool, que e volume compartilhado com
# o ETL. Rodando como root, os arquivos sairiam com dono errado no host Linux.
RUN useradd --uid 1001 --create-home --shell /usr/sbin/nologin alphractal
USER alphractal

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
EXPOSE 8080

# UseAppHost=false no csproj: o publish gera DLL, sem executavel proprio.
ENTRYPOINT ["dotnet", "Alphractal.Fees.Api.dll"]
