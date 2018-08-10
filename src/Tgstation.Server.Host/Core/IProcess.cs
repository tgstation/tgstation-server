using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Abstraction over a <see cref="System.Diagnostics.Process"/>
	/// </summary>
	interface IProcess : IProcessBase
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
	}
}