using System;
using System.IO;
using System.Management.Automation;

namespace GitSearch
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
				return string.IsNullOrEmpty(path) ? GetCurrentDirectory() : path;
			}
			set
			{
				var currentDirectory = GetCurrentDirectory();
				path = string.IsNullOrEmpty(value) ? 
					currentDirectory : 
					System.IO.Path.GetFullPath(System.IO.Path.Combine(currentDirectory, value));
			}
		}
		private string path;

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

		private string GetCurrentDirectory()
		{
			return ((PathInfo)GetVariableValue("pwd")).ToString();
		}
	}
}
