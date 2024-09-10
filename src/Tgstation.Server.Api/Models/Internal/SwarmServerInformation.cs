using System;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents information about a running <see cref="SwarmServer"/>.
	/// </summary>
	public class SwarmServerInformation : SwarmServer, IEquatable<SwarmServerInformation>
	{
		/// <summary>
		/// If the <see cref="SwarmServerResponse"/> is the controller.
		/// </summary>
		public bool Controller { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmServerInformation"/> class.
		/// </summary>
		public SwarmServerInformation()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmServerInformation"/> class.
		/// </summary>
		/// <param name="copy">The <see cref="SwarmServerInformation"/> to copy.</param>
		public SwarmServerInformation(SwarmServerInformation copy)
			: base(copy)
		{
			Controller = copy.Controller;
		}

		/// <inheritdoc />
		public bool Equals(SwarmServerInformation other)
			=> base.Equals(other)
				&& other.Controller == Controller;
	}
}
