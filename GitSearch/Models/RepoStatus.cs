using System.IO;

namespace GitSearch.Models
{
	class RepoStatus
	{
		private DirectoryInfo directory;

		public string Name 
		{
			get { return directory.Name; } 
		}

		public string FullPath
		{
			get { return directory.FullName; }
			set { directory = new DirectoryInfo(value); }
		}

		public bool DetachedHead { get; set; }

		public string Branch { get; set; }

		public bool LocalChanges { get; set; }

		public bool UnpushedChanges { get; set; }

		public bool RemoteChanges { get; set; }
	}
}
