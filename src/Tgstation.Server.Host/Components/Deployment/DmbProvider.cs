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
		public string Directory => ioManager.ResolvePath(CompileJob.DirectoryName.ToString() + directoryAppend);

		/// <summary>
		/// The <see cref="CompileJob"/> for the <see cref="DmbProvider"/>
		/// </summary>
		public CompileJob CompileJob { get; }

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="DmbProvider"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// Extra path to add to the end of <see cref="Api.Models.Internal.CompileJob.DirectoryName"/>
		/// </summary>
		readonly string directoryAppend;

		/// <summary>
		/// The <see cref="Action"/> to run when <see cref="Dispose"/> is called
		/// </summary>
		Action onDispose;

		/// <summary>
		/// Construct a <see cref="DmbProvider"/>
		/// </summary>
		/// <param name="compileJob">The value of <see cref="CompileJob"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="onDispose">The value of <see cref="onDispose"/></param>
		/// <param name="directoryAppend">The optional value of <see cref="directoryAppend"/></param>
		public DmbProvider(CompileJob compileJob, IIOManager ioManager, Action onDispose, string directoryAppend = null)
		{
			CompileJob = compileJob ?? throw new ArgumentNullException(nameof(compileJob));
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
