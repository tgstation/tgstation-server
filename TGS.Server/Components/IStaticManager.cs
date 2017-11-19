using TGS.Interface.Components;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	interface IStaticManager : ITGStatic
	{
		/// <summary>
		/// Creates symlinks for all static files to a <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to create symlinks in</param>
		void SymlinkTo(string path);
	}
}
