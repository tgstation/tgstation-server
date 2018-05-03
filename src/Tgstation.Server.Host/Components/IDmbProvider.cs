using System;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Provides absolute paths to the latest compiled .dmbs
	/// </summary>
    interface IDmbProvider : IDisposable
    {
		/// <summary>
		/// The file name of the .dmb
		/// </summary>
		string DmbName { get; }

		/// <summary>
		/// The primary game directory with a trailing directory separator
		/// </summary>
		string PrimaryDirectory { get; }

		/// <summary>
		/// The secondary game directory with a trailing directory separator
		/// </summary>
		string SecondaryDirectory { get; }

		/// <summary>
		/// The <see cref="Host.Models.RevisionInformation"/> of the .dmb
		/// </summary>
		RevisionInformation RevisionInformation { get; }
	}
}
