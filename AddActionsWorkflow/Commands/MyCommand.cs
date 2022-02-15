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

        var workflowName = $"build-{Guid.NewGuid().ToString().Substring(0, 5)}";
        proc.StartInfo.Arguments = $"new workflow -n {workflowName} --no-update-check";
        proc.ErrorDataReceived += (s, e) =>
        {
            Debug.WriteLine(e.ToString());
        };
        proc.Start();

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

        rootGitDir = await git.StandardOutput.ReadToEndAsync();

        if (git.ExitCode == 0)
        {
            rootGitDir = rootGitDir.Replace('/', '\\').Replace("\n", "");
        }

        return rootGitDir;
    }
}
