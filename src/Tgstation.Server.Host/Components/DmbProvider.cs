using System;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class DmbProvider : IDmbProvider
	{
		/// <inheritdoc />
		public string DmbName => String.Concat(CompileJob.DmeName, DreamMaker.DmbExtension);

		/// <inheritdoc />
		public string PrimaryDirectory => ioManager.ResolvePath(ioManager.ConcatPath(CompileJob.DirectoryName.ToString(), DreamMaker.ADirectoryName));

		/// <inheritdoc />
		public string SecondaryDirectory => ioManager.ResolvePath(ioManager.ConcatPath(CompileJob.DirectoryName.ToString(), DreamMaker.BDirectoryName));

		/// <inheritdoc />
		public RevisionInformation RevisionInformation => CompileJob.RevisionInformation;

		/// <summary>
		/// The <see cref="CompileJob"/> for the <see cref="DmbProvider"/>
		/// </summary>
		public CompileJob CompileJob { get; }

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="DmbProvider"/>
		/// </summary>
		readonly IIOManager ioManager;
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
		public DmbProvider(CompileJob compileJob, IIOManager ioManager, Action onDispose)
		{
			CompileJob = compileJob ?? throw new ArgumentNullException(nameof(compileJob));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
		}

		~DmbProvider() => Dispose();

		/// <inheritdoc />
		public void Dispose()
		{
			onDispose?.Invoke();
			GC.SuppressFinalize(this);
		}

		public void KeepAlive() => onDispose = null;
	}
}
