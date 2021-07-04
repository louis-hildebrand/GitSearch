using System.IO;

namespace GitSearch.Models
{
	public abstract class RepoStatus
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

		public string Branch { get; set; }
		
		public bool DetachedHead
		{ 
			get { return "HEAD".Equals(Branch); }
		}

		public abstract bool IsUpToDate { get; }
	}
}
