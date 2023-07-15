using LibGit2Sharp;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AddActionsWorkflow.Commands;

[Command(PackageIds.LaunchRemoteUrlCommand)]
internal class LaunchRemoteUrlCommand : BaseCommand<LaunchRemoteUrlCommand>
{
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        // get the repo URI
        var dirInfo = new DirectoryInfo((await VS.Solutions.GetCurrentSolutionAsync()).FullPath);
        
        string path = dirInfo.FullName;

        while (!Directory.Exists(Path.Combine(path, ".git")))
        {
            path = Path.GetFullPath(Path.Combine(path, ".."));
        }

        var stdOutBuffer = new StringBuilder();
        var stdErrBuffer = new StringBuilder();
        var remoteUri = string.Empty;

        try
        {
            using (var repo = new Repository(path))
            {
                var headRemote = repo.Head.RemoteName;
                var remote = repo.Network.Remotes.FirstOrDefault(r => r.Name == headRemote);
                remoteUri = remote.Url;
            }
            _ = Process.Start(remoteUri);
        } 
        catch (Exception ex) 
        {
            var argError = new UriFormatException(ex.Message);
            await argError.LogAsync();
        }
    }
}
