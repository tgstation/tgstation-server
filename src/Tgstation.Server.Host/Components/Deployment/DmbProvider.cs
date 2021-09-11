using System;

using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <inheritdoc />
	sealed class DmbProvider : IDmbProvider
	{
		/// <inheritdoc />
		public string DmbName => String.Concat(CompileJob.DmeName, DreamMaker.DmbExtension);

		/// <inheritdoc />
		public string Directory { get; }

		/// <summary>
		/// The <see cref="CompileJob"/> for the <see cref="DmbProvider"/>.
		/// </summary>
		public CompileJob CompileJob { get; }

		/// <summary>
		/// The <see cref="Action"/> to run when <see cref="Dispose"/> is called.
		/// </summary>
		Action? onDispose;

		/// <summary>
		/// Initializes a new instance of the <see cref="DmbProvider"/> class.
		/// </summary>
		/// <param name="compileJob">The value of <see cref="CompileJob"/>.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> to use.</param>
		/// <param name="onDispose">The value of <see cref="onDispose"/>.</param>
		/// <param name="directoryAppend">Extra path to add to the end of <see cref="Api.Models.Internal.CompileJob.DirectoryName"/>.</param>
		public DmbProvider(CompileJob compileJob, IIOManager ioManager, Action onDispose, string? directoryAppend = null)
		{
			CompileJob = compileJob ?? throw new ArgumentNullException(nameof(compileJob));
			if (ioManager == null)
				throw new ArgumentNullException(nameof(ioManager));

			this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));

			Directory = ioManager.ResolvePath(CompileJob.DirectoryName.ToString() + (directoryAppend ?? String.Empty));
		}

		/// <inheritdoc />
		public void Dispose() => onDispose?.Invoke();

		/// <inheritdoc />
		public void KeepAlive() => onDispose = null;
	}
}
