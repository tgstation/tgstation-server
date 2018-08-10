using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Represents process lifetime
	/// </summary>
	interface IProcessBase : IDisposable
	{
		/// <summary>
		/// The <see cref="Task{TResult}"/> resulting in the exit code of the process
		/// </summary>
		Task<int> Lifetime { get; }
	}
}
