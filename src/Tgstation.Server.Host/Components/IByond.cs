using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For managing the BYOND installation
	/// </summary>
	interface IByond
	{
		/// <summary>
		/// Change the current BYOND version
		/// </summary>
		/// <param name="version">The new <see cref="Version"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		Task ChangeVersion(Version version, CancellationToken cancellationToken);

		/// <summary>
		/// Get the currently installed BYOND version
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>The current BYOND version</returns>
		Task<Version> GetVersion(CancellationToken cancellationToken);

		/// <summary>
		/// Lock the current installation's location and run an <paramref name="operation"/>
		/// </summary>
		/// <typeparam name="T">The return type of <paramref name="operation"/></typeparam>
		/// <param name="operation">A <see cref="Func{T1, T2, TResult}"/> taking the path to either dm.exe and dreamdaemon.exe and returning a <see cref="Task"/></param>
		/// <param name="stagedIfExists">Use the staged installation if possible</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the return type of <paramref name="operation"/></returns>
		Task<T> UseExecutables<T>(Func<string, string, Task<T>> operation, bool stagedIfExists);

		/// <summary>
		/// Clears the cache folder
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task ClearCache(CancellationToken cancellationToken);
	}
}