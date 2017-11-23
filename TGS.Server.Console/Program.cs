using TGS.Server.IO;

namespace TGS.Server.Console
{
	/// <summary>
	/// Entry class for the program
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// The <see cref="IServerFactory"/> for the <see cref="Program"/>
		/// </summary>
		static readonly IServerFactory ServerFactory = new ServerFactory(new IOManager());

		/// <summary>
		/// The entry point for the program. Runs a new <see cref="Console"/>
		/// </summary>
		/// <param name="args">The arguments for the program</param>
		public static void Main(string[] args)
		{
			using (var C = new Console(ServerFactory))
				C.Run(args);
		}
	}
}