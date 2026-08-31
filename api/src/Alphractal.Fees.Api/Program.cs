using Alphractal.Fees.Api.BackgroundServices;
using Alphractal.Fees.Api.Configuration;
using Alphractal.Fees.Api.Infrastructure;
using Alphractal.Fees.Api.Providers;
using Alphractal.Fees.Api.Repositories;
using Alphractal.Fees.Api.Services;
using Microsoft.Extensions.Options;

// .env ANTES do builder: WebApplication.CreateBuilder le as variaveis de
// ambiente no momento em que e construido. Carregar depois nao teria efeito.
// Mesma convencao do etl/ e do infra/ — um mecanismo de segredo por repositorio.
var envFile = DotEnvFile.Load();

var builder = WebApplication.CreateBuilder(args);

// ── Options ────────────────────────────────────────────────────────────────
builder.Services
    .AddOptions<ClickHouseOptions>()
    .Bind(builder.Configuration.GetSection(ClickHouseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<FeesOptions>()
    .Bind(builder.Configuration.GetSection(FeesOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Regras que cruzam campos (RN-10, RN-04) — DataAnnotations nao alcanca.
builder.Services.AddSingleton<IValidateOptions<FeesOptions>, FeesOptionsValidator>();

// ── MVC: apenas Controllers e Models. Nenhuma view Razor. ──────────────────
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ColdPathExceptionHandler>();

// ── CORS para o painel React ───────────────────────────────────────────────
const string PainelCors = "painel";
builder.Services.AddCors(options => options.AddPolicy(PainelCors, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ── Caminho frio: leitura no ClickHouse ────────────────────────────────────
// A fabrica e singleton (so monta a connection string); o repositorio e scoped
// e abre uma conexao por requisicao.
builder.Services.AddSingleton<IClickHouseConnectionFactory, ClickHouseConnectionFactory>();
builder.Services.AddScoped<IFeesHistoryRepository, ClickHouseFeesHistoryRepository>();

// ── Caminho quente: ingestao WebSocket + janela em memoria ─────────────────
// A janela e singleton porque É o estado do processo: um produtor (a ingestao),
// N leitores (requisicoes). Scoped criaria uma janela vazia por requisicao.
builder.Services.AddSingleton(serviceProvider =>
{
    var fees = serviceProvider.GetRequiredService<IOptions<FeesOptions>>().Value;
    return new HotBlockWindow(fees.HotWindowBlocks);
});
builder.Services.AddSingleton<INewBlockProvider, NethereumNewBlockProvider>();
builder.Services.AddSingleton<IChainMetricsProvider, NethereumChainMetricsProvider>();
// Cliente nomeado + provider singleton: o cache de cotacao (RN-03) precisa
// sobreviver entre requisicoes, e um cliente tipado seria transient.
builder.Services.AddHttpClient(HttpEthPriceProvider.HttpClientName, client =>
    client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<EthPriceBroadcaster>();
builder.Services.AddSingleton<HttpEthPriceProvider>();
builder.Services.AddSingleton<IEthPriceProvider>(serviceProvider =>
    serviceProvider.GetRequiredService<HttpEthPriceProvider>());
builder.Services.AddSingleton<IHostedService>(serviceProvider =>
    serviceProvider.GetRequiredService<HttpEthPriceProvider>());

// Sem estado e sem I/O: singleton serve.
builder.Services.AddSingleton<FeeCalculator>();
builder.Services.AddSingleton<SnapshotBuilder>();
builder.Services.AddSingleton<FeesBroadcaster>();
builder.Services.AddSingleton<PriorityFeeState>();
builder.Services.AddSingleton<ISpoolWriter, NdjsonSpoolWriter>();
builder.Services.AddHostedService<BlockIngestionService>();
builder.Services.AddHostedService<MempoolSamplingService>();

// Caminho quente completo: newHeads → calculo → SSE → spool.
// Falta: reconciliacao de reorg no spool e amostragem de mempool.

var app = builder.Build();

app.Logger.LogInformation(
    envFile is null
        ? "Nenhum .env encontrado; usando appsettings, user-secrets e variaveis de ambiente."
        : "Configuracao carregada de {EnvFile}.",
    envFile);

app.UseExceptionHandler();
app.UseCors(PainelCors);
app.MapControllers();

app.Run();
