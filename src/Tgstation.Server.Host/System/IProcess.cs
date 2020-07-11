using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Abstraction over a <see cref="global::System.Diagnostics.Process"/>
	/// </summary>
	interface IProcess : IProcessBase, IDisposable
	{
		/// <summary>
		/// The <see cref="IProcess"/>' ID
		/// </summary>
		int Id { get; }

		/// <summary>
		/// The <see cref="Task"/> representing the time until the <see cref="IProcess"/> becomes "idle"
		/// </summary>
		Task Startup { get; }

		/// <summary>
		/// Get the stderr output of the <see cref="IProcess"/>
		/// </summary>
		/// <returns>The stderr output of the <see cref="IProcess"/></returns>
		string GetErrorOutput();

		/// <summary>
		/// Get the stdout output of the <see cref="IProcess"/>
		/// </summary>
		/// <returns>The stdout output of the <see cref="IProcess"/></returns>
		string GetStandardOutput();

		/// <summary>
		/// Get the stderr and stdout output of the <see cref="IProcess"/>
		/// </summary>
		/// <returns>The stderr and stdout output of the <see cref="IProcess"/></returns>
		string GetCombinedOutput();

		/// <summary>
		/// Terminates the process
		/// </summary>
		void Terminate();

		/// <summary>
		/// Get the name of the account executing the <see cref="IProcess"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the name of the account executing the <see cref="IProcess"/>.</returns>
		Task<string> GetExecutingUsername(CancellationToken cancellationToken);
	}
}