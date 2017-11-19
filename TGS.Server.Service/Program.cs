using System.ServiceProcess;

namespace TGS.Server.Service
{
	public static class Program
	{
		/// <summary>
		/// The entry point for the program. Calls <see cref="ServiceBase.Run(ServiceBase)"/> with a new <see cref="Service"/> as a parameter
		/// </summary>
		public static void Main()
		{
			using (var S = new Service(new ServerFactory(new IOManager())))
				ServiceBase.Run(S);
		}
	}
}
