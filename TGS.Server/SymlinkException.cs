using System;
using System.IO;

namespace TGS.Server
{
	/// <summary>
	/// <see cref="Exception"/> <see cref="Type"/> to be thrown when <see cref="NativeMethods.CreateSymbolicLink(string, string, NativeMethods.SymbolicLink)"/> fails
	/// </summary>
	sealed class SymlinkException : IOException
	{
		/// <summary>
		/// Construct a <see cref="SymlinkException"/>
		/// </summary>
		/// <param name="link">The link that was attempted to be created</param>
		/// <param name="target">The attempted symlink target</param>
		/// <param name="errorCode">The Win32 error code after the operation</param>
		public SymlinkException(string link, string target, int errorCode) : base(String.Format("Failed to create symlink from {0} to {1}! Error: {2}", target, link, errorCode)) { }
	}
}
