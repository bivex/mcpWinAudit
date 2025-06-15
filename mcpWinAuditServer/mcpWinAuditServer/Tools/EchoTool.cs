using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using System.Diagnostics;
using System.Linq;

namespace mcpWinAuditServer.Tools {
[McpServerToolType]
public static class ProcessTool {
    [McpServerTool, Description ( "Lists all running processes on the system with performance-related information." )]
    public static Task<object> ListAllProcesses() {
        var processes = Process.GetProcesses().Select(p => 
        {
            try 
            {
                return new 
                {
                    Id = p.Id,
                    ProcessName = p.ProcessName,
                    MainWindowTitle = p.MainWindowTitle,
                    Responding = p.Responding,
                    WorkingSet64MB = (p.WorkingSet64 / (1024.0 * 1024.0)), // Memory in MB
                    PrivateMemorySize64MB = (p.PrivateMemorySize64 / (1024.0 * 1024.0)), // Private Memory in MB
                    ThreadsCount = p.Threads.Count,
                    HandleCount = p.HandleCount,
                    TotalProcessorTimeSeconds = p.TotalProcessorTime.TotalSeconds
                };
            }
            catch (System.InvalidOperationException) 
            {
                // Process may have exited
                return null;
            }
            catch (System.ComponentModel.Win32Exception) 
            {
                // Access denied for some process properties
                return null;
            }
        }).Where(p => p != null).ToList(); // Filter out nulls from processes that exited or had access issues
        return Task.FromResult<object>(processes);
    }
}
}