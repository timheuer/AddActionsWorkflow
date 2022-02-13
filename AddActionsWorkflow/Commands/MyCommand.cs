using System.Diagnostics;
using System.IO;

namespace AddActionsWorkflow;

[Command(PackageIds.MyCommand)]
internal sealed class MyCommand : BaseCommand<MyCommand>
{
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        var dirInfo = new DirectoryInfo((await VS.Solutions.GetCurrentSolutionAsync()).FullPath);

        var proc = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = dirInfo.Parent.FullName,
            FileName = "dotnet",
            Arguments = "new workflow -n build --no-update-check"
        };

        _ = Process.Start(proc);
    }
}
