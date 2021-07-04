using GitSearch.Models;
using System.Linq;
using System.Management.Automation;

namespace GitSearch.Commands
{
	[Cmdlet(VerbsCommon.Get, "GitStatus")]
	[OutputType(typeof(LongRepoStatus))]
	public class GetLongGitStatusCommand : GetGitStatusCommand
	{
		protected override void ProcessRecord()
		{
			var ps = PowerShell.Create();

			var repoStatus = new LongRepoStatus
			{
				FullPath = Path
			};

			// Move into the repo
			ps.AddCommand("Push-Location")
				.AddArgument(Path)
				.Invoke();

			RefreshIndex(ps);

			repoStatus.Branch = GetCurrentBranch(ps);

			// Look for local changes

			repoStatus.HasUntracked = CheckUntrackedFiles(ps);

			repoStatus.HasModified = CheckModifiedFiles(ps);

			repoStatus.HasUnmerged = CheckUnmergedFiles(ps);

			repoStatus.HasStaged = CheckStagedFiles(ps);

			// Look for differences between the local and remote repos
			var remoteName = GetRemoteName(ps);
			if (!repoStatus.DetachedHead && !string.IsNullOrEmpty(remoteName))
			{
				repoStatus.LocalCommits = CountLocalCommits(ps, remoteName);

				repoStatus.RemoteCommits = CountRemoteCommits(ps, remoteName);
			}

			// Output result and return to starting directory
			ps.AddCommand("Pop-Location")
				.Invoke();

			WriteObject(repoStatus);
		}

		private bool CheckUntrackedFiles(PowerShell ps)
		{
			return ps
				.AddCommand("git")
				.AddArgument("ls-files")
				.AddArgument("--others")
				.AddArgument("--exclude-standard")
				.Invoke()
				.Any();
		}

		private bool CheckModifiedFiles(PowerShell ps)
		{
			return ps
				.AddCommand("git")
				.AddArgument("ls-files")
				.AddArgument("--deleted")
				.AddArgument("--modified")
				.AddArgument("--exclude-standard")
				.Invoke()
				.Any();
		}

		private bool CheckUnmergedFiles(PowerShell ps)
		{
			return ps
				.AddCommand("git")
				.AddArgument("ls-files")
				.AddArgument("--unmerged")
				.Invoke()
				.Any();
		}

		private bool CheckStagedFiles(PowerShell ps)
		{
			return ps
				.AddCommand("git")
				.AddArgument("diff")
				.AddArgument("--name-only")
				.AddArgument("--cached")
				.Invoke()
				.Any();
		}

		private int CountLocalCommits(PowerShell ps, string remoteName)
		{
			var localCommits = ps
					.AddCommand("git")
					.AddArgument("rev-list")
					.AddArgument("--count")
					.AddArgument($"{remoteName}..HEAD")
					.Invoke()
					.First();
			return int.Parse((string)localCommits.BaseObject);
		}

		private int CountRemoteCommits(PowerShell ps, string remoteName)
		{
			var remoteCommits = ps
				.AddCommand("git")
				.AddArgument("rev-list")
				.AddArgument("--count")
				.AddArgument($"HEAD..{remoteName}")
				.Invoke()
				.First();
			return int.Parse((string)remoteCommits.BaseObject);
		}
	}
}
