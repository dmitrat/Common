using System;
using System.Collections.Generic;
using OutWit.Common.Settings.Interfaces;
using OutWit.Communication.Server;
using OutWit.Communication.Server.DependencyInjection;
using OutWit.Communication.Server.WebSocket.Utils;
using OutWit.Common.Settings.Samples.Wpf.Module.Csv;
using OutWit.Common.Settings.Samples.Wpf.Module.Database;
using OutWit.Common.Settings.Samples.Wpf.Module.Json;
using OutWit.Common.Settings.Samples.Wpf.Module.SharedDatabase;
using OutWit.Common.Settings.Samples.Service.Contracts;
using OutWit.Common.Settings.Samples.Service.Services;

var appModule = new ApplicationModule();
appModule.Initialize();

var netModule = new NetworkModule();
netModule.Initialize();

var advModule = new AdvancedModule();
advModule.Initialize();

var sharedModule = new SharedDatabaseModule();
sharedModule.Initialize();

var managers = new List<ISettingsManager>
{
    appModule.Manager,
    netModule.Manager,
    advModule.Manager,
    sharedModule.Manager
};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(managers);
builder.Services.AddSingleton<ISettingsService, SettingsServiceImpl>();

builder.Services.AddWitRpcServer<ISettingsService, SettingsServiceImpl>(
    "settings",
    ctx =>
    {
        ctx.WithWebSocket("http://localhost:5050/api", maxNumberOfClients: 10);
        ctx.WithMemoryPack();
        ctx.WithoutAuthorization();
        ctx.WithoutEncryption();
    },
    autoStart: true);

var app = builder.Build();

app.MapGet("/health", () => "OK");

Console.WriteLine("Settings Service started on http://localhost:5050");
app.Run("http://localhost:5000");
