using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Metadata about a server instance.
	/// </summary>
	public abstract class Instance : NamedEntity
	{
		/// <summary>
		/// The path to where the <see cref="Instance"/> is located. Can only be changed while the <see cref="Instance"/> is offline. Must not exist when the instance is created.
		/// </summary>
		[Required]
		[RequestOptions(FieldPresence.Required, PutOnly = true)]
		public string? Path { get; set; }

		/// <summary>
		/// If the <see cref="Instance"/> is online.
		/// </summary>
		[Required]
		[RequestOptions(FieldPresence.Ignored, PutOnly = true)]
		public bool? Online { get; set; }

		/// <summary>
		/// If <see cref="IConfigurationFile"/>s can be used on the <see cref="Instance"/>.
		/// </summary>
		[Required]
		[EnumDataType(typeof(ConfigurationType))]
		public ConfigurationType? ConfigurationType { get; set; }

		/// <summary>
		/// The time interval in minutes the repository is automatically pulled and compiles. 0 disables.
		/// </summary>
		/// <remarks>Auto-updates intervals start counting when set, TGS is started, or from the completion of the previous update. Incompatible with <see cref="AutoUpdateCron"/>.</remarks>
		[Required]
		public uint? AutoUpdateInterval { get; set; }

		/// <summary>
		/// A cron expression indicating when auto-updates should trigger. Must be a valid 6 part cron schedule (SECONDS MINUTES HOURS DAY/MONTH MONTH DAY/WEEK). Empty <see cref="string"/> disables.
		/// </summary>
		/// <remarks>Updates will not be triggered if the previous update is still running. Incompatible with <see cref="AutoUpdateInterval"/>.</remarks>
		[Required]
		[StringLength(Limits.MaximumStringLength)]
		public string? AutoUpdateCron { get; set; }

		/// <summary>
		/// The maximum number of chat bots the <see cref="Instance"/> may contain.
		/// </summary>
		[Required]
		public ushort? ChatBotLimit { get; set; }
	}
}
