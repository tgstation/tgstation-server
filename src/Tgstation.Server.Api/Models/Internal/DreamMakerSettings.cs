using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Configurable settings for <see cref="DreamMaker"/>
	/// </summary>
	[Model(RightsType.DreamMaker, ReadRight = DreamMakerRights.Read, CanCrud = true, RequiresInstance = true)]
	public class DreamMakerSettings
	{
		/// <summary>
		/// The .dme file <see cref="DreamMakerSettings"/> tries to compile with without the extension
		/// </summary>
		[Permissions(WriteRight = DreamMakerRights.SetDme)]
		public string ProjectName { get; set; }
	}
}
