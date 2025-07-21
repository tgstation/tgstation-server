using System;
using System.IO.Abstractions;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// An <see cref="IIOManager"/> that resolve relative paths from another <see cref="IIOManager"/> to a subdirectory of that.
	/// </summary>
	sealed class ResolvingIOManager : DefaultIOManager
	{
		/// <summary>
		/// Path to the subdirectory attached to path resolutions.
		/// </summary>
		readonly string subdirectory;

		/// <summary>
		/// Initializes a new instance of the <see cref="ResolvingIOManager"/> class.
		/// </summary>
		/// <param name="fileSystem">The <see cref="IFileSystem"/> for the <see cref="DefaultIOManager"/>.</param>
		/// <param name="subdirectory">The value of <see cref="subdirectory"/>.</param>
		public ResolvingIOManager(
			IFileSystem fileSystem,
			string subdirectory)
			: base(fileSystem)
		{
			this.subdirectory = subdirectory ?? throw new ArgumentNullException(nameof(subdirectory));
		}

		/// <inheritdoc />
		public override string ResolvePath(string path)
		{
			if (!IsPathRooted(path))
				return base.ResolvePath(ConcatPath(subdirectory, path));
			return path;
		}
	}
}
