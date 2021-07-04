using GitSearch.Models;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace GitSearch.Commands
{
	[Cmdlet(VerbsCommon.Get, "GitStatus")]
	[OutputType(typeof(RepoStatus))]
	public class GetGitStatusCommand : PSCmdlet
	{
		[Parameter(
			Position = 0,
			ValueFromPipeline = true
		)]
		public string Path
		{
			get
			{
				return string.IsNullOrEmpty(path) ?
					Directory.GetCurrentDirectory() :
					System.IO.Path.GetFullPath(path);
			}
			set { path = value; }
		}
		private string path;

		protected override void BeginProcessing()
		{
			base.BeginProcessing();

			var currentDirectory = ((PathInfo)GetVariableValue("pwd")).ToString();
			Directory.SetCurrentDirectory(currentDirectory);
		}

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

			// Look for local changes
			repoStatus.Branch = GetCurrentBranch(ps);

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
			WriteObject(repoStatus);

			ps.AddCommand("Pop-Location")
				.Invoke();
		}

		private void RefreshIndex(PowerShell ps)
		{
			ps.AddCommand("git")
				.AddParameter("update-index")
				.AddParameter("--refresh")
				.Invoke();
		}

		private string GetCurrentBranch(PowerShell ps)
		{
			var branchPSObject = ps
				.AddCommand("git")
				.AddArgument("rev-parse")
				.AddArgument("--symbolic-full-name")
				.AddArgument("--abbrev-ref")
				.AddArgument("HEAD")
				.Invoke()
				.First();
			return (string)branchPSObject.BaseObject;
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

		private string GetRemoteName(PowerShell ps)
		{
			var refName = (string)ps
				.AddCommand("git")
				.AddArgument("symbolic-ref")
				.AddArgument("--quiet")
				.AddArgument("HEAD")
				.Invoke()
				.First()
				.BaseObject;
			var remoteNameObject = ps
				.AddCommand("git")
				.AddArgument("for-each-ref")
				.AddArgument(refName)
				.AddArgument("--format")
				.AddArgument("%(upstream:short)")
				.Invoke()
				.FirstOrDefault();

			if (remoteNameObject == null)
				return null;
			else
				return (string)remoteNameObject.BaseObject;
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
