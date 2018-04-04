using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Metadata about a server instance
	/// </summary>
	[Model(typeof(InstanceManagerRights), CanCrud = true, CanList = true)]
	public sealed class Instance
	{
		/// <summary>
		/// The id of the <see cref="Instance"/>. Not modifiable
		/// </summary>
		[Permissions(DenyWrite = true)]
		public long Id { get; set; }

		/// <summary>
		/// The name of the <see cref="Instance"/>
		/// </summary>
		[Permissions(WriteRight = InstanceManagerRights.Rename)]
		public string Name { get; set; }

		/// <summary>
		/// The path to where the <see cref="Instance"/> is located
		/// </summary>
		[Permissions(WriteRight = InstanceManagerRights.Relocate)]
		public string Path { get; set; }

		/// <summary>
		/// If the <see cref="Instance"/> is online
		/// </summary>
		[Permissions(ComplexWrite = true)]
		public bool Online { get; set; }
	}
}
