using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using System.Diagnostics;
using System.Linq;

namespace mcpWinAuditServer.Tools {
[McpServerToolType]
public static class ProcessTool {
    [McpServerTool, Description ( "Lists all running processes on the system." )]
    public static Task<object> ListAllProcesses() {
        var processes = Process.GetProcesses().Select(p => new 
        {
            Id = p.Id,
            ProcessName = p.ProcessName,
            MainWindowTitle = p.MainWindowTitle,
            Responding = p.Responding
        }).ToList();
        return Task.FromResult<object>(processes);
    }
}
}