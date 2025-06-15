using System.ComponentModel;
using ModelContextProtocol.Server;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Linq;

namespace mcpWinAuditServer.Tools {
[McpServerToolType]
public static class McpProcessTool {
    [McpServerTool, Description ( "Lists all running processes on the system with performance-related information." )]
    public static async Task<object> ListAllProcesses()
    {
        var processes = Process.GetProcesses().Select ( p =>
        {
            try
            {
                return new
                {
                    Id = p.Id,
                    ProcessName = p.ProcessName,
                    MainWindowTitle = string.IsNullOrEmpty ( p.MainWindowTitle ) ? "[No Main Window]" : p.MainWindowTitle,
                    Responding = p.Responding,
                    WorkingSet64MB = ( p.WorkingSet64 / ( 1024.0 * 1024.0 ) ), // Memory in MB
                    PrivateMemorySize64MB = ( p.PrivateMemorySize64 / ( 1024.0 * 1024.0 ) ), // Private Memory in MB
                    ThreadsCount = p.Threads.Count,
                    HandleCount = p.HandleCount,
                    TotalProcessorTimeSeconds = p.TotalProcessorTime.TotalSeconds
                };
            }
            catch ( System.InvalidOperationException )
            {
                // Process may have exited
                return null;
            }
            catch ( System.ComponentModel.Win32Exception )
            {
                // Access denied for some process properties
                return null;
            }
        } ).Where ( p => p != null ).ToList(); // Filter out nulls from processes that exited or had access issues
        return Task.FromResult<object> ( processes );
    }

    [McpServerTool, Description ( "Retrieves error and warning events from the System log for the last 1 days." )]
    [SupportedOSPlatform ( "windows" )]
    public static Task<object> GetLast1DaysFailedSystemEvents()
    {
        List<object> events = new List<object>();
        DateTime thirtyOneDaysAgo = DateTime.Now.AddDays ( -1 );

        try
        {
            EventLog log = new EventLog ( "System" );
            foreach ( EventLogEntry entry in log.Entries )
            {
                if ( entry.TimeGenerated >= thirtyOneDaysAgo &&
                        ( entry.EntryType == EventLogEntryType.Error ||
                          entry.EntryType == EventLogEntryType.Warning ) )
                {
                    events.Add ( new
                    {
                        TimeGenerated = entry.TimeGenerated,
                        Source = entry.Source,
                        EntryType = entry.EntryType.ToString(),
                        Message = entry.Message,
                        EventID = entry.InstanceId
                    } );
                }
            }
            log.Close();
        }
        catch ( System.Security.SecurityException )
        {
            return Task.FromResult<object> ( "Access Denied: Cannot read System Event Log. Run as administrator." );
        }
        catch ( Exception ex )
        {
            return Task.FromResult<object> ( $"Error reading System Event Log: {ex.Message}" );
        }

        return Task.FromResult<object> ( events );
    }

    [McpServerTool, Description ( "Retrieves the top 15 processes impacting system performance based on CPU and memory usage." )]
    public static async Task<object> GetTop15PerformanceImpactingProcesses()
    {
        var allProcesses = await ListAllProcesses() as IEnumerable<dynamic>;

        if ( allProcesses == null )
        {
            return "Could not retrieve process data.";
        }

        var topProcesses = allProcesses
            .OrderByDescending ( p => p.TotalProcessorTimeSeconds )
            .ThenByDescending ( p => p.PrivateMemorySize64MB )
            .Take ( 15 )
            .ToList();

        return topProcesses;
    }
}
}