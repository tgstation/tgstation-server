using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <inheritdoc />
	sealed class ByondExecutableLock : IByondExecutableLock
	{
		/// <inheritdoc />
		public Version Version { get; }

		/// <inheritdoc />
		public string DreamDaemonPath { get; }

		/// <inheritdoc />
		public string DreamMakerPath { get; }

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ByondExecutableLock"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// <see cref="SemaphoreSlim"/> used to guard access to the <see cref="trustedFilePath"/>.
		/// </summary>
		readonly SemaphoreSlim trustedFileSemaphore;

		/// <summary>
		/// The path to the BYOND trusted .dmbs configuration file.
		/// </summary>
		readonly string trustedFilePath;

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondExecutableLock"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="trustedFileSemaphore">The value of <see cref="trustedFileSemaphore"/>.</param>
		/// <param name="version">The value of <see cref="Version"/>.</param>
		/// <param name="dreamDaemonPath">The value of <see cref="DreamDaemonPath"/>.</param>
		/// <param name="dreamMakerPath">The value of <see cref="DreamMakerPath"/>.</param>
		/// <param name="trustedFilePath">The value of <see cref="trustedFilePath"/>.</param>
		public ByondExecutableLock(
			IIOManager ioManager,
			SemaphoreSlim trustedFileSemaphore,
			Version version,
			string dreamDaemonPath,
			string dreamMakerPath,
			string trustedFilePath)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.trustedFileSemaphore = trustedFileSemaphore ?? throw new ArgumentNullException(nameof(trustedFileSemaphore));
			Version = version ?? throw new ArgumentNullException(nameof(version));
			DreamDaemonPath = dreamDaemonPath ?? throw new ArgumentNullException(nameof(dreamDaemonPath));
			DreamMakerPath = dreamMakerPath ?? throw new ArgumentNullException(nameof(dreamMakerPath));
			this.trustedFilePath = trustedFilePath ?? throw new ArgumentNullException(nameof(trustedFilePath));
		}

		// at one point in design, byond versions were to delete themselves if they weren't the active version
		// That changed at some point so these functions are intentioanlly left blank

		/// <inheritdoc />
		public void Dispose()
		{
		}

		/// <inheritdoc />
		public void DoNotDeleteThisSession()
		{
		}

		/// <inheritdoc />
		public async Task TrustDmbPath(string fullDmbPath, CancellationToken cancellationToken)
		{
			if (fullDmbPath == null)
				throw new ArgumentNullException(nameof(fullDmbPath));

			using (await SemaphoreSlimContext.Lock(trustedFileSemaphore, cancellationToken))
			{
				string trustedFileText;

				if (await ioManager.FileExists(trustedFilePath, cancellationToken))
				{
					var trustedFileBytes = await ioManager.ReadAllBytes(trustedFilePath, cancellationToken);
					trustedFileText = Encoding.UTF8.GetString(trustedFileBytes);
					trustedFileText = $"{trustedFileText.Trim()}{Environment.NewLine}";
				}
				else
				{
					trustedFileText = String.Empty;
				}

				if (trustedFileText.Contains(fullDmbPath, StringComparison.Ordinal))
					return;

				trustedFileText = $"{trustedFileText}{fullDmbPath}{Environment.NewLine}";

				var newTrustedFileBytes = Encoding.UTF8.GetBytes(trustedFileText);
				await ioManager.WriteAllBytes(trustedFilePath, newTrustedFileBytes, cancellationToken);
			}
		}
	}
}
