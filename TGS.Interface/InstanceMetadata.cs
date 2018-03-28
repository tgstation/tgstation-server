using System.Runtime.Serialization;

namespace TGS.Interface
{
	/// <summary>
	/// Metadata about an <see cref="Components.ITGInstance"/>
	/// </summary>
	//Namespace required for compatibility reasons
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/TGServiceInterface")]
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
	}
}
