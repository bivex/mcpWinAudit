using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace mcpWinAuditServer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
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
        }
    }

    [McpServerToolType]
    public static class EchoTool
    {
        [McpServerTool, Description("Echoes the message back to the client.")]
        public static string Echo(string message) => $"hello {message}";
    }
}
