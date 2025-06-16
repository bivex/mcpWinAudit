using System.ComponentModel;
using ModelContextProtocol.Server;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Management;

namespace mcpWinAuditServer.Tools {
public class SystemEventData {
    public required DateTime TimeGenerated { get; set; }
    public required string Source { get; set; }
    public required string EntryType { get; set; }
    public required string Message { get; set; }
    public required long EventID { get; set; }
}

public class ProcessInfo {
    public required int Id { get; set; }
    public required string ProcessName { get; set; }
    public required string MainWindowTitle { get; set; }
    public required bool Responding { get; set; }
    public required double WorkingSet64MB { get; set; }
    public required double PrivateMemorySize64MB { get; set; }
    public required int ThreadsCount { get; set; }
    public required int HandleCount { get; set; }
    public required double TotalProcessorTimeSeconds { get; set; }
}

public struct ProcessListResult
{
    public required bool Success { get; set; }
    public required string ErrorMessage { get; set; }
    public required IEnumerable<ProcessInfo> Processes { get; set; }
}

[McpServerToolType]
public static class McpProcessTool {
    [McpServerTool, Description ( "Lists all running processes on the system with performance-related information." )]
    public static Task<ProcessListResult> ListAllProcesses()
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
                    return null; // Return null for class
                }
                catch ( System.ComponentModel.Win32Exception )
                {
                    // Access denied for some process properties
                    return null; // Return null for class
                }
            } ).Where ( p => p != null ).Select(p => p!).ToList(); // Filter out nulls and assert non-null

            return Task.FromResult(new ProcessListResult { Success = true, Processes = processes, ErrorMessage = string.Empty });
        }
        catch ( Exception ex )
        {
            return Task.FromResult(new ProcessListResult { Success = false, ErrorMessage = $"Error retrieving process list: {ex.Message}", Processes = new List<ProcessInfo>() });
        }
    }

    [McpServerTool, Description ( "Retrieves error and warning events from the System log for the last 1 days." )]
    [SupportedOSPlatform ( "windows" )]
    public static Task<object> GetLast1DaysFailedSystemEvents()
    {
        List<SystemEventData> events = new List<SystemEventData>();
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
                    events.Add ( new SystemEventData
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
        ProcessListResult result = await ListAllProcesses();

        if ( !result.Success )
        {
            return result.ErrorMessage; // Error message as string
        }

        // Explicitly cast to List<dynamic> to ensure properties are accessed dynamically
        // if the runtime environment or framework is causing type information loss.
        List<dynamic> allProcesses = result.Processes.Cast<dynamic>().ToList();

        var topProcesses = allProcesses
            .OrderByDescending ( p => p.TotalProcessorTimeSeconds )
            .ThenByDescending ( p => p.PrivateMemorySize64MB )
            .Take ( 15 )
            .ToList();

        return topProcesses; // List of dynamic as object
    }

    [McpServerTool, Description ( "Analyzes the System Event Log for problematic errors and warnings that occurred after the last system startup." )]
    [SupportedOSPlatform ( "windows" )]
    public static Task<object> AnalyzeStartupLogs()
    {
        try
        {
            DateTime lastBootTime = DateTime.MinValue;

            // Use WMI to get the last boot up time
            using ( var searcher = new ManagementObjectSearcher ( "SELECT LastBootUpTime FROM Win32_OperatingSystem" ) )
            {
                foreach ( ManagementObject mo in searcher.Get() )
                {
                    if ( mo["LastBootUpTime"] != null )
                    {
                        lastBootTime = ManagementDateTimeConverter.ToDateTime ( mo["LastBootUpTime"].ToString() );
                        break;
                    }
                }
            }

            if ( lastBootTime == DateTime.MinValue )
            {
                return Task.FromResult<object> ( "Could not determine last system boot time using WMI." );
            }

            EventLog systemLog = new EventLog ( "System" );

            var problematicEvents = new List<SystemEventData>();
            foreach ( EventLogEntry entry in systemLog.Entries )
            {
                if ( entry.TimeGenerated > lastBootTime &&
                        ( entry.EntryType == EventLogEntryType.Error ||
                          entry.EntryType == EventLogEntryType.Warning ) )
                {
                    problematicEvents.Add ( new SystemEventData
                    {
                        TimeGenerated = entry.TimeGenerated,
                        Source = entry.Source,
                        EntryType = entry.EntryType.ToString(),
                        Message = entry.Message,
                        EventID = entry.InstanceId
                    } );
                }
            }
            systemLog.Close();

            if ( !problematicEvents.Any() )
            {
                return Task.FromResult<object> ( "No problematic events found since last system startup." );
            }

            var groupedEvents = problematicEvents
                .GroupBy ( e => new { e.Source, e.EventID } )
                .Select ( g => new
                {
                    Source = g.Key.Source,
                    EventID = g.Key.EventID,
                    Count = g.Count(),
                    LastOccurrence = g.Max ( e => e.TimeGenerated ),
                    ExampleMessage = g.First().Message // Get an example message for context
                } )
                .OrderByDescending ( x => x.Count )
                .ToList();

            return Task.FromResult<object> ( groupedEvents );
        }
        catch ( System.Security.SecurityException )
        {
            return Task.FromResult<object> ( "Access Denied: Cannot read System Event Log. Run as administrator." );
        }
        catch ( Exception ex )
        {
            return Task.FromResult<object> ( $"Error analyzing startup logs: {ex.Message}" );
        }
    }
}
}