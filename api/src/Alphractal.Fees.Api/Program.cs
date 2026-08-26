using Alphractal.Fees.Api.Configuration;

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

// ── MVC: apenas Controllers e Models. Nenhuma view Razor. ──────────────────
builder.Services.AddControllers();

// ── CORS para o painel React ───────────────────────────────────────────────
const string PainelCors = "painel";
builder.Services.AddCors(options => options.AddPolicy(PainelCors, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ── Registros do caminho quente e do caminho frio ──────────────────────────
// Providers/    → Nethereum, cotacao ETH/USD
// Repositories/ → leitura no ClickHouse, spool NDJSON
// Services/     → RN-01 a RN-05, janela de 300 blocos, broadcaster SSE
// BackgroundServices/BlockIngestionService → IHostedService da ingestao
// Registrar aqui conforme cada peca existir.

var app = builder.Build();

app.UseCors(PainelCors);
app.MapControllers();

app.Run();
