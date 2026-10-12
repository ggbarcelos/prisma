using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Prisma.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuredApiAddress = builder.Configuration["ApiBaseAddress"];
var apiAddress = string.IsNullOrWhiteSpace(configuredApiAddress)
    ? new Uri(builder.HostEnvironment.BaseAddress)
    : new Uri(configuredApiAddress, UriKind.Absolute);

if (apiAddress.Scheme != Uri.UriSchemeHttp && apiAddress.Scheme != Uri.UriSchemeHttps)
{
    throw new InvalidOperationException("ApiBaseAddress deve usar HTTP ou HTTPS.");
}

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiAddress });

await builder.Build().RunAsync();
