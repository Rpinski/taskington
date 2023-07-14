namespace Taskington.Base.SystemOperations;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

internal class MacOsSystemOperations
{
    public MacOsSystemOperations()
    {
        SyncDirectoryMessage.Subscribe(SyncDirectory);
        SyncFileMessage.Subscribe(SyncFile);
        LoadSystemPlaceholdersMessage.Subscribe(LoadMacOsSystemPlaceholders);
    }

    private static void RunProcess(string fileName, params string[] arguments)
    {
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                FileName = fileName,
                Arguments = string.Join(" ", arguments.Select(arg => arg switch
                {
                    var a when a.Contains(' ') => $"\"{arg}\"",
                    _ => arg
                }))
            }
        };
        process.Start();
        process.StandardOutput.ReadToEnd();
    }

    private static void RunRSync(params string[] arguments) =>
        RunProcess("rsync", arguments);

    public static void SyncDirectory(SyncDirectoryMessage message)
    {
        if (!Directory.Exists(message.To))
        {
            Directory.CreateDirectory(message.To);
        }
        RunRSync("-r", "--delete", "-t", $"{message.From}/", message.To);
    }

    public void SyncFile(SyncFileMessage message) =>
        RunRSync("-t", Path.Combine(message.From, message.FileName), message.To);

    internal static Placeholders LoadMacOsSystemPlaceholders()
    {
        var placeholders = new Placeholders();

        var drives = DriveInfo.GetDrives()
            .Where(drive =>
                drive.IsReady &&
                drive.DriveType is not DriveType.NoRootDirectory and not DriveType.CDRom &&
                drive.RootDirectory.Name != "/");
        foreach (var drive in drives)
        {
            placeholders[$"drive:{drive.VolumeLabel}"] = drive.RootDirectory.FullName.TrimEnd(Path.DirectorySeparatorChar);
        }

        placeholders["AppDataRoaming"] = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return placeholders;
    }
}
