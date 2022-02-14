using System.Diagnostics;
using System.IO;

namespace AddActionsWorkflow;

[Command(PackageIds.MyCommand)]
internal sealed class MyCommand : BaseCommand<MyCommand>
{
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        var dirInfo = new DirectoryInfo((await VS.Solutions.GetCurrentSolutionAsync()).FullPath);
        var slnDir = dirInfo.Parent.FullName;
        var workflowName = $"build-{Guid.NewGuid().ToString().Substring(0,5)}";

        var proc = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = slnDir,
            FileName = "dotnet",
            Arguments = $"new workflow -n {workflowName} --no-update-check"
        };

        _ = Process.Start(proc);

        // add solution folder
        var sln = await VS.Solutions.GetCurrentSolutionAsync();
        bool folderExists = false;
        SolutionFolder proj = null;

        // if the folder exists, use it otherwise create
        foreach (var item in sln.Children)
        {
            if (item.Name.ToLower() == "solution items")
            {
                proj = item as SolutionFolder;
                folderExists = true;
            }
            break;
        }

        if (!folderExists)
            proj = await sln.AddSolutionFolderAsync("Solution Items");

        // location here is a hack, need to really resolve with https://github.com/timheuer/AddActionsWorkflow/issues/1
        _ = await proj?.AddExistingFilesAsync(Path.Combine(slnDir, @$".github\workflows\{workflowName}.yaml"));
    }
}
