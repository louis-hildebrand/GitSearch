using GitSearch.Models;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace GitSearch.Commands
{
	[Cmdlet(VerbsCommon.Get, "GitStatus")]
	[OutputType(typeof(string))]
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

			var repoStatus = new RepoStatus
			{
				FullPath = Path
			};

			// Move into the repo
			ps.AddCommand("Push-Location")
				.AddArgument(Path)
				.Invoke();

			// Refresh index
			ps.AddCommand("git")
				.AddParameter("update-index")
				.AddParameter("--refresh")
				.Invoke();

			// Get current branch
			var branchPSObject = ps
				.AddCommand("git")
				.AddArgument("rev-parse")
				.AddArgument("--symbolic-full-name")
				.AddArgument("--abbrev-ref")
				.AddArgument("HEAD")
				.Invoke()
				.First();

			repoStatus.Branch = (string)branchPSObject.BaseObject;

			// Check for untracked files
			repoStatus.HasUntracked = ps
				.AddCommand("git")
				.AddArgument("ls-files")
				.AddArgument("--others")
				.AddArgument("--exclude-standard")
				.Invoke()
				.Any();

			// Check for deleted or modified files
			repoStatus.HasModified = ps
				.AddCommand("git")
				.AddArgument("ls-files")
				.AddArgument("--deleted")
				.AddArgument("--modified")
				.AddArgument("--exclude-standard")
				.Invoke()
				.Any();

			// Check for unmerged files
			repoStatus.HasUnmerged = ps
				.AddCommand("git")
				.AddArgument("ls-files")
				.AddArgument("--unmerged")
				.Invoke()
				.Any();

			// Check for staged files
			repoStatus.HasStaged = ps
				.AddCommand("git")
				.AddArgument("diff")
				.AddArgument("--name-only")
				.AddArgument("--cached")
				.Invoke()
				.Any();

			// Check for differences between the remote and local repos
			var remoteNameObject = ps
				.AddCommand("git")
				.AddArgument("for-each-ref")
				.AddArgument("--format=%(upstream:short)")
				.Invoke()
				.FirstOrDefault();
			if (!repoStatus.DetachedHead && remoteNameObject != null)
			{
				var remoteName = (string)remoteNameObject.BaseObject;

				// TODO Look for local changes that aren't present in the remote
				var localCommits = ps
					.AddCommand("git")
					.AddArgument("rev-list")
					.AddArgument("--count")
					.AddArgument($"{remoteName}..HEAD")
					.Invoke()
					.First();
				repoStatus.LocalCommits = int.Parse((string)localCommits.BaseObject);

				// TODO Look for changes in the remote that aren't present locally
				var remoteCommits = ps
					.AddCommand("git")
					.AddArgument("rev-list")
					.AddArgument("--count")
					.AddArgument($"HEAD..{remoteName}")
					.Invoke()
					.First();
				repoStatus.RemoteCommits = int.Parse((string)remoteCommits.BaseObject);
			}

			WriteObject(repoStatus);

			ps.AddCommand("Pop-Location")
				.Invoke();
		}
	}
}
