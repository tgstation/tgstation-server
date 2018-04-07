using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	public class DreamMakerSettings
	{
		/// <summary>
		/// How often the <see cref="DreamMakerSettings"/> automatically compiles in minutes
		/// </summary>
		[Permissions(WriteRight = DreamMakerRights.SetAutoCompile)]
		public int? AutoCompileInterval { get; set; }

		/// <summary>
		/// The .dme file <see cref="DreamMakerSettings"/> tries to compile with
		/// </summary>
		[Permissions(WriteRight = DreamMakerRights.SetDme)]
		public string TargetDme { get; set; }
	}
}
