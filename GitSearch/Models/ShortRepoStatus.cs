namespace GitSearch.Models
{
	public class ShortRepoStatus : RepoStatus
	{
		public bool LocalChanges
		{
			get { return localChanges; }
			set { localChanges = value; }
		}
		private bool localChanges;

		public bool? RemoteChanges
		{
			get { return remoteChanges; }
			set { remoteChanges = value; }
		}
		private bool? remoteChanges;

		public override bool IsUpToDate
		{
			get
			{
				return !localChanges
					&& remoteChanges != true;
			}
		}
	}
}
