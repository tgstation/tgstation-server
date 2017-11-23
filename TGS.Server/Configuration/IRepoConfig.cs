using System;
using System.Collections.Generic;

namespace TGS.Server.Configuration
{
	interface IRepoConfig: IEquatable<IRepoConfig>
	{
		/// <summary>
		/// If this json is setup to support <see cref="TGS.Interface.Components.ITGRepository.GenerateChangelog(out string)"/>
		/// </summary>
		bool ChangelogSupport { get; }
		/// <summary>
		/// Path to the repository's changelog generator script
		/// </summary>
		string PathToChangelogPy { get; }
		/// <summary>
		/// Arguments for the changelog gennerator script
		/// </summary>
		string ChangelogPyArguments { get; }
		/// <summary>
		/// List of python pip dependencies for the changelog generator script
		/// </summary>
		IReadOnlyList<string> PipDependancies { get; }
		/// <summary>
		/// Paths to commit and push to the remote repository
		/// </summary>
		IReadOnlyList<string> PathsToStage { get; }
		/// <summary>
		/// Directory's whose contents should not be touched when the <see cref="Components.IInstance"/> updates
		/// </summary>
		IReadOnlyList<string> StaticDirectoryPaths { get; }
		/// <summary>
		/// DLL's used by DreamDaemon call()() operations. Must be handled as symlinks to avoid lockups during update operations
		/// </summary>
		IReadOnlyList<string> DLLPaths { get; }
	}
}
