using System.Runtime.Serialization;

namespace TGS.Interface
{
	/// <summary>
	/// Metadata about an <see cref="Components.ITGInstance"/>
	/// </summary>
	[DataContract]
	public sealed class InstanceMetadata
	{
		/// <summary>
		/// The name of the <see cref="Components.ITGInstance"/>
		/// </summary>
		[DataMember]
		public string Name { get; set; }
		/// <summary>
		/// The path of the <see cref="Components.ITGInstance"/>
		/// </summary>
		[DataMember]
		public string Path { get; set; }
		/// <summary>
		/// Whether or not the <see cref="Components.ITGInstance"/> is enabled
		/// </summary>
		[DataMember]
		public bool Enabled { get; set; }
		/// <summary>
		/// The logging ID of the <see cref="Components.ITGInstance"/>. Will be 0 if <see cref="Enabled"/> is <see langword="false"/>
		/// </summary>
		[DataMember]
		public byte LoggingID { get; set; }
	}
}
