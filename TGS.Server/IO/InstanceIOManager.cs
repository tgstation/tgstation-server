using System.IO;
using TGS.Server.Configuration;

namespace TGS.Server.IO
{
	/// <summary>
	/// An <see cref="IOManager"/> that resolves relative paths to a specified directory
	/// </summary>
	sealed class InstanceIOManager : IOManager
	{
		/// <summary>
		/// The <see cref="IInstanceConfig"/> for the <see cref="InstanceIOManager"/>
		/// </summary>
		readonly IInstanceConfig Config;

		/// <summary>
		/// Construct an <see cref="InstanceIOManager"/>
		/// </summary>
		/// <param name="config">The value of <see cref="Config"/></param>
		public InstanceIOManager(IInstanceConfig config)
		{
			Config = config;
		}

		/// <inheritdoc />
		public override string ResolvePath(string path)
		{
			if (!Path.IsPathRooted(path))
				path = Path.Combine(Config.Directory, path);
			return base.ResolvePath(path);
		}
	}
}
