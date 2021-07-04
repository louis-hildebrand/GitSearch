using GitSearch.Utility;
using System.IO;
using System.Management.Automation;

namespace GitSearch.Commands
{
	public abstract class GetGitStatusCommand : PSCmdlet
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
			set
			{
				path = value;
				GitService = new GitService(Path);
			}
		}
		private string path;

		protected IGitService GitService { get; set; }

		protected override void BeginProcessing()
		{
			base.BeginProcessing();

			var currentDirectory = ((PathInfo)GetVariableValue("pwd")).ToString();
			Directory.SetCurrentDirectory(currentDirectory);

			if (GitService == null)
				GitService = new GitService(Path);
		}

		protected override abstract void ProcessRecord();

		protected void RefreshIndex()
		{
			GitService.Call("update-index --refresh");
		}

		protected string GetCurrentBranch()
		{
			return GitService.CallWithOutput("rev-parse " +
				"--symbolic-full-name --abbrev-ref HEAD");
		}

		protected string GetRemoteName()
		{
			var refName = GitService.CallWithOutput("symbolic-ref --quiet HEAD");

			return GitService.CallWithOutput("for-each-ref " +
				$"{refName} --format=%(upstream:short)");
		}
	}
}
