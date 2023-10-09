using System;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <inheritdoc />
	sealed class DmbProvider : IDmbProvider
	{
		/// <inheritdoc />
		public string DmbName => String.Concat(CompileJob.DmeName, DreamMaker.DmbExtension);

		/// <inheritdoc />
		public string Directory => ioManager.ResolvePath(CompileJob.DirectoryName.ToString() + directoryAppend);

		/// <inheritdoc />
		public Models.CompileJob CompileJob { get; }

		/// <inheritdoc />
		public ByondVersion ByondVersion { get; }

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="DmbProvider"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// Extra path to add to the end of <see cref="CompileJob.DirectoryName"/>.
		/// </summary>
		readonly string directoryAppend;

		/// <summary>
		/// The <see cref="Action"/> to run when <see cref="Dispose"/> is called.
		/// </summary>
		Action onDispose;

		/// <summary>
		/// Initializes a new instance of the <see cref="DmbProvider"/> class.
		/// </summary>
		/// <param name="compileJob">The value of <see cref="CompileJob"/>.</param>
		/// <param name="byondVersion">The value of <see cref="ByondVersion"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="onDispose">The value of <see cref="onDispose"/>.</param>
		/// <param name="directoryAppend">The optional value of <see cref="directoryAppend"/>.</param>
		public DmbProvider(Models.CompileJob compileJob, ByondVersion byondVersion, IIOManager ioManager, Action onDispose, string directoryAppend = null)
		{
			CompileJob = compileJob ?? throw new ArgumentNullException(nameof(compileJob));
			ByondVersion = byondVersion ?? throw new ArgumentNullException(nameof(byondVersion));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
			this.directoryAppend = directoryAppend ?? String.Empty;
		}

		/// <inheritdoc />
		public void Dispose() => onDispose?.Invoke();

		/// <inheritdoc />
		public void KeepAlive() => onDispose = null;
	}
}
