namespace TGS.Server.Console
{
	public static class Program
	{
		/// <summary>
		/// The entry point for the program. Runs a new <see cref="Console"/>
		/// </summary>
		/// <param name="args">The arguments for the program</param>
		public static void Main(string[] args)
		{
			using (var C = new Console(new ServerFactory(new IOManager())))
				C.Run(args);
		}
	}
}