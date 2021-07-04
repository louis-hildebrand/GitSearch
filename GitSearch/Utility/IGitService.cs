namespace GitSearch.Utility
{
	public interface IGitService
	{
		void Call(string arguments);

		string CallWithOutput(string arguments);
	}
}
