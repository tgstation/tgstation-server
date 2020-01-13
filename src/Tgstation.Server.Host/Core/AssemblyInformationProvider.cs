using System.Reflection;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class AssemblyInformationProvider : IAssemblyInformationProvider
	{
		/// <inheritdoc />
		public string Path { get; }

		/// <inheritdoc />
		public AssemblyName Name { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AssemblyInformationProvider"/> <see langword="class"/>.
		/// </summary>
		public AssemblyInformationProvider()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			Path = assembly.Location;
			Name = assembly.GetName();
		}
	}
}
