namespace TGS.Server.Service
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
		/// The <see cref="IServiceRunner"/> for the <see cref="Program"/>
		/// </summary>
		static readonly IServiceRunner ServiceRunner = new ServiceRunner();

		/// <summary>
		/// The entry point for the program. Calls <see cref="ServiceBase.Run(ServiceBase)"/> with a new <see cref="Service"/> as a parameter
		/// </summary>
		public static void Main()
		{
			using (var S = new Service(ServerFactory))
				ServiceRunner.Run(S);
		}
	}
}
