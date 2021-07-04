using System.IO;

namespace GitSearch.Models
{
	public class LongRepoStatus : RepoStatus
	{
		public bool HasUntracked
		{
			get { return hasUntracked; }
			set { hasUntracked = value; }
		}
		private bool hasUntracked;

		public bool HasModified
		{
			get { return hasModified; }
			set { hasModified = value; }
		}
		private bool hasModified;

		public bool HasStaged
		{
			get { return hasStaged; }
			set { hasStaged = value; }
		}
		private bool hasStaged;

		public bool HasUnmerged
		{
			get { return hasUnmerged; }
			set { hasUnmerged = value; }
		}
		private bool hasUnmerged;

		public int? LocalCommits
		{
			get { return localCommits; }
			set { localCommits = value; }
		}
		private int? localCommits;

		public int? RemoteCommits
		{
			get { return remoteCommits; }
			set { remoteCommits = value; }
		}
		private int? remoteCommits;

		public override bool IsUpToDate
		{
			get
			{
				return !hasUntracked
					&& !hasModified
					&& !hasUnmerged
					&& !hasStaged
					&& (localCommits?.Equals(0) ?? true)
					&& (remoteCommits?.Equals(0) ?? true);
			}
		}
	}
}
