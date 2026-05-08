using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Taxi.Client.Identity;
using Taxi.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddTransient<BearerTokenHandler>();

builder.Services.AddHttpClient(
    "TaxiServerClient",
    client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("TaxiServerClient"));
builder.Services.AddScoped<ServiceApi>();

await builder.Build().RunAsync();

