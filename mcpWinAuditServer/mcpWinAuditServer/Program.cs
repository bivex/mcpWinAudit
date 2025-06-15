using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

// Create the MCP Server with Standard I/O Transport and Tools from the current assembly
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton(_ =>
{
    var client = new HttpClient() { BaseAddress = new Uri("https://api.squiggle.com.au/") };
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("mcp-win-audit-server", "1.0"));
    return client;
});

var app = builder.Build();

await app.RunAsync();
