using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateEmptyApplicationBuilder ( settings: null );

// Create the MCP Server with Standard I/O Transport and Tools from the current assembly
builder.Services.AddMcpServer()
.WithStdioServerTransport()
.WithToolsFromAssembly();

builder.Logging.AddConsole ( options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
} );

var app = builder.Build();

await app.RunAsync();
