using GitSearch.Models;
using GitSearch.Utility;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;

namespace GitSearch.Commands
{
	[Cmdlet(VerbsCommon.Get, "ShortGitStatus")]
	[OutputType(typeof(ShortRepoStatus))]
	public class GetShortGitStatusCommand : GetGitStatusCommand
	{
		private PowerShell PowerShell { get; } = PowerShell.Create();

		private IGitService GitService { get; } = new GitService();

		protected override void ProcessRecord()
		{
			var repoStatus = new ShortRepoStatus
			{
				FullPath = Path
			};

			// Move into the repo
			PowerShell.AddCommand("Push-Location")
				.AddArgument(Path)
				.Invoke();

			RefreshIndex(PowerShell);

			repoStatus.Branch = GetCurrentBranch(PowerShell);

			repoStatus.LocalChanges = CheckLocalChanges();

			repoStatus.RemoteChanges = CheckRemoteChanges();

			// Output result and return to starting directory
			PowerShell.AddCommand("Pop-Location")
				.Invoke();

			WriteObject(repoStatus);
		}

		private bool CheckLocalChanges()
		{
			// Check for files that are untracked, deleted, modified, or unmerged
			var localChanges = GitService.CallWithOutput("ls-files " +
				"--others --deleted --modified --unmerged --exclude standard "
			);
			if (!string.IsNullOrEmpty(localChanges))
				return true;

			// Check for files that are staged but not yet committed
			var stagedFiles = GitService.CallWithOutput("diff --name-only --cached");
			return !string.IsNullOrEmpty(stagedFiles);
		}

		private bool? CheckRemoteChanges()
		{
			var remoteName = GetRemoteName(PowerShell);

			if (string.IsNullOrEmpty(remoteName))
				return null;

			var diffOutput = GitService.CallWithOutput($"diff HEAD {remoteName}");

			return !string.IsNullOrEmpty(diffOutput);
		}
	}
}
