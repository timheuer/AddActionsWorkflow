using AddActionsWorkflow.Options;
using CliWrap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddActionsWorkflow.Commands
{
    [Command(PackageIds.LaunchRemoteUrlCommand)]
    internal class LaunchRemoteUrlCommand : BaseCommand<LaunchRemoteUrlCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            // get the repo URI
            var dirInfo = new DirectoryInfo((await VS.Solutions.GetCurrentSolutionAsync()).FullPath);
            var slnDir = dirInfo.Parent.FullName;

            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            var result = await Cli.Wrap("git")
                .WithArguments("remote get-url origin --push")
                .WithWorkingDirectory(slnDir)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            var stdOut = stdOutBuffer.ToString();
            var stdErr = stdErrBuffer.ToString();

            if (result.ExitCode == 0)
            {
                _ = Process.Start(stdOut);
            }
            else
            {
                var argError = new UriFormatException(stdErr);
                await argError.LogAsync();
            }
        }
    }
}
