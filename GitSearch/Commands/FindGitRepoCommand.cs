using GitSearch.Models;
using GitSearch.Utility;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace GitSearch.Commands
{
	[Cmdlet(VerbsCommon.Find, "GitRepo")]
	[OutputType(typeof(IEnumerable<RepoStatus>))]
	public class FindGitRepoCommand : PSCmdlet
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
			{ path = value; }
		}
		private string path;

		[Parameter]
		public SwitchParameter Short
		{
			get { return shortStatus; }
			set { shortStatus = value; }
		}
		private bool shortStatus;

		[Parameter]
		public SwitchParameter Recurse
		{
			get { return recurse; }
			set { recurse = value; }
		}
		private bool recurse;

		protected override void BeginProcessing()
		{
			base.BeginProcessing();

			var currentDirectory = ((PathInfo)GetVariableValue("pwd")).ToString();
			Directory.SetCurrentDirectory(currentDirectory);
		}

		protected override void ProcessRecord()
		{
			GetRepoStatuses(Path);
		}

		private IEnumerable<RepoStatus> GetRepoStatuses(string path)
		{
			var directories = Directory.GetDirectories(path);

			var repoStatuses = new List<RepoStatus>();

			foreach (var dir in directories)
			{
				var repoTestCmdlet = new TestGitRepoCommand
				{
					Path = dir
				};

				if (repoTestCmdlet.IsGitRepo())
				{
					GetGitStatusCommand repoStatusCmdlet;
					if (shortStatus)
					{
						repoStatusCmdlet = new GetShortGitStatusCommand
						{
							Path = dir
						};
					}
					else
					{
						repoStatusCmdlet = new GetLongGitStatusCommand
						{
							Path = dir
						};
					}

					var repoStatus = repoStatusCmdlet.GetRepoStatus();

					WriteObject(repoStatus);

					repoStatuses.Add(repoStatus);
				}
				else if (recurse)
				{
					var subdirRepoStatuses = GetRepoStatuses(dir);

					repoStatuses.AddRange(subdirRepoStatuses);
				}
			}

			return repoStatuses;
		}
	}
}
