# mcpWinAudit

## Available Commands

| Command | Description |
|---|---|
| `AnalyzeStartupLogs()` | Analyzes the System Event Log for problematic errors and warnings that occurred after the last system startup. |
| `CheckFilePermissions(filePath)` | Checks the access permissions for a given file. |
| `CreateDirectory(path)` | Creates a new directory at the specified path. |
| `DeleteDirectory(path)` | Deletes a directory at the specified path. |
| `GetCurrentDirectory()` | Retrieves the current working directory. |
| `GetLast1DaysFailedSystemEvents()` | Retrieves error and warning events from the System log for the last 1 days. |
| `GetTop15PerformanceImpactingProcesses()` | Retrieves the top 15 processes impacting system performance based on CPU and memory usage. |
| `ListAllProcesses()` | Lists all running processes on the system with performance-related information. |
| `ListFiles(path, recursive)` | Lists files and directories in a directory. Optionally, lists recursively. |
| `MoveDirectory(sourcePath, destinationPath)` | Moves a directory from a source path to a destination path. |
| `MoveFile(sourcePath, destinationPath)` | Moves a file from a source path to a destination path. |
| `RenameFile(oldPath, newPath)` | Renames a file from an old path to a new path. |
