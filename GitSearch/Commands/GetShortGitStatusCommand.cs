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
		public override RepoStatus GetRepoStatus()
		{
			var repoStatus = new ShortRepoStatus
			{
				FullPath = Path
			};

			repoStatus.Branch = GetCurrentBranch();

			repoStatus.LocalChanges = CheckLocalChanges();

			var remoteBranchName = GetRemoteBranchName();
			if (!string.IsNullOrEmpty(remoteBranchName))
			{
				FetchRemoteBranch(remoteBranchName);

				repoStatus.RemoteChanges = CheckRemoteChanges(remoteBranchName);
			}

			return repoStatus;
		}

		protected override void ProcessRecord()
		{
			var repoStatus = GetRepoStatus();

			WriteObject(repoStatus);
		}

		private bool CheckLocalChanges()
		{
			// Check for files that are untracked, deleted, modified, or unmerged
			var localChanges = GitService.CallWithOutput("ls-files " +
				"--others --deleted --modified --unmerged --exclude-standard "
			);
			if (!string.IsNullOrEmpty(localChanges))
				return true;

			// Check for files that are staged but not yet committed
			var stagedFiles = GitService.CallWithOutput("diff --name-only --cached");
			return !string.IsNullOrEmpty(stagedFiles);
		}

		private bool? CheckRemoteChanges(string remoteBranchName)
		{
			var diffOutput = GitService.CallWithOutput($"diff HEAD {remoteBranchName}");

			return !string.IsNullOrEmpty(diffOutput);
		}
	}
}
