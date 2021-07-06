using GitSearch.Models;
using GitSearch.Utility;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace GitSearch.Commands
{
	[Cmdlet(VerbsCommon.Get, "GitStatus")]
	[OutputType(typeof(RepoStatus))]
	public class GetGitStatusCommand : PSCmdlet
	{
		#region Parameters

		[Parameter(Position = 0, ValueFromPipeline = true)]
		[Alias("FullName")]
		public string[] Path { get; set; }

		[Parameter]
		public SwitchParameter Fetch
		{
			get { return fetch; }
			set { fetch = value; }
		}
		private bool fetch;

		[Parameter]
		[Alias("Full")]
		public SwitchParameter FullStatus
		{
			get { return fullStatus; }
			set { fullStatus = value; }
		}
		private bool fullStatus;

		#endregion

		#region Services

		protected IGitService GitService { get; set; }

		#endregion

		#region Override

		protected override void BeginProcessing()
		{
			base.BeginProcessing();

			var currentDirectory = ((PathInfo)GetVariableValue("pwd")).ToString();
			Directory.SetCurrentDirectory(currentDirectory);
		}

		protected override void ProcessRecord()
		{
			var repos = new HashSet<string>();

			foreach (var pattern in Path)
			{
				var matches = InvokeProvider.ChildItem
					.GetNames(pattern, ReturnContainers.ReturnMatchingContainers, false)
					.Where(x => new TestGitRepoCommand { Path = x }.IsGitRepo());

				repos.UnionWith(matches);
			}

			foreach (var dir in repos)
			{
				var repoStatus = GetRepoStatus(dir);

				WriteObject(repoStatus);
			}
		}

		#endregion Override

		#region General helper methods

		private RepoStatus GetRepoStatus(string path)
		{
			GitService = new GitService(path);

			if (fullStatus)
				return GetLongRepoStatus(path);
			else
				return GetShortRepoStatus(path);
		}

		private bool IsGitRepo(string path)
		{
			var testGitRepoCmdlet = new TestGitRepoCommand
			{
				Path = path
			};

			return testGitRepoCmdlet.Invoke<bool>().First();
		}

		protected string GetCurrentBranch()
		{
			return GitService.CallWithOutput("rev-parse " +
				"--symbolic-full-name --abbrev-ref HEAD");
		}

		protected string GetRemoteBranchName()
		{
			var refName = GitService.CallWithOutput("symbolic-ref --quiet HEAD");

			return GitService.CallWithOutput("for-each-ref " +
				$"{refName} --format=%(upstream:short)");
		}

		protected void FetchRemoteBranch(string remoteBranchName)
		{
			if (!fetch)
				return;

			var slashIndex = remoteBranchName.IndexOf('/');
			var remoteName = remoteBranchName.Substring(0, slashIndex);
			var branchName = remoteBranchName.Substring(slashIndex + 1);

			GitService.Call($"fetch {remoteName} {branchName}");
		}

		#endregion

		#region Short status helper methods

		private ShortRepoStatus GetShortRepoStatus(string path)
		{
			var repoStatus = new ShortRepoStatus
			{
				FullPath = path
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

		#endregion

		#region Long status helper methods

		private LongRepoStatus GetLongRepoStatus(string path)
		{
			var repoStatus = new LongRepoStatus
			{
				FullPath = path
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

			return repoStatus;
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

		#endregion
	}
}
