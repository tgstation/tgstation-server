using TGServiceInterface;

namespace TGCommandLine
{
	abstract class ConsoleCommand : Command
	{
		/// <summary>
		/// The <see cref="TGServiceInterface.Interface"/> currently in use by the <see cref="Program"/>
		/// </summary>
		public static Interface Interface;
	}
}
