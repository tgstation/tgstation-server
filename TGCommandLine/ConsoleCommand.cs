using TGServiceInterface;

namespace TGCommandLine
{
	abstract class ConsoleCommand : Command
	{
		/// <summary>
		/// The <see cref="IInterface"/> currently in use by the <see cref="Program"/>
		/// </summary>
		public static IInterface Interface;
	}
}
