using IIDXTierTable;
using IIDXTierTable.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<BrowserStorageService>();
builder.Services.AddScoped<IidxCsvParser>();
builder.Services.AddScoped<TierTableDataService>();

await builder.Build().RunAsync();
