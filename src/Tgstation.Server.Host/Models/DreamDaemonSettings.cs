using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class DreamDaemonSettings : Api.Models.Internal.DreamDaemonSettings
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The PID of a currently running DD instance
		/// </summary>
		public int? ProcessId { get; set; }

		/// <summary>
		/// The access token used for communication with DD
		/// </summary>
		public string AccessToken { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.Instance.Id"/>
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The parent <see cref="Models.Instance"/>
		/// </summary>
		[Required]
		public Instance Instance { get; set; }
	}
}
