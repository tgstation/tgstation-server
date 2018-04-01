using System.Threading.Tasks;

namespace Tgstation.Server.Host.Console
{
	/// <summary>
	/// Contains the entrypoint for the application
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// Entrypoint for the application
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		public static Task Main()
		{
			System.Console.WriteLine("Hello world!");
			return Task.CompletedTask;
		}
	}
}
