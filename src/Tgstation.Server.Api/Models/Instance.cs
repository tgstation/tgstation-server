using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Metadata about a server instance
	/// </summary>
	public class Instance
	{
		/// <summary>
		/// The id of the <see cref="Instance"/>. Not modifiable
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The name of the <see cref="Instance"/>
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumStringLength)]
		public string? Name { get; set; }

		/// <summary>
		/// The path to where the <see cref="Instance"/> is located. Can only be changed while the <see cref="Instance"/> is offline. Must not exist when the instance is created
		/// </summary>
		[Required]
		public string? Path { get; set; }

		/// <summary>
		/// If the <see cref="Instance"/> is online
		/// </summary>
		[Required]
		public bool? Online { get; set; }

		/// <summary>
		/// If <see cref="ConfigurationFile"/> can be used on the <see cref="Instance"/>
		/// </summary>
		[Required]
		[EnumDataType(typeof(ConfigurationType))]
		public ConfigurationType? ConfigurationType { get; set; }

		/// <summary>
		/// The time interval in minutes the repository is automatically pulled and compiles. 0 disables
		/// </summary>
		[Required]
		public uint? AutoUpdateInterval { get; set; }

		/// <summary>
		/// The maximum number of <see cref="ChatBot"/>s the <see cref="Instance"/> may contain.
		/// </summary>
		[Required]
		public ushort? ChatBotLimit { get; set; }

		/// <summary>
		/// The <see cref="Job"/> representing a change of <see cref="Path"/>
		/// </summary>
		/// <remarks>Due to how <see cref="Job"/>s are children of <see cref="Instance"/>s but moving one requires the <see cref="Instance"/> to be offline, interactions with this <see cref="Job"/> are performed in a non-standard fashion. The <see cref="Job"/> is read by querying the <see cref="Instance"/> again (either via list or ID lookup) and cancelled by making any sort of update to the <see cref="Instance"/>. Once the <see cref="Instance"/> comes back <see cref="Online"/> it can be queried like a normal job</remarks>
		[NotMapped]
		public Job? MoveJob { get; set; }

		/// <summary>
		/// Create a clone of the essential <see cref="Instance"/> metadata
		/// </summary>
		/// <returns>A clone of the essential <see cref="Instance"/> metadata</returns>
		public Instance CloneMetadata() => new Instance
		{
			Id = Id,
			Name = Name,
			Path = Path,
			Online = Online,
			ConfigurationType = ConfigurationType,
			AutoUpdateInterval = AutoUpdateInterval
		};
	}
}
