using System;

using Grpc.Core;

namespace Tgstation.Server.Host.Swarm.Grpc
{
	/// <summary>
	/// Protobuf representation of a <see cref="Version"/> in semver format.
	/// </summary>
	public sealed partial class GrpcVersion
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GrpcVersion"/> class.
		/// </summary>
		/// <param name="version">The <see cref="Version"/> to build from.</param>
		public GrpcVersion(Version version)
			: this()
		{
			ArgumentNullException.ThrowIfNull(version);

			Major = version.Major;
			Minor = version.Minor;
			Patch = version.Build;
		}

		/// <summary>
		/// Convert to a <see cref="Version"/>.
		/// </summary>
		/// <returns>The converted <see cref="Version"/>.</returns>
		public Version ToVersion()
		{
			try
			{
				return new Version(Major, Minor, Patch);
			}
			catch (Exception ex)
			{
				throw new RpcException(
					new Status(StatusCode.InvalidArgument, ex.Message));
			}
		}
	}
}
