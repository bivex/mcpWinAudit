using System.Threading.Tasks;
using ModelContextProtocol.Tools;

namespace mcpWinAuditServer
{
    public class EchoTool : Tool
    {
        public override string Name => "EchoTool";
        public override string Description => "A simple tool that echoes back the input.";

        public override Task<object> Execute(object input)
        {
            return Task.FromResult(input);
        }
    }
} 