using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.System
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

		/// <summary>
		/// Set's the owned <see cref="global::System.Diagnostics.Process.PriorityClass"/> to <see cref="global::System.Diagnostics.ProcessPriorityClass.AboveNormal"/>
		/// </summary>
		void SetHighPriority();
	}
}
