using System;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents an installed <see cref="ByondVersion"/>.
	/// </summary>
	public sealed class ByondResponse : ByondVersion
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ByondResponse"/> class.
		/// </summary>
		/// <param name="byondVersion">The <see cref="ByondVersion"/> to copy.</param>
		public ByondResponse(ByondVersion byondVersion)
			: base(byondVersion)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondResponse"/> class.
		/// </summary>
		[Obsolete("JSON constructor", true)]
		public ByondResponse()
		{
		}
	}
}
