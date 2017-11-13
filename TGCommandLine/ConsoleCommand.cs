using TGServiceInterface;

namespace TGCommandLine
{
	abstract class ConsoleCommand : Command
	{
		/// <summary>
		/// The <see cref="IServerInterface"/> currently in use by the <see cref="Program"/>
		/// </summary>
		public static IServerInterface Interface;
	}
}
