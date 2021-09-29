using System;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Response
{
	/// <inheritdoc />
	public sealed class SwarmServerResponse : SwarmServer
	{
		/// <summary>
		/// If the <see cref="SwarmServerResponse"/> is the controller.
		/// </summary>
		public bool Controller { get; set; }

		/// <summary>
		/// See <see cref="SwarmServer.Address"/>.
		/// </summary>
		public new Uri Address
		{
			get => base.Address ?? throw new InvalidOperationException("Address was null!");
			set => base.Address = value;
		}

		/// <summary>
		/// See <see cref="SwarmServer.Identifier"/>.
		/// </summary>
		public new string Identifier
		{
			get => base.Identifier ?? throw new InvalidOperationException("Identifier was null!");
			set => base.Identifier = value;
		}
	}
}
