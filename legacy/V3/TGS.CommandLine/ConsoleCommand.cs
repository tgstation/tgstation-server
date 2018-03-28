using TGS.Interface;

namespace TGS.CommandLine
{
	abstract class ConsoleCommand : Command
	{
		/// <summary>
		/// The <see cref="IServer"/> currently in use by the <see cref="Program"/>
		/// </summary>
		public static IServer Server;
		/// <summary>
		/// The <see cref="IInstance"/> currently in use by the <see cref="Program"/>
		/// </summary>
		public static IInstance Instance;
	}
}
