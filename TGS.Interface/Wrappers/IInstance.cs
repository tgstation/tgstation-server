using TGS.Interface.Components;

namespace TGS.Interface.Wrappers
{
	/// <summary>
	/// Wrapper for <see cref="ITGInstance"/> components
	/// </summary>
	public interface IInstance : ITGInstance
	{
		/// <summary>
		/// Get the <see cref="InstanceMetadata"/> for the <see cref="IInstance"/>
		/// </summary>
		InstanceMetadata Metadata { get; }

		/// <summary>
		/// The <see cref="ITGAdministration"/> component. Will be <see langword="null"/> if the connected user is not an administrator of the <see cref="IInstance"/>
		/// </summary>
		ITGAdministration Administration { get; }

		/// <summary>
		/// The <see cref="ITGByond"/> component
		/// </summary>
		ITGByond Byond { get; }

		/// <summary>
		/// The <see cref="ITGChat"/> component
		/// </summary>
		ITGChat Chat { get; }

		/// <summary>
		/// The <see cref="ITGCompiler"/> component
		/// </summary>
		ITGCompiler Compiler { get; }

		/// <summary>
		/// The <see cref="ITGConfig"/> component
		/// </summary>
		ITGConfig Config { get; }

		/// <summary>
		/// The <see cref="ITGDreamDaemon"/> component
		/// </summary>
		ITGDreamDaemon DreamDaemon { get; }

		/// <summary>
		/// The <see cref="ITGInterop"/> component
		/// </summary>
		ITGInterop Interop { get; }

		/// <summary>
		/// The <see cref="ITGRepository"/> component
		/// </summary>
		ITGRepository Repository { get; }
	}
}
