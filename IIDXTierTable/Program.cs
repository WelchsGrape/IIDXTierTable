using IIDXTierTable;
using IIDXTierTable.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
builder.Configuration.AddJsonFile(
	$"appsettings.{builder.HostEnvironment.Environment}.json",
	optional: true,
	reloadOnChange: false);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
	?? throw new InvalidOperationException("ApiBaseUrl 설정이 필요합니다.");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<BrowserStorageService>();
builder.Services.AddScoped<IidxCsvParser>();
builder.Services.AddScoped<TierTableDataService>();
builder.Services.AddScoped<RankPointService>();

await builder.Build().RunAsync();
