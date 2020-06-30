using System;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// Provides absolute paths to the latest compiled .dmbs
	/// </summary>
	public interface IDmbProvider : IDisposable
	{
		/// <summary>
		/// The file name of the .dmb
		/// </summary>
		string DmbName { get; }

		/// <summary>
		/// The primary game directory with a trailing directory separator
		/// </summary>
		string Directory { get; }

		/// <summary>
		/// The <see cref="CompileJob"/> of the .dmb
		/// </summary>
		CompileJob CompileJob { get; }

		/// <summary>
		/// Disposing the <see cref="IDmbProvider"/> won't cause a cleanup of the working directory
		/// </summary>
		void KeepAlive();
	}
}
