using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Represents the host
	/// </summary>
	public interface IServer : IDisposable
	{
		/// <summary>
		/// Runs the <see cref="IServer"/>
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task RunAsync();
	}
}
