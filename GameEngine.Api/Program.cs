using System.Reflection;
using System.Text.Json.Serialization;
using GameEngine.Api.Hosting;
using GameEngine.Configuration;
using GameEngine.Interfaces;
using GameEngine.Manager;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── 合成起点（Composition Root）─────────────────────────────────
// 設定は GameConfigLoader（シングルトン）から1度だけ解決して登録する。
// GameConfigLoader.Instance への直アクセスはこのホストの起動に集約する。
var config = GameConfigLoader.Instance;
builder.Services.AddSingleton(config);

// 確定セーブ用リポジトリ（任意）。MongoDB が利用できなければ登録せず、
// GameSessionManager は IPlayerRepository? 既定値（null）でセーブ/ロード無効のまま動作する。
IPlayerRepository? playerRepository = TryCreatePlayerRepository(config, builder.Logging);
if (playerRepository != null)
{
    builder.Services.AddSingleton(playerRepository);
}

// 進行中セッションのサーバ常駐マネージャ。
// 注意: エンジンの AddGameEngine はステートフルなサービスを Singleton 登録するため、
// 複数セッションを捌く API ではそのまま使えない。GameSessionManager がセッションごとに
// 専用の object graph（バス/プレイヤー/敵ファクトリ/勝敗記録/EventManager/GameSystem）を手組みする。
builder.Services.AddSingleton(sp => new GameSessionManager(
    sp.GetRequiredService<GameConfig>(),
    sp.GetService<IPlayerRepository>()));

// ── MVC / JSON / Swagger ────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // enum を文字列で授受する（"Attack" / "Battle" / "BuyPotion" など）。
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Console RPG Game API",
        Version = "v1",
        Description = "ステップ駆動の RPG エンジンを HTTP で駆動する API。各リクエスト=1ゲームステップ。"
    });

    // XML コメントを Swagger に取り込む（GenerateDocumentationFile=true）。
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Swagger は開発・本番ともに公開する（PoC のため）。
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Console RPG Game API v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();

// ── ヘルパー ─────────────────────────────────────────────────────

// MongoDB リポジトリを試行生成する。失敗時は警告ログを出して null（セーブ無効）を返す。
static IPlayerRepository? TryCreatePlayerRepository(GameConfig config, ILoggingBuilder logging)
{
    try
    {
        return new MongoPlayerRepository(
            config.MongoDB.ConnectionString,
            config.MongoDB.DatabaseName,
            config.MongoDB.CollectionName);
    }
    catch (Exception ex)
    {
        // 起動時はまだ DI コンテナが無いため Console へ直接通知する。
        Console.WriteLine($"Warning: セーブ機能を初期化できませんでした: {ex.Message}");
        Console.WriteLine("API はセーブ/ロード無効で続行します（POST /save・/load や /players の保存系は 503 を返します）。");
        return null;
    }
}

/// <summary>WebApplicationFactory ベースの統合テスト（フェーズ5）から参照できるよう公開する。</summary>
public partial class Program { }
