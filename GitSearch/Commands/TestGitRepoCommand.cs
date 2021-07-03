using System.IO;
using System.Management.Automation;

namespace GitSearch.Commands
{
	[Cmdlet(VerbsDiagnostic.Test, "GitRepo")]
	[OutputType(typeof(bool))]
	public class TestGitRepoCommand : PSCmdlet
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
			WriteDebug($"Starting search at {Path}");

			var directory = new DirectoryInfo(Path);

			if (!directory.Exists)
			{
				WriteObject(false);
				return;
			}

			while (true)
			{
				var gitFolderPath = directory.FullName + System.IO.Path.DirectorySeparatorChar + ".git";

				WriteDebug($"Looking for .git folder at {gitFolderPath}");

				if (Directory.Exists(gitFolderPath))
				{
					WriteObject(true);
					return;
				}

				directory = directory.Parent;
				if (directory == null)
				{
					WriteObject(false);
					return;
				}
			}
		}
	}
}
