using System;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// A request to install a <see cref="ByondVersion"/>.
	/// </summary>
	public sealed class ByondVersionRequest : ByondVersion
	{
		/// <summary>
		/// If a custom BYOND version is to be uploaded.
		/// </summary>
		public bool? UploadCustomZip { get; set; }

		/// <summary>
		/// The remote repository for non-<see cref="EngineType.Byond"/> <see cref="EngineType"/>s. By default, this is the original git repository of the target <see cref="EngineType"/>.
		/// </summary>
		public Uri? SourceRepository { get; set; }
	}
}
