using System.Net;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class Token : Internal.Token
	{
		/// <inheritdoc />
		new public IPAddress IssuedTo
		{
			get => new IPAddress(base.IssuedTo);
			set => base.IssuedTo = value.GetAddressBytes();
		}

		/// <inheritdoc />
		new public IPAddress LastUsedBy
		{
			get => new IPAddress(base.LastUsedBy);
			set => base.LastUsedBy = value.GetAddressBytes();
		}
	}
}
