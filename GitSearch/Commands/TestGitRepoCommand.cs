using GitSearch.Utility;
using System.IO;
using System.Management.Automation;

namespace GitSearch.Commands
{
	[Cmdlet(VerbsDiagnostic.Test, "GitRepo")]
	[OutputType(typeof(bool))]
	public class TestGitRepoCommand : PSCmdlet
	{
		[Parameter(Position = 0, ValueFromPipeline = true)]
		[Alias("FullName")]
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

		[Parameter]
		public SwitchParameter Fast
		{
			get { return fast; }
			set { fast = value; }
		}
		private bool fast;

		private IGitService GitService { get; set; }

		protected override void BeginProcessing()
		{
			base.BeginProcessing();

			var currentDirectory = ((PathInfo)GetVariableValue("pwd")).ToString();
			Directory.SetCurrentDirectory(currentDirectory);

			if (GitService == null)
				GitService = new GitService(Path);
		}

		protected override void ProcessRecord()
		{
			bool isGitRepo = IsGitRepo();

			WriteObject(isGitRepo);
		}

		public bool IsGitRepo()
		{
			if (!Directory.Exists(Path))
				return false;

			// Only check for the presence of a .git repo
			if (fast)
			{
				var directory = new DirectoryInfo(Path);

				while (true)
				{
					var gitFolderPath = directory.FullName + System.IO.Path.DirectorySeparatorChar + ".git";

					WriteDebug($"Looking for .git folder at {gitFolderPath}");

					if (Directory.Exists(gitFolderPath))
						return true;

					directory = directory.Parent;
					if (directory == null)
						return false;
				}
			}
			// Call git rev-parse --git-dir
			else
			{
				var gitFolder = GitService.CallWithOutput("rev-parse --git-dir");

				return !string.IsNullOrEmpty(gitFolder);
			}
		}
	}
}
