using AddActionsWorkflow.Options;
using CliWrap;
using LibGit2Sharp;
using Microsoft;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddActionsWorkflow;

[Command(PackageIds.AddWorkflowCommand)]
internal sealed class AddWorkflowCommand : BaseCommand<AddWorkflowCommand>
{
    string finaleWorkflowname = string.Empty;
    string branchName = "main";

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        var dirInfo = new DirectoryInfo((await VS.Solutions.GetCurrentSolutionAsync()).FullPath);
        var slnDir = dirInfo.Parent.FullName;

        // create the workflow file with options
        var options = await General.GetLiveInstanceAsync();

        // try to get the repo root
        string repoRoot = await GetGitRootDirAsync(dirInfo.FullName, options.UseCurrentBranchName);
        var workflowCreated = await CreateWorkflowTemplateAsync(repoRoot, options);

        if (workflowCreated)
        {
            // add solution folder
            var sln = await VS.Solutions.GetCurrentSolutionAsync();
            bool folderExists = false;
            SolutionFolder proj = null;

            // if the folder exists, use it otherwise create
            foreach (var item in sln.Children)
            {
                if (item.Name.ToLower() == options.SolutionFolderName.ToLower())
                {
                    proj = item as SolutionFolder;
                    folderExists = true;
                    break;
                }
            }

            if (!folderExists)
                proj = await sln.AddSolutionFolderAsync(options.SolutionFolderName);

            _ = await proj?.AddExistingFilesAsync(Path.Combine(slnDir, @$".github\workflows\{finaleWorkflowname}.yaml"));
            await VS.StatusBar.ShowMessageAsync("GitHub Actions Workflow creation finished.");
        }
        else
        {
            // didn't happen, show an error
            await VS.StatusBar.ShowMessageAsync("GitHub Actions Workflow creation failed.");
        }
    }

    internal async Task<bool> CreateWorkflowTemplateAsync(string workingDirectory, General options)
    {
        await VS.StatusBar.ShowMessageAsync("Creating GitHub Actions workflow file...");
        var rootGitDir = workingDirectory;
        finaleWorkflowname = options.RandomizeFileName ? $"{options.DefaultName}-{Guid.NewGuid().ToString().Substring(0, 5)}" : options.DefaultName;
        var overwriteFile = options.OverwriteExisting ? "--force" : "";

        bool created = true;
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        var result = await Cli.Wrap("dotnet")
            .WithArguments($"new workflow -n {finaleWorkflowname} -b {branchName} --no-update-check {overwriteFile}")
            .WithWorkingDirectory(workingDirectory)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        var stdOut = stdOutBuffer.ToString();
        var stdErr = stdErrBuffer.ToString();

        if (result.ExitCode != 0) created = false;

        return created;
    }

    internal async Task<String> GetGitRootDirAsync(string workingDirectory, bool useCurrentBranch)
    {
        await VS.StatusBar.ShowMessageAsync("Establishing git root directory...");
        var rootGitDir = workingDirectory;

        FindGitFolder(rootGitDir, out string gitPath);

        try
        {
            using (var repo = new Repository(gitPath))
            {
                if (useCurrentBranch) branchName = repo.Head.FriendlyName;
                rootGitDir = repo.Info.WorkingDirectory;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

        return rootGitDir;
    }

    internal void FindGitFolder(string path, out string foundPath)
    {
        foundPath = null;
        // Check if the current directory contains a .git folder
        if (Directory.Exists(Path.Combine(path, ".git")))
        {
            foundPath = path;
            return;
        }
        else
        {
            string parentPath = Directory.GetParent(path)?.FullName;
            if (!string.IsNullOrEmpty(parentPath))
            {
                FindGitFolder(parentPath, out foundPath); // Recursively search the parent directory
            }
        }
        return;
    }
}
