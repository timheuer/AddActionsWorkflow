using AddActionsWorkflow.Options;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AddActionsWorkflow;

[Command(PackageIds.MyCommand)]
internal sealed class MyCommand : BaseCommand<MyCommand>
{
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        var dirInfo = new DirectoryInfo((await VS.Solutions.GetCurrentSolutionAsync()).FullPath);
        var slnDir = dirInfo.Parent.FullName;

        // try to get the repo root
        string repoRoot = await GetGitRootDirAsync(slnDir);
        using Process proc = new();
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.StartInfo.WorkingDirectory = repoRoot;
        proc.StartInfo.FileName = "dotnet";

        var general = await General.GetLiveInstanceAsync();
        var workflowName = general.RandomizeFileName ? $"{general.DefaultName}-{Guid.NewGuid().ToString().Substring(0, 5)}" : general.DefaultName;
        var overwriteFile = general.OverwriteExisting ? "--force" : "";
        proc.StartInfo.Arguments = $"new workflow -n {workflowName} --no-update-check {overwriteFile}";
        proc.Start();
        proc.WaitForExit();

        // add solution folder
        var sln = await VS.Solutions.GetCurrentSolutionAsync();
        bool folderExists = false;
        SolutionFolder proj = null;

        // if the folder exists, use it otherwise create
        foreach (var item in sln.Children)
        {
            if (item.Name.ToLower() == general.SolutionFolderName.ToLower())
            {
                proj = item as SolutionFolder;
                folderExists = true;
                break;
            }
        }

        if (!folderExists)
            proj = await sln.AddSolutionFolderAsync(general.SolutionFolderName);

        _ = await proj?.AddExistingFilesAsync(Path.Combine(slnDir, @$".github\workflows\{workflowName}.yaml"));
    }

    internal async Task<String> GetGitRootDirAsync(string workingDirectory)
    {
        var rootGitDir = workingDirectory;

        using Process git = new();
        git.StartInfo.WorkingDirectory = workingDirectory;
        git.StartInfo.UseShellExecute = false;
        git.StartInfo.RedirectStandardOutput = true;
        git.StartInfo.RedirectStandardError = true;
        git.StartInfo.FileName = "git";
        git.StartInfo.Arguments = "rev-parse --show-toplevel";
        git.Start();
        git.WaitForExit();

        if (git.ExitCode == 0)
        {
            rootGitDir = await git.StandardOutput.ReadToEndAsync();
            rootGitDir = rootGitDir.Replace('/', '\\').Replace("\n", "");
        }
        else
        {
            git.Kill();
        }

        return rootGitDir;
    }
}
