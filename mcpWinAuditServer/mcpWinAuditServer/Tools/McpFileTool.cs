using System.ComponentModel;
using ModelContextProtocol.Server;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Versioning;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace mcpWinAuditServer.Tools {

    [McpServerToolType]
    public static class McpFileTool {

        [McpServerTool, Description("Lists files and directories in a directory. Optionally, lists recursively.")]
        public static Task<object> ListFiles(string path, bool recursive = false)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return Task.FromResult<object>($"Error: Directory not found at {path}");
                }

                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                
                var files = Directory.GetFiles(path, "*", searchOption)
                                     .Select(f => new { Type = "File", Path = f, LastWriteTime = File.GetLastWriteTime(f) });
                
                var directories = Directory.GetDirectories(path, "*", searchOption)
                                           .Select(d => new { Type = "Directory", Path = d, LastWriteTime = Directory.GetLastWriteTime(d) });

                var combinedList = files.Concat<object>(directories).ToList();
                return Task.FromResult<object>(combinedList);
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult<object>($"Access Denied: Cannot access directory at {path}. Run as administrator or check permissions.");
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>($"Error listing files: {ex.Message}");
            }
        }

        [McpServerTool, Description("Moves a file from a source path to a destination path.")]
        public static Task<object> MoveFile(string sourcePath, string destinationPath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    return Task.FromResult<object>($"Error: Source file not found at {sourcePath}");
                }

                File.Move(sourcePath, destinationPath);
                return Task.FromResult<object>($"File moved successfully from {sourcePath} to {destinationPath}");
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult<object>($"Access Denied: Cannot move file. Run as administrator or check permissions for {sourcePath} or {destinationPath}.");
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>($"Error moving file: {ex.Message}");
            }
        }

        [McpServerTool, Description("Renames a file from an old path to a new path.")]
        public static Task<object> RenameFile(string oldPath, string newPath)
        {
            try
            {
                if (!File.Exists(oldPath))
                {
                    return Task.FromResult<object>($"Error: File not found at {oldPath}");
                }

                File.Move(oldPath, newPath); // File.Move can be used for renaming in the same directory
                return Task.FromResult<object>($"File renamed successfully from {oldPath} to {newPath}");
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult<object>($"Access Denied: Cannot rename file. Run as administrator or check permissions for {oldPath}.");
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>($"Error renaming file: {ex.Message}");
            }
        }

        [McpServerTool, Description("Checks the access permissions for a given file.")]
        [SupportedOSPlatform("windows")]
        public static Task<object> CheckFilePermissions(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return Task.FromResult<object>($"Error: File not found at {filePath}");
                }

                FileInfo fileInfo = new FileInfo(filePath);
                FileSecurity fileSecurity = fileInfo.GetAccessControl();

                List<object> accessRules = new List<object>();
                foreach (FileSystemAccessRule rule in fileSecurity.GetAccessRules(true, true, typeof(NTAccount)))
                {
                    accessRules.Add(new
                    {
                        IdentityReference = rule.IdentityReference?.Value,
                        FileSystemRights = rule.FileSystemRights.ToString(),
                        AccessControlType = rule.AccessControlType.ToString(),
                        IsInherited = rule.IsInherited
                    });
                }

                return Task.FromResult<object>(accessRules);
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult<object>($"Access Denied: Cannot access file security information for {filePath}. Run as administrator.");
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>($"Error checking file permissions: {ex.Message}");
            }
        }

        [McpServerTool, Description("Creates a new directory at the specified path.")]
        public static Task<object> CreateDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    return Task.FromResult<object>($"Error: Directory already exists at {path}");
                }

                Directory.CreateDirectory(path);
                return Task.FromResult<object>($"Directory created successfully at {path}");
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult<object>($"Access Denied: Cannot create directory at {path}. Run as administrator or check permissions.");
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>($"Error creating directory: {ex.Message}");
            }
        }

        [McpServerTool, Description("Deletes a directory at the specified path.")]
        public static Task<object> DeleteDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return Task.FromResult<object>($"Error: Directory not found at {path}");
                }

                Directory.Delete(path, true); // The 'true' parameter allows recursive deletion
                return Task.FromResult<object>($"Directory deleted successfully at {path}");
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult<object>($"Access Denied: Cannot delete directory at {path}. Run as administrator or check permissions.");
            }
            catch (IOException ex) when (ex.Message.Contains("The directory is not empty"))
            {
                return Task.FromResult<object>($"Error: Directory at {path} is not empty. Cannot delete a non-empty directory without recursive flag (which is enabled by default).");
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>($"Error deleting directory: {ex.Message}");
            }
        }

        [McpServerTool, Description("Retrieves the current working directory.")]
        public static Task<object> GetCurrentDirectory()
        {
            try
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                return Task.FromResult<object>(currentDirectory);
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>($"Error retrieving current directory: {ex.Message}");
            }
        }

        [McpServerTool, Description("Moves a directory from a source path to a destination path.")]
        public static Task<object> MoveDirectory(string sourcePath, string destinationPath)
        {
            try
            {
                if (!Directory.Exists(sourcePath))
                {
                    return Task.FromResult<object>($"Error: Source directory not found at {sourcePath}");
                }

                Directory.Move(sourcePath, destinationPath);
                return Task.FromResult<object>($"Directory moved successfully from {sourcePath} to {destinationPath}");
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult<object>($"Access Denied: Cannot move directory. Run as administrator or check permissions for {sourcePath} or {destinationPath}.");
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>($"Error moving directory: {ex.Message}");
            }
        }
    }
} 