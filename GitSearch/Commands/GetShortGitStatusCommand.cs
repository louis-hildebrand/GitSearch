using GitSearch.Models;
using System.Linq;
using System.Management.Automation;

namespace GitSearch.Commands
{
	[Cmdlet(VerbsCommon.Get, "ShortGitStatus")]
	[OutputType(typeof(ShortRepoStatus))]
	public class GetShortGitStatusCommand : GetGitStatusCommand
	{
		protected override void ProcessRecord()
		{
			var ps = PowerShell.Create();

			var repoStatus = new ShortRepoStatus
			{
				FullPath = Path
			};

			// Move into the repo
			ps.AddCommand("Push-Location")
				.AddArgument(Path)
				.Invoke();

			RefreshIndex(ps);

			repoStatus.Branch = GetCurrentBranch(ps);

			repoStatus.LocalChanges = CheckLocalChanges(ps);

			repoStatus.RemoteChanges = CheckRemoteChanges(ps);

			// Output result and return to starting directory
			ps.AddCommand("Pop-Location")
				.Invoke();

			WriteObject(repoStatus);
		}

		private bool CheckLocalChanges(PowerShell ps)
		{
			// Check for files that are untracked, deleted, modified, or unmerged
			var localChanges = ps
				.AddCommand("git")
				.AddArgument("ls-files")
				.AddArgument("--others")
				.AddArgument("--deleted")
				.AddArgument("--modified")
				.AddArgument("--unmerged")
				.AddArgument("--exclude-standard")
				.Invoke()
				.Any();

			if (localChanges)
			{
				return true;
			}
			
			// Check for files that are staged but not yet committed
			return ps
				.AddCommand("git")
				.AddArgument("diff")
				.AddArgument("--name-only")
				.AddArgument("--cached")
				.Invoke()
				.Any();
		}

		private bool? CheckRemoteChanges(PowerShell ps)
		{
			var remoteName = GetRemoteName(ps);

			if (string.IsNullOrEmpty(remoteName))
			{
				return null;
			}

			return ps
				.AddCommand("git")
				.AddArgument("diff")
				.AddArgument("HEAD")
				.AddArgument(remoteName)
				.Invoke()
				.Any();
		}
	}
}
