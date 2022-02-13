using System.Diagnostics;
using System.IO;

namespace AddActionsWorkflow;

[Command(PackageIds.MyCommand)]
internal sealed class MyCommand : BaseCommand<MyCommand>
{
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        var dirInfo = new DirectoryInfo(VS.Solutions.GetCurrentSolution().FullPath);

        var proc = new ProcessStartInfo();
        proc.UseShellExecute = false;
        proc.RedirectStandardOutput = true;
        proc.RedirectStandardError = true;
        proc.WorkingDirectory = dirInfo.Parent.FullName;
        proc.FileName = "dotnet";
        proc.Arguments = "new workflow -n build --no-update-check";

        Process.Start(proc);
    }
}
