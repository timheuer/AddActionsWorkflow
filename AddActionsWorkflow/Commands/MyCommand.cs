using AddActionsWorkflow.Options;
using CliWrap;
using Microsoft;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AddActionsWorkflow;

[Command(PackageIds.MyCommand)]
internal sealed class MyCommand : BaseCommand<MyCommand>
{
    string finaleWorkflowname = "";

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        var dirInfo = new DirectoryInfo((await VS.Solutions.GetCurrentSolutionAsync()).FullPath);
        var slnDir = dirInfo.Parent.FullName;

        // try to get the repo root
        string repoRoot = await GetGitRootDirAsync(slnDir);

        // create the workflow file with options
        var options = await General.GetLiveInstanceAsync();
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
            await VS.StatusBar.ShowMessageAsync("GitHub Actions Worklfow creation finished.");
        }
        else
        {
            // didn't happen, show an error
            await VS.StatusBar.ShowMessageAsync("GitHub Actions Worklfow creation failed.");
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
            .WithArguments($"new workflow -n {finaleWorkflowname} --no-update-check {overwriteFile}")
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

    internal async Task<String> GetGitRootDirAsync(string workingDirectory)
    {
        await VS.StatusBar.ShowMessageAsync("Establishing git root directory...");
        var rootGitDir = workingDirectory;
        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();

        var result = await Cli.Wrap("git")
            .WithArguments("rev-parse --show-toplevel")
            .WithWorkingDirectory(workingDirectory)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        var stdOut = stdOutBuffer.ToString();
        var stdErr = stdErrBuffer.ToString();

        if (result.ExitCode == 0)
        {
            rootGitDir = stdOut;
            rootGitDir = rootGitDir.Replace('/', '\\').Replace("\n", "");
        }

        return rootGitDir;
    }
}
