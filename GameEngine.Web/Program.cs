using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GameEngine.Web;
using GameEngine.Web.Services;
using GameEngine.Web.State;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ホスト型配信のため WASM は API と同一オリジンから配信される。
// BaseAddress = ホスト環境のベースアドレス（= API オリジン）。"/api/..." は API に直接届く（CORS 不要）。
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// API は enum を文字列で授受する（JsonStringEnumConverter）。クライアント側でも同コンバータを登録しないと
// ExpectedInput / GamePhase / ShopActionType / GameActionChoice の送受で破綻する。
// 共有オプションを Singleton 登録し、HttpClient の JSON 拡張に渡して使う。
builder.Services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    Converters = { new JsonStringEnumConverter() }
});

// W2 コアループ: API ラッパーと、sessionId(localStorage)+累積ログを保持するクライアント状態。
builder.Services.AddScoped<GameApiClient>();
builder.Services.AddScoped<SessionStore>();

await builder.Build().RunAsync();
