using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	sealed class ServerSettings : Api.Models.Internal.ServerSettings
	{
		public long Id { get; set; }

		/// <summary>
		/// The value used for the <see cref="SymmetricSecurityKey"/> to encrypt JWTs
		/// </summary>
		[Required]
		public byte[] TokenSecret { get; set; }
	}
}
