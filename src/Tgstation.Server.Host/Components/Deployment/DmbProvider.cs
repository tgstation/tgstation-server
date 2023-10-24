using System;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <inheritdoc />
	sealed class DmbProvider : DmbProviderBase, IDmbProvider
	{
		/// <inheritdoc />
		public override string Directory => ioManager.ResolvePath(CompileJob.DirectoryName.ToString() + directoryAppend);

		/// <inheritdoc />
		public override Models.CompileJob CompileJob { get; }

		/// <inheritdoc />
		public override EngineVersion EngineVersion { get; }

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="DmbProvider"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// Extra path to add to the end of <see cref="CompileJob.DirectoryName"/>.
		/// </summary>
		readonly string directoryAppend;

		/// <summary>
		/// The <see cref="Action"/> to run when <see cref="DisposeAsync"/> is called.
		/// </summary>
		Action onDispose;

		/// <summary>
		/// Initializes a new instance of the <see cref="DmbProvider"/> class.
		/// </summary>
		/// <param name="compileJob">The value of <see cref="CompileJob"/>.</param>
		/// <param name="engineVersion">The value of <see cref="EngineVersion"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="onDispose">The value of <see cref="onDispose"/>.</param>
		/// <param name="directoryAppend">The optional value of <see cref="directoryAppend"/>.</param>
		public DmbProvider(Models.CompileJob compileJob, EngineVersion engineVersion, IIOManager ioManager, Action onDispose, string directoryAppend = null)
		{
			CompileJob = compileJob ?? throw new ArgumentNullException(nameof(compileJob));
			EngineVersion = engineVersion ?? throw new ArgumentNullException(nameof(engineVersion));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
			this.directoryAppend = directoryAppend ?? String.Empty;
		}

		/// <inheritdoc />
		public override ValueTask DisposeAsync()
		{
			onDispose?.Invoke();
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc />
		public override void KeepAlive() => onDispose = null;
	}
}
