using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

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
                    MainWindowTitle = string.IsNullOrEmpty(p.MainWindowTitle) ? "[No Main Window]" : p.MainWindowTitle,
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

    [McpServerTool, Description("Retrieves error and warning events from the System log for the last 31 days.")]
    public static Task<object> GetLast31DaysFailedSystemEvents()
    {
        List<object> events = new List<object>();
        DateTime thirtyOneDaysAgo = DateTime.Now.AddDays(-31);

        try
        {
            EventLog log = new EventLog("System");
            foreach (EventLogEntry entry in log.Entries)
            {
                if (entry.TimeGenerated >= thirtyOneDaysAgo && 
                    (entry.EntryType == EventLogEntryType.Error || 
                     entry.EntryType == EventLogEntryType.Warning))
                {
                    events.Add(new
                    {
                        TimeGenerated = entry.TimeGenerated,
                        Source = entry.Source,
                        EntryType = entry.EntryType.ToString(),
                        Message = entry.Message,
                        EventID = entry.EventID
                    });
                }
            }
            log.Close();
        }
        catch (System.Security.SecurityException) {
            return Task.FromResult<object>("Access Denied: Cannot read System Event Log. Run as administrator.");
        }
        catch (Exception ex) {
            return Task.FromResult<object>($"Error reading System Event Log: {ex.Message}");
        }

        return Task.FromResult<object>(events);
    }
}
}