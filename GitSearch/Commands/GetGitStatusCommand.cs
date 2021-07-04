using GitSearch.Models;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
			set { path = value; }
		}
		private string path;

		protected override void BeginProcessing()
		{
			base.BeginProcessing();

			var currentDirectory = ((PathInfo)GetVariableValue("pwd")).ToString();
			Directory.SetCurrentDirectory(currentDirectory);
		}

		protected override abstract void ProcessRecord();

		protected void RefreshIndex(PowerShell ps)
		{
			ps.AddCommand("git")
				.AddArgument("update-index")
				.AddArgument("--refresh")
				.Invoke();
		}

		protected string GetCurrentBranch(PowerShell ps)
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

		protected string GetRemoteName(PowerShell ps)
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
	}
}
