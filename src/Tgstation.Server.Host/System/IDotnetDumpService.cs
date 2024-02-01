using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Service for managing the dotnet-dump installation.
	/// </summary>
	public interface IDotnetDumpService
	{
		/// <summary>
		/// Attempt to install dotnet-dump if it is not installed.
		/// </summary>
		/// <param name="deploymentPipeline">If this operation is part of the deployment pipeline.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask EnsureInstalled(bool deploymentPipeline, CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to dump a given <paramref name="process"/>.
		/// </summary>
		/// <param name="process">The <see cref="IProcess"/> to dump.</param>
		/// <param name="outputFile">The path to the output dump file.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns><see langword="true"/> if the dump proceeded, <see langword="false"/> if dotnet-dump was not installed.</returns>
		ValueTask<bool> Dump(IProcess process, string outputFile, CancellationToken cancellationToken);
	}
}
