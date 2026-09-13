using Azure.Data.Tables;
using Jenwa.Inquiry;
using Jenwa.Inquiry.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton(AppConfig.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton(provider =>
    new TableServiceClient(provider.GetRequiredService<AppConfig>().StorageConnection));
builder.Services.AddSingleton<ThrottleStore>();
builder.Services.AddSingleton<SourceStore>();
builder.Services.AddSingleton<InquiryStore>();
builder.Services.AddHttpClient<LineClient>(client => client.Timeout = TimeSpan.FromSeconds(10));

builder.Build().Run();
