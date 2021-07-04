﻿using System.Diagnostics;
using System.Text;

namespace GitSearch.Utility
{
	public class GitService : IGitService
	{
		private StringBuilder Output;

		public void Call(string arguments)
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = "git",
				Arguments = arguments,
				CreateNoWindow = true,
				UseShellExecute = false
			};
			var process = Process.Start(startInfo);
			process.WaitForExit();
		}

		public string CallWithOutput(string arguments)
		{
			Output = new StringBuilder();

			var startInfo = new ProcessStartInfo
			{
				FileName = "git",
				Arguments = arguments,
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true
			};
			var process = new Process
			{
				StartInfo = startInfo
			};
			process.OutputDataReceived += (sender, args) => Output.Append(args.Data);

			process.Start();
			process.BeginOutputReadLine();
			process.WaitForExit();

			return Output.ToString();
		}
	}
}
