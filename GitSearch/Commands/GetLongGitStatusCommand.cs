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
			var repoStatus = new LongRepoStatus
			{
				FullPath = Path
			};

			repoStatus.Branch = GetCurrentBranch();

			// Look for local changes
			repoStatus.HasUntracked = CheckUntrackedFiles();

			repoStatus.HasModified = CheckModifiedFiles();

			repoStatus.HasUnmerged = CheckUnmergedFiles();

			repoStatus.HasStaged = CheckStagedFiles();

			// Look for differences between the local and remote repos
			var remoteBranchName = GetRemoteBranchName();
			if (!repoStatus.DetachedHead && !string.IsNullOrEmpty(remoteBranchName))
			{
				FetchRemoteBranch(remoteBranchName);

				repoStatus.LocalCommits = CountLocalCommits(remoteBranchName);

				repoStatus.RemoteCommits = CountRemoteCommits(remoteBranchName);
			}

			WriteObject(repoStatus);
		}

		private bool CheckUntrackedFiles()
		{
			var untrackedFiles = GitService.CallWithOutput("ls-files " +
				"--others --exclude-standard");

			return !string.IsNullOrEmpty(untrackedFiles);
		}

		private bool CheckModifiedFiles()
		{
			var modifiedFiles = GitService.CallWithOutput("ls-files " +
				"--deleted --modified --exclude-standard");

			return !string.IsNullOrEmpty(modifiedFiles);
		}

		private bool CheckUnmergedFiles()
		{
			var unmergedFiles = GitService.CallWithOutput("ls-files " +
				"--unmerged");

			return !string.IsNullOrEmpty(unmergedFiles);
		}

		private bool CheckStagedFiles()
		{
			var stagedFiles = GitService.CallWithOutput("ls-files " +
				"diff --name-only --cached");

			return !string.IsNullOrEmpty(stagedFiles);
		}

		private int CountLocalCommits(string remoteBranchName)
		{
			var numLocalCommits = GitService.CallWithOutput("rev-list " +
				$"--count {remoteBranchName}..HEAD");

			return int.Parse(numLocalCommits);
		}

		private int CountRemoteCommits(string remoteBranchName)
		{
			var numRemoteCommits = GitService.CallWithOutput("rev-list " +
				$"--count HEAD..{remoteBranchName}");

			return int.Parse(numRemoteCommits);
		}
	}
}
