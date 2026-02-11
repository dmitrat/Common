using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using OutWit.Common.Settings.Configuration;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Blazor;
using OutWit.Communication.Client.WebSocket.Utils;
using OutWit.Common.Settings.Samples.Service.UI;
using OutWit.Common.Settings.Samples.Serializers;

SettingsBuilder.RegisterMemoryPack(b => b.AddCustomSerializers());

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

builder.Services.AddWitRpcChannel(options =>
{
    options.BaseUrl = "ws://localhost:5050";
    options.ApiPath = "api";
    options.UseEncryption = false;
    options.ConfigureClient = client =>
    {
        client.WithMemoryPack();
        client.WithoutAuthorization();
        client.WithoutAutoReconnect();
        client.WithoutRetryPolicy();
    };
});

await builder.Build().RunAsync();
