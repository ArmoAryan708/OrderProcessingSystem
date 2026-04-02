using OPS.BackgroundServices.Workers;
using OPS.Database.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
await host.RunAsync();
