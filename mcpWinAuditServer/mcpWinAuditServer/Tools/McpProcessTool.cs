using System.ComponentModel;
using ModelContextProtocol.Server;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Linq;
using System.Collections.Generic;
using System;

namespace mcpWinAuditServer.Tools {
    public struct SystemEventData
    {
        public DateTime TimeGenerated { get; set; }
        public string Source { get; set; }
        public string EntryType { get; set; }
        public string Message { get; set; }
        public long EventID { get; set; }
    }

    public struct ProcessInfo
    {
        public int Id { get; set; }
        public string ProcessName { get; set; }
        public string MainWindowTitle { get; set; }
        public bool Responding { get; set; }
        public double WorkingSet64MB { get; set; }
        public double PrivateMemorySize64MB { get; set; }
        public int ThreadsCount { get; set; }
        public int HandleCount { get; set; }
        public double TotalProcessorTimeSeconds { get; set; }
    }

    public struct ProcessListResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public IEnumerable<ProcessInfo> Processes { get; set; }
    }

[McpServerToolType]
public static class McpProcessTool {
    [McpServerTool, Description ( "Lists all running processes on the system with performance-related information." )]
    public static async Task<object> ListAllProcesses()
    {
        try
        {
            var processes = Process.GetProcesses().Select ( p =>
            {
                try
                {
                    return new ProcessInfo
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
                    return (ProcessInfo?)null; // Return nullable struct
                }
                catch ( System.ComponentModel.Win32Exception )
                {
                    // Access denied for some process properties
                    return (ProcessInfo?)null; // Return nullable struct
                }
            } ).Where ( p => p.HasValue ).Select(p => p.Value).ToList(); // Filter out nulls and get values

            return Task.FromResult<object> ( new ProcessListResult { Success = true, Processes = processes } );
        }
        catch ( Exception ex )
        {
            return Task.FromResult<object> ( new ProcessListResult { Success = false, ErrorMessage = $"Error retrieving process list: {ex.Message}" } );
        }
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
        var result = await ListAllProcesses() as ProcessListResult?;

        if ( !result.HasValue || !result.Value.Success )
        {
            return result.HasValue ? result.Value.ErrorMessage : "Could not retrieve process data due to an unknown error.";
        }

        var allProcesses = result.Value.Processes;

        var topProcesses = allProcesses
            .OrderByDescending ( p => p.TotalProcessorTimeSeconds )
            .ThenByDescending ( p => p.PrivateMemorySize64MB )
            .Take ( 15 )
            .ToList();

        return topProcesses;
    }
}
}